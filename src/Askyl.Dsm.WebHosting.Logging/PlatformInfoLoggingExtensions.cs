using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Logging;

/// <summary>Category marker for ILogger&lt;T&gt; — no implementation required.</summary>
public interface ILogPlatformInfoService { }

/// <summary>
/// Structured logging extension methods for platform information detection.
/// </summary>
public static partial class PlatformInfoLoggingExtensions
{
    /// <summary>
    /// Logs detected CPU architecture.
    /// </summary>
    [LoggerMessage(EventId = 1810, Level = LogLevel.Information, Message = "Detected architecture = {Architecture}")]
    public static partial void DetectedArchitecture(this ILogger<ILogPlatformInfoService> logger, string architecture);

    /// <summary>
    /// Logs detected operating system.
    /// </summary>
    [LoggerMessage(EventId = 1811, Level = LogLevel.Information, Message = "Detected OS = {OperatingSystem}")]
    public static partial void DetectedOS(this ILogger<ILogPlatformInfoService> logger, string operatingSystem);
}
