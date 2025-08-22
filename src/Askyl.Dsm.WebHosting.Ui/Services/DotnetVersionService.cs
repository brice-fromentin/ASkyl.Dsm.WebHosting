using System.Diagnostics;
using Askyl.Dsm.WebHosting.Tools.Runtime;
using Askyl.Dsm.WebHosting.Data.Runtime;

namespace Askyl.Dsm.WebHosting.Ui.Services;

public interface IDotnetVersionService
{
    Task<List<FrameworkInfo>> GetInstalledVersionsAsync();
    Task<bool> IsVersionInstalledAsync(string version, string frameworkType = "ASP.NET Core");
}

public class DotnetVersionService : IDotnetVersionService
{
    public async Task<List<FrameworkInfo>> GetInstalledVersionsAsync()
        => await VersionsDetector.GetInstalledVersionsAsync();

    public async Task<bool> IsVersionInstalledAsync(string version, string frameworkType = "ASP.NET Core")
    {
        // Ensure cache is populated
        await GetInstalledVersionsAsync();
        return VersionsDetector.IsVersionInstalled(version, frameworkType);
    }
}
