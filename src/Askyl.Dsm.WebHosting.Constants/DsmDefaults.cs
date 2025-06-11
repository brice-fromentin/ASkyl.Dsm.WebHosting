using System;

namespace Askyl.Dsm.WebHosting.Constants;

public static class DsmDefaults
{

    public const string ConfigurationFileName = "/etc/synoinfo.conf";
    public const string KeyExternalHostIp = "external_host_ip";

    public const string KeyExternalHttpsPort = "external_port_dsm_https";

    public const int DefaultHttpsPort = 5001;

    public const string HttpClientName = "brad babble diboo";

    public const string DsmApiHandshakePath = "query.cgi";

    public const string DsmApiInfo = "SYNO.API.Info";

    public const string DsmApiAuth = "SYNO.API.Auth";

    // FileStation APIs
    public const string DsmApiFileStationInfo = "SYNO.FileStation.Info";
    public const string DsmApiFileStationList = "SYNO.FileStation.List";
    public const string DsmApiFileStationSearch = "SYNO.FileStation.Search";
    public const string DsmApiFileStationVirtualFolder = "SYNO.FileStation.VirtualFolder";
    public const string DsmApiFileStationFavorite = "SYNO.FileStation.Favorite";
    public const string DsmApiFileStationThumb = "SYNO.FileStation.Thumb";
    public const string DsmApiFileStationDirSize = "SYNO.FileStation.DirSize";
    public const string DsmApiFileStationMd5 = "SYNO.FileStation.MD5";
    public const string DsmApiFileStationCheckPermission = "SYNO.FileStation.CheckPermission";
    public const string DsmApiFileStationUpload = "SYNO.FileStation.Upload";
    public const string DsmApiFileStationDownload = "SYNO.FileStation.Download";
    public const string DsmApiFileStationSharing = "SYNO.FileStation.Sharing";
    public const string DsmApiFileStationCreateFolder = "SYNO.FileStation.CreateFolder";
    public const string DsmApiFileStationRename = "SYNO.FileStation.Rename";
    public const string DsmApiFileStationCopyMove = "SYNO.FileStation.CopyMove";
    public const string DsmApiFileStationDelete = "SYNO.FileStation.Delete";
    public const string DsmApiFileStationExtract = "SYNO.FileStation.Extract";
    public const string DsmApiFileStationCompress = "SYNO.FileStation.Compress";
    public const string DsmApiFileStationBackgroundTask = "SYNO.FileStation.BackgroundTask";

    public const string DsmApiReverseProxy = "SYNO.Core.AppPortal.ReverseProxy";

    public static readonly string[] RequiredApis = [DsmApiAuth, DsmApiFileStationInfo, DsmApiFileStationList, DsmApiReverseProxy];

    public static readonly string RequiredApisJoined = String.Join(",", DsmDefaults.RequiredApis);
}
