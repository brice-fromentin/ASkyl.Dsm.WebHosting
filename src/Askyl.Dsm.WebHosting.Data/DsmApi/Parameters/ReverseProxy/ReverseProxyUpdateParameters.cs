using Askyl.Dsm.WebHosting.Constants.DSM.API;
using Askyl.Dsm.WebHosting.Data.Attributes;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Parameters.ReverseProxy;

[DsmParameterName("entry")]
public class ReverseProxyUpdateParameters(ApiInformationCollection informations, Models.ReverseProxy.ReverseProxy proxy) : ApiParametersBase<Models.ReverseProxy.ReverseProxy>(informations, proxy)
{
    public override string Name => ApiNames.AppPortalReverseProxy;

    public override int Version => 1;

    public override string Method => ApiMethods.Update;

    public override SerializationFormats SerializationFormat => SerializationFormats.Json;
}

/*
api=SYNO.Core.AppPortal.ReverseProxy
&method=update
&version=1
&entry=
{
    "description": "test-proxy-name",
    "proxy_connect_timeout": 60,
    "proxy_read_timeout": 60,
    "proxy_send_timeout": 60,
    "proxy_http_version": 1,
    "proxy_intercept_errors": false,
    "UUID": "20409e24-43fa-4239-9199-e42f330356bb",
    "backend": {
        "fqdn": "test-target-hostname",
        "port": 80,
        "protocol": 0
    },
    "customize_headers": [],
    "frontend": {
        "acl": null,
        "fqdn": "test-hostname",
        "port": 80,
        "protocol": 0,
        "https": {
            "hsts": false
        }
    },
    "_key": "95b443a4-eb94-4e06-b7f1-7c144d720171"
}
*/
