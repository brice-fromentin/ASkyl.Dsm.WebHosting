
namespace Askyl.Dsm.WebHosting.Constants.Network;

/// <summary>
/// Network and HTTP-related constants used throughout the application.
/// </summary>
public static class NetworkConstants
{
    #region Addresses

    /// <summary>
    /// Localhost address for internal connections.
    /// </summary>
    public const string Localhost = "localhost";

    #endregion

    #region HTTP Headers

    /// <summary>
    /// Cookie header name for HTTP requests.
    /// </summary>
    public const string CookieHeader = "Cookie";

    #endregion

    #region Session Management

    /// <summary>
    /// Session ID cookie prefix used by DSM.
    /// </summary>
    public const string SsidCookiePrefix = "_SSID=";

    #endregion

    #region MIME Types

    /// <summary>
    /// The MIME type for JSON content.
    /// </summary>
    public const string ApplicationJson = "application/json";

    #endregion
}
