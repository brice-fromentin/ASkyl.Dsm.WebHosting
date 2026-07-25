namespace Askyl.Dsm.WebHosting.Data.Contracts;

/// <summary>
/// Service for creating log archive streams.
/// </summary>
public interface ILogDownloadService
{
    /// <summary>
    /// Creates a ZIP archive stream containing application logs, package logs, and debug logs.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation, with a MemoryStream containing the ZIP archive.</returns>
    Task<Stream> CreateLogZipStreamAsync(CancellationToken cancellationToken = default);
}
