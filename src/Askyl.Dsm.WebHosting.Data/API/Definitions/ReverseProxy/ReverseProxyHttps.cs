using System.Text.Json.Serialization;

using Askyl.Dsm.WebHosting.SourceGenerators;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions.ReverseProxy;

[GenerateClone]
public partial class ReverseProxyHttps(bool hsts)
{
    public ReverseProxyHttps() : this(false) { }

    [JsonPropertyName("hsts")]
    public bool Hsts { get; set; } = hsts;
}