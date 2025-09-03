using Askyl.Dsm.WebHosting.Constants.Runtime;
using Askyl.Dsm.WebHosting.Data.Runtime;
using Askyl.Dsm.WebHosting.Ui.Services;

namespace Askyl.Dsm.WebHosting.Ui.Models;

/// <summary>
/// Wraps AspNetCoreReleaseInfo with custom ToString() formatting for UI display in FluentSelect.
/// </summary>
public sealed class AspNetChannel(AspNetCoreReleaseInfo releaseInfo, IDotnetVersionService? dotnetVersionService = null)
{
    private readonly IDotnetVersionService? _dotnetVersionService = dotnetVersionService;

    /// <summary>
    /// The underlying release information.
    /// </summary>
    public AspNetCoreReleaseInfo ReleaseInfo { get; } = releaseInfo ?? throw new ArgumentNullException(nameof(releaseInfo));

    /// <summary>
    /// Creates a channel info instance from an existing AspNetCoreReleaseInfo.
    /// </summary>
    public static AspNetChannel FromReleaseInfo(AspNetCoreReleaseInfo releaseInfo, IDotnetVersionService? dotnetVersionService = null) 
        => new(releaseInfo, dotnetVersionService);

    /// <summary>
    /// Custom string representation for FluentSelect display.
    /// Format: "8.0 (LTS) ✓" or "9.0"
    /// </summary>
    public override string ToString()
    {
        var displayText = ReleaseInfo.ProductVersion;
        
        if (ReleaseInfo.IsLts)
        {
            displayText += " (LTS)";
        }
        
        if (_dotnetVersionService?.IsChannelInstalled(ReleaseInfo.ProductVersion, DotNetFrameworkTypes.AspNetCore) == true)
        {
            displayText += " ✓";
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
