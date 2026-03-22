namespace Askyl.Dsm.WebHosting.Constants.WebApi;

/// <summary>
/// Defines log download API route constants for the application's REST endpoints.
/// </summary>
public static class LogDownloadRoutes
{
    #region Route Configuration

    /// <summary>
    /// Base route prefix for the Log Download controller (versioned).
    /// </summary>
    public const string ControllerBaseRoute = "api/v1/logdownload";

    #endregion

    #region Route Segments

    /// <summary>
    /// Route segment for log download endpoint.
    /// </summary>
    public const string LogsRoute = "logs";

    #endregion

    #region Computed Routes

    /// <summary>
    /// Full route for the log download endpoint.
    /// </summary>
    public static string LogsFullRoute => String.Join("/", ControllerBaseRoute, LogsRoute);

    #endregion
}
