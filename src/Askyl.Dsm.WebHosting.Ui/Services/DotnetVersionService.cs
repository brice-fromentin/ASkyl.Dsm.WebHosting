using Askyl.Dsm.WebHosting.Constants.Runtime;
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

    public bool IsChannelInstalled(string channel, string frameworkType = DotNetFrameworkTypes.AspNetCore)
        => VersionsDetector.IsChannelInstalled(channel, frameworkType);

    public bool IsVersionInstalled(string version, string frameworkType = DotNetFrameworkTypes.AspNetCore)
        => VersionsDetector.IsVersionInstalled(version, frameworkType);
}
