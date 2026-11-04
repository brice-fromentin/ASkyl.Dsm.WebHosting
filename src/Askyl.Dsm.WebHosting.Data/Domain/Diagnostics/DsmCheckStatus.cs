namespace Askyl.Dsm.WebHosting.Data.Domain.Diagnostics;

/// <summary>
/// Outcome of one diagnostic check.
/// </summary>
public enum DsmCheckStatus
{
    /// <summary>The check passed.</summary>
    Passed,

    /// <summary>The check failed; the run stopped here.</summary>
    Failed,

    /// <summary>The check did not run, because an earlier one failed.</summary>
    Skipped
}
