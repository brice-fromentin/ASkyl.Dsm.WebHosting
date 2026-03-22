namespace Askyl.Dsm.WebHosting.Constants.API;

/// <summary>
/// Defines Log Download API specific parameters and constants.
/// </summary>
public static class LogDownloadDefaults
{
    /// <summary>
    /// Base route prefix for the Log Download controller (versioned).
    /// </summary>
    public const string ControllerBaseRoute = "api/v1/logdownload";

    /// <summary>
    /// Log download endpoint route.
    /// </summary>
    public const string LogsRoute = "logs";

    /// <summary>
    /// Full route for the log download endpoint.
    /// </summary>
    public static String LogsFullRoute => String.Join("/", ControllerBaseRoute, LogsRoute);
}
