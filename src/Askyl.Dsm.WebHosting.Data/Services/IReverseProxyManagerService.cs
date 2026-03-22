using Askyl.Dsm.WebHosting.Data.WebSites;

namespace Askyl.Dsm.WebHosting.Data.Services;

public interface IReverseProxyManagerService
{
    Task CreateAsync(WebSiteConfiguration site);

    Task UpdateAsync(WebSiteConfiguration site);

    Task DeleteAsync(WebSiteConfiguration site);
}
