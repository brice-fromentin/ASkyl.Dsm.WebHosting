
namespace Askyl.Dsm.WebHosting.Data.WebSites;

public interface IReverseProxyManager
{
    Task CreateAsync(WebSiteConfiguration site);
    Task UpdateAsync(WebSiteConfiguration site);
    Task DeleteAsync(WebSiteConfiguration site);
}
