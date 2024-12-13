using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions;

public class ReverseProxyFrontend
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