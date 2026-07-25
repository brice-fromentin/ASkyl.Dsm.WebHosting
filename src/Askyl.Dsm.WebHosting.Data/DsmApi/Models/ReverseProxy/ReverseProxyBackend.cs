using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Models.ReverseProxy;

public record ReverseProxyBackend(
    [property: JsonPropertyName("fqdn")] string? Fqdn,
    [property: JsonPropertyName("port")] int Port,
    [property: JsonPropertyName("protocol")] int Protocol)
{
    [SetsRequiredMembers]
    public ReverseProxyBackend() : this(null, 0, 0) { }
}
