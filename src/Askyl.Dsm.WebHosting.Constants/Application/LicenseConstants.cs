namespace Askyl.Dsm.WebHosting.Constants.Application;

/// <summary>
/// Defines license-related constants for the application.
/// </summary>
public static class LicenseConstants
{
    #region File Size Limits

    /// <summary>
    /// Maximum allowed license file size in bytes (100KB).
    /// </summary>
    public const int MaxLicenseSizeBytes = 100 * 1024;

    #endregion

    #region License Files

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

    #endregion
}
