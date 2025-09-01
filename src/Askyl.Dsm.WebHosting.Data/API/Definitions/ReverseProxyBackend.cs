using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions;

public class ReverseProxyBackend : IGenericCloneable<ReverseProxyBackend>
{

    [JsonPropertyName("fqdn")]
    public string Fqdn { get; set; } = default!;

    [JsonPropertyName("port")]
    public int Port { get; set; }

    [JsonPropertyName("protocol")]
    public int Protocol { get; set; }

    public ReverseProxyBackend Clone()
        => new() { Fqdn = this.Fqdn, Port = this.Port, Protocol = this.Protocol };
}
