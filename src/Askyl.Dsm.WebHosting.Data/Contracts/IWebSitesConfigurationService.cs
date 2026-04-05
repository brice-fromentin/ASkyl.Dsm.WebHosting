using Askyl.Dsm.WebHosting.Data.Domain.WebSites;

namespace Askyl.Dsm.WebHosting.Data.Contracts;

public interface IWebSitesConfigurationService
{
    Task<WebSiteConfiguration?> GetSiteAsync(Guid siteId, CancellationToken cancellationToken = default);

    Task<IEnumerable<WebSiteConfiguration>> GetAllSitesAsync(CancellationToken cancellationToken = default);

    Task<IEnumerable<WebSiteConfiguration>> GetSitesToStartAsync(CancellationToken cancellationToken = default);

    Task AddSiteAsync(WebSiteConfiguration site, CancellationToken cancellationToken = default);

    Task UpdateSiteAsync(WebSiteConfiguration site, CancellationToken cancellationToken = default);

    Task RemoveSiteAsync(Guid siteId, CancellationToken cancellationToken = default);
}
