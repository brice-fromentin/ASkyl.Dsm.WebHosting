using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Constants.WebApi;
using Askyl.Dsm.WebHosting.Data.Results;
using Askyl.Dsm.WebHosting.Data.Services;
using Askyl.Dsm.WebHosting.Data.WebSites;
using Askyl.Dsm.WebHosting.Tools.Extensions;

namespace Askyl.Dsm.WebHosting.Ui.Client.Services;

/// <summary>
/// Client-side proxy for IWebSiteHostingService that calls REST API endpoints.
/// </summary>
/// <param name="httpClientFactory">HttpClientFactory to create the named client.</param>
public class WebSiteHostingService(IHttpClientFactory httpClientFactory) : IWebSiteHostingService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(ApplicationConstants.HttpClientName);

    /// <inheritdoc/>
    public async Task<WebSiteInstancesResult> GetAllWebsitesAsync()
        => await _httpClient.GetJsonOrDefaultAsync<WebSiteInstancesResult>(WebsiteHostingRoutes.AllFullRoute, () => WebSiteInstancesResult.CreateFailure("Failed to load websites"));

    /// <inheritdoc/>
    public async Task<WebSiteInstanceResult> AddWebsiteAsync(WebSiteConfiguration configuration)
        => await _httpClient.PostJsonOrDefaultAsync<WebSiteConfiguration, WebSiteInstanceResult>(WebsiteHostingRoutes.AddFullRoute, configuration, () => WebSiteInstanceResult.CreateFailure("Failed to add website"));

    /// <inheritdoc/>
    public async Task<WebSiteInstanceResult> UpdateWebsiteAsync(WebSiteConfiguration configuration)
        => await _httpClient.PostJsonOrDefaultAsync<WebSiteConfiguration, WebSiteInstanceResult>(WebsiteHostingRoutes.UpdateFullRoute, configuration, () => WebSiteInstanceResult.CreateFailure("Failed to update website"));

    /// <inheritdoc/>
    public async Task<ApiResult> RemoveWebsiteAsync(Guid id)
        => await _httpClient.DeleteJsonOrDefaultAsync<ApiResult>(WebsiteHostingRoutes.RemoveFullRoute + "/" + id, () => ApiResult.CreateFailure("Failed to remove website"));

    /// <inheritdoc/>
    public async Task<ApiResult> StartWebsiteAsync(Guid id)
        => await _httpClient.PostJsonOrDefaultAsync<object, ApiResult>(WebsiteHostingRoutes.StartFullRoute + "/" + id, null, () => ApiResult.CreateFailure("Failed to start website"));

    /// <inheritdoc/>
    public async Task<ApiResult> StopWebsiteAsync(Guid id)
        => await _httpClient.PostJsonOrDefaultAsync<object, ApiResult>(WebsiteHostingRoutes.StopFullRoute + "/" + id, null, () => ApiResult.CreateFailure("Failed to stop website"));
}
