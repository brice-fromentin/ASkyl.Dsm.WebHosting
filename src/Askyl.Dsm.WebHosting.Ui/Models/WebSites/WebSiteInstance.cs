using Askyl.Dsm.WebHosting.Data.WebSites;

namespace Askyl.Dsm.WebHosting.Ui.Models.WebSites;

public class WebSiteInstance(WebSiteConfiguration configuration)
{
    public static WebSiteInstance New()
        => new(new());
        
    public WebSiteConfiguration Configuration { get; set; } = configuration ?? new();

    public ProcessInfo? Process { get; set; }

    public bool IsRunning => Process != null;

    public string State => Process switch
    {
        null => "Stopped",
        { IsResponding: false } => "Not Responding",
        _ => "Running"
    };
}
