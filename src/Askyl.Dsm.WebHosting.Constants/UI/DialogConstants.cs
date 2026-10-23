namespace Askyl.Dsm.WebHosting.Constants.UI;

/// <summary>
/// Constants for dialog sizing and layout.
/// </summary>
public static class DialogConstants
{
    /// <summary>
    /// Width for small dialogs (60% of container).
    /// </summary>
    public const string WidthSmall = "60%";

    /// <summary>
    /// Width for medium dialogs (75% of container).
    /// </summary>
    public const string WidthMedium = "75%";

    /// <summary>
    /// Width for large dialogs (80% of container).
    /// </summary>
    public const string WidthLarge = "80%";

    /// <summary>
    /// Width that holds the ASP.NET releases grid: its 580px of columns, the dialog's 48px of padding and a
    /// vertical scrollbar. FluentUI 4 sized this dialog to its content; FluentUI 5 would clip the last column.
    /// </summary>
    public const string WidthReleases = "680px";
}
