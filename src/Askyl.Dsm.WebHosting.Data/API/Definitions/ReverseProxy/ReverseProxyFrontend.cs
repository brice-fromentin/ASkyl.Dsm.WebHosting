using System.Text.Json.Serialization;

using Askyl.Dsm.WebHosting.SourceGenerators;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions.ReverseProxy;

[GenerateClone]
public partial class ReverseProxyFrontend(string? fqdn, int port, int protocol, ReverseProxyHttps https)
{
    public ReverseProxyFrontend() : this(null, 0, 0, new()) { }

    [JsonPropertyName("acl")]
    public object Acl { get; set; } = default!;

    [JsonPropertyName("fqdn")]
    public string? Fqdn { get; set; } = fqdn;

    [JsonPropertyName("https")]
    public ReverseProxyHttps Https { get; set; } = https;

    [JsonPropertyName("port")]
    public int Port { get; set; } = port;

    [JsonPropertyName("protocol")]
    public int Protocol { get; set; } = protocol;
}