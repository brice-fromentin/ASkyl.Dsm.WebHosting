namespace Askyl.Dsm.WebHosting.Constants.API;

/// <summary>
/// Defines Framework Management API specific parameters and constants.
/// </summary>
public static class FrameworkManagementDefaults
{
    /// <summary>
    /// Base route prefix for the Framework Management controller (versioned).
    /// </summary>
    public const string ControllerBaseRoute = "api/v1/frameworks";

    /// <summary>
    /// Route to install a framework version.
    /// </summary>
    public const string InstallRoute = "install";

    /// <summary>
    /// Route to uninstall a framework version.
    /// </summary>
    public const string UninstallRoute = "uninstall";

    /// <summary>
    /// Full route for the install endpoint.
    /// </summary>
    public static String InstallFullRoute => String.Join("/", ControllerBaseRoute, InstallRoute);

    /// <summary>
    /// Full route for the uninstall endpoint.
    /// </summary>
    public static String UninstallFullRoute => String.Join("/", ControllerBaseRoute, UninstallRoute);

    /// <summary>
    /// Full route for the uninstall endpoint with version parameter.
    /// </summary>
    public static String UninstallWithVersionFullRoute(string version) => String.Join("/", ControllerBaseRoute, UninstallRoute, version);
}
