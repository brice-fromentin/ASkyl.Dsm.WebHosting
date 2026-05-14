using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Logging;

/// <summary>
/// Structured logging extension methods for file manager operations.
/// </summary>
public static partial class FileManagerServiceLoggingExtensions
{
    /// <summary>
    /// Logs FileManager initialization with base path.
    /// </summary>
    [LoggerMessage(EventId = 1112, Level = LogLevel.Information, Message = "Initializing FileManager with base path: {BasePath}")]
    public static partial void FileManagerInitializing(this ILogger logger, string basePath);

    /// <summary>
    /// Logs successful FileManager initialization.
    /// </summary>
    [LoggerMessage(EventId = 1113, Level = LogLevel.Information, Message = "FileManager initialized successfully")]
    public static partial void FileManagerInitialized(this ILogger logger);

    /// <summary>
    /// Logs directory existence check.
    /// </summary>
    [LoggerMessage(EventId = 1114, Level = LogLevel.Debug, Message = "Ensuring directory exists: {DirectoryPath}")]
    public static partial void EnsuringDirectoryExists(this ILogger logger, string directoryPath);

    /// <summary>
    /// Logs directory deletion.
    /// </summary>
    [LoggerMessage(EventId = 1115, Level = LogLevel.Information, Message = "Deleting directory: {DirectoryPath}")]
    public static partial void DeletingDirectory(this ILogger logger, string directoryPath);

    /// <summary>
    /// Logs skipped directory deletion (directory does not exist).
    /// </summary>
    [LoggerMessage(EventId = 1116, Level = LogLevel.Debug, Message = "Directory does not exist, skipping deletion: {DirectoryPath}")]
    public static partial void DirectoryNotFoundSkippingDeletion(this ILogger logger, string directoryPath);

    /// <summary>
    /// Logs full file path resolution.
    /// </summary>
    [LoggerMessage(EventId = 1117, Level = LogLevel.Debug, Message = "Getting full file path: {FullPath}")]
    public static partial void GettingFullPath(this ILogger logger, string fullPath);
}
