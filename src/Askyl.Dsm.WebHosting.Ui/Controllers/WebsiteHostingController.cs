using Microsoft.AspNetCore.Mvc;

using Askyl.Dsm.WebHosting.Constants.API;
using Askyl.Dsm.WebHosting.Data.Results;
using Askyl.Dsm.WebHosting.Data.Services;
using Askyl.Dsm.WebHosting.Data.WebSites;

namespace Askyl.Dsm.WebHosting.Ui.Controllers;

/// <summary>
/// Controller for website management operations (CRUD + start/stop).
/// </summary>
[ApiController]
[Route(WebsiteHostingDefaults.ControllerBaseRoute)]
public class WebsiteHostingController(IWebSiteHostingService hostingService) : ControllerBase
{
    /// <summary>
    /// Gets all websites with their current runtime status.
    /// </summary>
    /// <returns>A list of WebSiteInstance objects.</returns>
    [HttpGet(WebsiteHostingDefaults.AllRoute)]
    public async Task<ActionResult<List<WebSiteInstance>>> GetAllWebsitesAsync()
        => Ok(await hostingService.GetAllWebsitesAsync());

    /// <summary>
    /// Adds a new website.
    /// </summary>
    /// <param name="configuration">The website configuration.</param>
    /// <returns>The created WebSiteInstance with runtime status.</returns>
    [HttpPost(WebsiteHostingDefaults.AddRoute)]
    public async Task<ActionResult<WebSiteInstance>> AddWebsite([FromBody] WebSiteConfiguration configuration)
        => Ok(await hostingService.AddWebsiteAsync(configuration));

    /// <summary>
    /// Updates an existing website.
    /// </summary>
    /// <param name="configuration">The updated website configuration.</param>
    /// <returns>The updated WebSiteInstance with runtime status.</returns>
    [HttpPost(WebsiteHostingDefaults.UpdateRoute)]
    public async Task<ActionResult<WebSiteInstance>> UpdateWebsite([FromBody] WebSiteConfiguration configuration)
        => Ok(await hostingService.UpdateWebsiteAsync(configuration));

    /// <summary>
    /// Removes a website.
    /// </summary>
    /// <param name="id">The ID of the website to remove.</param>
    /// <returns>An ApiResult indicating success or failure (always HTTP 200).</returns>
    [HttpDelete(WebsiteHostingDefaults.RemoveRoute + "/{id}")]
    public async Task<ActionResult<ApiResult>> RemoveWebsite(Guid id)
        => Ok(await hostingService.RemoveWebsiteAsync(id));

    /// <summary>
    /// Starts a website.
    /// </summary>
    /// <param name="id">The ID of the website to start.</param>
    /// <returns>An ApiResult indicating success or failure (always HTTP 200).</returns>
    [HttpPost(WebsiteHostingDefaults.StartRoute + "/{id}")]
    public async Task<ActionResult<ApiResult>> StartWebsite(Guid id)
        => Ok(await hostingService.StartWebsiteAsync(id));

    /// <summary>
    /// Stops a website.
    /// </summary>
    /// <param name="id">The ID of the website to stop.</param>
    /// <returns>An ApiResult indicating success or failure (always HTTP 200).</returns>
    [HttpPost(WebsiteHostingDefaults.StopRoute + "/{id}")]
    public async Task<ActionResult<ApiResult>> StopWebsite(Guid id)
        => Ok(await hostingService.StopWebsiteAsync(id));
}
