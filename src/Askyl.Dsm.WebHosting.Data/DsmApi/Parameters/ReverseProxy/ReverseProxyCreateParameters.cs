using Askyl.Dsm.WebHosting.Constants.DSM.API;
// Removed - using same namespace
using Askyl.Dsm.WebHosting.Data.Attributes;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Parameters.ReverseProxy;

[DsmParameterName("entry")]
public class ReverseProxyCreateParameters(ApiInformationCollection informations) : ApiParametersBase<Models.ReverseProxy.ReverseProxy>(informations)
{
    public override string Name => ApiNames.AppPortalReverseProxy;

    public override int Version => 1;

    public override string Method => ApiMethods.Create;

    public override SerializationFormats SerializationFormat => SerializationFormats.Json;
}

/*
api=SYNO.Core.AppPortal.ReverseProxy
&method=create
&version=1
&entry=
{
    "description": "test-proxy-name",
    "proxy_connect_timeout": 60,
    "proxy_read_timeout": 60,
    "proxy_send_timeout": 60,
    "proxy_http_version": 1,
    "proxy_intercept_errors": false,
    "frontend": {
        "acl": null,
        "fqdn": "test-hostname",
        "port": 80,
        "protocol": 0,
        "https": {
            "hsts": false
        }
    },
    "backend": {
        "fqdn": "test-target-hostname",
        "port": 80,
        "protocol": 0
    },
    "customize_headers": []
}
*/