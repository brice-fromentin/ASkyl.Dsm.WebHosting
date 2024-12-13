using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.Data.API.Definitions;

namespace Askyl.Dsm.WebHosting.Data.API.Responses;

public class ReverseProxyListResponse : ApiResponseBase<ReverseProxyList>
{
}

public class ReverseProxyList
{
    [JsonPropertyName("entries")]
    public List<ReverseProxy> Entries { get; set; } = default!;
}

/*
{
    "data": {
        "entries": [
            {
                "UUID": "ec167e25-cb8c-4cd6-bca7-5a22c14f61e4",
                "backend": {
                    "fqdn": "localhost",
                    "port": 7120,
                    "protocol": 0
                },
                "customize_headers": [],
                "description": "john",
                "frontend": {
                    "acl": null,
                    "fqdn": "test.john.com",
                    "https": {
                        "hsts": true
                    },
                    "port": 443,
                    "protocol": 1
                },
                "proxy_connect_timeout": 60,
                "proxy_http_version": 1,
                "proxy_intercept_errors": false,
                "proxy_read_timeout": 60,
                "proxy_send_timeout": 60
            }
        ]
    },
    "success": true
}
*/