using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Logging;

/// <summary>Category marker for ILogger&lt;T&gt; — no implementation required.</summary>
public interface ILogDsmSettingsService { }

/// <summary>
/// Structured logging extension methods for DSM settings service.
/// </summary>
public static partial class DsmSettingsServiceLoggingExtensions
{
    /// <summary>
    /// Logs that the DSM configuration file was not found at the expected path.
    /// </summary>
    [LoggerMessage(EventId = 2800001, Level = LogLevel.Warning, Message = "Configuration file not found: {ConfigurationFileName}")]
    public static partial void ConfigurationFileNotFound(this ILogger<ILogDsmSettingsService> logger, string configurationFileName);

    /// <summary>
    /// Logs the number of settings parsed from the configuration file.
    /// </summary>
    [LoggerMessage(EventId = 2800002, Level = LogLevel.Information, Message = "Configuration loaded with {Count} settings")]
    public static partial void ConfigurationLoaded(this ILogger<ILogDsmSettingsService> logger, int count);

    /// <summary>
    /// Logs that reading the configuration file failed with an unexpected exception.
    /// </summary>
    [LoggerMessage(EventId = 2800004, Level = LogLevel.Error, Message = "Failed to read DSM settings")]
    public static partial void SettingsReadFailed(this ILogger<ILogDsmSettingsService> logger, Exception exception);

    /// <summary>
    /// Logs the resolved configuration file path for debugging local development setup.
    /// </summary>
    [LoggerMessage(EventId = 2800006, Level = LogLevel.Debug, Message = "Resolving DSM settings from: {ConfigPath} (configured: {HasConfigurationOverride})")]
    public static partial void ResolvingConfigPath(this ILogger<ILogDsmSettingsService> logger, string configPath, bool hasConfigurationOverride);

    /// <summary>
    /// Logs that DSM is reached over loopback on the port named by its web configuration.
    /// </summary>
    [LoggerMessage(EventId = 2800007, Level = LogLevel.Information, Message = "Reaching DSM over loopback on port {Port}")]
    public static partial void ReachingDsmOverLoopback(this ILogger<ILogDsmSettingsService> logger, int port);

    /// <summary>
    /// Logs that DSM's web configuration is missing or cannot give a port, so DSM's default HTTPS port is used.
    /// </summary>
    [LoggerMessage(EventId = 2800008, Level = LogLevel.Warning, Message = "DSM web configuration missing or unreadable, using the default port {Port}: {Path}")]
    public static partial void WebConfigurationUnreadable(this ILogger<ILogDsmSettingsService> logger, int port, string path, Exception? exception);

    /// <summary>
    /// Logs that DSM is reached at the URL a developer set in ADWH_DSM_URL instead of over loopback.
    /// </summary>
    [LoggerMessage(EventId = 2800009, Level = LogLevel.Information, Message = "Reaching DSM at {Host}:{Port} from ADWH_DSM_URL")]
    public static partial void ReachingDsmFromUrl(this ILogger<ILogDsmSettingsService> logger, string host, int port);
}
