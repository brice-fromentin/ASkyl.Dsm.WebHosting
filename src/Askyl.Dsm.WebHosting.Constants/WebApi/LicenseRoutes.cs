namespace Askyl.Dsm.WebHosting.Constants.WebApi;

/// <summary>
/// Defines license API route constants for the application's REST endpoints.
/// </summary>
public static class LicenseRoutes
{
    /// <summary>
    /// Base route prefix for the License controller (versioned).
    /// </summary>
    public const string ControllerBaseRoute = "api/v1/licenses";

    /// <summary>
    /// Route segment to get all licenses.
    /// </summary>
    public const string AllRoute = "all";

    /// <summary>
    /// Full route for the licenses endpoint.
    /// </summary>
    public static string AllFullRoute => String.Join("/", ControllerBaseRoute, AllRoute);
}
