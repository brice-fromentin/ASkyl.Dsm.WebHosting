namespace Askyl.Dsm.WebHosting.Data.WebSites;

public interface IWebSitesConfigurationService
{
    Task<WebSiteConfiguration?> GetSiteAsync(Guid siteId);

    Task<IEnumerable<WebSiteConfiguration>> GetAllSitesAsync();

    Task<IEnumerable<WebSiteConfiguration>> GetSitesToStartAsync();

    Task AddSiteAsync(WebSiteConfiguration site);

    Task UpdateSiteAsync(WebSiteConfiguration site);

    Task RemoveSiteAsync(Guid siteId);
}