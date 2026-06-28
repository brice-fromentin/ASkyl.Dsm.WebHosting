using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Models.ReverseProxy;

public record ReverseProxyHttps([property: JsonPropertyName("hsts")] bool Hsts)
{
    [SetsRequiredMembers]
    public ReverseProxyHttps() : this(false) { }
}
