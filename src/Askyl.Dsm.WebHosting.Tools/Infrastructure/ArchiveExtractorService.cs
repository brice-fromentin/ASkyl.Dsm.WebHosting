using System.Formats.Tar;
using System.IO.Compression;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Logging;
using Askyl.Dsm.WebHosting.Tools.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Tools.Infrastructure;

/// <summary>
/// Service that extracts compressed archive files (tar.gz format).
/// </summary>
public sealed class ArchiveExtractorService(IFileManagerService fileManager, ILogger<ILogArchiveExtractorService> logger) : IArchiveExtractorService
{
    /// <inheritdoc/>
    public void Decompress(string inputFile, string? exclude = null)
    {
        if (String.IsNullOrWhiteSpace(inputFile))
        {
            throw new ArgumentException("Input file path cannot be null or empty", nameof(inputFile));
        }

        if (!File.Exists(inputFile))
        {
            throw new FileNotFoundException("Archive file not found", inputFile);
        }

        var targetDirectory = fileManager.GetDirectory(String.Empty);
        var doExclusion = !String.IsNullOrWhiteSpace(exclude);

        try
        {
            using var archiveStream = File.OpenRead(inputFile);
            using var gzipStream = new GZipStream(archiveStream, CompressionMode.Decompress);
            using var tarReader = new TarReader(gzipStream);

            for (var entry = tarReader.GetNextEntry(); entry is not null; entry = tarReader.GetNextEntry())
            {
                var entryName = entry.Name;

                if (doExclusion && Path.GetFileName(entryName).Equals(exclude, StringComparison.OrdinalIgnoreCase))
                {
                    logger.SkippingArchiveEntry(entryName);
                    continue;
                }

                // Validate extracted path stays within target directory (prevent zip slip)
                var absoluteTargetPath = Path.GetFullPath(Path.Combine(targetDirectory, entryName));

                if (!absoluteTargetPath.StartsWith(targetDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    logger.ArchiveEntryEscapeAttempt(entryName);
                    continue;
                }

                // Create Entry on runtimes folder
                if (entry.EntryType == TarEntryType.Directory)
                {
                    Directory.CreateDirectory(absoluteTargetPath);
                }
                else
                {
                    entry.ExtractToFile(absoluteTargetPath, true);
                }
            }

            logger.ArchiveExtracted(inputFile, targetDirectory);
        }
        catch (InvalidDataException exception)
        {
            logger.ArchiveExtractionCorrupted(exception, inputFile);
            throw;
        }
        catch (UnauthorizedAccessException exception)
        {
            logger.ArchiveExtractionPermissionDenied(exception, targetDirectory);
            throw;
        }
        catch (IOException exception)
        {
            logger.ArchiveExtractionIoError(exception, inputFile);
            throw;
        }
    }
}
