using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Logging;

/// <summary>Category marker for ILogger&lt;T&gt; — no implementation required.</summary>
public interface ILogWebSitesConfigurationService { }

/// <summary>
/// Structured logging extension methods for configuration events.
/// </summary>
public static partial class ConfigurationLoggingExtensions
{
    /// <summary>
    /// Logs that configuration was loaded and cached with site count.
    /// </summary>
    [LoggerMessage(EventId = 1900001, Level = LogLevel.Information, Message = "Configuration loaded and cached. Found {SiteCount} sites")]
    public static partial void ConfigurationLoadedAndCached(this ILogger<ILogWebSitesConfigurationService> logger, int siteCount);

    /// <summary>
    /// Logs successful service initialization with base directory.
    /// </summary>
    [LoggerMessage(EventId = 1900002, Level = LogLevel.Debug, Message = "Service initialization completed successfully. Base directory: {BaseDirectory}")]
    public static partial void ServiceInitializationCompleted(this ILogger<ILogWebSitesConfigurationService> logger, string baseDirectory);

    /// <summary>
    /// Logs that configuration file was not found and empty collection created.
    /// </summary>
    [LoggerMessage(EventId = 1900003, Level = LogLevel.Information, Message = "Configuration file not found, creating empty collection")]
    public static partial void ConfigurationFileNotFound(this ILogger<ILogWebSitesConfigurationService> logger);

    /// <summary>
    /// Logs that configuration file is empty.
    /// </summary>
    [LoggerMessage(EventId = 1900004, Level = LogLevel.Warning, Message = "Configuration file is empty, creating new collection")]
    public static partial void ConfigurationFileEmpty(this ILogger<ILogWebSitesConfigurationService> logger);

    /// <summary>
    /// Logs that configuration deserialization returned null.
    /// </summary>
    [LoggerMessage(EventId = 1900005, Level = LogLevel.Warning, Message = "Configuration deserialization returned null, creating new collection")]
    public static partial void ConfigurationDeserializationNull(this ILogger<ILogWebSitesConfigurationService> logger);

    /// <summary>
    /// Logs successful configuration load with site count.
    /// </summary>
    [LoggerMessage(EventId = 1900006, Level = LogLevel.Debug, Message = "Configuration loaded successfully with {SiteCount} sites")]
    public static partial void ConfigurationLoadedSuccessfully(this ILogger<ILogWebSitesConfigurationService> logger, int siteCount);

    /// <summary>
    /// Logs that configuration file is corrupted (invalid JSON).
    /// </summary>
    [LoggerMessage(EventId = 1900007, Level = LogLevel.Error, Message = "Configuration file is corrupted (invalid JSON). Backup created and new empty configuration initialized")]
    public static partial void ConfigurationCorrupted(this ILogger<ILogWebSitesConfigurationService> logger, Exception ex);

    /// <summary>
    /// Logs failure to load configuration from file path.
    /// </summary>
    [LoggerMessage(EventId = 1900008, Level = LogLevel.Error, Message = "Failed to load configuration from {FilePath}")]
    public static partial void FailedToLoadConfiguration(this ILogger<ILogWebSitesConfigurationService> logger, Exception ex, string filePath);

    /// <summary>
    /// Logs successful configuration save to file path.
    /// </summary>
    [LoggerMessage(EventId = 1900009, Level = LogLevel.Information, Message = "Configuration saved successfully to {FilePath}")]
    public static partial void ConfigurationSaved(this ILogger<ILogWebSitesConfigurationService> logger, string filePath);

    /// <summary>
    /// Logs failure to save configuration to file path.
    /// </summary>
    [LoggerMessage(EventId = 1900010, Level = LogLevel.Error, Message = "Failed to save configuration to {FilePath}")]
    public static partial void FailedToSaveConfiguration(this ILogger<ILogWebSitesConfigurationService> logger, Exception ex, string filePath);

    /// <summary>
    /// Logs that corrupted configuration was backed up.
    /// </summary>
    [LoggerMessage(EventId = 1900011, Level = LogLevel.Information, Message = "Corrupted configuration backed up to {BackupPath}")]
    public static partial void ConfigurationBackedUp(this ILogger<ILogWebSitesConfigurationService> logger, string backupPath);

    /// <summary>
    /// Logs failure to create backup of corrupted configuration.
    /// </summary>
    [LoggerMessage(EventId = 1900012, Level = LogLevel.Warning, Message = "Failed to create backup of corrupted configuration")]
    public static partial void FailedToCreateBackup(this ILogger<ILogWebSitesConfigurationService> logger, Exception ex);

    /// <summary>
    /// Logs the start of an add site operation.
    /// </summary>
    [LoggerMessage(EventId = 1900013, Level = LogLevel.Debug, Message = "Adding site: {SiteName}")]
    public static partial void AddSiteStarting(this ILogger<ILogWebSitesConfigurationService> logger, string siteName);

    /// <summary>
    /// Logs the start of an update site operation.
    /// </summary>
    [LoggerMessage(EventId = 1900014, Level = LogLevel.Debug, Message = "Updating site: {SiteName}")]
    public static partial void UpdateSiteStarting(this ILogger<ILogWebSitesConfigurationService> logger, string siteName);

    /// <summary>
    /// Logs the start of a remove site operation.
    /// </summary>
    [LoggerMessage(EventId = 1900015, Level = LogLevel.Debug, Message = "Removing site: {SiteName}")]
    public static partial void RemoveSiteStarting(this ILogger<ILogWebSitesConfigurationService> logger, string siteName);

    /// <summary>
    /// Logs the duration of an add site operation.
    /// </summary>
    [LoggerMessage(EventId = 1900016, Level = LogLevel.Debug, Message = "Add site completed in {Duration}ms for '{SiteName}'")]
    public static partial void AddSiteDuration(this ILogger<ILogWebSitesConfigurationService> logger, long duration, string siteName);

    /// <summary>
    /// Logs the duration of an update site operation.
    /// </summary>
    [LoggerMessage(EventId = 1900017, Level = LogLevel.Debug, Message = "Update site completed in {Duration}ms for '{SiteName}'")]
    public static partial void UpdateSiteDuration(this ILogger<ILogWebSitesConfigurationService> logger, long duration, string siteName);

    /// <summary>
    /// Logs the duration of a remove site operation.
    /// </summary>
    [LoggerMessage(EventId = 1900018, Level = LogLevel.Debug, Message = "Remove site completed in {Duration}ms for '{SiteName}'")]
    public static partial void RemoveSiteDuration(this ILogger<ILogWebSitesConfigurationService> logger, long duration, string siteName);
}
