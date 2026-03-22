using Askyl.Dsm.WebHosting.Constants.Application;

namespace Askyl.Dsm.WebHosting.Data.WebSites;

public class WebSitesConfiguration
{
    public List<WebSiteConfiguration> Sites { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    public string Version { get; set; } = ApplicationConstants.DefaultConfigurationVersion;
}