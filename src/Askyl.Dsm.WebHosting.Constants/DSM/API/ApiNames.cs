namespace Askyl.Dsm.WebHosting.Constants.DSM.API;

/// <summary>
/// Contains all Synology DSM API names used throughout the application.
/// </summary>
public static class ApiNames
{
    #region Core APIs

    /// <summary>
    /// Handshake API for initial connection and capability discovery.
    /// </summary>
    public const string Handshake = "query.cgi";

    /// <summary>
    /// API information endpoint for discovering available APIs.
    /// </summary>
    public const string Info = "SYNO.API.Info";

    /// <summary>
    /// Authentication API for login and session management.
    /// </summary>
    public const string Auth = "SYNO.API.Auth";

    #endregion

    #region FileStation APIs

    /// <summary>
    /// FileStation information API.
    /// </summary>
    public const string FileStationInfo = "SYNO.FileStation.Info";

    /// <summary>
    /// FileStation list API for directory listings.
    /// </summary>
    public const string FileStationList = "SYNO.FileStation.List";

    #endregion

    #region Core System APIs

    /// <summary>
    /// Core ACL (Access Control List) API.
    /// </summary>
    public const string CoreAcl = "SYNO.Core.ACL";

    /// <summary>
    /// Core user API for user management.
    /// </summary>
    public const string CoreUser = "SYNO.Core.User";

    #endregion

    #region AppPortal APIs

    /// <summary>
    /// AppPortal reverse proxy API for web application routing.
    /// </summary>
    public const string AppPortalReverseProxy = "SYNO.Core.AppPortal.ReverseProxy";

    #endregion

    #region Required APIs Collection

    /// <summary>
    /// Comma-separated list of required API names for API handshake requests.
    /// </summary>
    public static readonly string RequiredApisJoined = $"{Auth},{FileStationInfo},{FileStationList},{CoreAcl},{CoreUser},{AppPortalReverseProxy}";

    #endregion
}
