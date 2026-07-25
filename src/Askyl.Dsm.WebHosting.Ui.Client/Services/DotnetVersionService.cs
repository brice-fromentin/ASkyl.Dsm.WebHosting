using System.Text.RegularExpressions;
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
public partial class DotnetVersionService(IHttpClientFactory httpClientFactory, ILocalizer localizer) : IDotnetVersionService
{
    [GeneratedRegex(@"^\d+\.\d+(\.\d+)?$")]
    private static partial Regex VersionPattern();

    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(ApplicationConstants.HttpClientName);

    /// <inheritdoc/>
    public async Task<InstalledVersionsResult> GetInstalledVersionsAsync(CancellationToken cancellationToken = default)
        => await _httpClient.GetJsonOrDefaultAsync(RuntimeManagementRoutes.VersionsFullRoute, () => InstalledVersionsResult.CreateFailure(localizer[LK.Error.FailedToLoadInstalledVersions]), cancellationToken);

    /// <inheritdoc/>
    public async Task<ApiResultBool> IsChannelInstalledAsync(string channel, string frameworkType = DotNetFrameworkTypes.AspNetCore, CancellationToken cancellationToken = default)
    {
        var url = RuntimeManagementRoutes.ChannelInstalledFullRoute(channel);
        return await _httpClient.GetJsonOrDefaultAsync(url, () => ApiResultBool.CreateFailure(localizer[LK.Error.FailedToCheckChannelInstalled, channel]), cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ApiResultBool> IsVersionInstalledAsync(string version, string frameworkType = DotNetFrameworkTypes.AspNetCore, CancellationToken cancellationToken = default)
    {
        var url = RuntimeManagementRoutes.VersionInstalledFullRoute(version);
        return await _httpClient.GetJsonOrDefaultAsync(url, () => ApiResultBool.CreateFailure(localizer[LK.Error.FailedToCheckVersionInstalled, version]), cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ChannelsResult> GetChannelsAsync(CancellationToken cancellationToken = default)
        => await _httpClient.GetJsonOrDefaultAsync(RuntimeManagementRoutes.ChannelsFullRoute, () => ChannelsResult.CreateFailure(localizer[LK.Error.FailedToLoadChannels]), cancellationToken);

    /// <inheritdoc/>
    public async Task<ReleasesResult> GetReleasesWithStatusAsync(string channel, CancellationToken cancellationToken = default)
    {
        var url = RuntimeManagementRoutes.ReleasesWithStatusFullRoute(channel);
        return await _httpClient.GetJsonOrDefaultAsync(url, () => ReleasesResult.CreateFailure(localizer[LK.Error.FailedToLoadReleasesForChannel, channel]), cancellationToken);
    }

    /// <inheritdoc/>
    public async Task RefreshCacheAsync(CancellationToken cancellationToken = default)
    {
        // Client-side: Just reload versions which will get fresh data from server
        await GetInstalledVersionsAsync(cancellationToken);
    }

    public bool IsValidVersionFormat(string version)
        => !String.IsNullOrWhiteSpace(version) && VersionPattern().IsMatch(version);
}
