using Askyl.Dsm.WebHosting.Constants;
using Askyl.Dsm.WebHosting.Data.API.Definitions;

namespace Askyl.Dsm.WebHosting.Data.API.Parameters.ReverseProxyAPI;

public class ReverseProxyCreateParameters(ApiInformationCollection informations) : ApiParametersBase<ReverseProxy>(informations)
{
    public override string Name => DsmDefaults.DsmApiReverseProxy;

    public override int Version => 1;

    public override string Method => "";

    public override SerializationFormats SerializationFormat => SerializationFormats.Json;
}

/*
{
  "description": "sniffou",
  "proxy_connect_timeout": 60,
  "proxy_read_timeout": 60,
  "proxy_send_timeout": 60,
  "proxy_http_version": 1,
  "proxy_intercept_errors": false,
  "frontend": {
    "acl": null,
    "fqdn": "johnou",
    "port": 844,
    "protocol": 1,
    "https": {
      "hsts": true
    }
  },
  "backend": {
    "fqdn": "Yiihaa",
    "port": 987,
    "protocol": 0
  },
  "customize_headers": [
    {
      "name": "Yahhooo",
      "value": "Rachouk"
    }
  ]
}
*/