namespace Askyl.Dsm.WebHosting.Constants.UI;

/// <summary>
/// Constants for component spacing.
/// </summary>
public static class LayoutConstants
{
    /// <summary>
    /// Gap between the items of a FluentStack. FluentUI 4 applied it by default; FluentUI 5 applies none.
    /// </summary>
    public const string StackGap = "10px";

    /// <summary>
    /// The custom property a fluent-tab reserves its bold label width from. The web component fills it when it is
    /// connected, before Blazor inserts the label, so it stays empty and selecting the widest tab widens the tab list.
    /// </summary>
    public const string TabLabelProperty = "--textContent";
}
