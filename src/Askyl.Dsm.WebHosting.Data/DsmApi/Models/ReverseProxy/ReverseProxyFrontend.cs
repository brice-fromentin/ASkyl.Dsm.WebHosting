using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.SourceGenerators;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Models.ReverseProxy;

[GenerateClone]
public partial class ReverseProxyFrontend(string? fqdn, int port, int protocol, ReverseProxyHttps https) : IEquatable<ReverseProxyFrontend>
{
    public ReverseProxyFrontend() : this(null, 0, 0, new()) { }

    [JsonPropertyName("acl")]
    public object Acl { get; set; } = default!;

    [JsonPropertyName("fqdn")]
    public string? Fqdn { get; set; } = fqdn;

    [JsonPropertyName("https")]
    public ReverseProxyHttps Https { get; set; } = https;

    [JsonPropertyName("port")]
    public int Port { get; set; } = port;

    [JsonPropertyName("protocol")]
    public int Protocol { get; set; } = protocol;

    public bool Equals(ReverseProxyFrontend? other)
    {
        if (other is null || ReferenceEquals(this, other))
        {
            return ReferenceEquals(this, other);
        }

        return String.Equals(Fqdn, other.Fqdn, StringComparison.OrdinalIgnoreCase) &&
               Int32.Equals(Port, other.Port) &&
               Int32.Equals(Protocol, other.Protocol) &&
               Https.Equals(other.Https) &&
               Equals(Acl, other.Acl);
    }

    public override bool Equals(object? obj) => Equals(obj as ReverseProxyFrontend);

    public override int GetHashCode() => HashCode.Combine(
        Acl,
        Fqdn?.ToLowerInvariant(),
        Https,
        Port,
        Protocol
    );
}
