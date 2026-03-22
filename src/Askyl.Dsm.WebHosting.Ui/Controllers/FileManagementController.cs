using Microsoft.AspNetCore.Mvc;

using Askyl.Dsm.WebHosting.Constants.API;
using Askyl.Dsm.WebHosting.Data.Results;
using Askyl.Dsm.WebHosting.Data.Services;

namespace Askyl.Dsm.WebHosting.Ui.Controllers;

/// <summary>
/// Controller for file system operations via Synology FileStation API.
/// </summary>
[ApiController]
[Route(FileManagementDefaults.ControllerBaseRoute)]
public class FileManagementController(IFileSystemService fileSystemService) : ControllerBase
{
    /// <summary>
    /// Gets the list of shared folders from DSM.
    /// </summary>
    /// <returns>A SharedFoldersResult containing a list of file system items.</returns>
    [HttpGet(FileManagementDefaults.SharedFoldersRoute)]
    public async Task<ActionResult<SharedFoldersResult>> GetSharedFoldersAsync()
        => Ok(await fileSystemService.GetSharedFoldersAsync());

    /// <summary>
    /// Gets the contents of a directory.
    /// </summary>
    /// <param name="path">The path to the directory.</param>
    /// <param name="directoryOnly">If true, returns only directories (no files). Default is false.</param>
    /// <returns>A DirectoryContentsResult containing a list of file system items.</returns>
    [HttpGet(FileManagementDefaults.DirectoryContentsRoute)]
    public async Task<ActionResult<DirectoryContentsResult>> GetDirectoryContentsAsync(string path, [FromQuery] bool directoryOnly)
        => Ok(await fileSystemService.GetDirectoryContentsAsync(path, directoryOnly));
}
