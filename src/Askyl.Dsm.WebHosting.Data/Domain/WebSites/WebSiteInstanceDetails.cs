namespace Askyl.Dsm.WebHosting.Data.Domain.WebSites;

/// <summary>
/// Server-side website instance with process details.
/// Inherits from <see cref="WebSiteInstance"/> and adds runtime process information.
/// </summary>
public sealed class WebSiteInstanceDetails : WebSiteInstance
{
    /// <summary>
    /// Gets or sets the runtime process information (server-side only).
    /// </summary>
    public ProcessInfo? Process { get; set; }

    public WebSiteInstanceDetails()
    {
    }

    public WebSiteInstanceDetails(WebSiteConfiguration configuration)
        : base(configuration)
    {
    }
}
