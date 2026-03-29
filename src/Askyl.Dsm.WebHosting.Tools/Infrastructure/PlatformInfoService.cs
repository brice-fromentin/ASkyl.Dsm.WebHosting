using System.Runtime.InteropServices;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Constants.Runtime;
using Askyl.Dsm.WebHosting.Data.Contracts;

namespace Askyl.Dsm.WebHosting.Tools.Infrastructure;

/// <summary>
/// Service that provides platform information including operating system, architecture, and configured channel version.
/// </summary>
public sealed class PlatformInfoService : IPlatformInfoService
{
    private enum Platform
    {
        Linux,
        MacOS,
        Windows
    }

    private readonly ILogger<PlatformInfoService> _logger;

    public string ChannelVersion { get; private set; } = null!; // Initialized in constructor, will throw if config missing
    public string CurrentArchitecture { get; private set; } = String.Empty;
    public string CurrentOS { get; private set; } = String.Empty;

    public PlatformInfoService(ILogger<PlatformInfoService> logger)
    {
        _logger = logger;
        InitializePlatformInfo();
    }

    private void InitializePlatformInfo()
    {
        var basePath = AppContext.BaseDirectory;

        // Load configuration (settings file is required for the application to function)
        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile(ApplicationConstants.SettingsFileName, optional: false, reloadOnChange: false)
            .Build();

        var channelVersion = configuration[ApplicationConstants.ChannelVersionKey];

        if (String.IsNullOrWhiteSpace(channelVersion))
        {
            throw new InvalidOperationException($"Required setting '{ApplicationConstants.ChannelVersionKey}' is missing or empty in '{ApplicationConstants.SettingsFileName}'. The application cannot function without a configured .NET channel version.");
        }

        ChannelVersion = channelVersion;

        // Detect architecture
        var osArchitecture = RuntimeInformation.OSArchitecture;
        CurrentArchitecture = MapArchitectureToOsString(osArchitecture);
        _logger.LogInformation("Detected architecture = {Architecture}", CurrentArchitecture);

        // Detect operating system
        var platform = DetectPlatform();
        CurrentOS = MapPlatformToOsString(platform, allowAllPlatforms: IsDebugMode());
        _logger.LogInformation("Detected OS = {OperatingSystem}", CurrentOS);
    }

    private static string MapArchitectureToOsString(Architecture architecture)
    {
        return architecture switch
        {
            Architecture.X64 => RuntimeConstants.ArchitectureX64,
            Architecture.Arm => RuntimeConstants.ArchitectureArm,
            Architecture.Arm64 => RuntimeConstants.ArchitectureArm64,
            _ => throw new NotSupportedException($"Architecture {architecture} is not supported")
        };
    }

    private static string MapPlatformToOsString(Platform platform, bool allowAllPlatforms)
    {
        return platform switch
        {
            Platform.Linux => RuntimeConstants.OsLinux,
            Platform.MacOS => allowAllPlatforms ? RuntimeConstants.OsOsx : throw new NotSupportedException("macOS is not supported in release mode"),
            Platform.Windows => allowAllPlatforms ? RuntimeConstants.OsWindows : throw new NotSupportedException("Windows is not supported in release mode"),
            _ => throw new NotSupportedException("Operating System is not supported")
        };
    }

    private static Platform DetectPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return Platform.Linux;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return Platform.MacOS;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Platform.Windows;
        }

        throw new NotSupportedException("Operating System is not supported");
    }

    private static bool IsDebugMode()
    {
#if DEBUG
        return true;
#else
        return false;
#endif
    }
}
