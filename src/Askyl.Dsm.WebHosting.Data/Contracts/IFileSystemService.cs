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
    /// <returns>A SharedFoldersResult containing a list of file items.</returns>
    Task<SharedFoldersResult> GetSharedFoldersAsync();

    /// <summary>
    /// Gets the contents of a directory.
    /// </summary>
    /// <param name="path">The path to the directory.</param>
    /// <param name="directoryOnly">If true, returns only directories (no files). Default is false.</param>
    /// <returns>A DirectoryContentsResult containing a list of file system items.</returns>
    Task<DirectoryContentsResult> GetDirectoryContentsAsync(string path, bool directoryOnly);

    /// <summary>
    /// Sets HTTP group permissions for a file or directory.
    /// Note: This is now handled internally by WebSiteHostingService and should not be called from client.
    /// </summary>
    /// <param name="path">The path to set permissions for.</param>
    /// <param name="isDirectory">Indicates if the target is a directory.</param>
    /// <returns>An ApiResult indicating success or failure.</returns>
    Task<ApiResult> SetHttpGroupPermissionsAsync(string path, bool isDirectory);
}
