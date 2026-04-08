using System.Diagnostics;
using System.Text.RegularExpressions;
using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Domain.Runtime;
using Askyl.Dsm.WebHosting.Tools.Threading;
using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Tools.Runtime;

/// <summary>
/// Service that detects installed .NET framework versions by executing dotnet --info.
/// Implements ISemaphoreOwner for thread-safe cache initialization.
/// </summary>
public sealed partial class VersionsDetectorService(ILogger<VersionsDetectorService> logger) : IVersionsDetectorService, ISemaphoreOwner
{
    #region ISemaphoreOwner Implementation

    public SemaphoreSlim Semaphore { get; } = new(1, 1);

    #endregion

    #region Fields

    private List<FrameworkInfo> _cachedFrameworks = [];
    private bool _cacheInitialized = false;

    #endregion

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
        if (Volatile.Read(ref _cacheInitialized))
        {
            return [.. _cachedFrameworks];  // Return copy to prevent external modification
        }

        using (await SemaphoreLock.AcquireAsync(this))
        {
            // Double-check lock pattern - another thread may have initialized while waiting
            if (Volatile.Read(ref _cacheInitialized))
            {
                return [.. _cachedFrameworks];
            }

            await RefreshCacheAsync();
            Volatile.Write(ref _cacheInitialized, true);  // Mark as initialized for fast subsequent calls
            return [.. _cachedFrameworks];
        }
    }

    /// <inheritdoc/>
    public bool IsChannelInstalled(string channel, string frameworkType = "ASP.NET Core")
        => _cachedFrameworks.Any(x => x.Type == frameworkType && x.Version.StartsWith(channel + "."));

    /// <inheritdoc/>
    public bool IsVersionInstalled(string version, string frameworkType = "ASP.NET Core")
        => _cachedFrameworks.Any(x => x.Type == frameworkType && x.Version == version);

    /// <inheritdoc/>
    public async Task RefreshCacheAsync(CancellationToken cancellationToken = default)
    {
        // Re-execute process to get fresh data
        List<FrameworkInfo> frameworks = [];

        try
        {
            var dotnetPath = Path.Combine(ApplicationConstants.RuntimesRootPath, "dotnet");
            var output = await ExecuteProcessAndGetOutputAsync(dotnetPath, "--info", cancellationToken);

            if (!String.IsNullOrEmpty(output))
            {
                frameworks = ParseDotnetInfo(output);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Framework cache refresh cancelled");
            throw;  // Re-throw to signal cancellation to caller
        }
        catch (Exception ex)
        {
            // Log but don't throw - keep existing cache if refresh fails
            logger.LogWarning(ex, "Failed to refresh framework cache");
            return;  // Preserve existing cached data on failure
        }

        _cachedFrameworks = frameworks;  // Update cache with fresh data
    }

    #region Process Management

    private async Task<string> ExecuteProcessAndGetOutputAsync(string fileName, string arguments, CancellationToken cancellationToken = default)
    {
        using var process = new Process();

        process.StartInfo.FileName = fileName;
        process.StartInfo.Arguments = arguments;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.CreateNoWindow = true;

        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

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
        // Detect main sections using constants
        if (trimmedLine.StartsWith(DotnetInfoParserConstants.SdkSectionHeader))
        {
            return DotnetInfoParserConstants.FrameworkTypeSdk;
        }
        else if (trimmedLine.StartsWith(DotnetInfoParserConstants.RuntimeSectionHeader))
        {
            return DotnetInfoParserConstants.FrameworkTypeRuntime;
        }
        else if (trimmedLine.StartsWith(DotnetInfoParserConstants.MainSdkSectionHeader))
        {
            // Main SDK base section
            return DotnetInfoParserConstants.FrameworkTypeMainSdk;
        }

        return null;
    }

    private void ParseVersionsInSection(List<FrameworkInfo> frameworks, string currentSection, string trimmedLine)
    {
        // Parse versions in each section using constants
        if (currentSection == DotnetInfoParserConstants.FrameworkTypeSdk)
        {
            // Format: "  9.0.300 [/usr/local/share/dotnet/sdk]"
            TryAddFrameworkFromRegex(frameworks, SdkVersionRegex(), trimmedLine, DotnetInfoParserConstants.FrameworkTypeSdk);
        }
        else if (currentSection == DotnetInfoParserConstants.FrameworkTypeRuntime)
        {
            // Format: "  Microsoft.AspNetCore.App 9.0.5 [/usr/local/share/dotnet/shared/Microsoft.AspNetCore.App]"
            if (trimmedLine.Contains(DotnetInfoParserConstants.AspNetCoreProductName))
            {
                TryAddFrameworkFromRegex(frameworks, AspNetCoreVersionRegex(), trimmedLine, DotnetInfoParserConstants.FrameworkTypeAspNetCore);
            }
            else if (trimmedLine.Contains(DotnetInfoParserConstants.NetCoreProductName))
            {
                TryAddFrameworkFromRegex(frameworks, NetCoreVersionRegex(), trimmedLine, DotnetInfoParserConstants.FrameworkTypeRuntime);
            }
        }
        else if (currentSection == DotnetInfoParserConstants.FrameworkTypeMainSdk)
        {
            // Format: " Version:           9.0.301"
            if (trimmedLine.StartsWith(DotnetInfoParserConstants.VersionLinePrefix))
            {
                TryAddFrameworkFromRegex(frameworks, MainSdkVersionRegex(), trimmedLine, DotnetInfoParserConstants.FrameworkTypeMainSdk);
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
            DotnetInfoParserConstants.FrameworkTypeMainSdk => 1,
            DotnetInfoParserConstants.FrameworkTypeSdk => 2,
            DotnetInfoParserConstants.FrameworkTypeRuntime => 3,
            DotnetInfoParserConstants.FrameworkTypeAspNetCore => 4,
            _ => 5
        };
    }

    #endregion
}
