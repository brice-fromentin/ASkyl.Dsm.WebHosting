using System.Formats.Tar;
using System.IO.Compression;

namespace Askyl.Dsm.WebHosting.Tools.Runtime;

public static class GzUnTar
{
    public static void Decompress(string inputFile, string? exclude = null)
    {
        var targetDirectory = FileSystem.GetDirectory(String.Empty);
        var doExclusion = !String.IsNullOrWhiteSpace(exclude);

        using var archiveStream = File.OpenRead(inputFile);
        using var gzipStream = new GZipStream(archiveStream, CompressionMode.Decompress);
        using var tarReader = new TarReader(gzipStream);

        for (var entry = tarReader.GetNextEntry(); entry is not null; entry = tarReader.GetNextEntry())
        {
            var entryName = entry.Name;

            if (doExclusion && Path.GetFileName(entryName).Equals(exclude, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Skipping " + entryName);
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
