namespace Askyl.Dsm.WebHosting.Constants.WebApi;

/// <summary>
/// Defines license API route constants for the application's REST endpoints.
/// </summary>
public static class LicenseRoutes
{
    #region Route Configuration

    /// <summary>
    /// Base route prefix for the License controller (versioned).
    /// </summary>
    public const string ControllerBaseRoute = "api/v1/licenses";

    #endregion

    #region Route Segments

    /// <summary>
    /// Route segment to get all licenses.
    /// </summary>
    public const string AllRoute = "all";

    #endregion

    #region Computed Routes

    /// <summary>
    /// Full route for the licenses endpoint.
    /// </summary>
    public static readonly string AllFullRoute = String.Join("/", ControllerBaseRoute, AllRoute);

    #endregion
}
