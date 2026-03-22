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

    /// <summary>
    /// FileStation search API for file searches.
    /// </summary>
    public const string FileStationSearch = "SYNO.FileStation.Search";

    /// <summary>
    /// FileStation virtual folder API.
    /// </summary>
    public const string FileStationVirtualFolder = "SYNO.FileStation.VirtualFolder";

    /// <summary>
    /// FileStation favorite API for managing favorites.
    /// </summary>
    public const string FileStationFavorite = "SYNO.FileStation.Favorite";

    /// <summary>
    /// FileStation thumbnail API.
    /// </summary>
    public const string FileStationThumb = "SYNO.FileStation.Thumb";

    /// <summary>
    /// FileStation directory size API.
    /// </summary>
    public const string FileStationDirSize = "SYNO.FileStation.DirSize";

    /// <summary>
    /// FileStation MD5 checksum API.
    /// </summary>
    public const string FileStationMd5 = "SYNO.FileStation.MD5";

    /// <summary>
    /// FileStation permission check API.
    /// </summary>
    public const string FileStationCheckPermission = "SYNO.FileStation.CheckPermission";

    /// <summary>
    /// FileStation upload API.
    /// </summary>
    public const string FileStationUpload = "SYNO.FileStation.Upload";

    /// <summary>
    /// FileStation download API.
    /// </summary>
    public const string FileStationDownload = "SYNO.FileStation.Download";

    /// <summary>
    /// FileStation sharing API.
    /// </summary>
    public const string FileStationSharing = "SYNO.FileStation.Sharing";

    /// <summary>
    /// FileStation create folder API.
    /// </summary>
    public const string FileStationCreateFolder = "SYNO.FileStation.CreateFolder";

    /// <summary>
    /// FileStation rename API.
    /// </summary>
    public const string FileStationRename = "SYNO.FileStation.Rename";

    /// <summary>
    /// FileStation copy/move API.
    /// </summary>
    public const string FileStationCopyMove = "SYNO.FileStation.CopyMove";

    /// <summary>
    /// FileStation delete API.
    /// </summary>
    public const string FileStationDelete = "SYNO.FileStation.Delete";

    /// <summary>
    /// FileStation extract API for archive extraction.
    /// </summary>
    public const string FileStationExtract = "SYNO.FileStation.Extract";

    /// <summary>
    /// FileStation compress API for archive creation.
    /// </summary>
    public const string FileStationCompress = "SYNO.FileStation.Compress";

    /// <summary>
    /// FileStation background task API.
    /// </summary>
    public const string FileStationBackgroundTask = "SYNO.FileStation.BackgroundTask";

    #endregion

    #region Core System APIs

    /// <summary>
    /// Core ACL (Access Control List) API.
    /// </summary>
    public const string CoreAcl = "SYNO.Core.ACL";

    #endregion

    #region AppPortal APIs

    /// <summary>
    /// AppPortal reverse proxy API for web application routing.
    /// </summary>
    public const string AppPortalReverseProxy = "SYNO.Core.AppPortal.ReverseProxy";

    #endregion

    #region Required APIs Collection

    /// <summary>
    /// Collection of required APIs that must be available for the application to function.
    /// </summary>
    public static readonly string[] RequiredApis = [Auth, FileStationInfo, FileStationList, CoreAcl, AppPortalReverseProxy];

    /// <summary>
    /// Comma-separated list of required API names for API handshake requests.
    /// </summary>
    public static readonly string RequiredApisJoined = String.Join(",", RequiredApis);

    #endregion
}
