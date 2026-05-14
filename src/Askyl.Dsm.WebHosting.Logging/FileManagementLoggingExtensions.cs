using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Logging;

/// <summary>
/// Structured logging extension methods for file system and archive operations.
/// </summary>
public static partial class FileManagementLoggingExtensions
{
    #region FileSystemService

    /// <summary>
    /// Logs retrieval of shared folders from DSM FileStation API.
    /// </summary>
    [LoggerMessage(EventId = 1100, Level = LogLevel.Debug, Message = "Retrieving shared folders from DSM FileStation API")]
    public static partial void RetrievingSharedFolders(this ILogger logger);

    /// <summary>
    /// Logs the count of shared folders retrieved.
    /// </summary>
    [LoggerMessage(EventId = 1101, Level = LogLevel.Information, Message = "Retrieved {Count} shared folders")]
    public static partial void RetrievedSharedFolders(this ILogger logger, int count);

    /// <summary>
    /// Logs an error during shared folder retrieval.
    /// </summary>
    [LoggerMessage(EventId = 1102, Level = LogLevel.Error, Message = "Error retrieving shared folders")]
    public static partial void ErrorRetrievingSharedFolders(this ILogger logger, Exception ex);

    /// <summary>
    /// Logs retrieval of directory contents.
    /// </summary>
    [LoggerMessage(EventId = 1103, Level = LogLevel.Debug, Message = "Retrieving directory contents for path: {Path}, DirectoryOnly: {DirectoryOnly}")]
    public static partial void RetrievingDirectoryContents(this ILogger logger, string path, bool directoryOnly);

    /// <summary>
    /// Logs the count of directories retrieved from a path.
    /// </summary>
    [LoggerMessage(EventId = 1104, Level = LogLevel.Debug, Message = "Retrieved {DirectoryCount} directories from {Path}")]
    public static partial void RetrievedDirectories(this ILogger logger, int directoryCount, string path);

    /// <summary>
    /// Logs the count of directories and files retrieved from a path.
    /// </summary>
    [LoggerMessage(EventId = 1105, Level = LogLevel.Debug, Message = "Retrieved {DirectoryCount} directories and {FileCount} files from {Path}")]
    public static partial void RetrievedDirectoriesAndFiles(this ILogger logger, int directoryCount, int fileCount, string path);

    /// <summary>
    /// Logs an error during directory retrieval.
    /// </summary>
    [LoggerMessage(EventId = 1106, Level = LogLevel.Error, Message = "Error retrieving directory contents for {Path}")]
    public static partial void ErrorRetrievingDirectory(this ILogger logger, Exception ex, string path);

    /// <summary>
    /// Logs setting of HTTP group permissions for a virtual path.
    /// </summary>
    [LoggerMessage(EventId = 1107, Level = LogLevel.Debug, Message = "Setting HTTP group permissions for virtual path: {Path}")]
    public static partial void SettingHttpGroupPermissions(this ILogger logger, string path);

    /// <summary>
    /// Logs path validation failure.
    /// </summary>
    [LoggerMessage(EventId = 1108, Level = LogLevel.Warning, Message = "Path validation failed: {Path}")]
    public static partial void PathValidationFailed(this ILogger logger, string path);

    /// <summary>
    /// Logs the target path and directory status.
    /// </summary>
    [LoggerMessage(EventId = 1109, Level = LogLevel.Debug, Message = "Target path: {TargetPath}, IsDirectory: {IsDirectory}")]
    public static partial void TargetPathInfo(this ILogger logger, string targetPath, bool isDirectory);

    /// <summary>
    /// Logs ACL permission failure with error details.
    /// </summary>
    [LoggerMessage(EventId = 1110, Level = LogLevel.Error, Message = "Failed to set ACL permissions for {Path}: Success={Success}, ErrorCode={ErrorCode}")]
    public static partial void FailedToSetAclPermissions(this ILogger logger, string path, bool? success, int? errorCode);

    /// <summary>
    /// Logs successful ACL permission setting with task ID.
    /// </summary>
    [LoggerMessage(EventId = 1111, Level = LogLevel.Information, Message = "ACL permissions set successfully for {Path}, TaskId: {TaskId}")]
    public static partial void AclPermissionsSet(this ILogger logger, string path, string taskId);

    #endregion

    #region FileManagerService

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

    #endregion

    #region LogDownloadService

    /// <summary>
    /// Logs creation of log archive stream.
    /// </summary>
    [LoggerMessage(EventId = 1118, Level = LogLevel.Debug, Message = "Creating log archive stream (BaseDirectory: {BaseDirectory})")]
    public static partial void CreatingLogArchiveStream(this ILogger logger, string baseDirectory);

    /// <summary>
    /// Logs added debug log file.
    /// </summary>
    [LoggerMessage(EventId = 1119, Level = LogLevel.Debug, Message = "Added debug log file: {DebugLogFilePath}")]
    public static partial void AddedDebugLogFile(this ILogger logger, string debugLogFilePath);

    /// <summary>
    /// Logs missing debug log file.
    /// </summary>
    [LoggerMessage(EventId = 1120, Level = LogLevel.Warning, Message = "Debug log file not found at path: {DebugLogFilePath}")]
    public static partial void DebugLogNotFound(this ILogger logger, string debugLogFilePath);

    /// <summary>
    /// Logs created log archive with size.
    /// </summary>
    [LoggerMessage(EventId = 1121, Level = LogLevel.Information, Message = "Created log archive stream with size {Size} bytes")]
    public static partial void CreatedLogArchiveStream(this ILogger logger, long size);

    /// <summary>
    /// Logs added application log from directory.
    /// </summary>
    [LoggerMessage(EventId = 1122, Level = LogLevel.Debug, Message = "Added {LogName} from directory: {DirectoryPath} (BaseDirectory: {BaseDirectory})")]
    public static partial void AddedAppLog(this ILogger logger, string logName, string directoryPath, string baseDirectory);

    /// <summary>
    /// Logs missing application log directory.
    /// </summary>
    [LoggerMessage(EventId = 1123, Level = LogLevel.Warning, Message = "{LogName} directory not found at path: {DirectoryPath} (BaseDirectory: {BaseDirectory})")]
    public static partial void AppLogDirectoryNotFound(this ILogger logger, string logName, string directoryPath, string baseDirectory);

    /// <summary>
    /// Logs file added to archive.
    /// </summary>
    [LoggerMessage(EventId = 1124, Level = LogLevel.Debug, Message = "Added file to archive: {EntryName}")]
    public static partial void AddedFileToArchive(this ILogger logger, string entryName);

    #endregion
}
