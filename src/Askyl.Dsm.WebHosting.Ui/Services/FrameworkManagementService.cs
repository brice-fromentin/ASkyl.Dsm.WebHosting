using Askyl.Dsm.WebHosting.Constants.Runtime;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Exceptions;
using Askyl.Dsm.WebHosting.Data.Results;
using Askyl.Dsm.WebHosting.Tools.Runtime;

namespace Askyl.Dsm.WebHosting.Ui.Services;

public class FrameworkManagementService(
    IDotnetVersionService dotnetVersionService,
    IPlatformInfoService platformInfo,
    IDownloaderService downloader,
    IFileManagerService fileManager,
    IArchiveExtractorService archiveExtractor,
    ILogger<FrameworkManagementService> logger) : IFrameworkManagementService
{
    private readonly IDotnetVersionService _dotnetVersionService = dotnetVersionService;

    public async Task<InstallationResult> InstallFrameworkAsync(string version, string channel, CancellationToken cancellationToken = default)
    {
        if (String.IsNullOrEmpty(version))
        {
            logger.LogWarning("Install failed - Version is required");
            return InstallationResult.CreateFailure("Version is required");
        }

        try
        {
            // Download the specific framework version
            var fileName = await downloader.DownloadVersionToAsync(version, channel, true, cancellationToken);

            // Extract and install
            archiveExtractor.Decompress(fileName);

            // Force cache refresh to detect the new installation
            await _dotnetVersionService.RefreshCacheAsync();

            logger.LogInformation("ASP.NET Core {Version} installed successfully", version);
            return InstallationResult.CreateSuccess($"ASP.NET Core {version} has been installed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while installing ASP.NET Core {Version}", version);
            return InstallationResult.CreateFailure($"Installation failed: {ex.Message}");
        }
    }

    public async Task<InstallationResult> UninstallFrameworkAsync(string version, CancellationToken cancellationToken = default)
    {
        if (String.IsNullOrEmpty(version))
        {
            logger.LogWarning("Uninstall failed - Version is required");
            return InstallationResult.CreateFailure("Version is required");
        }

        try
        {
            // Prevent uninstall if this is the only release for the configured channel
            // Ensure uninstall allowed according to configured channel (if any)
            await EnsureUninstallAllowedForChannelAsync(version, cancellationToken);

            // Delete the directories related to the specified version
            fileManager.DeleteDirectory($"host/fxr/{version}");
            fileManager.DeleteDirectory($"shared/Microsoft.AspNetCore.App/{version}");
            fileManager.DeleteDirectory($"shared/Microsoft.NETCore.App/{version}");

            // Force cache refresh to detect the removal
            await _dotnetVersionService.RefreshCacheAsync();

            logger.LogInformation("ASP.NET Core {Version} uninstalled successfully", version);
            return InstallationResult.CreateSuccess($"ASP.NET Core {version} has been uninstalled successfully.");
        }
        catch (LastReleaseUninstallException ex)
        {
            logger.LogWarning("Uninstall failed - {Message}", ex.Message);
            return InstallationResult.CreateFailure(ex.Message);
        }
        catch (MissingChannelConfigurationException ex)
        {
            logger.LogWarning("Uninstall failed - {Message}", ex.Message);
            return InstallationResult.CreateFailure(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while uninstalling ASP.NET Core {Version}", version);
            return InstallationResult.CreateFailure($"Uninstallation failed: {ex.Message}");
        }
    }

    private async Task EnsureUninstallAllowedForChannelAsync(string version, CancellationToken cancellationToken)
    {
        var configuredChannel = platformInfo.ChannelVersion;

        if (String.IsNullOrWhiteSpace(configuredChannel))
        {
            throw new MissingChannelConfigurationException();
        }

        var installedResult = await _dotnetVersionService.GetInstalledVersionsAsync(cancellationToken);
        var installed = installedResult.Value ?? [];

        var channelPrefix = configuredChannel + ".";

        var releasesInChannel = installed.Where(f => f.Type == DotNetFrameworkTypes.AspNetCore && f.Version.StartsWith(channelPrefix, StringComparison.OrdinalIgnoreCase))
                                         .Select(f => f.Version)
                                         .Distinct(StringComparer.OrdinalIgnoreCase)
                                         .ToList();

        if (releasesInChannel.Count <= 1 && releasesInChannel.Contains(version, StringComparer.OrdinalIgnoreCase))
        {
            throw new LastReleaseUninstallException(version, configuredChannel);
        }
    }
}
