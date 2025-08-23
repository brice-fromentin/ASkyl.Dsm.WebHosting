using Askyl.Dsm.WebHosting.Tools.Runtime;
using Askyl.Dsm.WebHosting.Data.Runtime;

namespace Askyl.Dsm.WebHosting.Ui.Services;

public interface IDotnetVersionService
{
    Task<List<FrameworkInfo>> GetInstalledVersionsAsync();
    bool IsVersionInstalled(string version, string frameworkType = "ASP.NET Core");
}

public class DotnetVersionService : IDotnetVersionService
{
    public Task<List<FrameworkInfo>> GetInstalledVersionsAsync()
        => VersionsDetector.GetInstalledVersionsAsync();

    public bool IsVersionInstalled(string version, string frameworkType = "ASP.NET Core")
        => VersionsDetector.IsVersionInstalled(version, frameworkType);
}
