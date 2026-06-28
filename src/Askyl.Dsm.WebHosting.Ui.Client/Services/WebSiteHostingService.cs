using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Constants.WebApi;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Domain.WebSites;
using Askyl.Dsm.WebHosting.Data.Results;
using Askyl.Dsm.WebHosting.Globalization;
using Askyl.Dsm.WebHosting.Tools.Extensions;

namespace Askyl.Dsm.WebHosting.Ui.Client.Services;

/// <summary>
/// Client-side proxy for IWebSiteHostingService that calls REST API endpoints.
/// </summary>
/// <param name="httpClientFactory">HttpClientFactory to create the named client.</param>
/// <param name="localizer">Localizer for user-facing strings.</param>
public class WebSiteHostingService(IHttpClientFactory httpClientFactory, ILocalizer localizer) : IWebSiteHostingService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(ApplicationConstants.HttpClientName);

    /// <inheritdoc/>
    public async Task<WebSiteInstancesResult> GetAllWebsitesAsync(CancellationToken cancellationToken = default)
        => await _httpClient.GetJsonOrDefaultAsync(WebsiteHostingRoutes.AllFullRoute, () => WebSiteInstancesResult.CreateFailure(localizer[LK.Error.FailedToLoadWebsites]), cancellationToken);

    /// <inheritdoc/>
    public async Task<WebSiteInstanceResult> AddWebsiteAsync(WebSiteConfiguration configuration, CancellationToken cancellationToken = default)
        => await _httpClient.PostJsonOrDefaultAsync(WebsiteHostingRoutes.AddFullRoute, configuration, () => WebSiteInstanceResult.CreateFailure(localizer[LK.Error.FailedToAddWebsite]), cancellationToken);

    /// <inheritdoc/>
    public async Task<WebSiteInstanceResult> UpdateWebsiteAsync(WebSiteConfiguration configuration, CancellationToken cancellationToken = default)
        => await _httpClient.PostJsonOrDefaultAsync(WebsiteHostingRoutes.UpdateFullRoute, configuration, () => WebSiteInstanceResult.CreateFailure(localizer[LK.Error.FailedToUpdateWebsite]), cancellationToken);

    /// <inheritdoc/>
    public async Task<ApiResult> RemoveWebsiteAsync(Guid id, CancellationToken cancellationToken = default)
        => await _httpClient.DeleteJsonOrDefaultAsync($"{WebsiteHostingRoutes.RemoveFullRoute}/{id}", () => ApiResult.CreateFailure(localizer[LK.Error.FailedToRemoveWebsite]), cancellationToken);

    /// <inheritdoc/>
    public async Task<ApiResult> StartWebsiteAsync(Guid id, CancellationToken cancellationToken = default)
        => await _httpClient.PostJsonOrDefaultAsync<object, ApiResult>($"{WebsiteHostingRoutes.StartFullRoute}/{id}", null, () => ApiResult.CreateFailure(localizer[LK.Error.FailedToStartWebsite]), cancellationToken);

    /// <inheritdoc/>
    public async Task<ApiResult> StopWebsiteAsync(Guid id, CancellationToken cancellationToken = default)
        => await _httpClient.PostJsonOrDefaultAsync<object, ApiResult>($"{WebsiteHostingRoutes.StopFullRoute}/{id}", null, () => ApiResult.CreateFailure(localizer[LK.Error.FailedToStopWebsite]), cancellationToken);
}
