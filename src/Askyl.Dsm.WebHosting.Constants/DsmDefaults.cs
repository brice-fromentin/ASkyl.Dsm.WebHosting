using System;

namespace Askyl.Dsm.WebHosting.Constants;

public static class DsmDefaults
{
    public const string KeyExternalHostIp = "external_host_ip";

    public const string KeyExternalHttpsPort = "external_port_dsm_https";

    public const int DefaultHttpsPort = 5001;

    public const string HttpClientName = "brad babble diboo";

    public const string DsmApiHandshakePath = "query.cgi";

    public const string DsmApiInfo = "SYNO.API.Info";

    public const string DsmApiAuth = "SYNO.API.Auth";

    public const string DsmApiFileStationList = "SYNO.FileStation.List";

    public static readonly string[] RequiredApi = [DsmApiAuth, DsmApiFileStationList];

    public static readonly string RequiredApiJoined = String.Join(",", DsmDefaults.RequiredApi);
}
