using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Models.ReverseProxy;

public record ReverseProxyFrontend(string? Fqdn, int Port, int Protocol, ReverseProxyHttps Https)
{
    public ReverseProxyFrontend() : this(null, 0, 0, new()) { }

    [JsonPropertyName("acl")]
    public object Acl { get; init; } = default!;

    [JsonPropertyName("fqdn")]
    public string? Fqdn { get; init; } = Fqdn;

    [JsonPropertyName("https")]
    public ReverseProxyHttps Https { get; init; } = Https;

    [JsonPropertyName("port")]
    public int Port { get; init; } = Port;

    [JsonPropertyName("protocol")]
    public int Protocol { get; init; } = Protocol;
}
