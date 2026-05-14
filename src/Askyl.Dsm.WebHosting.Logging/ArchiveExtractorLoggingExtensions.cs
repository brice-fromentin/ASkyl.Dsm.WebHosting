using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Logging;

/// <summary>Category marker for ILogger&lt;T&gt; — no implementation required.</summary>
public interface ILogArchiveExtractorService { }

/// <summary>
/// Structured logging extension methods for archive extraction operations.
/// </summary>
public static partial class ArchiveExtractorLoggingExtensions
{
    /// <summary>
    /// Logs skipping of an archive entry.
    /// </summary>
    [LoggerMessage(EventId = 1800, Level = LogLevel.Debug, Message = "Skipping archive entry: {EntryName}")]
    public static partial void SkippingArchiveEntry(this ILogger<ILogArchiveExtractorService> logger, string entryName);

    /// <summary>
    /// Logs that an archive entry attempts to escape the target directory.
    /// </summary>
    [LoggerMessage(EventId = 1801, Level = LogLevel.Warning, Message = "Archive entry '{EntryName}' attempts to escape target directory. Skipping.")]
    public static partial void ArchiveEntryEscapeAttempt(this ILogger<ILogArchiveExtractorService> logger, string entryName);

    /// <summary>
    /// Logs successful archive extraction.
    /// </summary>
    [LoggerMessage(EventId = 1802, Level = LogLevel.Debug, Message = "Successfully extracted archive: {InputFile} to {TargetDirectory}")]
    public static partial void ArchiveExtracted(this ILogger<ILogArchiveExtractorService> logger, string inputFile, string targetDirectory);

    /// <summary>
    /// Logs archive extraction failure due to corruption or invalid format.
    /// </summary>
    [LoggerMessage(EventId = 1803, Level = LogLevel.Error, Message = "Failed to extract archive. The file may be corrupted or not in valid tar.gz format: {InputFile}")]
    public static partial void ArchiveExtractionCorrupted(this ILogger<ILogArchiveExtractorService> logger, Exception ex, string inputFile);

    /// <summary>
    /// Logs archive extraction failure due to permission denial.
    /// </summary>
    [LoggerMessage(EventId = 1804, Level = LogLevel.Error, Message = "Permission denied when extracting archive to target directory: {TargetDirectory}")]
    public static partial void ArchiveExtractionPermissionDenied(this ILogger<ILogArchiveExtractorService> logger, Exception ex, string targetDirectory);

    /// <summary>
    /// Logs archive extraction failure due to I/O error.
    /// </summary>
    [LoggerMessage(EventId = 1805, Level = LogLevel.Error, Message = "I/O error occurred while extracting archive: {InputFile}")]
    public static partial void ArchiveExtractionIoError(this ILogger<ILogArchiveExtractorService> logger, Exception ex, string inputFile);
}
