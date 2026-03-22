using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.SourceGenerators;

namespace Askyl.Dsm.WebHosting.Data.WebSites;

[GenerateClone]
public partial class WebSiteInstance
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
        // Default constructor creates instance with empty configuration (Id = Guid.Empty, IsRunning = false)
    }

    public WebSiteInstance(WebSiteConfiguration configuration)
    {
        Configuration = configuration;
    }

    /// <summary>
    /// Creates a new website instance for creation (not edit).
    /// Returns an instance with empty IDs to indicate it's a new entity.
    /// </summary>
    public static WebSiteInstance New() => new();

    /// <summary>
    /// Gets a human-readable state description.
    /// Uses serialized IsRunning for client-side, Process for server-side detail.
    /// </summary>
    [JsonIgnore]
    public string State
    {
        get
        {
            // If no process, rely on serialized IsRunning flag
            if (Process == null)
            {
                return IsRunning ? "Running" : "Stopped";
            }

            // Has process - check responsiveness for detailed state
            return Process.IsResponding ? "Running" : "Not Responding";
        }
    }

    [JsonIgnore]
    public ProcessInfo? Process { get; set; }
}
