using Askyl.Dsm.WebHosting.Data.Domain.WebSites;
using Askyl.Dsm.WebHosting.Data.Results;

namespace Askyl.Dsm.WebHosting.Data.Contracts;

/// <summary>
/// Service for managing website instances and their lifecycle.
/// </summary>
public interface IWebSiteHostingService
{
    /// <summary>
    /// Gets all website instances with their current runtime status.
    /// </summary>
    /// <returns>A WebSiteInstancesResult containing a list of WebSiteInstance objects.</returns>
    Task<WebSiteInstancesResult> GetAllWebsitesAsync();

    /// <summary>
    /// Adds a new website configuration and creates an instance.
    /// </summary>
    /// <param name="configuration">The website configuration to add.</param>
    /// <returns>A WebSiteInstanceResult containing the created website instance.</returns>
    Task<WebSiteInstanceResult> AddWebsiteAsync(WebSiteConfiguration configuration);

    /// <summary>
    /// Updates an existing website configuration and refreshes the instance.
    /// </summary>
    /// <param name="configuration">The updated website configuration.</param>
    /// <returns>A WebSiteInstanceResult containing the updated website instance.</returns>
    Task<WebSiteInstanceResult> UpdateWebsiteAsync(WebSiteConfiguration configuration);

    /// <summary>
    /// Removes a website by ID.
    /// </summary>
    /// <param name="id">The unique identifier of the website to remove.</param>
    /// <returns>An ApiResult indicating success or failure.</returns>
    Task<ApiResult> RemoveWebsiteAsync(Guid id);

    /// <summary>
    /// Starts a website by ID.
    /// </summary>
    /// <param name="id">The unique identifier of the website to start.</param>
    /// <returns>An ApiResult indicating success or failure.</returns>
    Task<ApiResult> StartWebsiteAsync(Guid id);

    /// <summary>
    /// Stops a website by ID.
    /// </summary>
    /// <param name="id">The unique identifier of the website to stop.</param>
    /// <returns>An ApiResult indicating success or failure.</returns>
    Task<ApiResult> StopWebsiteAsync(Guid id);
}
