namespace Askyl.Dsm.WebHosting.Constants.WebApi;

/// <summary>
/// Defines the DSM connection diagnostic API route constants.
/// </summary>
public static class DsmDiagnosticRoutes
{
    /// <summary>
    /// Base route of the DsmDiagnostic controller (versioned); a GET on it returns the report.
    /// </summary>
    public const string ControllerBaseRoute = "api/v1/dsm-diagnostic";

    /// <summary>
    /// Re-check endpoint route segment.
    /// </summary>
    public const string RecheckRoute = "recheck";

    /// <summary>
    /// Full route for the report endpoint.
    /// </summary>
    public const string ReportFullRoute = ControllerBaseRoute;

    /// <summary>
    /// Full route for the re-check endpoint.
    /// </summary>
    public static readonly string RecheckFullRoute = String.Join("/", ControllerBaseRoute, RecheckRoute);
}
