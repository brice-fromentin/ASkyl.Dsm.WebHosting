using Askyl.Dsm.WebHosting.Constants.WebApi;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Domain.WebSites;
using Askyl.Dsm.WebHosting.Data.Results;
using Askyl.Dsm.WebHosting.Ui.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Askyl.Dsm.WebHosting.Ui.Controllers;

/// <summary>
/// Controller for website management operations (CRUD + start/stop).
/// </summary>
/// <remarks>
/// CSRF Protection: The session cookie uses SameSite=Strict, which prevents browsers
/// from sending it on cross-origin requests. Combined with [AuthorizeSession] validation
/// against the DSM server, this provides adequate CSRF defense without requiring
/// [ValidateAntiForgeryToken] on each endpoint.
/// </remarks>
[ApiController]
[Route(WebsiteHostingRoutes.ControllerBaseRoute)]
[AuthorizeSession]
public class WebsiteHostingController(IWebSiteHostingService hostingService) : ControllerBase
{
    /// <summary>
    /// Gets all websites with their current runtime status.
    /// </summary>
    /// <returns>A list of WebSiteInstance objects.</returns>
    [HttpGet(WebsiteHostingRoutes.AllRoute)]
    public async Task<ActionResult<List<WebSiteInstance>>> GetAllWebsitesAsync()
        => Ok(await hostingService.GetAllWebsitesAsync());

    /// <summary>
    /// Adds a new website.
    /// </summary>
    /// <param name="configuration">The website configuration.</param>
    /// <returns>The created WebSiteInstance with runtime status.</returns>
    [HttpPost(WebsiteHostingRoutes.AddRoute)]
    public async Task<ActionResult<WebSiteInstance>> AddWebsite([FromBody] WebSiteConfiguration configuration)
        => Ok(await hostingService.AddWebsiteAsync(configuration));

    /// <summary>
    /// Updates an existing website.
    /// </summary>
    /// <param name="configuration">The updated website configuration.</param>
    /// <returns>The updated WebSiteInstance with runtime status.</returns>
    [HttpPost(WebsiteHostingRoutes.UpdateRoute)]
    public async Task<ActionResult<WebSiteInstance>> UpdateWebsite([FromBody] WebSiteConfiguration configuration)
        => Ok(await hostingService.UpdateWebsiteAsync(configuration));

    /// <summary>
    /// Removes a website.
    /// </summary>
    /// <param name="id">The ID of the website to remove.</param>
    /// <returns>An ApiResult indicating success or failure (always HTTP 200).</returns>
    [HttpDelete(WebsiteHostingRoutes.RemoveRoute + "/{id}")]
    public async Task<ActionResult<ApiResult>> RemoveWebsite(Guid id)
        => Ok(await hostingService.RemoveWebsiteAsync(id));

    /// <summary>
    /// Starts a website.
    /// </summary>
    /// <param name="id">The ID of the website to start.</param>
    /// <returns>An ApiResult indicating success or failure (always HTTP 200).</returns>
    [HttpPost(WebsiteHostingRoutes.StartRoute + "/{id}")]
    public async Task<ActionResult<ApiResult>> StartWebsite(Guid id)
        => Ok(await hostingService.StartWebsiteAsync(id));

    /// <summary>
    /// Stops a website.
    /// </summary>
    /// <param name="id">The ID of the website to stop.</param>
    /// <returns>An ApiResult indicating success or failure (always HTTP 200).</returns>
    [HttpPost(WebsiteHostingRoutes.StopRoute + "/{id}")]
    public async Task<ActionResult<ApiResult>> StopWebsite(Guid id)
        => Ok(await hostingService.StopWebsiteAsync(id));
}
