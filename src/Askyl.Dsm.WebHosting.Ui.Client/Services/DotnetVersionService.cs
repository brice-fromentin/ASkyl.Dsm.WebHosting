using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Constants.Runtime;
using Askyl.Dsm.WebHosting.Constants.WebApi;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Results;
using Askyl.Dsm.WebHosting.Globalization;
using Askyl.Dsm.WebHosting.Tools.Extensions;

namespace Askyl.Dsm.WebHosting.Ui.Client.Services;

/// <summary>
/// Client-side proxy for IDotnetVersionService that calls REST API endpoints.
/// </summary>
/// <param name="httpClientFactory">HttpClientFactory to create the named client.</param>
/// <param name="localizer">Localizer for user-facing strings.</param>
public class DotnetVersionService(IHttpClientFactory httpClientFactory, ILocalizer localizer) : IDotnetVersionService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(ApplicationConstants.HttpClientName);

    /// <inheritdoc/>
    public async Task<InstalledVersionsResult> GetInstalledVersionsAsync(CancellationToken cancellationToken = default)
        => await _httpClient.GetJsonOrDefaultAsync(RuntimeManagementRoutes.VersionsFullRoute, () => InstalledVersionsResult.CreateFailure(localizer[L.Error.FailedToLoadInstalledVersions]), cancellationToken);

    /// <inheritdoc/>
    public async Task<ApiResultBool> IsChannelInstalledAsync(string channel, string frameworkType = DotNetFrameworkTypes.AspNetCore, CancellationToken cancellationToken = default)
    {
        var url = RuntimeManagementRoutes.ChannelInstalledFullRoute(channel);
        return await _httpClient.GetJsonOrDefaultAsync(url, () => ApiResultBool.CreateFailure(localizer[L.Error.FailedToCheckChannelInstalled, channel]), cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ApiResultBool> IsVersionInstalledAsync(string version, string frameworkType = DotNetFrameworkTypes.AspNetCore, CancellationToken cancellationToken = default)
    {
        var url = RuntimeManagementRoutes.VersionInstalledFullRoute(version);
        return await _httpClient.GetJsonOrDefaultAsync(url, () => ApiResultBool.CreateFailure(localizer[L.Error.FailedToCheckVersionInstalled, version]), cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ChannelsResult> GetChannelsAsync(CancellationToken cancellationToken = default)
        => await _httpClient.GetJsonOrDefaultAsync(RuntimeManagementRoutes.ChannelsFullRoute, () => ChannelsResult.CreateFailure(localizer[L.Error.FailedToLoadChannels]), cancellationToken);

    /// <inheritdoc/>
    public async Task<ReleasesResult> GetReleasesWithStatusAsync(string channel, CancellationToken cancellationToken = default)
    {
        var url = RuntimeManagementRoutes.ReleasesWithStatusFullRoute(channel);
        return await _httpClient.GetJsonOrDefaultAsync(url, () => ReleasesResult.CreateFailure(localizer[L.Error.FailedToLoadReleasesForChannel, channel]), cancellationToken);
    }

    /// <inheritdoc/>
    public async Task RefreshCacheAsync()
    {
        // Client-side: Just reload versions which will get fresh data from server
        await GetInstalledVersionsAsync(CancellationToken.None);
    }

    public bool IsValidVersionFormat(string version)
    {
        throw new NotImplementedException();
    }
}
