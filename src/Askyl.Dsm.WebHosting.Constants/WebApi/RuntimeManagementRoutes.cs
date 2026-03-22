namespace Askyl.Dsm.WebHosting.Constants.WebApi;

/// <summary>
/// Defines runtime management API route constants for the application's REST endpoints.
/// </summary>
public static class RuntimeManagementRoutes
{
    #region Route Configuration

    /// <summary>
    /// Base route prefix for the Runtime Management controller (versioned).
    /// </summary>
    public const string ControllerBaseRoute = "api/v1/runtime";

    #endregion

    #region Route Segments

    /// <summary>
    /// Route segment to get installed .NET versions.
    /// </summary>
    public const string VersionsRoute = "versions";

    /// <summary>
    /// Route segment to check if a channel is installed.
    /// </summary>
    public const string ChannelInstalledRoute = "channels/installed";

    /// <summary>
    /// Route segment to check if a version is installed.
    /// </summary>
    public const string VersionInstalledRoute = "versions/installed";

    /// <summary>
    /// Route segment to get available channels.
    /// </summary>
    public const string ChannelsRoute = "channels";

    /// <summary>
    /// Route segment to get releases with installation status.
    /// </summary>
    public const string ReleasesWithStatusRoute = "releases/status";

    #endregion

    #region Computed Routes

    /// <summary>
    /// Full route for the versions endpoint.
    /// </summary>
    public static string VersionsFullRoute => String.Join("/", ControllerBaseRoute, VersionsRoute);

    /// <summary>
    /// Full route for the channel installed endpoint.
    /// </summary>
    public static string ChannelInstalledFullRoute(string productVersion) => String.Join("/", ControllerBaseRoute, ChannelInstalledRoute, productVersion);

    /// <summary>
    /// Full route for the version installed endpoint.
    /// </summary>
    public static string VersionInstalledFullRoute(string version) => String.Join("/", ControllerBaseRoute, VersionInstalledRoute, version);

    /// <summary>
    /// Full route for the channels endpoint.
    /// </summary>
    public static string ChannelsFullRoute => String.Join("/", ControllerBaseRoute, ChannelsRoute);

    /// <summary>
    /// Full route for the releases with status endpoint.
    /// </summary>
    public static string ReleasesWithStatusFullRoute(string productVersion) => String.Join("/", ControllerBaseRoute, ReleasesWithStatusRoute, productVersion);

    #endregion
}
