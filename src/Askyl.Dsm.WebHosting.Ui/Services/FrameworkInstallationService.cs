using System.Diagnostics;
using Askyl.Dsm.WebHosting.Tools.Runtime;

namespace Askyl.Dsm.WebHosting.Ui.Services;

public interface IFrameworkInstallationService
{
    Task<bool> InstallFrameworkAsync(string version, string channel);
}

public class FrameworkInstallationService : IFrameworkInstallationService
{
    public async Task<bool> InstallFrameworkAsync(string version, string channel)
    {
        try
        {
            FileSystem.Initialize("../runtimes");
            
            // Download the specific framework version
            var fileName = await Downloader.DownloadVersionToAsync(version, channel, true);

            // Extract and install
            GzUnTar.Decompress(fileName);

            // Refresh the cache to detect the new installation
            await VersionsDetector.GetInstalledVersionsAsync();
            
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
