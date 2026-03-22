using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.SourceGenerators;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions.ReverseProxy;

[GenerateClone]
public partial class ReverseProxy
{
    [JsonPropertyName("UUID")]
    public Guid? UUID { get; set; }

    [JsonPropertyName("_key")]
    public string? Key { get; set; }

    [JsonPropertyName("backend")]
    public ReverseProxyBackend Backend { get; set; } = default!;

    [JsonPropertyName("customize_headers")]
    public List<ReverseProxyCustomHeader> CustomizeHeaders { get; set; } = [];

    [JsonPropertyName("description")]
    public string Description { get; set; } = default!;

    [JsonPropertyName("frontend")]
    public ReverseProxyFrontend Frontend { get; set; } = default!;

    [JsonPropertyName("proxy_connect_timeout")]
    public int ProxyConnectTimeout { get; set; } = 60;

    [JsonPropertyName("proxy_http_version")]
    public int ProxyHttpVersion { get; set; } = 1;

    [JsonPropertyName("proxy_intercept_errors")]
    public bool ProxyInterceptErrors { get; set; } = false;

    [JsonPropertyName("proxy_read_timeout")]
    public int ProxyReadTimeout { get; set; } = 60;

    [JsonPropertyName("proxy_send_timeout")]
    public int ProxySendTimeout { get; set; } = 60;
}
