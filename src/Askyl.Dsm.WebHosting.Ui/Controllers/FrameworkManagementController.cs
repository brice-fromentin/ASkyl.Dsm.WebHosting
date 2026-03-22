using Microsoft.AspNetCore.Mvc;

using Askyl.Dsm.WebHosting.Constants.API;
using Askyl.Dsm.WebHosting.Data.API.Parameters;
using Askyl.Dsm.WebHosting.Data.Results;
using Askyl.Dsm.WebHosting.Data.Services;
using Askyl.Dsm.WebHosting.Ui.Authorization;

namespace Askyl.Dsm.WebHosting.Ui.Controllers;

/// <summary>
/// Controller for framework (ASP.NET) installation and uninstallation operations.
/// Requires authentication via server-side session.
/// </summary>
[ApiController]
[Route(FrameworkManagementDefaults.ControllerBaseRoute)]
[AuthorizeSession]
public class FrameworkManagementController(IFrameworkManagementService managementService) : ControllerBase
{
    /// <summary>
    /// Installs the specified ASP.NET framework version.
    /// </summary>
    /// <param name="model">The installation model containing version and channel.</param>
    /// <returns>An InstallationResult indicating success or failure (always HTTP 200).</returns>
    [HttpPost(FrameworkManagementDefaults.InstallRoute)]
    public async Task<ActionResult<InstallationResult>> InstallFramework([FromBody] InstallFrameworkModel model)
        => Ok(await managementService.InstallFrameworkAsync(model.Version, model.Channel));

    /// <summary>
    /// Uninstalls the specified ASP.NET framework version.
    /// </summary>
    /// <param name="version">The version to uninstall.</param>
    /// <returns>An InstallationResult indicating success or failure (always HTTP 200).</returns>
    [HttpPost(FrameworkManagementDefaults.UninstallRoute + "/{version}")]
    public async Task<ActionResult<InstallationResult>> UninstallFramework(string version)
        => Ok(await managementService.UninstallFrameworkAsync(version));
}
