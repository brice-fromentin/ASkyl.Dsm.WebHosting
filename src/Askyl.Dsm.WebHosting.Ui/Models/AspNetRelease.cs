using Askyl.Dsm.WebHosting.Data.Runtime;

namespace Askyl.Dsm.WebHosting.Ui.Models;

public sealed class AspNetRelease
{
    public static AspNetRelease Create(AspNetCoreReleaseInfo releaseInfo, bool isInstalled = false)
    {
        return new AspNetRelease
        {
            Version = releaseInfo.Version,
            IsSecurity = releaseInfo.IsSecurity,
            ReleaseDate = releaseInfo.ReleaseDate,
            IsInstalled = isInstalled
        };
    }

    public required string Version { get; init; }
    public required bool IsSecurity { get; init; }
    public required DateTimeOffset? ReleaseDate { get; init; }
    public required bool IsInstalled { get; init; }
}
