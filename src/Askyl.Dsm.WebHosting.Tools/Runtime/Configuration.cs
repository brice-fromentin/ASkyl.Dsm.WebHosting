using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Askyl.Dsm.WebHosting.Constants.Application;

namespace Askyl.Dsm.WebHosting.Tools.Runtime;

public static class Configuration
{
    private enum Platform
    {
        Linux,
        MacOS,
        Windows
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

    static Configuration()
    {
        var basePath = AppContext.BaseDirectory;
        var configFilePath = Path.Combine(basePath, ApplicationConstants.SettingsFileName);

        if (File.Exists(configFilePath))
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile(ApplicationConstants.SettingsFileName, optional: true, reloadOnChange: false)
                .Build();

            ChannelVersion = configuration[ApplicationConstants.ChannelVersionKey] ?? String.Empty;
        }
        else
        {
            ChannelVersion = String.Empty; // No configuration file present
        }

        // Get Executing Architecture
        var osArchitecture = RuntimeInformation.OSArchitecture;
        CurrentArchitecture = osArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.Arm => "arm",
            Architecture.Arm64 => "arm64",
            _ => throw new NotSupportedException($"Architecture {osArchitecture} is not supported"),
        };

        Console.WriteLine($"Detected architecture = {CurrentArchitecture}");

        // Retrieve Operating System information using switch expression (similar style to architecture).
#if DEBUG
        var platform = DetectPlatform();
        CurrentOS = platform switch
        {
            Platform.Linux => "linux",
            Platform.MacOS => "osx",
            Platform.Windows => "windows",
            _ => throw new NotSupportedException("Operating System is not supported in debug mode.")
        };
#else
    var platform = DetectPlatform();
    CurrentOS = platform switch
    {
        Platform.Linux => "linux",
        _ => throw new NotSupportedException("Operating System is not supported")
    };
#endif

        Console.WriteLine($"Detected OS = {CurrentOS}");
    }

    public static string ChannelVersion { get; }
    public static string CurrentArchitecture { get; }
    public static string CurrentOS { get; }
}
