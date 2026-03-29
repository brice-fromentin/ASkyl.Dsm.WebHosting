using Askyl.Dsm.WebHosting.Constants.Runtime;
using Askyl.Dsm.WebHosting.Data.Domain.Runtime;
using Askyl.Dsm.WebHosting.Data.Results;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Tools.Runtime;

namespace Askyl.Dsm.WebHosting.Ui.Services;

/// <summary>
/// Implementation of IDotnetVersionService that wraps VersionsDetector and Downloader.
/// This service is registered in Ui only (server-side) since it requires access to
/// the file system for .NET installation detection.
/// </summary>
public class DotnetVersionService(IDownloaderService downloader) : IDotnetVersionService
{
    public async Task<InstalledVersionsResult> GetInstalledVersionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var versions = await VersionsDetector.GetInstalledVersionsAsync();
            return InstalledVersionsResult.CreateSuccess(versions);
        }
        catch (Exception ex)
        {
            return InstalledVersionsResult.CreateFailure($"Failed to get installed versions: {ex.Message}");
        }
    }

    public async Task<ApiResultBool> IsChannelInstalledAsync(string channel, string frameworkType = DotNetFrameworkTypes.AspNetCore, CancellationToken cancellationToken = default)
    {
        try
        {
            var isInstalled = await VersionsDetector.IsChannelInstalledAsync(channel, frameworkType);
            return ApiResultBool.CreateSuccess(isInstalled);
        }
        catch (Exception ex)
        {
            return ApiResultBool.CreateFailure($"Failed to check if channel '{channel}' is installed: {ex.Message}");
        }
    }

    public async Task<ApiResultBool> IsVersionInstalledAsync(string version, string frameworkType = DotNetFrameworkTypes.AspNetCore, CancellationToken cancellationToken = default)
    {
        try
        {
            var isInstalled = await VersionsDetector.IsVersionInstalledAsync(version, frameworkType);
            return ApiResultBool.CreateSuccess(isInstalled);
        }
        catch (Exception ex)
        {
            return ApiResultBool.CreateFailure($"Failed to check if version '{version}' is installed: {ex.Message}");
        }
    }

    public async Task<ChannelsResult> GetChannelsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await GetInstalledVersionsAsync(cancellationToken);

            var channels = await downloader.GetAspNetCoreChannelsAsync(cancellationToken);

            var channelList = channels.Select(channel => AspNetChannel.FromReleaseInfo(channel)).ToList();
            return ChannelsResult.CreateSuccess(channelList);
        }
        catch (Exception ex)
        {
            return ChannelsResult.CreateFailure($"Failed to get ASP.NET Core channels: {ex.Message}");
        }
    }

    public async Task<ReleasesResult> GetReleasesWithStatusAsync(string channel, CancellationToken cancellationToken = default)
    {
        try
        {
            var releases = await downloader.GetAspNetCoreReleasesAsync(channel, cancellationToken);

            var releaseList = releases.Select(release =>
            {
                var isInstalledResult = IsVersionInstalledAsync(release.Version, DotNetFrameworkTypes.AspNetCore).GetAwaiter().GetResult();
                var isInstalled = isInstalledResult.Value ?? false;
                return AspNetRelease.Create(release, isInstalled);
            }).ToList();

            return ReleasesResult.CreateSuccess(releaseList);
        }
        catch (Exception ex)
        {
            return ReleasesResult.CreateFailure($"Failed to get releases for channel '{channel}': {ex.Message}");
        }
    }
}
