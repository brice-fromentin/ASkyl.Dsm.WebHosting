namespace Askyl.Dsm.WebHosting.Constants.WebApi;

/// <summary>
/// Defines authentication API route constants for the application's REST endpoints.
/// </summary>
public static class AuthenticationRoutes
{
    /// <summary>
    /// Base route prefix for the Authentication controller (versioned).
    /// </summary>
    public const string ControllerBaseRoute = "api/v1/authentication";

    /// <summary>
    /// Login endpoint route segment.
    /// </summary>
    public const string LoginRoute = "login";

    /// <summary>
    /// Logout endpoint route segment.
    /// </summary>
    public const string LogoutRoute = "logout";

    /// <summary>
    /// Status endpoint route segment.
    /// </summary>
    public const string StatusRoute = "status";

    /// <summary>
    /// Full route for the login endpoint.
    /// </summary>
    public static string LoginFullRoute => String.Join("/", ControllerBaseRoute, LoginRoute);

    /// <summary>
    /// Full route for the logout endpoint.
    /// </summary>
    public static string LogoutFullRoute => String.Join("/", ControllerBaseRoute, LogoutRoute);

    /// <summary>
    /// Full route for the status endpoint.
    /// </summary>
    public static string StatusFullRoute => String.Join("/", ControllerBaseRoute, StatusRoute);
}
