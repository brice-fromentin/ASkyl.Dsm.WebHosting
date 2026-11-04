using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Logging;

/// <summary>Category marker for ILogger&lt;T&gt; — no implementation required.</summary>
public interface ILogFileSystemService { }

/// <summary>
/// Structured logging extension methods for FileStation system operations.
/// </summary>
public static partial class FileSystemServiceLoggingExtensions
{
    /// <summary>
    /// Logs retrieval of shared folders from DSM FileStation API.
    /// </summary>
    [LoggerMessage(EventId = 1100001, Level = LogLevel.Debug, Message = "Retrieving shared folders from DSM FileStation API")]
    public static partial void RetrievingSharedFolders(this ILogger<ILogFileSystemService> logger);

    /// <summary>
    /// Logs the count of shared folders retrieved.
    /// </summary>
    [LoggerMessage(EventId = 1100002, Level = LogLevel.Information, Message = "Retrieved {Count} shared folders")]
    public static partial void RetrievedSharedFolders(this ILogger<ILogFileSystemService> logger, int count);

    /// <summary>
    /// Logs an error during shared folder retrieval.
    /// </summary>
    [LoggerMessage(EventId = 1100003, Level = LogLevel.Error, Message = "Error retrieving shared folders")]
    public static partial void ErrorRetrievingSharedFolders(this ILogger<ILogFileSystemService> logger, Exception ex);

    /// <summary>
    /// Logs retrieval of directory contents.
    /// </summary>
    [LoggerMessage(EventId = 1100004, Level = LogLevel.Debug, Message = "Retrieving directory contents for path: {Path}, DirectoryOnly: {DirectoryOnly}")]
    public static partial void RetrievingDirectoryContents(this ILogger<ILogFileSystemService> logger, string path, bool directoryOnly);

    /// <summary>
    /// Logs the count of directories retrieved from a path.
    /// </summary>
    [LoggerMessage(EventId = 1100005, Level = LogLevel.Debug, Message = "Retrieved {DirectoryCount} directories from {Path}")]
    public static partial void RetrievedDirectories(this ILogger<ILogFileSystemService> logger, int directoryCount, string path);

    /// <summary>
    /// Logs the count of directories and files retrieved from a path.
    /// </summary>
    [LoggerMessage(EventId = 1100006, Level = LogLevel.Debug, Message = "Retrieved {DirectoryCount} directories and {FileCount} files from {Path}")]
    public static partial void RetrievedDirectoriesAndFiles(this ILogger<ILogFileSystemService> logger, int directoryCount, int fileCount, string path);

    /// <summary>
    /// Logs an error during directory retrieval.
    /// </summary>
    [LoggerMessage(EventId = 1100007, Level = LogLevel.Error, Message = "Error retrieving directory contents for {Path}")]
    public static partial void ErrorRetrievingDirectory(this ILogger<ILogFileSystemService> logger, Exception ex, string path);

    /// <summary>
    /// Logs path validation failure.
    /// </summary>
    [LoggerMessage(EventId = 1100009, Level = LogLevel.Warning, Message = "Path validation failed: {Path}")]
    public static partial void PathValidationFailed(this ILogger<ILogFileSystemService> logger, string path);

    /// <summary>
    /// Logs the http group given access to one folder of a site's path.
    /// </summary>
    [LoggerMessage(EventId = 1100013, Level = LogLevel.Information, Message = "http group given access to {SharePath} (Read: {Read}; passage only otherwise)")]
    public static partial void SiteAccessGranted(this ILogger<ILogFileSystemService> logger, string sharePath, bool read);

    /// <summary>
    /// Logs a folder left as it is because it already holds an explicit http entry.
    /// </summary>
    [LoggerMessage(EventId = 1100014, Level = LogLevel.Information, Message = "{SharePath} already holds an explicit http entry, left as it is")]
    public static partial void SiteAccessLeftAlone(this ILogger<ILogFileSystemService> logger, string sharePath);

    /// <summary>
    /// Logs DSM refusing one step of a grant.
    /// </summary>
    [LoggerMessage(EventId = 1100015, Level = LogLevel.Error, Message = "DSM refused the ACL {Step} on {SharePath}: error code {ErrorCode}")]
    public static partial void SiteAccessRefused(this ILogger<ILogFileSystemService> logger, string sharePath, string step, int? errorCode);

    /// <summary>
    /// Logs an ACL task DSM did not finish in time.
    /// </summary>
    [LoggerMessage(EventId = 1100016, Level = LogLevel.Error, Message = "DSM's ACL task on {SharePath} did not finish within {Seconds} s")]
    public static partial void AclTaskTimedOut(this ILogger<ILogFileSystemService> logger, string sharePath, int seconds);

    /// <summary>
    /// Logs an ACL task that could not be released once over.
    /// </summary>
    [LoggerMessage(EventId = 1100017, Level = LogLevel.Debug, Message = "Could not release DSM's ACL task {TaskId}")]
    public static partial void AclTaskReleaseFailed(this ILogger<ILogFileSystemService> logger, Exception ex, string taskId);

    /// <summary>
    /// Logs a grant that failed on an exception.
    /// </summary>
    [LoggerMessage(EventId = 1100018, Level = LogLevel.Error, Message = "Granting the http group access for {ApplicationPath} failed")]
    public static partial void SiteAccessGrantError(this ILogger<ILogFileSystemService> logger, Exception ex, string applicationPath);
}
