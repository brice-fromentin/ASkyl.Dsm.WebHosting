namespace Askyl.Dsm.WebHosting.Constants.UI;

/// <summary>
/// Constants for dialog sizing and layout.
/// </summary>
public static class DialogConstants
{
    /// <summary>
    /// Width for medium dialogs (75% of container).
    /// </summary>
    public const string WidthMedium = "75%";

    /// <summary>
    /// Width for large dialogs (80% of container).
    /// </summary>
    public const string WidthLarge = "80%";

    /// <summary>
    /// Width of the ASP.NET releases dialog, wide enough for full version numbers beside its fixed columns.
    /// FluentUI 4 sized this dialog to its content; at FluentUI 5's default 600px the versions were truncated.
    /// </summary>
    public const string WidthReleases = "680px";

    /// <summary>
    /// Width of the licenses dialog, fitted to its preformatted texts (82 columns at most) beside the tab list.
    /// At a share of the screen the text filled half of the dialog on a wide screen; measured with no horizontal scroll.
    /// </summary>
    public const string WidthLicenses = "820px";
}
