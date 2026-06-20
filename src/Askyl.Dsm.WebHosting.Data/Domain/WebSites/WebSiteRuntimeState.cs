namespace Askyl.Dsm.WebHosting.Data.Domain.WebSites;

/// <summary>
/// Represents the runtime state of a website instance.
/// Immutable record for thread-safe state management.
/// </summary>
public sealed record WebSiteRuntimeState(bool IsRunning, ProcessInfo? ProcessDetails);
