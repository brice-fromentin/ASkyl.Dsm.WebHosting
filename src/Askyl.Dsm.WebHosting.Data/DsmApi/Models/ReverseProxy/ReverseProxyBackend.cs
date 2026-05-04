using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Models.ReverseProxy;

public record ReverseProxyBackend(string? Fqdn, int Port, int Protocol)
{
    public ReverseProxyBackend() : this(null, 0, 0) { }

    [JsonPropertyName("fqdn")]
    public string? Fqdn { get; init; } = Fqdn;

    [JsonPropertyName("port")]
    public int Port { get; init; } = Port;

    [JsonPropertyName("protocol")]
    public int Protocol { get; init; } = Protocol;
}
