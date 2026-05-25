namespace Askyl.Dsm.WebHosting.Constants.DSM.API;

/// <summary>
/// Common DSM API error codes shared across all API endpoints.
/// </summary>
public static class DsmConstants
{
    /// <summary>
    /// DSM API error code indicating authentication failure (invalid or expired SID).
    /// Returned by any API when the session is not authenticated.
    /// </summary>
    public const int ErrorCodeAuthenticationFailed = -4;
}
