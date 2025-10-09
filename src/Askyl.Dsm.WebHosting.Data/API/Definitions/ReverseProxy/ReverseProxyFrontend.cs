using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.SourceGenerators;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions.ReverseProxy;

[GenerateClone]
public partial class ReverseProxyFrontend
{
    [JsonPropertyName("acl")]
    public object Acl { get; set; } = default!;

    [JsonPropertyName("fqdn")]
    public string Fqdn { get; set; } = default!;

    [JsonPropertyName("https")]
    public ReverseProxyHttps Https { get; set; } = default!;

    [JsonPropertyName("port")]
    public int Port { get; set; }

    [JsonPropertyName("protocol")]
    public int Protocol { get; set; }
}