namespace Askyl.Dsm.WebHosting.Data.WebSites;

public interface IWebSitesConfigurationService
{
    Task<WebSitesConfiguration> LoadConfigurationAsync();

    Task SaveConfigurationAsync(WebSitesConfiguration collection);

    Task EnsureLoadedAsync();

    Task<WebSiteConfiguration?> GetSiteAsync(string siteName);

    Task<IEnumerable<WebSiteConfiguration>> GetAllSitesAsync();

    Task<IEnumerable<WebSiteConfiguration>> GetSitesToStartAsync();

    Task AddSiteAsync(WebSiteConfiguration site);

    Task UpdateSiteAsync(WebSiteConfiguration site);

    Task RemoveSiteAsync(string siteName);

    Task<bool> SiteExistsAsync(string siteName);
}