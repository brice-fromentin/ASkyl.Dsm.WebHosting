using Askyl.Dsm.WebHosting.Data.Domain.FileSystem;
using Askyl.Dsm.WebHosting.Data.Results;

namespace Askyl.Dsm.WebHosting.Data.Contracts;

/// <summary>
/// Facade service for file system operations via Synology FileStation API.
/// </summary>
public interface IFileSystemService
{
    /// <summary>
    /// Gets the list of shared folders from DSM as simple data items.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A SharedFoldersResult containing a list of file items.</returns>
    Task<SharedFoldersResult> GetSharedFoldersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the contents of a directory.
    /// </summary>
    /// <param name="path">The path to the directory.</param>
    /// <param name="directoryOnly">If true, returns only directories (no files). Default is false.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A DirectoryContentsResult containing a list of file system items.</returns>
    Task<DirectoryContentsResult> GetDirectoryContentsAsync(string path, bool directoryOnly, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gives DSM's http group the least a site needs: Read on the application's folder, and passage only through
    /// each folder above it, up to and including the shared folder. Each folder keeps its own entries; one that
    /// already holds an explicit http entry is left as it is. Called only once the user has agreed to it.
    /// </summary>
    /// <param name="request">The application's share path and real path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An ApiResult indicating success or failure.</returns>
    Task<ApiResult> GrantSiteAccessAsync(SiteAccessRequest request, CancellationToken cancellationToken = default);
}
