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

    public const string DsmApiFileStationList = "SYNO.FileStation.List";

    public const string DsmApiReverseProxy = "SYNO.Core.AppPortal.ReverseProxy";

    public static readonly string[] RequiredApis = [DsmApiAuth, DsmApiFileStationList, DsmApiReverseProxy];

    public static readonly string RequiredApisJoined = String.Join(",", DsmDefaults.RequiredApis);
}
