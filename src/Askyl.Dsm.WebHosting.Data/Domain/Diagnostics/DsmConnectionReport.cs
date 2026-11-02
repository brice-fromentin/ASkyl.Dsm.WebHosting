using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.Domain.Diagnostics;

/// <summary>
/// The result of one run of the DSM connection diagnostic.
/// </summary>
/// <param name="IsHealthy">Whether every check passed.</param>
/// <param name="CheckedAt">When the run started.</param>
/// <param name="Port">The DSM HTTPS port checked; 0 in a healthy report.</param>
/// <param name="Checks">Every check, in order; empty in a healthy report.</param>
public record DsmConnectionReport(
    bool IsHealthy,
    DateTimeOffset CheckedAt,
    int Port,
    IReadOnlyList<DsmConnectionCheckResult> Checks)
{
    /// <summary>
    /// Initializes an empty report for deserialization.
    /// </summary>
    [JsonConstructor]
    public DsmConnectionReport() : this(false, default, 0, []) { }
}
