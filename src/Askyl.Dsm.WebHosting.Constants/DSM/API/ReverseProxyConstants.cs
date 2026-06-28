namespace Askyl.Dsm.WebHosting.Constants.DSM.API;

/// <summary>
/// Defines constants for DSM reverse proxy API operations.
/// </summary>
public static class ReverseProxyConstants
{
    /// <summary>
    /// Prefix added to reverse proxy descriptions to identify ADWH-managed proxies.
    /// </summary>
    public const string DescriptionPrefix = "ADWH - ";

    /// <summary>
    /// Default proxy timeout value in seconds for connect, read, and send operations.
    /// </summary>
    public const int DefaultProxyTimeoutSeconds = 60;

    /// <summary>
    /// Default proxy HTTP version for reverse proxy connections (HTTP/1.1).
    /// </summary>
    public const int DefaultProxyHttpVersion = 1;

    #region ACL

    /// <summary>
    /// ACL permission type for allowing access.
    /// </summary>
    public const string AclPermissionTypeAllow = "allow";

    /// <summary>
    /// ACL owner type value for group-based access control entries.
    /// </summary>
    public const string AclOwnerTypeGroup = "group";

    /// <summary>
    /// ACL owner name for the HTTP web server group.
    /// </summary>
    public const string AclOwnerNameHttp = "http";

    #endregion

    #region Error Codes

    /// <summary>
    /// DSM API error code indicating resource not found (HTTP 404).
    /// </summary>
    public const int ErrorCodeNotFound = 404;

    /// <summary>
    /// DSM API generic error code for not found errors.
    /// </summary>
    public const int ErrorCodeGenericNotFound = -4;

    /// <summary>
    /// DSM API specific error code for resource not found.
    /// </summary>
    public const int ErrorCodeResourceNotFound = 4004;

    #endregion
}
