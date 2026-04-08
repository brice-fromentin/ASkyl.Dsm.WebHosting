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
        var targetDirectory = fileManager.GetDirectory(String.Empty);
        var doExclusion = !String.IsNullOrWhiteSpace(exclude);

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

            // Create Entry on runtimes folder
            var targetPath = Path.Combine(targetDirectory, entryName);
            if (entry.EntryType == TarEntryType.Directory)
            {
                Directory.CreateDirectory(targetPath);
            }
            else
            {
                entry.ExtractToFile(targetPath, true);
            }
        }
    }
}
