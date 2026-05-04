using System.Formats.Tar;
using System.IO.Compression;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Tools.Infrastructure;

/// <summary>
/// Service that extracts compressed archive files (tar.gz format).
/// </summary>
public sealed class ArchiveExtractorService(IFileManagerService fileManager, ILogger<ArchiveExtractorService> logger) : IArchiveExtractorService
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
                    logger.LogDebug("Skipping archive entry: {EntryName}", entryName);
                    continue;
                }

                // Validate extracted path stays within target directory (prevent zip slip)
                var absoluteTargetPath = Path.GetFullPath(Path.Combine(targetDirectory, entryName));

                if (!absoluteTargetPath.StartsWith(targetDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogWarning("Archive entry '{EntryName}' attempts to escape target directory. Skipping.", entryName);
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

            logger.LogDebug("Successfully extracted archive: {InputFile} to {TargetDirectory}", inputFile, targetDirectory);
        }
        catch (InvalidDataException exception)
        {
            logger.LogError(exception, "Failed to extract archive. The file may be corrupted or not in valid tar.gz format: {InputFile}", inputFile);
            throw;
        }
        catch (UnauthorizedAccessException exception)
        {
            logger.LogError(exception, "Permission denied when extracting archive to target directory: {TargetDirectory}", targetDirectory);
            throw;
        }
        catch (IOException exception)
        {
            logger.LogError(exception, "I/O error occurred while extracting archive: {InputFile}", inputFile);
            throw;
        }
    }
}
