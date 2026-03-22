namespace Askyl.Dsm.WebHosting.Constants.API;

/// <summary>
/// Defines License API specific parameters and constants.
/// </summary>
public static class LicenseDefaults
{
    /// <summary>
    /// Base route prefix for the License controller (versioned).
    /// </summary>
    public const string ControllerBaseRoute = "api/v1/licenses";

    /// <summary>
    /// Route to get all licenses.
    /// </summary>
    public const string AllRoute = "all";

    /// <summary>
    /// Full route for the licenses endpoint.
    /// </summary>
    public static String AllFullRoute => String.Join("/", ControllerBaseRoute, AllRoute);

    /// <summary>
    /// List of license file names to fetch from wwwroot/licenses/.
    /// </summary>
    public static readonly string[] LicenseFileNames =
    [
        "Application.txt",
        "FluentUI Blazor.txt",
        "NET.txt",
        "Serilog.txt"
    ];
}
