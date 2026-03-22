namespace Askyl.Dsm.WebHosting.Constants.WebApi;

/// <summary>
/// Defines website hosting API route constants for the application's REST endpoints.
/// </summary>
public static class WebsiteHostingRoutes
{
    /// <summary>
    /// Base route prefix for the Website Hosting controller (versioned).
    /// </summary>
    public const string ControllerBaseRoute = "api/v1/websites";

    /// <summary>
    /// Route segment to get all websites.
    /// </summary>
    public const string AllRoute = "all";

    /// <summary>
    /// Route segment to add a new website.
    /// </summary>
    public const string AddRoute = "add";

    /// <summary>
    /// Route segment to update an existing website.
    /// </summary>
    public const string UpdateRoute = "update";

    /// <summary>
    /// Route segment to remove a website.
    /// </summary>
    public const string RemoveRoute = "remove";

    /// <summary>
    /// Route segment to start a website.
    /// </summary>
    public const string StartRoute = "start";

    /// <summary>
    /// Route segment to stop a website.
    /// </summary>
    public const string StopRoute = "stop";

    /// <summary>
    /// Full route for the all endpoint.
    /// </summary>
    public static string AllFullRoute => String.Join("/", ControllerBaseRoute, AllRoute);

    /// <summary>
    /// Full route for the add endpoint.
    /// </summary>
    public static string AddFullRoute => String.Join("/", ControllerBaseRoute, AddRoute);

    /// <summary>
    /// Full route for the update endpoint.
    /// </summary>
    public static string UpdateFullRoute => String.Join("/", ControllerBaseRoute, UpdateRoute);

    /// <summary>
    /// Full route for the remove endpoint.
    /// </summary>
    public static string RemoveFullRoute => String.Join("/", ControllerBaseRoute, RemoveRoute);

    /// <summary>
    /// Full route for the start endpoint.
    /// </summary>
    public static string StartFullRoute => String.Join("/", ControllerBaseRoute, StartRoute);

    /// <summary>
    /// Full route for the stop endpoint.
    /// </summary>
    public static string StopFullRoute => String.Join("/", ControllerBaseRoute, StopRoute);
}
