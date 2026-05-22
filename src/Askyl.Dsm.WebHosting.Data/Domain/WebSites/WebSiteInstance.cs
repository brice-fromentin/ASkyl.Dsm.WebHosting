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
    /// </summary>
    public WebSiteConfiguration Configuration { get; set; } = new();

    /// <summary>
    /// Gets or sets whether this instance is currently running.
    /// This is the serialized state sent to clients (independent of Process object).
    /// </summary>
    public bool IsRunning { get; set; }

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
    public string State => IsRunning ? "Running" : "Stopped";

    /// <summary>
    /// Gets or sets the required .NET framework channel detected from the assembly (e.g., "8.0").
    /// This is a runtime-derived property, not persisted in configuration.
    /// </summary>
    public string? RequiredFramework { get; set; }
}
