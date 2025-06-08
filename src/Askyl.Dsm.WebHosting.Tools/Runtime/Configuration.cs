using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;

namespace Askyl.Dsm.WebHosting.Tools.Runtime;

public static class Configuration
{
    static Configuration()
    {
        // Load configuration
        var configuration = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory)
                                                      .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                                                      .Build();

        // Get Parameters
        ChannelVersion = GetValue(configuration, "Download:ChannelVersion");

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

        // Retrieve Operating System information
        var osPlatform = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "linux" :
                 throw new NotSupportedException("Operating System is not supported");

        CurrentOS = osPlatform;
        
        Console.WriteLine($"Detected OS = {CurrentOS}");
    }

    static string GetValue(IConfigurationRoot configuration, string key)
        => configuration[key] ?? throw new NullReferenceException($"{key} not found.");

    public static string ChannelVersion { get; }
    public static string CurrentArchitecture { get; }
    public static string CurrentOS { get; }
}
