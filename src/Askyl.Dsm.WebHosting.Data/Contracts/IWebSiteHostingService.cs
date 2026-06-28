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
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A WebSiteInstancesResult containing a list of WebSiteInstance objects.</returns>
    Task<WebSiteInstancesResult> GetAllWebsitesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new website configuration and creates an instance.
    /// </summary>
    /// <param name="configuration">The website configuration to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A WebSiteInstanceResult containing the created website instance.</returns>
    Task<WebSiteInstanceResult> AddWebsiteAsync(WebSiteConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing website configuration and refreshes the instance.
    /// </summary>
    /// <param name="configuration">The updated website configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A WebSiteInstanceResult containing the updated website instance.</returns>
    Task<WebSiteInstanceResult> UpdateWebsiteAsync(WebSiteConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a website by ID.
    /// </summary>
    /// <param name="id">The unique identifier of the website to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An ApiResult indicating success or failure.</returns>
    Task<ApiResult> RemoveWebsiteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts a website by ID.
    /// </summary>
    /// <param name="id">The unique identifier of the website to start.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An ApiResult indicating success or failure.</returns>
    Task<ApiResult> StartWebsiteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops a website by ID.
    /// </summary>
    /// <param name="id">The unique identifier of the website to stop.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An ApiResult indicating success or failure.</returns>
    Task<ApiResult> StopWebsiteAsync(Guid id, CancellationToken cancellationToken = default);
}
