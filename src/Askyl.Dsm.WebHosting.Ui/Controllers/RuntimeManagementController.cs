using Askyl.Dsm.WebHosting.Constants.WebApi;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Domain.Runtime;
using Askyl.Dsm.WebHosting.Ui.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Askyl.Dsm.WebHosting.Ui.Controllers;

/// <summary>
/// Controller for runtime management operations (.NET versions, ASP.NET channels).
/// Requires authentication via server-side session.
/// </summary>
/// <remarks>
/// CSRF Protection: The session cookie uses SameSite=Strict, which prevents browsers
/// from sending it on cross-origin requests. Combined with [AuthorizeSession] validation
/// against the DSM server, this provides adequate CSRF defense without requiring
/// [ValidateAntiForgeryToken] on each endpoint.
/// </remarks>
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
    public async Task<ActionResult<List<FrameworkInfo>>> GetInstalledVersionsAsync(CancellationToken cancellationToken)
        => Ok(await dotnetVersionService.GetInstalledVersionsAsync(cancellationToken));

    /// <summary>
    /// Checks if a specific channel is installed for a given framework type.
    /// </summary>
    /// <param name="productVersion">The product version/channel to check (e.g., "8.0").</param>
    /// <returns>True if the channel is installed, false otherwise.</returns>
    [HttpGet(RuntimeManagementRoutes.ChannelInstalledRoute + "/{productVersion}")]
    public async Task<ActionResult<bool>> IsChannelInstalled(string productVersion, CancellationToken cancellationToken)
        => Ok(await dotnetVersionService.IsChannelInstalledAsync(productVersion, cancellationToken: cancellationToken));

    /// <summary>
    /// Checks if a specific version is installed for a given framework type.
    /// </summary>
    /// <param name="version">The version to check (e.g., "8.0.1").</param>
    /// <returns>True if the version is installed, false otherwise.</returns>
    [HttpGet(RuntimeManagementRoutes.VersionInstalledRoute + "/{version}")]
    public async Task<ActionResult<bool>> IsVersionInstalled(string version, CancellationToken cancellationToken)
        => Ok(await dotnetVersionService.IsVersionInstalledAsync(version, cancellationToken: cancellationToken));

    /// <summary>
    /// Gets the list of available ASP.NET channels.
    /// </summary>
    /// <returns>A list of AspNetCoreReleaseInfo objects representing available channels.</returns>
    [HttpGet(RuntimeManagementRoutes.ChannelsRoute)]
    public async Task<ActionResult<List<AspNetCoreReleaseInfo>>> GetChannelsAsync(CancellationToken cancellationToken)
        => Ok(await dotnetVersionService.GetChannelsAsync(cancellationToken));

    /// <summary>
    /// Gets the list of ASP.NET releases with installation status for a given channel.
    /// </summary>
    /// <param name="productVersion">The product version/channel to check (e.g., "8.0").</param>
    /// <returns>A list of AspNetRelease objects with installation status.</returns>
    [HttpGet(RuntimeManagementRoutes.ReleasesWithStatusRoute + "/{productVersion}")]
    public async Task<ActionResult<List<AspNetRelease>>> GetReleasesWithStatusAsync(string productVersion, CancellationToken cancellationToken)
        => Ok(await dotnetVersionService.GetReleasesWithStatusAsync(productVersion, cancellationToken));
}
