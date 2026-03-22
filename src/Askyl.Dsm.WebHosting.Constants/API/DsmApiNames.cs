namespace Askyl.Dsm.WebHosting.Constants.API;

/// <summary>
/// Contains all Synology DSM API names used throughout the application.
/// </summary>
public static class DsmApiNames
{
    public const string Handshake = "query.cgi";

    public const string Info = "SYNO.API.Info";
    public const string Auth = "SYNO.API.Auth";

    #region FileStation APIs

    public const string FileStationInfo = "SYNO.FileStation.Info";
    public const string FileStationList = "SYNO.FileStation.List";
    public const string FileStationSearch = "SYNO.FileStation.Search";
    public const string FileStationVirtualFolder = "SYNO.FileStation.VirtualFolder";
    public const string FileStationFavorite = "SYNO.FileStation.Favorite";
    public const string FileStationThumb = "SYNO.FileStation.Thumb";
    public const string FileStationDirSize = "SYNO.FileStation.DirSize";
    public const string FileStationMd5 = "SYNO.FileStation.MD5";
    public const string FileStationCheckPermission = "SYNO.FileStation.CheckPermission";
    public const string FileStationUpload = "SYNO.FileStation.Upload";
    public const string FileStationDownload = "SYNO.FileStation.Download";
    public const string FileStationSharing = "SYNO.FileStation.Sharing";
    public const string FileStationCreateFolder = "SYNO.FileStation.CreateFolder";
    public const string FileStationRename = "SYNO.FileStation.Rename";
    public const string FileStationCopyMove = "SYNO.FileStation.CopyMove";
    public const string FileStationDelete = "SYNO.FileStation.Delete";
    public const string FileStationExtract = "SYNO.FileStation.Extract";
    public const string FileStationCompress = "SYNO.FileStation.Compress";
    public const string FileStationBackgroundTask = "SYNO.FileStation.BackgroundTask";

    #endregion

    #region Core APIs

    public const string CoreAcl = "SYNO.Core.ACL";

    #endregion

    #region AppPortal

    public const string AppPortalReverseProxy = "SYNO.Core.AppPortal.ReverseProxy";

    #endregion

    #region Required APIs Collection

    public static readonly string[] RequiredApis = [Auth, FileStationInfo, FileStationList, CoreAcl, AppPortalReverseProxy];

    public static readonly string RequiredApisJoined = String.Join(",", RequiredApis);

    #endregion
}
