using System.Diagnostics;
using System.Text.RegularExpressions;

using Askyl.Dsm.WebHosting.Data.Runtime;

namespace Askyl.Dsm.WebHosting.Tools.Runtime;

public static partial class VersionsDetector
{
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

    #region Fields

    private static List<FrameworkInfo> _cachedFrameworks = [];

    #endregion

    #region Public API

    public static async Task<List<FrameworkInfo>> GetInstalledVersionsAsync()
    {
        List<FrameworkInfo> frameworks = [];

        try
        {
            // Use only the global dotnet version
            frameworks = await GetDotnetFrameworksAsync("../runtimes/dotnet");
        }
        catch
        {
            // Ignore errors - no .NET installation found
        }

        // Update cache
        _cachedFrameworks = frameworks;

        return frameworks;
    }

    public static bool IsChannelInstalled(string channel, string _ = "ASP.NET Core")
        => _cachedFrameworks.Any(x => x.Type == "ASP.NET Core" && x.Version.StartsWith(channel + "."));

    public static bool IsVersionInstalled(string version, string frameworkType = "ASP.NET Core")
        => _cachedFrameworks.Any(x => x.Type == frameworkType && x.Version == version);

    #endregion

    #region Process Management

    private static async Task<List<FrameworkInfo>> GetDotnetFrameworksAsync(string dotnetPath)
    {
        try
        {
            using var process = new Process();

            process.StartInfo.FileName = dotnetPath;
            process.StartInfo.Arguments = "--info";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0 && !String.IsNullOrEmpty(output))
            {
                return ParseDotnetInfo(output);
            }
        }
        catch
        {
            // Ignore errors
        }

        return [];
    }

    #endregion

    #region Output Parsing

    private static List<FrameworkInfo> ParseDotnetInfo(string output)
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

    private static string? DetectCurrentSection(string trimmedLine)
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

    private static void ParseVersionsInSection(List<FrameworkInfo> frameworks, string currentSection, string trimmedLine)
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

    private static void TryAddFrameworkFromRegex(List<FrameworkInfo> frameworks, Regex regex, string trimmedLine, string frameworkType)
    {
        var match = regex.Match(trimmedLine);

        if (match.Success)
        {
            var version = match.Groups[1].Value;
            AddFrameworkIfNotExists(frameworks, frameworkType, version);
        }
    }

    private static void AddFrameworkIfNotExists(List<FrameworkInfo> frameworks, string type, string version)
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

    private static int GetFrameworkOrder(string frameworkType)
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
