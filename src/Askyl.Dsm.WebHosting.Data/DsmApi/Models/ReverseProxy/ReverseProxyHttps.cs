using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Models.ReverseProxy;

public record ReverseProxyHttps(bool Hsts)
{
    public ReverseProxyHttps() : this(false) { }

    [JsonPropertyName("hsts")]
    public bool Hsts { get; init; } = Hsts;
}
