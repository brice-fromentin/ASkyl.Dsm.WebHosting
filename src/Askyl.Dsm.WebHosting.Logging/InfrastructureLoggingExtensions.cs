using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Logging;

/// <summary>
/// Structured logging extension methods for infrastructure services (archive extraction, download, version detection, platform info).
/// </summary>
public static partial class InfrastructureLoggingExtensions
{
    #region ArchiveExtractorService

    /// <summary>
    /// Logs skipping of an archive entry.
    /// </summary>
    [LoggerMessage(EventId = 1800, Level = LogLevel.Debug, Message = "Skipping archive entry: {EntryName}")]
    public static partial void SkippingArchiveEntry(this ILogger logger, string entryName);

    /// <summary>
    /// Logs that an archive entry attempts to escape the target directory.
    /// </summary>
    [LoggerMessage(EventId = 1801, Level = LogLevel.Warning, Message = "Archive entry '{EntryName}' attempts to escape target directory. Skipping.")]
    public static partial void ArchiveEntryEscapeAttempt(this ILogger logger, string entryName);

    /// <summary>
    /// Logs successful archive extraction.
    /// </summary>
    [LoggerMessage(EventId = 1802, Level = LogLevel.Debug, Message = "Successfully extracted archive: {InputFile} to {TargetDirectory}")]
    public static partial void ArchiveExtracted(this ILogger logger, string inputFile, string targetDirectory);

    /// <summary>
    /// Logs archive extraction failure due to corruption or invalid format.
    /// </summary>
    [LoggerMessage(EventId = 1803, Level = LogLevel.Error, Message = "Failed to extract archive. The file may be corrupted or not in valid tar.gz format: {InputFile}")]
    public static partial void ArchiveExtractionCorrupted(this ILogger logger, Exception ex, string inputFile);

    /// <summary>
    /// Logs archive extraction failure due to permission denial.
    /// </summary>
    [LoggerMessage(EventId = 1804, Level = LogLevel.Error, Message = "Permission denied when extracting archive to target directory: {TargetDirectory}")]
    public static partial void ArchiveExtractionPermissionDenied(this ILogger logger, Exception ex, string targetDirectory);

    /// <summary>
    /// Logs archive extraction failure due to I/O error.
    /// </summary>
    [LoggerMessage(EventId = 1805, Level = LogLevel.Error, Message = "I/O error occurred while extracting archive: {InputFile}")]
    public static partial void ArchiveExtractionIoError(this ILogger logger, Exception ex, string inputFile);

    #endregion

    #region VersionsDetectorService

    /// <summary>
    /// Logs that dotnet executable was not found.
    /// </summary>
    [LoggerMessage(EventId = 1806, Level = LogLevel.Warning, Message = "dotnet executable not found at {DotnetPath}. Keeping existing cached data.")]
    public static partial void DotnetExecutableNotFound(this ILogger logger, string dotnetPath);

    /// <summary>
    /// Logs successful framework cache refresh.
    /// </summary>
    [LoggerMessage(EventId = 1807, Level = LogLevel.Debug, Message = "Successfully refreshed framework cache with {FrameworkCount} frameworks")]
    public static partial void FrameworkCacheRefreshed(this ILogger logger, int frameworkCount);

    /// <summary>
    /// Logs that dotnet --info returned empty output.
    /// </summary>
    [LoggerMessage(EventId = 1808, Level = LogLevel.Warning, Message = "dotnet --info returned empty output. Keeping existing cached data.")]
    public static partial void DotnetInfoEmptyOutput(this ILogger logger);

    /// <summary>
    /// Logs failure to refresh framework cache.
    /// </summary>
    [LoggerMessage(EventId = 1809, Level = LogLevel.Error, Message = "Failed to refresh framework cache. Keeping existing cached data.")]
    public static partial void FailedToRefreshFrameworkCache(this ILogger logger, Exception ex);

    #endregion

    #region PlatformInfoService

    /// <summary>
    /// Logs detected CPU architecture.
    /// </summary>
    [LoggerMessage(EventId = 1810, Level = LogLevel.Information, Message = "Detected architecture = {Architecture}")]
    public static partial void DetectedArchitecture(this ILogger logger, string architecture);

    /// <summary>
    /// Logs detected operating system.
    /// </summary>
    [LoggerMessage(EventId = 1811, Level = LogLevel.Information, Message = "Detected OS = {OperatingSystem}")]
    public static partial void DetectedOS(this ILogger logger, string operatingSystem);

    #endregion

    #region DownloaderService

    /// <summary>
    /// Logs download start with URL and destination.
    /// </summary>
    [LoggerMessage(EventId = 1812, Level = LogLevel.Information, Message = "Downloading {Url} to {Destination}")]
    public static partial void DownloadStarted(this ILogger logger, string url, string destination);

    /// <summary>
    /// Logs download completion with size.
    /// </summary>
    [LoggerMessage(EventId = 1813, Level = LogLevel.Information, Message = "Download completed: {Destination} ({Size} bytes)")]
    public static partial void DownloadCompleted(this ILogger logger, string destination, long size);

    /// <summary>
    /// Logs download failure.
    /// </summary>
    [LoggerMessage(EventId = 1814, Level = LogLevel.Error, Message = "Download failed: {Url}")]
    public static partial void DownloadFailed(this ILogger logger, Exception ex, string url);

    #endregion

    #region SystemProcessRunner

    /// <summary>
    /// Logs process spawn with working directory and arguments.
    /// </summary>
    [LoggerMessage(EventId = 1815, Level = LogLevel.Debug, Message = "Spawning process: {FileName} {Arguments} (WorkingDirectory: {WorkingDirectory})")]
    public static partial void ProcessSpawned(this ILogger logger, string fileName, string arguments, string workingDirectory);

    #endregion

    #region SystemProcessHandle

    /// <summary>
    /// Logs process exit with exit code.
    /// </summary>
    [LoggerMessage(EventId = 1816, Level = LogLevel.Information, Message = "Process {ProcessId} exited with code {ExitCode}")]
    public static partial void ProcessExited(this ILogger logger, int processId, int exitCode);

    /// <summary>
    /// Logs process wait timeout.
    /// </summary>
    [LoggerMessage(EventId = 1817, Level = LogLevel.Warning, Message = "Process {ProcessId} did not exit within {TimeoutMs}ms")]
    public static partial void ProcessWaitTimeout(this ILogger logger, int processId, long timeoutMs);

    #endregion

    #region ProcessTerminator

    /// <summary>
    /// Logs SIGTERM signal sent to process.
    /// </summary>
    [LoggerMessage(EventId = 1818, Level = LogLevel.Information, Message = "Sending SIGTERM to process {ProcessId}")]
    public static partial void SigTermSent(this ILogger logger, int processId);

    /// <summary>
    /// Logs SIGKILL signal sent to process.
    /// </summary>
    [LoggerMessage(EventId = 1819, Level = LogLevel.Warning, Message = "Sending SIGKILL to process {ProcessId}")]
    public static partial void SigKillSent(this ILogger logger, int processId);

    /// <summary>
    /// Logs failure to terminate process.
    /// </summary>
    [LoggerMessage(EventId = 1820, Level = LogLevel.Error, Message = "Failed to terminate process {ProcessId}")]
    public static partial void FailedToTerminateProcess(this ILogger logger, Exception ex, int processId);

    #endregion
}
