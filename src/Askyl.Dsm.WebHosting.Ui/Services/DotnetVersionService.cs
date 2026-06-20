using System.Text.RegularExpressions;
using Askyl.Dsm.WebHosting.Constants.Runtime;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Domain.Runtime;
using Askyl.Dsm.WebHosting.Data.Results;
using Askyl.Dsm.WebHosting.Globalization;
using Askyl.Dsm.WebHosting.Logging;
using Askyl.Dsm.WebHosting.Tools.Diagnostics;

namespace Askyl.Dsm.WebHosting.Ui.Services;

/// <summary>
/// Implementation of IDotnetVersionService that wraps VersionsDetector and Downloader.
/// This service is registered in Ui only (server-side) since it requires access to
/// the file system for .NET installation detection.
/// </summary>
/// <param name="logger">Logger instance.</param>
/// <param name="localizer">Localizer for user-facing strings.</param>
/// <param name="versionsDetector">Service for detecting installed .NET versions.</param>
/// <param name="downloader">Service for downloading .NET runtimes.</param>
public class DotnetVersionService(ILogger<ILogDotnetVersionService> logger, ILocalizer localizer, IVersionsDetectorService versionsDetector, IDownloaderService downloader) : IDotnetVersionService
{
    private static readonly Regex VersionPattern = new(@"^\d+\.\d+(\.\d+)?$", RegexOptions.Compiled);

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
            return InstalledVersionsResult.CreateFailure(localizer[L.Error.OperationFailed]);
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
            return ApiResultBool.CreateFailure(localizer[L.Error.OperationFailed]);
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
            return ApiResultBool.CreateFailure(localizer[L.Error.OperationFailed]);
        }
    }

    /// <summary>
    /// Forces a cache refresh by re-executing dotnet --info.
    /// Call this after install/uninstall operations.
    /// </summary>
    public async Task RefreshCacheAsync()
    {
        await versionsDetector.RefreshCacheAsync();
    }

    public async Task<ChannelsResult> GetChannelsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            logger.QueryingChannels();

            await GetInstalledVersionsAsync(cancellationToken);

            var channels = await downloader.GetAspNetCoreChannelsAsync(cancellationToken);

            var channelList = channels.ToList();
            return ChannelsResult.CreateSuccess(channelList);
        }
        catch (Exception ex)
        {
            logger.FailedToGetChannels(ex);
            return ChannelsResult.CreateFailure(localizer[L.Error.OperationFailed]);
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
            return ReleasesResult.CreateFailure(localizer[L.Error.OperationFailed]);
        }
    }

    public bool IsValidVersionFormat(string version)
        => !String.IsNullOrWhiteSpace(version) && VersionPattern.IsMatch(version);
}
