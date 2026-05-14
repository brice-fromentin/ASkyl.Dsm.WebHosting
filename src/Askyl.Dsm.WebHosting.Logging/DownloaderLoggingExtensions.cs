using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Logging;

/// <summary>
/// Structured logging extension methods for download operations.
/// </summary>
public static partial class DownloaderLoggingExtensions
{
    /// <summary>
    /// Logs download start with file name and destination.
    /// </summary>
    [LoggerMessage(EventId = 1812, Level = LogLevel.Information, Message = "Downloading {FileName} to {Destination}")]
    public static partial void DownloadStarted(this ILogger logger, string fileName, string destination);

    /// <summary>
    /// Logs download completion with size.
    /// </summary>
    [LoggerMessage(EventId = 1813, Level = LogLevel.Information, Message = "Download completed: {Destination} ({Size} bytes)")]
    public static partial void DownloadCompleted(this ILogger logger, string destination, long size);

    /// <summary>
    /// Logs that a download was skipped because the file already exists.
    /// </summary>
    [LoggerMessage(EventId = 1814, Level = LogLevel.Debug, Message = "Skipping download - file already exists: {Destination}")]
    public static partial void DownloadSkipped(this ILogger logger, string destination);

    /// <summary>
    /// Logs download failure.
    /// </summary>
    [LoggerMessage(EventId = 1815, Level = LogLevel.Error, Message = "Download failed: {FileName}")]
    public static partial void DownloadFailed(this ILogger logger, Exception ex, string fileName);
}
