using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Constants.Runtime;
using Askyl.Dsm.WebHosting.Constants.WebApi;
using Askyl.Dsm.WebHosting.Data.Domain.Runtime;
using Askyl.Dsm.WebHosting.Data.Results;
using Askyl.Dsm.WebHosting.Data.Services;
using Askyl.Dsm.WebHosting.Tools.Extensions;

namespace Askyl.Dsm.WebHosting.Ui.Client.Services;

/// <summary>
/// Client-side proxy for IDotnetVersionService that calls REST API endpoints.
/// </summary>
/// <param name="httpClientFactory">HttpClientFactory to create the named client.</param>
public class DotnetVersionService(IHttpClientFactory httpClientFactory) : IDotnetVersionService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(ApplicationConstants.HttpClientName);

    /// <inheritdoc/>
    public async Task<InstalledVersionsResult> GetInstalledVersionsAsync()
        => await _httpClient.GetJsonOrDefaultAsync<InstalledVersionsResult>(RuntimeManagementRoutes.VersionsFullRoute, () => InstalledVersionsResult.CreateFailure("Failed to load installed versions"));

    /// <inheritdoc/>
    public async Task<ApiResultBool> IsChannelInstalledAsync(string channel, string frameworkType = DotNetFrameworkTypes.AspNetCore)
    {
        var url = RuntimeManagementRoutes.ChannelInstalledFullRoute(channel);
        return await _httpClient.GetJsonOrDefaultAsync<ApiResultBool>(url, () => ApiResultBool.CreateFailure($"Failed to check if channel '{channel}' is installed"));
    }

    /// <inheritdoc/>
    public async Task<ApiResultBool> IsVersionInstalledAsync(string version, string frameworkType = DotNetFrameworkTypes.AspNetCore)
    {
        var url = RuntimeManagementRoutes.VersionInstalledFullRoute(version);
        return await _httpClient.GetJsonOrDefaultAsync<ApiResultBool>(url, () => ApiResultBool.CreateFailure($"Failed to check if version '{version}' is installed"));
    }

    /// <inheritdoc/>
    public async Task<ChannelsResult> GetChannelsAsync()
        => await _httpClient.GetJsonOrDefaultAsync<ChannelsResult>(RuntimeManagementRoutes.ChannelsFullRoute, () => ChannelsResult.CreateFailure("Failed to load available channels"));

    /// <inheritdoc/>
    public async Task<ReleasesResult> GetReleasesWithStatusAsync(string channel)
    {
        var url = RuntimeManagementRoutes.ReleasesWithStatusFullRoute(channel);
        return await _httpClient.GetJsonOrDefaultAsync<ReleasesResult>(url, () => ReleasesResult.CreateFailure($"Failed to load releases for channel '{channel}'"));
    }
}
