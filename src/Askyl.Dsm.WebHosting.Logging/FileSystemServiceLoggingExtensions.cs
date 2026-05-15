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
    /// Logs setting of HTTP group permissions for a virtual path.
    /// </summary>
    [LoggerMessage(EventId = 1100008, Level = LogLevel.Debug, Message = "Setting HTTP group permissions for virtual path: {Path}")]
    public static partial void SettingHttpGroupPermissions(this ILogger<ILogFileSystemService> logger, string path);

    /// <summary>
    /// Logs path validation failure.
    /// </summary>
    [LoggerMessage(EventId = 1100009, Level = LogLevel.Warning, Message = "Path validation failed: {Path}")]
    public static partial void PathValidationFailed(this ILogger<ILogFileSystemService> logger, string path);

    /// <summary>
    /// Logs the target path and directory status.
    /// </summary>
    [LoggerMessage(EventId = 1100010, Level = LogLevel.Debug, Message = "Target path: {TargetPath}, IsDirectory: {IsDirectory}")]
    public static partial void TargetPathInfo(this ILogger<ILogFileSystemService> logger, string targetPath, bool isDirectory);

    /// <summary>
    /// Logs ACL permission failure with error details.
    /// </summary>
    [LoggerMessage(EventId = 1100011, Level = LogLevel.Error, Message = "Failed to set ACL permissions for {Path}: Success={Success}, ErrorCode={ErrorCode}")]
    public static partial void FailedToSetAclPermissions(this ILogger<ILogFileSystemService> logger, string path, bool? success, int? errorCode);

    /// <summary>
    /// Logs successful ACL permission setting with task ID.
    /// </summary>
    [LoggerMessage(EventId = 1100012, Level = LogLevel.Information, Message = "ACL permissions set successfully for {Path}, TaskId: {TaskId}")]
    public static partial void AclPermissionsSet(this ILogger<ILogFileSystemService> logger, string path, string taskId);
}
