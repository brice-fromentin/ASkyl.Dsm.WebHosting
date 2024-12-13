using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions;

public class ReverseProxy
{
    [JsonPropertyName("UUID")]
    public string UUID { get; set; } = default!;

    [JsonPropertyName("backend")]
    public ReverseProxyBackend Backend { get; set; } = default!;

    [JsonPropertyName("customize_headers")]
    public List<ReverseProxyCustomHeader> CustomizeHeaders { get; set; } = default!;

    [JsonPropertyName("description")]
    public string Description { get; set; } = default!;

    [JsonPropertyName("frontend")]
    public ReverseProxyFrontend Frontend { get; set; } = default!;

    [JsonPropertyName("proxy_connect_timeout")]
    public int ProxyConnectTimeout { get; set; }

    [JsonPropertyName("proxy_http_version")]
    public int ProxyHttpVersion { get; set; }

    [JsonPropertyName("proxy_intercept_errors")]
    public bool ProxyInterceptErrors { get; set; }

    [JsonPropertyName("proxy_read_timeout")]
    public int ProxyReadTimeout { get; set; }

    [JsonPropertyName("proxy_send_timeout")]
    public int ProxySendTimeout { get; set; }
}
