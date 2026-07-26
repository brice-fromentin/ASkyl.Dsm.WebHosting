namespace Askyl.Dsm.WebHosting.Constants.DSM.API;

/// <summary>
/// Common DSM API error codes shared across all API endpoints.
/// </summary>
public static class DsmConstants
{
    /// <summary>
    /// Authentication format value for cookie-based sessions.
    /// </summary>
    public const string AuthFormatCookie = "cookie";

    /// <summary>
    /// DSM API error code indicating authentication failure (invalid or expired SID).
    /// Returned by any API when the session is not authenticated.
    /// </summary>
    public const int ErrorCodeAuthenticationFailed = -4;

    /// <summary>
    /// DSM API error code indicating insufficient user privilege.
    /// Returned by administrator-only APIs such as SYNO.Core.User when the caller is not an administrator.
    /// </summary>
    public const int ErrorCodePermissionDenied = 105;
}
