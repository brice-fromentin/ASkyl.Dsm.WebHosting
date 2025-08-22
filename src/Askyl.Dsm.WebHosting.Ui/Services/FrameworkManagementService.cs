using System.Diagnostics;
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
            // Delete the directories related to the specified version
            FileSystem.Initialize("../runtimes");
            FileSystem.DeleteDirectory($"host/fxr/{version}");
            FileSystem.DeleteDirectory($"shared/Microsoft.AspNetCore.App/{version}");
            FileSystem.DeleteDirectory($"shared/Microsoft.NETCore.App/{version}");

            // Refresh the cache to detect the removal
            await _dotnetVersionService.GetInstalledVersionsAsync();
            
            return InstallationResult.CreateSuccess($"ASP.NET Core {version} has been uninstalled successfully.");
        }
        catch (Exception ex)
        {
            return InstallationResult.CreateFailure($"Uninstallation failed: {ex.Message}");
        }
    }
}
