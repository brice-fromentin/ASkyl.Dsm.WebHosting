namespace Askyl.Dsm.WebHosting.Constants.API;

/// <summary>
/// Defines Runtime Management API specific parameters and constants.
/// </summary>
public static class RuntimeManagementDefaults
{
    /// <summary>
    /// Base route prefix for the Runtime Management controller (versioned).
    /// </summary>
    public const string ControllerBaseRoute = "api/v1/runtime";

    /// <summary>
    /// Route to get installed .NET versions.
    /// </summary>
    public const string VersionsRoute = "versions";

    /// <summary>
    /// Route to check if a channel is installed.
    /// </summary>
    public const string ChannelInstalledRoute = "channels/installed";

    /// <summary>
    /// Route to check if a version is installed.
    /// </summary>
    public const string VersionInstalledRoute = "versions/installed";

    /// <summary>
    /// Route to get available channels.
    /// </summary>
    public const string ChannelsRoute = "channels";

    /// <summary>
    /// Route to get releases with installation status.
    /// </summary>
    public const string ReleasesWithStatusRoute = "releases/status";

    /// <summary>
    /// Full route for the versions endpoint.
    /// </summary>
    public static String VersionsFullRoute => String.Join("/", ControllerBaseRoute, VersionsRoute);

    /// <summary>
    /// Full route for the channel installed endpoint.
    /// </summary>
    public static String ChannelInstalledFullRoute(string productVersion) => String.Join("/", ControllerBaseRoute, ChannelInstalledRoute, productVersion);

    /// <summary>
    /// Full route for the version installed endpoint.
    /// </summary>
    public static String VersionInstalledFullRoute(string version) => String.Join("/", ControllerBaseRoute, VersionInstalledRoute, version);

    /// <summary>
    /// Full route for the channels endpoint.
    /// </summary>
    public static String ChannelsFullRoute => String.Join("/", ControllerBaseRoute, ChannelsRoute);

    /// <summary>
    /// Full route for the releases with status endpoint.
    /// </summary>
    public static String ReleasesWithStatusFullRoute(string productVersion) => String.Join("/", ControllerBaseRoute, ReleasesWithStatusRoute, productVersion);
}
