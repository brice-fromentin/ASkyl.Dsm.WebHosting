using Askyl.Dsm.WebHosting.Constants.Runtime;

namespace Askyl.Dsm.WebHosting.Data.Domain.Runtime;

/// <summary>
/// Wraps AspNetCoreReleaseInfo with custom ToString() formatting for UI display in FluentSelect.
/// </summary>
public sealed class AspNetChannel(AspNetCoreReleaseInfo releaseInfo)
{
    /// <summary>
    /// The underlying release information.
    /// </summary>
    public AspNetCoreReleaseInfo ReleaseInfo { get; } = releaseInfo ?? throw new ArgumentNullException(nameof(releaseInfo));

    /// <summary>
    /// Creates a channel info instance from an existing AspNetCoreReleaseInfo.
    /// </summary>
    public static AspNetChannel FromReleaseInfo(AspNetCoreReleaseInfo releaseInfo)
        => new(releaseInfo);

    /// <summary>
    /// Custom string representation for FluentSelect display.
    /// Format: "8.0 (LTS)" or "9.0"
    /// </summary>
    public override string ToString()
    {
        var displayText = ReleaseInfo.ProductVersion;

        if (ReleaseInfo.IsLts)
        {
            displayText += " (LTS)";
        }

        return displayText;
    }

    // Convenience properties to access the underlying ReleaseInfo properties
    public string ProductVersion => ReleaseInfo.ProductVersion;

    public string Version => ReleaseInfo.Version;

    public DateTimeOffset? ReleaseDate => ReleaseInfo.ReleaseDate;

    public bool IsSecurity => ReleaseInfo.IsSecurity;

    public bool IsLts => ReleaseInfo.IsLts;

    public AspNetCoreReleaseType ReleaseType => ReleaseInfo.ReleaseType;
}
