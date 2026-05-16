using Askyl.Dsm.WebHosting.Constants.Runtime;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Domain.Runtime;
using Askyl.Dsm.WebHosting.Data.Results;
using Askyl.Dsm.WebHosting.Logging;
using Askyl.Dsm.WebHosting.Tools.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Ui.Services;

/// <summary>
/// Implementation of IDotnetVersionService that wraps VersionsDetector and Downloader.
/// This service is registered in Ui only (server-side) since it requires access to
/// the file system for .NET installation detection.
/// </summary>
/// <param name="logger">Logger instance.</param>
/// <param name="versionsDetector">Service for detecting installed .NET versions.</param>
/// <param name="downloader">Service for downloading .NET runtimes.</param>
public class DotnetVersionService(ILogger<ILogDotnetVersionService> logger, IVersionsDetectorService versionsDetector, IDownloaderService downloader) : IDotnetVersionService
{
    public async Task<InstalledVersionsResult> GetInstalledVersionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var versions = await versionsDetector.GetInstalledVersionsAsync();
            return InstalledVersionsResult.CreateSuccess(versions);
        }
        catch (Exception ex)
        {
            logger.FailedToGetInstalledVersions(ex);
            return InstalledVersionsResult.CreateFailure($"Failed to get installed versions: {ex.Message}");
        }
    }

    public async Task<ApiResultBool> IsChannelInstalledAsync(string channel, string frameworkType = DotNetFrameworkTypes.AspNetCore, CancellationToken cancellationToken = default)
    {
        try
        {
            var isInstalled = versionsDetector.IsChannelInstalled(channel, frameworkType);
            return ApiResultBool.CreateSuccess(isInstalled);
        }
        catch (Exception ex)
        {
            logger.FailedToCheckChannelInstalled(ex, channel);
            return ApiResultBool.CreateFailure($"Failed to check if channel '{channel}' is installed: {ex.Message}");
        }
    }

    public async Task<ApiResultBool> IsVersionInstalledAsync(string version, string frameworkType = DotNetFrameworkTypes.AspNetCore, CancellationToken cancellationToken = default)
    {
        try
        {
            var isInstalled = versionsDetector.IsVersionInstalled(version, frameworkType);
            return ApiResultBool.CreateSuccess(isInstalled);
        }
        catch (Exception ex)
        {
            logger.FailedToCheckVersionInstalled(ex, version);
            return ApiResultBool.CreateFailure($"Failed to check if version '{version}' is installed: {ex.Message}");
        }
    }

    /// <summary>
    /// Forces a cache refresh by re-executing dotnet --info.
    /// Call this after install/uninstall operations.
    /// </summary>
    public async Task RefreshCacheAsync()
    {
        using var timer = new OperationTimer(elapsed => logger.RefreshCacheDuration(elapsed));

        logger.RefreshCacheStarting();
        await versionsDetector.RefreshCacheAsync();
    }

    public async Task<ChannelsResult> GetChannelsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            logger.QueryingChannels();

            await GetInstalledVersionsAsync(cancellationToken);

            var channels = await downloader.GetAspNetCoreChannelsAsync(cancellationToken);

            var channelList = channels.Select(channel => AspNetChannel.FromReleaseInfo(channel)).ToList();
            return ChannelsResult.CreateSuccess(channelList);
        }
        catch (Exception ex)
        {
            logger.FailedToGetChannels(ex);
            return ChannelsResult.CreateFailure($"Failed to get ASP.NET Core channels: {ex.Message}");
        }
    }

    public async Task<ReleasesResult> GetReleasesWithStatusAsync(string channel, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.QueryingReleases(channel);

            var releases = await downloader.GetAspNetCoreReleasesAsync(channel, cancellationToken);

            var releaseList = new List<AspNetRelease>();

            foreach (var release in releases)
            {
                var isInstalledResult = await IsVersionInstalledAsync(release.Version, DotNetFrameworkTypes.AspNetCore);
                var isInstalled = isInstalledResult.Value ?? false;
                releaseList.Add(AspNetRelease.Create(release, isInstalled));
            }

            return ReleasesResult.CreateSuccess(releaseList);
        }
        catch (Exception ex)
        {
            logger.FailedToGetReleases(ex, channel);
            return ReleasesResult.CreateFailure($"Failed to get releases for channel '{channel}': {ex.Message}");
        }
    }
}
