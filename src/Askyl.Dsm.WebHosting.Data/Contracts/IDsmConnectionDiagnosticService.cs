using Askyl.Dsm.WebHosting.Data.Results;

namespace Askyl.Dsm.WebHosting.Data.Contracts;

/// <summary>
/// Checks whether this application can reach its DSM, and says what to fix when it cannot.
/// Anonymous by design: it is what a visitor sees when signing in is impossible.
/// </summary>
public interface IDsmConnectionDiagnosticService
{
    /// <summary>
    /// Returns the latest report, running the checks only when none exists yet.
    /// </summary>
    Task<DsmConnectionReportResult> GetReportAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Re-reads the DSM settings and runs the checks again.
    /// </summary>
    Task<DsmConnectionReportResult> RecheckAsync(CancellationToken cancellationToken = default);
}
