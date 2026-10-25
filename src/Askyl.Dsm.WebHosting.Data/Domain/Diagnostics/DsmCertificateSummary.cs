using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.Domain.Diagnostics;

/// <summary>
/// What the diagnostic shows of DSM's certificate.
/// </summary>
/// <param name="Names">The DNS names the certificate covers; may be empty, never an IP address.</param>
/// <param name="Issuer">The issuer's simple name; empty when it would be an IP address.</param>
/// <param name="NotAfter">The end of the validity period.</param>
public record DsmCertificateSummary(IReadOnlyList<string> Names, string Issuer, DateTimeOffset NotAfter)
{
    /// <summary>
    /// Initializes an empty summary for deserialization.
    /// </summary>
    [JsonConstructor]
    public DsmCertificateSummary() : this([], String.Empty, default) { }
}
