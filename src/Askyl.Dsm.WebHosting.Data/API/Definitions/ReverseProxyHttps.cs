using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions;

public class ReverseProxyHttps : IGenericCloneable<ReverseProxyHttps>
{
    [JsonPropertyName("hsts")]
    public bool Hsts { get; set; }

    public ReverseProxyHttps Clone()
        => new() { Hsts = this.Hsts };
}
