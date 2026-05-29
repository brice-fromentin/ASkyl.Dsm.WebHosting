using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Constants.WebApi;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Domain.WebSites;
using Askyl.Dsm.WebHosting.Data.Results;
using Askyl.Dsm.WebHosting.Globalization;
using Askyl.Dsm.WebHosting.Globalization.Resources;
using Askyl.Dsm.WebHosting.Tools.Extensions;
using Microsoft.Extensions.Localization;

namespace Askyl.Dsm.WebHosting.Ui.Client.Services;

/// <summary>
/// Client-side proxy for IWebSiteHostingService that calls REST API endpoints.
/// </summary>
/// <param name="httpClientFactory">HttpClientFactory to create the named client.</param>
/// <param name="localizer">Localizer for user-facing strings.</param>
public class WebSiteHostingService(IHttpClientFactory httpClientFactory, IStringLocalizer<SharedResource> localizer) : IWebSiteHostingService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(ApplicationConstants.HttpClientName);

    /// <inheritdoc/>
    public async Task<WebSiteInstancesResult> GetAllWebsitesAsync()
        => await _httpClient.GetJsonOrDefaultAsync<WebSiteInstancesResult>(WebsiteHostingRoutes.AllFullRoute, () => WebSiteInstancesResult.CreateFailure(localizer[L.Error.FailedToLoadWebsites]));

    /// <inheritdoc/>
    public async Task<WebSiteInstanceResult> AddWebsiteAsync(WebSiteConfiguration configuration)
        => await _httpClient.PostJsonOrDefaultAsync<WebSiteConfiguration, WebSiteInstanceResult>(WebsiteHostingRoutes.AddFullRoute, configuration, () => WebSiteInstanceResult.CreateFailure(localizer[L.Error.FailedToAddWebsite]));

    /// <inheritdoc/>
    public async Task<WebSiteInstanceResult> UpdateWebsiteAsync(WebSiteConfiguration configuration)
        => await _httpClient.PostJsonOrDefaultAsync<WebSiteConfiguration, WebSiteInstanceResult>(WebsiteHostingRoutes.UpdateFullRoute, configuration, () => WebSiteInstanceResult.CreateFailure(localizer[L.Error.FailedToUpdateWebsite]));

    /// <inheritdoc/>
    public async Task<ApiResult> RemoveWebsiteAsync(Guid id)
        => await _httpClient.DeleteJsonOrDefaultAsync<ApiResult>(WebsiteHostingRoutes.RemoveFullRoute + "/" + id, () => ApiResult.CreateFailure(localizer[L.Error.FailedToRemoveWebsite]));

    /// <inheritdoc/>
    public async Task<ApiResult> StartWebsiteAsync(Guid id)
        => await _httpClient.PostJsonOrDefaultAsync<object, ApiResult>(WebsiteHostingRoutes.StartFullRoute + "/" + id, null, () => ApiResult.CreateFailure(localizer[L.Error.FailedToStartWebsite]));

    /// <inheritdoc/>
    public async Task<ApiResult> StopWebsiteAsync(Guid id)
        => await _httpClient.PostJsonOrDefaultAsync<object, ApiResult>(WebsiteHostingRoutes.StopFullRoute + "/" + id, null, () => ApiResult.CreateFailure(localizer[L.Error.FailedToStopWebsite]));
}
