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
    /// Logs that a mandatory setting key is missing from the configuration file.
    /// </summary>
    [LoggerMessage(EventId = 2800003, Level = LogLevel.Warning, Message = "Mandatory setting missing: {SettingKey}")]
    public static partial void MandatorySettingMissing(this ILogger<ILogDsmSettingsService> logger, string settingKey);

    /// <summary>
    /// Logs that reading the configuration file failed with an unexpected exception.
    /// </summary>
    [LoggerMessage(EventId = 2800004, Level = LogLevel.Error, Message = "Failed to read DSM settings")]
    public static partial void SettingsReadFailed(this ILogger<ILogDsmSettingsService> logger, Exception exception);

    /// <summary>
    /// Logs that default values are being used for DSM settings.
    /// </summary>
    [LoggerMessage(EventId = 2800005, Level = LogLevel.Warning, Message = "Using default DSM settings")]
    public static partial void UsingDefaults(this ILogger<ILogDsmSettingsService> logger);

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
    /// Logs that DSM's web configuration exists but cannot give a port, so the hostname is used instead.
    /// </summary>
    [LoggerMessage(EventId = 2800008, Level = LogLevel.Warning, Message = "DSM web configuration unreadable, falling back to the hostname: {Path}")]
    public static partial void WebConfigurationUnreadable(this ILogger<ILogDsmSettingsService> logger, string path, Exception? exception);
}
