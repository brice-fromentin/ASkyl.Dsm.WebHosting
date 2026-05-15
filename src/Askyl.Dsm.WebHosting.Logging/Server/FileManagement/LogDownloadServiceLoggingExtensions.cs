using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Logging;

/// <summary>Category marker for ILogger&lt;T&gt; — no implementation required.</summary>
public interface ILogLogDownloadService { }

/// <summary>
/// Structured logging extension methods for log download and archive operations.
/// </summary>
public static partial class LogDownloadServiceLoggingExtensions
{
    /// <summary>
    /// Logs creation of log archive stream.
    /// </summary>
    [LoggerMessage(EventId = 1300001, Level = LogLevel.Debug, Message = "Creating log archive stream (BaseDirectory: {BaseDirectory})")]
    public static partial void CreatingLogArchiveStream(this ILogger<ILogLogDownloadService> logger, string baseDirectory);

    /// <summary>
    /// Logs added debug log file.
    /// </summary>
    [LoggerMessage(EventId = 1300002, Level = LogLevel.Debug, Message = "Added debug log file: {DebugLogFilePath}")]
    public static partial void AddedDebugLogFile(this ILogger<ILogLogDownloadService> logger, string debugLogFilePath);

    /// <summary>
    /// Logs missing debug log file.
    /// </summary>
    [LoggerMessage(EventId = 1300003, Level = LogLevel.Warning, Message = "Debug log file not found at path: {DebugLogFilePath}")]
    public static partial void DebugLogNotFound(this ILogger<ILogLogDownloadService> logger, string debugLogFilePath);

    /// <summary>
    /// Logs created log archive with size.
    /// </summary>
    [LoggerMessage(EventId = 1300004, Level = LogLevel.Information, Message = "Created log archive stream with size {Size} bytes")]
    public static partial void CreatedLogArchiveStream(this ILogger<ILogLogDownloadService> logger, long size);

    /// <summary>
    /// Logs added application log from directory.
    /// </summary>
    [LoggerMessage(EventId = 1300005, Level = LogLevel.Debug, Message = "Added {LogName} from directory: {DirectoryPath} (BaseDirectory: {BaseDirectory})")]
    public static partial void AddedAppLog(this ILogger<ILogLogDownloadService> logger, string logName, string directoryPath, string baseDirectory);

    /// <summary>
    /// Logs missing application log directory.
    /// </summary>
    [LoggerMessage(EventId = 1300006, Level = LogLevel.Warning, Message = "{LogName} directory not found at path: {DirectoryPath} (BaseDirectory: {BaseDirectory})")]
    public static partial void AppLogDirectoryNotFound(this ILogger<ILogLogDownloadService> logger, string logName, string directoryPath, string baseDirectory);

    /// <summary>
    /// Logs file added to archive.
    /// </summary>
    [LoggerMessage(EventId = 1300007, Level = LogLevel.Debug, Message = "Added file to archive: {EntryName}")]
    public static partial void AddedFileToArchive(this ILogger<ILogLogDownloadService> logger, string entryName);
}
