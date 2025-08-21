using Askyl.Dsm.WebHosting.Tools.Runtime;

namespace Askyl.Dsm.WebHosting.Ui.Models;

public sealed class ReleaseGridRow
{
    public static ReleaseGridRow Create(Downloader.AspNetCoreReleaseInfo releaseInfo, bool isInstalled = false)
    {
        return new ReleaseGridRow
        {
            Version = releaseInfo.Version,
            Latest = releaseInfo.IsLatest ? "✓" : String.Empty,
            Security = releaseInfo.IsSecurity ? "⚠" : String.Empty,
            ReleaseDate = releaseInfo.ReleaseDate?.ToString("yyyy-MM-dd") ?? String.Empty,
            Installed = isInstalled ? "✓" : String.Empty
        };
    }

    public required string Version { get; init; }
    public required string Latest { get; init; }
    public required string Security { get; init; }
    public required string ReleaseDate { get; init; }
    public required string Installed { get; init; }
}
