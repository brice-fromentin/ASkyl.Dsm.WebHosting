using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.SourceGenerators;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions.ReverseProxy;

[GenerateClone]
public partial class ReverseProxyBackend(string? fqdn, int port, int protocol)
{
    public ReverseProxyBackend() : this(null, 0, 0) { }

    [JsonPropertyName("fqdn")]
    public string? Fqdn { get; set; } = fqdn;

    [JsonPropertyName("port")]
    public int Port { get; set; } = port;

    [JsonPropertyName("protocol")]
    public int Protocol { get; set; } = protocol;
}
