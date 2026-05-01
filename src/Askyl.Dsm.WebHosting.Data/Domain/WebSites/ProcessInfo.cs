namespace Askyl.Dsm.WebHosting.Data.Domain.WebSites;

/// <summary>
/// Snapshot of process state at a point in time.
/// Does not hold a live Process reference to avoid serialization staleness.
/// </summary>
public record ProcessInfo(int Id, bool IsResponding);
