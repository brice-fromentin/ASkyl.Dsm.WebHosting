using Askyl.Dsm.WebHosting.Constants.Runtime;
using Askyl.Dsm.WebHosting.Data.Runtime;
using Askyl.Dsm.WebHosting.Tools.Runtime;
using Askyl.Dsm.WebHosting.Ui.Models;

namespace Askyl.Dsm.WebHosting.Ui.Services;

public interface IDotnetVersionService
{
    Task<List<FrameworkInfo>> GetInstalledVersionsAsync();

    bool IsChannelInstalled(string channel, string frameworkType);

    bool IsVersionInstalled(string version, string frameworkType);

    Task<List<AspNetChannel>> GetAspNetChannelsAsync();

    Task<List<AspNetRelease>> GetAspNetReleasesWithStatusAsync(string channel);
}

public class DotnetVersionService : IDotnetVersionService
{
    public Task<List<FrameworkInfo>> GetInstalledVersionsAsync()
        => VersionsDetector.GetInstalledVersionsAsync();

    public bool IsChannelInstalled(string channel, string frameworkType = DotNetFrameworkTypes.AspNetCore)
        => VersionsDetector.IsChannelInstalled(channel, frameworkType);

    public bool IsVersionInstalled(string version, string frameworkType = DotNetFrameworkTypes.AspNetCore)
        => VersionsDetector.IsVersionInstalled(version, frameworkType);

    public async Task<List<AspNetChannel>> GetAspNetChannelsAsync()
    {
        await GetInstalledVersionsAsync();

        var channels = await Downloader.GetAspNetCoreChannelsAsync();

        return [.. channels.Select(channel => AspNetChannel.FromReleaseInfo(channel, this))];
    }

    public async Task<List<AspNetRelease>> GetAspNetReleasesWithStatusAsync(string channel)
    {
        var releases = await Downloader.GetAspNetCoreReleasesAsync(channel);
        
        return [.. releases.Select(release =>
        {
            var isInstalled = IsVersionInstalled(release.Version, DotNetFrameworkTypes.AspNetCore);
            return AspNetRelease.Create(release, isInstalled);
        })];
    }
}
