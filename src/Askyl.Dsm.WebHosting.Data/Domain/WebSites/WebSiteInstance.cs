using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.Constants.Application;

namespace Askyl.Dsm.WebHosting.Data.Domain.WebSites;

/// <summary>
/// Client-facing website instance — JSON serializable.
/// </summary>
public class WebSiteInstance
{
    /// <summary>
    /// Gets the unique identifier for this instance (forwarding property to Configuration.Id).
    /// For new instances, this returns Guid.Empty until the configuration is persisted.
    /// </summary>
    public Guid Id => Configuration.Id;

    /// <summary>
    /// Gets or sets the associated website configuration.
    /// This is the source of truth for the instance identity.
    /// NOTE: Remains mutable (get; set;) because WebSiteHostingService replaces the configuration
    /// during site updates (UpdateOrCreateInstance) to sync with persisted changes.
    /// </summary>
    public WebSiteConfiguration Configuration { get; set; } = new();

    /// <summary>
    /// Gets or sets whether this instance is currently running.
    /// This is the serialized state sent to clients (independent of Process object).
    /// NOTE: Remains mutable (get; set;) because the lifecycle manager updates this property
    /// during start/stop operations via UpdateInstanceRuntimeState.
    /// </summary>
    public bool IsRunning { get; set; }

    /// <summary>
    /// Gets or sets the runtime process information (server-side only, not serialized to client).
    /// NOTE: Remains mutable (get; set;) because the lifecycle manager updates this property
    /// during start/stop operations via UpdateInstanceRuntimeState.
    /// </summary>
    [JsonIgnore]
    public ProcessInfo? Process { get; set; }

    public WebSiteInstance()
    {
    }

    public WebSiteInstance(WebSiteConfiguration configuration)
    {
        Configuration = configuration;
    }

    /// <summary>
    /// Gets a human-readable state description.
    /// </summary>
    public string State => IsRunning ? WebSiteConstants.StateRunning : WebSiteConstants.StateStopped;

    /// <summary>
    /// Gets or sets the required .NET framework channel detected from the assembly (e.g., "8.0").
    /// This is a runtime-derived property, not persisted in configuration.
    /// NOTE: Remains mutable (get; set;) because it is set after construction during initialization
    /// (InitializeAllInstancesAsync) and runtime attachment (AttachRuntimeInfo).
    /// </summary>
    public string? RequiredFramework { get; set; }
}
