using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.SourceGenerators;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Models.ReverseProxy;

[GenerateClone]
public partial class ReverseProxyCustomHeader : IEquatable<ReverseProxyCustomHeader>
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("value")]
    public string Value { get; set; } = default!;

    public bool Equals(ReverseProxyCustomHeader? other)
    {
        if (other is null || ReferenceEquals(this, other))
        {
            return ReferenceEquals(this, other);
        }

        return String.Equals(Name, other.Name, StringComparison.Ordinal) &&
               String.Equals(Value, other.Value, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj) => Equals(obj as ReverseProxyCustomHeader);

    public override int GetHashCode() => HashCode.Combine(
        Name,
        Value
    );
}
