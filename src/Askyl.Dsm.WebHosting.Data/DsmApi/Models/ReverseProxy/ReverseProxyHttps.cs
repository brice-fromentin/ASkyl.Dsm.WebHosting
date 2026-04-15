using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.SourceGenerators;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Models.ReverseProxy;

[GenerateClone]
public partial class ReverseProxyHttps(bool hsts) : IEquatable<ReverseProxyHttps>
{
    public ReverseProxyHttps() : this(false) { }

    [JsonPropertyName("hsts")]
    public bool Hsts { get; set; } = hsts;

    public bool Equals(ReverseProxyHttps? other)
    {
        if (other is null || ReferenceEquals(this, other))
        {
            return ReferenceEquals(this, other);
        }

        return Boolean.Equals(Hsts, other.Hsts);
    }

    public override bool Equals(object? obj) => Equals(obj as ReverseProxyHttps);

    public override int GetHashCode() => Hsts.GetHashCode();
}
