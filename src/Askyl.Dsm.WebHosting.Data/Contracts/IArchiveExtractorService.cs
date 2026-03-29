namespace Askyl.Dsm.WebHosting.Data.Contracts;

/// <summary>
/// Provides archive extraction operations for compressed files (tar.gz format).
/// </summary>
public interface IArchiveExtractorService
{
    /// <summary>
    /// Decompresses a tar.gz archive file to the target directory.
    /// </summary>
    /// <param name="inputFile">The path to the compressed archive file.</param>
    /// <param name="exclude">Optional file name to exclude from extraction.</param>
    void Decompress(string inputFile, string? exclude = null);
}
