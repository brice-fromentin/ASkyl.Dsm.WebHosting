using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.SourceGenerators;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions;

[GenerateClone]
public partial class ReverseProxyHttps
{
    [JsonPropertyName("hsts")]
    public bool Hsts { get; set; }
}
