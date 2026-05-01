using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Models.ReverseProxy;

public record ReverseProxy
{
    [JsonPropertyName("UUID")]
    public Guid? UUID { get; init; }

    [JsonPropertyName("_key")]
    public string? Key { get; init; }

    [JsonPropertyName("backend")]
    public ReverseProxyBackend Backend { get; init; } = default!;

    [JsonPropertyName("customize_headers")]
    public List<ReverseProxyCustomHeader> CustomizeHeaders { get; init; } = [];

    [JsonPropertyName("description")]
    public string Description { get; init; } = default!;

    [JsonPropertyName("frontend")]
    public ReverseProxyFrontend Frontend { get; init; } = default!;

    [JsonPropertyName("proxy_connect_timeout")]
    public int ProxyConnectTimeout { get; init; } = 60;

    [JsonPropertyName("proxy_http_version")]
    public int ProxyHttpVersion { get; init; } = 1;

    [JsonPropertyName("proxy_intercept_errors")]
    public bool ProxyInterceptErrors { get; init; } = false;

    [JsonPropertyName("proxy_read_timeout")]
    public int ProxyReadTimeout { get; init; } = 60;

    [JsonPropertyName("proxy_send_timeout")]
    public int ProxySendTimeout { get; init; } = 60;
}
