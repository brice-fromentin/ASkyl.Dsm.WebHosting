using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.Domain.WebSites;

/// <summary>
/// Represents the runtime state of a website instance.
/// Immutable record for thread-safe state management.
/// </summary>
public sealed record WebSiteRuntimeState(bool IsRunning, ProcessInfo? ProcessDetails, string StatusText)
{
    /// <summary>
    /// Creates a stopped state instance.
    /// </summary>
    public static WebSiteRuntimeState Stopped => new(false, null, "Stopped");

    /// <summary>
    /// Creates a running state instance with process details.
    /// </summary>
    public static WebSiteRuntimeState Running(ProcessInfo info) => new(true, info, "Running");

    /// <summary>
    /// Creates a not responding state instance with process details.
    /// </summary>
    public static WebSiteRuntimeState NotResponding(ProcessInfo info) => new(true, info, "Not Responding");

    /// <summary>
    /// Gets whether the process is currently responding to health checks.
    /// </summary>
    [JsonIgnore]
    public bool IsResponding => ProcessDetails?.IsResponding == true;
}
