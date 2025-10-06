using Askyl.Dsm.WebHosting.SourceGenerators;
using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.WebSites;

[GenerateClone]
public partial class WebSiteInstance(WebSiteConfiguration configuration)
{
    public WebSiteInstance() : this(new()) { }

    public static WebSiteInstance New()
        => new(new());

    public Guid Id { get; init; } = Guid.NewGuid();

    public WebSiteConfiguration Configuration { get; set; } = configuration ?? new();

    public bool IsRunning => Process != null;

    public string State => Process switch
    {
        null => "Stopped",
        { IsResponding: false } => "Not Responding",
        _ => "Running"
    };

    [JsonIgnore]
    public ProcessInfo? Process { get; set; }
}