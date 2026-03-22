using Microsoft.AspNetCore.Mvc;

using Askyl.Dsm.WebHosting.Constants.WebApi;
using Askyl.Dsm.WebHosting.Data.Runtime;
using Askyl.Dsm.WebHosting.Data.Services;
using Askyl.Dsm.WebHosting.Ui.Authorization;

namespace Askyl.Dsm.WebHosting.Ui.Controllers;

/// <summary>
/// Controller for runtime management operations (.NET versions, ASP.NET channels).
/// Requires authentication via server-side session.
/// </summary>
[ApiController]
[Route(RuntimeManagementRoutes.ControllerBaseRoute)]
[AuthorizeSession]
public class RuntimeManagementController(IDotnetVersionService dotnetVersionService) : ControllerBase
{
    /// <summary>
    /// Gets the list of installed .NET versions on the system.
    /// </summary>
    /// <returns>A list of FrameworkInfo objects representing installed frameworks.</returns>
    [HttpGet(RuntimeManagementRoutes.VersionsRoute)]
    public async Task<ActionResult<List<FrameworkInfo>>> GetInstalledVersionsAsync()
        => Ok(await dotnetVersionService.GetInstalledVersionsAsync());

    /// <summary>
    /// Checks if a specific channel is installed for a given framework type.
    /// </summary>
    /// <param name="productVersion">The product version/channel to check (e.g., "8.0").</param>
    /// <returns>True if the channel is installed, false otherwise.</returns>
    [HttpGet(RuntimeManagementRoutes.ChannelInstalledRoute + "/{productVersion}")]
    public async Task<ActionResult<bool>> IsChannelInstalled(string productVersion)
        => Ok(await dotnetVersionService.IsChannelInstalledAsync(productVersion));

    /// <summary>
    /// Checks if a specific version is installed for a given framework type.
    /// </summary>
    /// <param name="version">The version to check (e.g., "8.0.1").</param>
    /// <returns>True if the version is installed, false otherwise.</returns>
    [HttpGet(RuntimeManagementRoutes.VersionInstalledRoute + "/{version}")]
    public async Task<ActionResult<bool>> IsVersionInstalled(string version)
        => Ok(await dotnetVersionService.IsVersionInstalledAsync(version));

    /// <summary>
    /// Gets the list of available ASP.NET channels.
    /// </summary>
    /// <returns>A list of AspNetChannel objects representing available channels.</returns>
    [HttpGet(RuntimeManagementRoutes.ChannelsRoute)]
    public async Task<ActionResult<List<AspNetChannel>>> GetChannelsAsync()
        => Ok(await dotnetVersionService.GetChannelsAsync());

    /// <summary>
    /// Gets the list of ASP.NET releases with installation status for a given channel.
    /// </summary>
    /// <param name="productVersion">The product version/channel to check (e.g., "8.0").</param>
    /// <returns>A list of AspNetRelease objects with installation status.</returns>
    [HttpGet(RuntimeManagementRoutes.ReleasesWithStatusRoute + "/{productVersion}")]
    public async Task<ActionResult<List<AspNetRelease>>> GetReleasesWithStatusAsync(string productVersion)
        => Ok(await dotnetVersionService.GetReleasesWithStatusAsync(productVersion));
}
