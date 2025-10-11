
using Microsoft.Extensions.Logging;
using Askyl.Dsm.WebHosting.Constants.API;
using Askyl.Dsm.WebHosting.Constants.Network;
using Askyl.Dsm.WebHosting.Constants.UI;
using Askyl.Dsm.WebHosting.Data.API.Definitions.ReverseProxy;
using Askyl.Dsm.WebHosting.Data.API.Parameters.ReverseProxyAPI;
using Askyl.Dsm.WebHosting.Data.API.Responses;
using Askyl.Dsm.WebHosting.Data.WebSites;
using Askyl.Dsm.WebHosting.Tools.Extensions;
using Askyl.Dsm.WebHosting.Tools.Network;

namespace Askyl.Dsm.WebHosting.Tools.WebSites;

public class ReverseProxyManager(ILogger<ReverseProxyManager> logger, DsmApiClient dsmApiClient) : IReverseProxyManager
{
    public async Task CreateAsync(WebSiteConfiguration site)
    {
        logger.LogInformation("Creating reverse proxy rule for site {SiteName}", site.Name);

        var description = GetDescription(site.Name);
        var createParams = new ReverseProxyCreateParameters(dsmApiClient.ApiInformations);
        createParams.Parameters.Description = description;
        createParams.Parameters.Frontend = new(site.HostName, site.PublicPort, (int)site.Protocol, new(site.EnableHSTS));
        createParams.Parameters.Backend = new(NetworkConstants.Localhost, site.InternalPort, (int)ProtocolType.HTTP);

        var response = await dsmApiClient.ExecuteSimpleAsync(createParams);

        if (!response.IsValid())
        {
            logger.LogError("Failed to create reverse proxy rule for site {SiteName}. API error code: {ApiErrorCode}", site.Name, response?.Error?.Code);
            throw new InvalidOperationException($"Failed to create reverse proxy rule for site '{site.Name}'");
        }

        var proxy = await FindAsync(p => p.Description == description && p.Frontend.Port == site.PublicPort && p.Backend.Port == site.InternalPort);

        if (proxy is null)
        {
            logger.LogError("Failed to find reverse proxy rule for site {SiteName}", site.Name);

            throw new InvalidOperationException($"Failed to find reverse proxy rule for site '{site.Name}'");
        }

        site.IdReverseProxy = proxy.UUID;
    }

    public async Task UpdateAsync(WebSiteConfiguration site)
    {
        try
        {
            var proxy = await FindAsync(p => p.UUID == site.IdReverseProxy);

            if (proxy is null)
            {
                logger.LogWarning("Reverse proxy rule with UUID {uuid} not found for site {siteName}. Recreating it.", site.IdReverseProxy, site.Name);

                await CreateAsync(site);

                return;
            }

            await UpdateProxyRuleAsync(site, proxy);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while managing the reverse proxy rule for site {SiteName}", site.Name);
            site.IdReverseProxy = null;

            throw;
        }
    }

    public async Task DeleteAsync(WebSiteConfiguration site)
    {
        if (site is null)
        {
            throw new ArgumentNullException(nameof(site));
        }

        if (!site.IdReverseProxy.HasValue)
        {
            return;
        }

        logger.LogInformation("Deleting reverse proxy rule for site {SiteName}", site.Name);

        var deleteParams = new ReverseProxyDeleteParameters(dsmApiClient.ApiInformations);
        deleteParams.Parameters.Add(site.IdReverseProxy.Value);

        var deleteResponse = await dsmApiClient.ExecuteSimpleAsync(deleteParams);

        if (deleteResponse.IsValid())
        {
            return;
        }

        var errorMessage = $"Failed to delete reverse proxy rule for site '{site.Name}'. API error code: {deleteResponse?.Error?.Code}";
        logger.LogError(errorMessage);
        throw new InvalidOperationException(errorMessage);
    }

    private async Task UpdateProxyRuleAsync(WebSiteConfiguration site, ReverseProxy proxy)
    {
        logger.LogInformation("Updating reverse proxy rule for site {SiteName}", site.Name);

        var updateParams = new ReverseProxyUpdateParameters(dsmApiClient.ApiInformations, proxy);
        updateParams.Parameters.Description = GetDescription(site.Name);
        updateParams.Parameters.Frontend = new(site.HostName, site.PublicPort, (int)site.Protocol, new(site.EnableHSTS));
        updateParams.Parameters.Backend = new(NetworkConstants.Localhost, site.InternalPort, (int)ProtocolType.HTTP);

        var response = await dsmApiClient.ExecuteSimpleAsync(updateParams);

        if (response.IsValid())
        {
            logger.LogInformation("Reverse proxy rule for site {SiteName} updated successfully.", site.Name);
            return;
        }

        var errorMessage = $"Failed to update reverse proxy rule for site '{site.Name}'. API error code: {response?.Error?.Code}";
        throw new InvalidOperationException(errorMessage);
    }

    private async Task<ReverseProxy?> FindAsync(Func<ReverseProxy, bool> predicate)
    {
        logger.LogInformation("Finding a reverse proxy...");

        var parameters = new ReverseProxyListParameters(dsmApiClient.ApiInformations);
        var list = await dsmApiClient.ExecuteAsync<ReverseProxyListResponse>(parameters);

        return list?.Data?.Entries?.FirstOrDefault(predicate);
    }

    private static string GetDescription(string siteName) => $"{ReverseProxyConstants.RuleDescriptionPrefix}{siteName}";
}
