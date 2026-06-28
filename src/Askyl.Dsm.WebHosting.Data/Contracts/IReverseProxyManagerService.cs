using Askyl.Dsm.WebHosting.Data.Domain.WebSites;

namespace Askyl.Dsm.WebHosting.Data.Contracts;

public interface IReverseProxyManagerService
{
    Task CreateAsync(WebSiteConfiguration site, CancellationToken cancellationToken = default);

    Task UpdateAsync(WebSiteConfiguration site, CancellationToken cancellationToken = default);

    Task DeleteAsync(WebSiteConfiguration site, CancellationToken cancellationToken = default);
}
