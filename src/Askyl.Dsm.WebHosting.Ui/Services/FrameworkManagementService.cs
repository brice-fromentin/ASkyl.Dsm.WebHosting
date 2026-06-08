using Askyl.Dsm.WebHosting.Constants.Runtime;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Exceptions;
using Askyl.Dsm.WebHosting.Data.Results;
using Askyl.Dsm.WebHosting.Globalization;
using Askyl.Dsm.WebHosting.Logging;
using Askyl.Dsm.WebHosting.Tools.Diagnostics;

namespace Askyl.Dsm.WebHosting.Ui.Services;

public class FrameworkManagementService(
    IDotnetVersionService dotnetVersionService,
    IPlatformInfoService platformInfo,
    IDownloaderService downloader,
    IFileManagerService fileManager,
    IArchiveExtractorService archiveExtractor,
    ILogger<ILogFrameworkManagementService> logger,
    ILocalizer localizer) : IFrameworkManagementService
{
    public async Task<InstallationResult> InstallFrameworkAsync(string version, string channel, CancellationToken cancellationToken = default)
    {
        if (String.IsNullOrEmpty(version))
        {
            logger.InstallFailedVersionRequired();
            return InstallationResult.CreateFailure(localizer[L.Validation.VersionRequired]);
        }

        using var timer = new OperationTimer(elapsed => logger.InstallDuration(elapsed, version));

        logger.InstallStarting(version);

        try
        {
            // Download the specific framework version
            var fileName = await downloader.DownloadVersionToAsync(version, channel, true, cancellationToken);

            // Extract and install
            archiveExtractor.Decompress(fileName);

            // Force cache refresh to detect the new installation
            await dotnetVersionService.RefreshCacheAsync();

            logger.FrameworkInstalled(version);
            return InstallationResult.CreateSuccess(localizer[L.Success.InstallationCompleted]);
        }
        catch (Exception ex)
        {
            logger.FrameworkInstallError(ex, version);
            return InstallationResult.CreateFailure(localizer[L.Error.OperationFailed]);
        }
    }

    public async Task<InstallationResult> UninstallFrameworkAsync(string version, CancellationToken cancellationToken = default)
    {
        if (String.IsNullOrEmpty(version))
        {
            logger.UninstallFailedVersionRequired();
            return InstallationResult.CreateFailure(localizer[L.Validation.VersionRequired]);
        }

        if (!dotnetVersionService.IsValidVersionFormat(version))
        {
            return InstallationResult.CreateFailure(localizer[L.Validation.InvalidVersionFormat]);
        }

        using var timer = new OperationTimer(elapsed => logger.UninstallDuration(elapsed, version));

        logger.UninstallStarting(version);

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
            await dotnetVersionService.RefreshCacheAsync();

            logger.FrameworkUninstalled(version);
            return InstallationResult.CreateSuccess(localizer[L.Success.UninstallationCompleted]);
        }
        catch (LastReleaseUninstallException ex)
        {
            logger.UninstallFailed(ex.Message);
            return InstallationResult.CreateFailure(localizer[L.Error.OperationFailed]);
        }
        catch (MissingChannelConfigurationException ex)
        {
            logger.UninstallFailed(ex.Message);
            return InstallationResult.CreateFailure(localizer[L.Error.OperationFailed]);
        }
        catch (Exception ex)
        {
            logger.FrameworkUninstallError(ex, version);
            return InstallationResult.CreateFailure(localizer[L.Error.OperationFailed]);
        }
    }

    private async Task EnsureUninstallAllowedForChannelAsync(string version, CancellationToken cancellationToken)
    {
        var configuredChannel = platformInfo.ChannelVersion;

        if (String.IsNullOrWhiteSpace(configuredChannel))
        {
            throw new MissingChannelConfigurationException();
        }

        var installedResult = await dotnetVersionService.GetInstalledVersionsAsync(cancellationToken);
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
