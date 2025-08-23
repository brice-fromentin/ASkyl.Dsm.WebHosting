using Askyl.Dsm.WebHosting.Data.Exceptions;
using Askyl.Dsm.WebHosting.Tools.Runtime;
using Askyl.Dsm.WebHosting.Ui.Models;

namespace Askyl.Dsm.WebHosting.Ui.Services;

public interface IFrameworkManagementService
{
    Task<InstallationResult> InstallFrameworkAsync(string version, string channel);
    Task<InstallationResult> UninstallFrameworkAsync(string version);
}

public class FrameworkManagementService(IDotnetVersionService dotnetVersionService) : IFrameworkManagementService
{
    private readonly IDotnetVersionService _dotnetVersionService = dotnetVersionService;

    public async Task<InstallationResult> InstallFrameworkAsync(string version, string channel)
    {
        try
        {
            FileSystem.Initialize("../runtimes");

            // Download the specific framework version
            var fileName = await Downloader.DownloadVersionToAsync(version, channel, true);

            // Extract and install
            GzUnTar.Decompress(fileName);

            // Refresh the cache to detect the new installation
            await _dotnetVersionService.GetInstalledVersionsAsync();

            return InstallationResult.CreateSuccess($"ASP.NET Core {version} has been installed successfully.");
        }
        catch (Exception ex)
        {
            return InstallationResult.CreateFailure($"Installation failed: {ex.Message}");
        }
    }

    public async Task<InstallationResult> UninstallFrameworkAsync(string version)
    {
        try
        {
            // Prevent uninstall if this is the only release for the configured channel
            // Ensure uninstall allowed according to configured channel (if any)
            await EnsureUninstallAllowedForChannelAsync(version);

            // Delete the directories related to the specified version
            FileSystem.Initialize("../runtimes");
            FileSystem.DeleteDirectory($"host/fxr/{version}");
            FileSystem.DeleteDirectory($"shared/Microsoft.AspNetCore.App/{version}");
            FileSystem.DeleteDirectory($"shared/Microsoft.NETCore.App/{version}");

            // Refresh the cache to detect the removal
            await _dotnetVersionService.GetInstalledVersionsAsync();

            return InstallationResult.CreateSuccess($"ASP.NET Core {version} has been uninstalled successfully.");
        }
        catch (LastReleaseUninstallException ex)
        {
            return InstallationResult.CreateFailure(ex.Message);
        }
        catch (MissingChannelConfigurationException ex)
        {
            return InstallationResult.CreateFailure(ex.Message);
        }
        catch (Exception ex)
        {
            return InstallationResult.CreateFailure($"Uninstallation failed: {ex.Message}");
        }
    }

    private async Task EnsureUninstallAllowedForChannelAsync(string version)
    {
        var configuredChannel = Configuration.ChannelVersion;

        if (String.IsNullOrWhiteSpace(configuredChannel))
        {
            throw new MissingChannelConfigurationException();
        }

        var installed = await _dotnetVersionService.GetInstalledVersionsAsync();

        var channelPrefix = configuredChannel + ".";

        var releasesInChannel = installed.Where(f => f.Type == "ASP.NET Core" && f.Version.StartsWith(channelPrefix, StringComparison.OrdinalIgnoreCase))
                                         .Select(f => f.Version)
                                         .Distinct(StringComparer.OrdinalIgnoreCase)
                                         .ToList();

        if (releasesInChannel.Count <= 1 && releasesInChannel.Contains(version, StringComparer.OrdinalIgnoreCase))
        {
            throw new LastReleaseUninstallException(version, configuredChannel);
        }
    }
}
