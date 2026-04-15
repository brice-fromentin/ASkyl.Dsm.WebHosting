using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.SourceGenerators;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Models.ReverseProxy;

[GenerateClone]
public partial class ReverseProxyBackend(string? fqdn, int port, int protocol) : IEquatable<ReverseProxyBackend>
{
    public ReverseProxyBackend() : this(null, 0, 0) { }

    [JsonPropertyName("fqdn")]
    public string? Fqdn { get; set; } = fqdn;

    [JsonPropertyName("port")]
    public int Port { get; set; } = port;

    [JsonPropertyName("protocol")]
    public int Protocol { get; set; } = protocol;

    public bool Equals(ReverseProxyBackend? other)
    {
        if (other is null || ReferenceEquals(this, other))
        {
            return ReferenceEquals(this, other);
        }

        return String.Equals(Fqdn, other.Fqdn, StringComparison.OrdinalIgnoreCase) &&
               Int32.Equals(Port, other.Port) &&
               Int32.Equals(Protocol, other.Protocol);
    }

    public override bool Equals(object? obj) => Equals(obj as ReverseProxyBackend);

    public override int GetHashCode() => HashCode.Combine(
        Fqdn?.ToLowerInvariant(),
        Port,
        Protocol
    );
}
