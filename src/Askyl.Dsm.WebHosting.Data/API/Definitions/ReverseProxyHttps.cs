using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions;

public class ReverseProxyHttps
{
    [JsonPropertyName("hsts")]
    public bool Hsts { get; set; }
}