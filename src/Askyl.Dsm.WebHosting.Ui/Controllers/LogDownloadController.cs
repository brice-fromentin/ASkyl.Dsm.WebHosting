using Microsoft.AspNetCore.Mvc;

using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Constants.WebApi;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Ui.Authorization;

namespace Askyl.Dsm.WebHosting.Ui.Controllers;

/// <summary>
/// Controller for downloading log archives.
/// With session-based authentication, no temporary token is required.
/// The session cookie (DsmSid) is automatically validated by [AuthorizeSession].
/// </summary>
[ApiController]
[Route(LogDownloadRoutes.ControllerBaseRoute)]
[AuthorizeSession]
public class LogDownloadController(ILogDownloadService logDownloadService) : ControllerBase
{
    /// <summary>
    /// Downloads a ZIP archive containing application logs, package logs, and debug logs.
    /// Session-based authentication is required (DsmSid cookie).
    /// </summary>
    /// <returns>A file result with the log archive.</returns>
    [HttpGet(LogDownloadRoutes.LogsRoute)]
    public async Task<FileResult> DownloadLogs()
        => File(await logDownloadService.CreateLogZipStreamAsync(), "application/zip", $"adwh-logs-{DateTime.Now:yyyy-MM-dd_HHmmss}{LogConstants.ZipFileExtension}");
}
