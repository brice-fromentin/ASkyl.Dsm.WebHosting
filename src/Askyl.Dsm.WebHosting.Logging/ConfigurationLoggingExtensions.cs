using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Logging;

/// <summary>
/// Structured logging extension methods for configuration events.
/// </summary>
public static partial class ConfigurationLoggingExtensions
{
    /// <summary>
    /// Logs that configuration was loaded and cached with site count.
    /// </summary>
    [LoggerMessage(EventId = 1600, Level = LogLevel.Information, Message = "Configuration loaded and cached. Found {SiteCount} sites")]
    public static partial void ConfigurationLoadedAndCached(this ILogger logger, int siteCount);

    /// <summary>
    /// Logs successful service initialization with base directory.
    /// </summary>
    [LoggerMessage(EventId = 1601, Level = LogLevel.Debug, Message = "Service initialization completed successfully. Base directory: {BaseDirectory}")]
    public static partial void ServiceInitializationCompleted(this ILogger logger, string baseDirectory);

    /// <summary>
    /// Logs that configuration file was not found and empty collection created.
    /// </summary>
    [LoggerMessage(EventId = 1602, Level = LogLevel.Information, Message = "Configuration file not found, creating empty collection")]
    public static partial void ConfigurationFileNotFound(this ILogger logger);

    /// <summary>
    /// Logs that configuration file is empty.
    /// </summary>
    [LoggerMessage(EventId = 1603, Level = LogLevel.Warning, Message = "Configuration file is empty, creating new collection")]
    public static partial void ConfigurationFileEmpty(this ILogger logger);

    /// <summary>
    /// Logs that configuration deserialization returned null.
    /// </summary>
    [LoggerMessage(EventId = 1604, Level = LogLevel.Warning, Message = "Configuration deserialization returned null, creating new collection")]
    public static partial void ConfigurationDeserializationNull(this ILogger logger);

    /// <summary>
    /// Logs successful configuration load with site count.
    /// </summary>
    [LoggerMessage(EventId = 1605, Level = LogLevel.Debug, Message = "Configuration loaded successfully with {SiteCount} sites")]
    public static partial void ConfigurationLoadedSuccessfully(this ILogger logger, int siteCount);

    /// <summary>
    /// Logs that configuration file is corrupted (invalid JSON).
    /// </summary>
    [LoggerMessage(EventId = 1606, Level = LogLevel.Error, Message = "Configuration file is corrupted (invalid JSON). Backup created and new empty configuration initialized")]
    public static partial void ConfigurationCorrupted(this ILogger logger, Exception ex);

    /// <summary>
    /// Logs failure to load configuration from file path.
    /// </summary>
    [LoggerMessage(EventId = 1607, Level = LogLevel.Error, Message = "Failed to load configuration from {FilePath}")]
    public static partial void FailedToLoadConfiguration(this ILogger logger, Exception ex, string filePath);

    /// <summary>
    /// Logs successful configuration save to file path.
    /// </summary>
    [LoggerMessage(EventId = 1608, Level = LogLevel.Information, Message = "Configuration saved successfully to {FilePath}")]
    public static partial void ConfigurationSaved(this ILogger logger, string filePath);

    /// <summary>
    /// Logs failure to save configuration to file path.
    /// </summary>
    [LoggerMessage(EventId = 1609, Level = LogLevel.Error, Message = "Failed to save configuration to {FilePath}")]
    public static partial void FailedToSaveConfiguration(this ILogger logger, Exception ex, string filePath);

    /// <summary>
    /// Logs that corrupted configuration was backed up.
    /// </summary>
    [LoggerMessage(EventId = 1610, Level = LogLevel.Information, Message = "Corrupted configuration backed up to {BackupPath}")]
    public static partial void ConfigurationBackedUp(this ILogger logger, string backupPath);

    /// <summary>
    /// Logs failure to create backup of corrupted configuration.
    /// </summary>
    [LoggerMessage(EventId = 1611, Level = LogLevel.Warning, Message = "Failed to create backup of corrupted configuration")]
    public static partial void FailedToCreateBackup(this ILogger logger, Exception ex);
}
