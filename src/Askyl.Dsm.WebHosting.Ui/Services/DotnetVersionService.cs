using Askyl.Dsm.WebHosting.Data.Runtime;
using Askyl.Dsm.WebHosting.Tools.Runtime;

namespace Askyl.Dsm.WebHosting.Ui.Services;

public interface IDotnetVersionService
{
    Task<List<FrameworkInfo>> GetInstalledVersionsAsync();
    bool IsChannelInstalled(string channel, string frameworkType);
    bool IsVersionInstalled(string version, string frameworkType);
}

public class DotnetVersionService : IDotnetVersionService
{
    public Task<List<FrameworkInfo>> GetInstalledVersionsAsync()
        => VersionsDetector.GetInstalledVersionsAsync();

    public bool IsChannelInstalled(string channel, string frameworkType = "ASP.NET Core")
        => VersionsDetector.IsChannelInstalled(channel, frameworkType);

    public bool IsVersionInstalled(string version, string frameworkType = "ASP.NET Core")
        => VersionsDetector.IsVersionInstalled(version, frameworkType);
}
