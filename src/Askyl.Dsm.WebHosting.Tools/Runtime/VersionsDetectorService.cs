using System.Diagnostics;
using System.Text.RegularExpressions;
using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Domain.Runtime;

namespace Askyl.Dsm.WebHosting.Tools.Runtime;

/// <summary>
/// Service that detects installed .NET framework versions by executing dotnet --info.
/// </summary>
public sealed partial class VersionsDetectorService : IVersionsDetectorService
{
    private List<FrameworkInfo> _cachedFrameworks = [];
    private bool _cacheInitialized = false;

    #region Regex Patterns

    [GeneratedRegex(@"^\s*(\d+\.\d+\.\d+(?:-[\w\.-]+)?)")]
    private static partial Regex SdkVersionRegex();

    [GeneratedRegex(@"Microsoft\.AspNetCore\.App\s+(\d+\.\d+\.\d+(?:-[\w\.-]+)?)")]
    private static partial Regex AspNetCoreVersionRegex();

    [GeneratedRegex(@"Microsoft\.NETCore\.App\s+(\d+\.\d+\.\d+(?:-[\w\.-]+)?)")]
    private static partial Regex NetCoreVersionRegex();

    [GeneratedRegex(@"Version:\s*(\d+\.\d+\.\d+(?:-[\w\.-]+)?)")]
    private static partial Regex MainSdkVersionRegex();

    #endregion

    /// <inheritdoc/>
    public async Task<List<FrameworkInfo>> GetInstalledVersionsAsync()
    {
        // Return cached data if already initialized (BLAZING FAST!) ⚡
        if (_cacheInitialized)
        {
            return [.. _cachedFrameworks];  // Return copy to prevent external modification
        }

        await RefreshCacheAsync();  // Just call the public method!
        _cacheInitialized = true;  // Mark as initialized for fast subsequent calls

        return [.. _cachedFrameworks];
    }

    /// <inheritdoc/>
    public bool IsChannelInstalled(string channel, string frameworkType = "ASP.NET Core")
        => _cachedFrameworks.Any(x => x.Type == frameworkType && x.Version.StartsWith(channel + "."));

    /// <inheritdoc/>
    public bool IsVersionInstalled(string version, string frameworkType = "ASP.NET Core")
        => _cachedFrameworks.Any(x => x.Type == frameworkType && x.Version == version);

    /// <inheritdoc/>
    public async Task RefreshCacheAsync()
    {
        // Re-execute process to get fresh data
        List<FrameworkInfo> frameworks = [];

        try
        {
            var dotnetPath = Path.Combine(ApplicationConstants.RuntimesRootPath, "dotnet");
            var output = await ExecuteProcessAndGetOutputAsync(dotnetPath, "--info");

            if (!String.IsNullOrEmpty(output))
            {
                frameworks = ParseDotnetInfo(output);
            }
        }
        catch
        {
            // Ignore errors - keep existing cache if refresh fails
        }

        _cachedFrameworks = frameworks;  // Update cache with fresh data
    }

    #region Process Management

    private async Task<string> ExecuteProcessAndGetOutputAsync(string fileName, string arguments)
    {
        using var process = new Process();

        process.StartInfo.FileName = fileName;
        process.StartInfo.Arguments = arguments;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.CreateNoWindow = true;

        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        return process.ExitCode == 0 ? output : String.Empty;
    }

    #endregion

    #region Output Parsing

    private List<FrameworkInfo> ParseDotnetInfo(string output)
    {
        List<FrameworkInfo> frameworks = [];
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        string? currentSection = null;

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            // Skip empty lines
            if (String.IsNullOrWhiteSpace(trimmedLine))
            {
                continue;
            }

            // Detect main sections
            var detectedSection = DetectCurrentSection(trimmedLine);

            if (detectedSection is not null)
            {
                currentSection = detectedSection;
                continue;
            }

            // Parse versions in current section
            if (currentSection is not null)
            {
                ParseVersionsInSection(frameworks, currentSection, trimmedLine);
            }
        }

        return [.. frameworks.OrderBy(f => GetFrameworkOrder(f.Type)).ThenBy(f => f.Version)];
    }

    private string? DetectCurrentSection(string trimmedLine)
    {
        // Detect main sections
        if (trimmedLine.StartsWith(".NET SDKs installed:"))
        {
            return "SDK";
        }
        else if (trimmedLine.StartsWith(".NET runtimes installed:"))
        {
            return "Runtime";
        }
        else if (trimmedLine.StartsWith(".NET SDK:"))
        {
            // Main SDK base section
            return "Main SDK";
        }

        return null;
    }

    private void ParseVersionsInSection(List<FrameworkInfo> frameworks, string currentSection, string trimmedLine)
    {
        // Parse versions in each section
        if (currentSection == "SDK")
        {
            // Format: "  9.0.300 [/usr/local/share/dotnet/sdk]"
            TryAddFrameworkFromRegex(frameworks, SdkVersionRegex(), trimmedLine, "SDK");
        }
        else if (currentSection == "Runtime")
        {
            // Format: "  Microsoft.AspNetCore.App 9.0.5 [/usr/local/share/dotnet/shared/Microsoft.AspNetCore.App]"
            if (trimmedLine.Contains("Microsoft.AspNetCore.App"))
            {
                TryAddFrameworkFromRegex(frameworks, AspNetCoreVersionRegex(), trimmedLine, "ASP.NET Core");
            }
            else if (trimmedLine.Contains("Microsoft.NETCore.App"))
            {
                TryAddFrameworkFromRegex(frameworks, NetCoreVersionRegex(), trimmedLine, "Runtime");
            }
        }
        else if (currentSection == "Main SDK")
        {
            // Format: " Version:           9.0.301"
            if (trimmedLine.StartsWith("Version:"))
            {
                TryAddFrameworkFromRegex(frameworks, MainSdkVersionRegex(), trimmedLine, "SDK (Main)");
            }
        }
    }

    #endregion

    #region Framework Management

    private void TryAddFrameworkFromRegex(List<FrameworkInfo> frameworks, Regex regex, string trimmedLine, string frameworkType)
    {
        var match = regex.Match(trimmedLine);

        if (match.Success)
        {
            var version = match.Groups[1].Value;
            AddFrameworkIfNotExists(frameworks, frameworkType, version);
        }
    }

    private void AddFrameworkIfNotExists(List<FrameworkInfo> frameworks, string type, string version)
    {
        if (!frameworks.Any(f => f.Type == type && f.Version == version))
        {
            frameworks.Add(new FrameworkInfo
            {
                Type = type,
                Version = version
            });
        }
    }

    #endregion

    #region Utilities

    private int GetFrameworkOrder(string frameworkType)
    {
        return frameworkType switch
        {
            "SDK (Main)" => 1,
            "SDK" => 2,
            "Runtime" => 3,
            "ASP.NET Core" => 4,
            _ => 5
        };
    }

    #endregion
}
