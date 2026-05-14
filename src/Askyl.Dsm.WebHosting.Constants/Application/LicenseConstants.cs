namespace Askyl.Dsm.WebHosting.Constants.Application;

/// <summary>
/// Defines license-related constants for the application.
/// </summary>
public static class LicenseConstants
{
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
