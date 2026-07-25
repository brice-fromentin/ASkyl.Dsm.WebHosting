using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Models.ReverseProxy;

public record ReverseProxyFrontend(
    [property: JsonPropertyName("fqdn")] string? Fqdn,
    [property: JsonPropertyName("port")] int Port,
    [property: JsonPropertyName("protocol")] int Protocol,
    [property: JsonPropertyName("https")] ReverseProxyHttps Https)
{
    [SetsRequiredMembers]
    public ReverseProxyFrontend() : this(null, 0, 0, new()) { }

    [JsonPropertyName("acl")]
    public object Acl { get; init; } = default!;
}
