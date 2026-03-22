namespace Askyl.Dsm.WebHosting.Constants.API;

/// <summary>
/// Defines Website Hosting API specific parameters and constants.
/// </summary>
public static class WebsiteHostingDefaults
{
    /// <summary>
    /// Base route prefix for the Website Hosting controller (versioned).
    /// </summary>
    public const string ControllerBaseRoute = "api/v1/websites";

    /// <summary>
    /// Route to get all websites.
    /// </summary>
    public const string AllRoute = "all";

    /// <summary>
    /// Route to add a new website.
    /// </summary>
    public const string AddRoute = "add";

    /// <summary>
    /// Route to update an existing website.
    /// </summary>
    public const string UpdateRoute = "update";

    /// <summary>
    /// Route to remove a website.
    /// </summary>
    public const string RemoveRoute = "remove";

    /// <summary>
    /// Route to start a website.
    /// </summary>
    public const string StartRoute = "start";

    /// <summary>
    /// Route to stop a website.
    /// </summary>
    public const string StopRoute = "stop";

    /// <summary>
    /// Full route for the all endpoint.
    /// </summary>
    public static String AllFullRoute => String.Join("/", ControllerBaseRoute, AllRoute);

    /// <summary>
    /// Full route for the add endpoint.
    /// </summary>
    public static String AddFullRoute => String.Join("/", ControllerBaseRoute, AddRoute);

    /// <summary>
    /// Full route for the update endpoint.
    /// </summary>
    public static String UpdateFullRoute => String.Join("/", ControllerBaseRoute, UpdateRoute);

    /// <summary>
    /// Full route for the remove endpoint.
    /// </summary>
    public static String RemoveFullRoute => String.Join("/", ControllerBaseRoute, RemoveRoute);

    /// <summary>
    /// Full route for the start endpoint.
    /// </summary>
    public static String StartFullRoute => String.Join("/", ControllerBaseRoute, StartRoute);

    /// <summary>
    /// Full route for the stop endpoint.
    /// </summary>
    public static String StopFullRoute => String.Join("/", ControllerBaseRoute, StopRoute);
}
