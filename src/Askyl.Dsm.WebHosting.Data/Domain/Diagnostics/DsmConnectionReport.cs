using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.Domain.Diagnostics;

/// <summary>
/// The result of one run of the DSM connection diagnostic.
/// </summary>
/// <param name="IsHealthy">Whether every check passed.</param>
/// <param name="CheckedAt">When the run started.</param>
/// <param name="Host">The configured DSM hostname; null when missing, and in a healthy report.</param>
/// <param name="Port">The configured DSM HTTPS port; 0 in a healthy report.</param>
/// <param name="Checks">Every check, in order; empty in a healthy report.</param>
/// <param name="HostIsIpAddress">Whether the configured host is an IP address, which the report does not carry.</param>
public record DsmConnectionReport(
    bool IsHealthy,
    DateTimeOffset CheckedAt,
    string? Host,
    int Port,
    IReadOnlyList<DsmConnectionCheckResult> Checks,
    bool HostIsIpAddress = false)
{
    /// <summary>
    /// Initializes an empty report for deserialization.
    /// </summary>
    [JsonConstructor]
    public DsmConnectionReport() : this(false, default, null, 0, []) { }
}
