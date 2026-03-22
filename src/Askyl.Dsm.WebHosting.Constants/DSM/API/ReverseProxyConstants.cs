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
}
