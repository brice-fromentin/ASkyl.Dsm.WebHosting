namespace Askyl.Dsm.WebHosting.Constants.WebApi;

/// <summary>
/// Defines framework management API route constants for the application's REST endpoints.
/// </summary>
public static class FrameworkManagementRoutes
{
    #region Route Configuration

    /// <summary>
    /// Base route prefix for the Framework Management controller (versioned).
    /// </summary>
    public const string ControllerBaseRoute = "api/v1/frameworks";

    #endregion

    #region Route Segments

    /// <summary>
    /// Route segment to install a framework version.
    /// </summary>
    public const string InstallRoute = "install";

    /// <summary>
    /// Route segment to uninstall a framework version.
    /// </summary>
    public const string UninstallRoute = "uninstall";

    #endregion

    #region Computed Routes

    /// <summary>
    /// Full route for the install endpoint.
    /// </summary>
    public static readonly string InstallFullRoute = String.Join("/", ControllerBaseRoute, InstallRoute);

    /// <summary>
    /// Full route for the uninstall endpoint.
    /// </summary>
    public static readonly string UninstallFullRoute = String.Join("/", ControllerBaseRoute, UninstallRoute);

    /// <summary>
    /// Full route for the uninstall endpoint with version parameter.
    /// </summary>
    public static string UninstallWithVersionFullRoute(string version) => String.Join("/", ControllerBaseRoute, UninstallRoute, version);

    #endregion
}
