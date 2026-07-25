using Askyl.Dsm.WebHosting.Constants.WebApi;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Results;
using Askyl.Dsm.WebHosting.Ui.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Askyl.Dsm.WebHosting.Ui.Controllers;

/// <summary>
/// Controller for file system operations via Synology FileStation API.
/// </summary>
/// <remarks>
/// CSRF Protection: The session cookie uses SameSite=Strict, which prevents browsers
/// from sending it on cross-origin requests. Combined with [AuthorizeSession] validation
/// against the DSM server, this provides adequate CSRF defense without requiring
/// [ValidateAntiForgeryToken] on each endpoint.
/// </remarks>
[ApiController]
[Route(FileManagementRoutes.ControllerBaseRoute)]
[AuthorizeSession]
public class FileManagementController(IFileSystemService fileSystemService) : ControllerBase
{
    /// <summary>
    /// Gets the list of shared folders from DSM.
    /// </summary>
    /// <returns>A SharedFoldersResult containing a list of file system items.</returns>
    [HttpGet(FileManagementRoutes.SharedFoldersRoute)]
    public async Task<ActionResult<SharedFoldersResult>> GetSharedFoldersAsync(CancellationToken cancellationToken)
        => Ok(await fileSystemService.GetSharedFoldersAsync(cancellationToken));

    /// <summary>
    /// Gets the contents of a directory.
    /// </summary>
    /// <param name="path">The path to the directory.</param>
    /// <param name="directoryOnly">If true, returns only directories (no files). Default is false.</param>
    /// <returns>A DirectoryContentsResult containing a list of file system items.</returns>
    [HttpGet(FileManagementRoutes.DirectoryContentsRoute)]
    public async Task<ActionResult<DirectoryContentsResult>> GetDirectoryContentsAsync(string path, [FromQuery] bool directoryOnly, CancellationToken cancellationToken)
        => Ok(await fileSystemService.GetDirectoryContentsAsync(path, directoryOnly, cancellationToken));
}
