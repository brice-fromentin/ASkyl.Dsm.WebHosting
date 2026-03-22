using System.Text;

namespace Askyl.Dsm.WebHosting.Constants.API;

/// <summary>
/// Defines Authentication API specific parameters and constants.
/// </summary>
public static class AuthenticationDefaults
{
    /// <summary>
    /// Cookie format for authentication response.
    /// </summary>
    public const string FormatCookie = "cookie";

    /// <summary>
    /// SID format for authentication response.
    /// </summary>
    public const string FormatSid = "sid";

    /// <summary>
    /// Base route prefix for the Authentication controller (versioned).
    /// </summary>
    public const string ControllerBaseRoute = "api/v1/authentication";

    /// <summary>
    /// Authentication route - login endpoint.
    /// </summary>
    public const string LoginRoute = "login";

    /// <summary>
    /// Authentication route - logout endpoint.
    /// </summary>
    public const string LogoutRoute = "logout";

    /// <summary>
    /// Authentication route - status endpoint.
    /// </summary>
    public const string StatusRoute = "status";

    /// <summary>
    /// Full route for the login endpoint.
    /// </summary>
    public static String LoginFullRoute => String.Join("/", ControllerBaseRoute, LoginRoute);

    /// <summary>
    /// Full route for the logout endpoint.
    /// </summary>
    public static String LogoutFullRoute => String.Join("/", ControllerBaseRoute, LogoutRoute);

    /// <summary>
    /// Full route for the status endpoint.
    /// </summary>
    public static String StatusFullRoute => String.Join("/", ControllerBaseRoute, StatusRoute);
}
