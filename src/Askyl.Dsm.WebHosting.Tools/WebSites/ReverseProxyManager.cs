
using Askyl.Dsm.WebHosting.Constants.API;
using Askyl.Dsm.WebHosting.Constants.Network;
using Askyl.Dsm.WebHosting.Constants.UI;
using Askyl.Dsm.WebHosting.Data.API.Definitions.ReverseProxy;
using Askyl.Dsm.WebHosting.Data.API.Parameters.ReverseProxyAPI;
using Askyl.Dsm.WebHosting.Data.API.Responses;
using Askyl.Dsm.WebHosting.Data.WebSites;
using Askyl.Dsm.WebHosting.Tools.Extensions;
using Askyl.Dsm.WebHosting.Tools.Network;
using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Tools.WebSites;

public class ReverseProxyManager(ILogger<ReverseProxyManager> logger, DsmApiClient dsmApiClient) : IReverseProxyManager
{
    private static string GetDescription(string siteName) => $"{ReverseProxyConstants.DescriptionPrefix}{siteName}";

    public async Task CreateAsync(WebSiteConfiguration site)
    {
        logger.LogInformation("Creating reverse proxy for site {SiteName}", site.Name);

        var description = GetDescription(site.Name);
        var createParams = new ReverseProxyCreateParameters(dsmApiClient.ApiInformations);
        createParams.Parameters.Description = description;
        createParams.Parameters.Frontend = new(site.HostName, site.PublicPort, (int)site.Protocol, new(site.EnableHSTS));
        createParams.Parameters.Backend = new(NetworkConstants.Localhost, site.InternalPort, (int)ProtocolType.HTTP);

        var response = await dsmApiClient.ExecuteSimpleAsync(createParams);

        if (!response.IsValid())
        {
            logger.LogError("Failed to create reverse proxy for site {SiteName}. API error code: {ApiErrorCode}", site.Name, response?.Error?.Code);
            throw new InvalidOperationException($"Failed to create reverse proxy for site '{site.Name}'");
        }

        var proxy = await FindAsync(p => p.Description == description && p.Frontend.Port == site.PublicPort && p.Backend.Port == site.InternalPort);

        if (proxy is null)
        {
            logger.LogError("Failed to find reverse proxy for site {SiteName}", site.Name);

            throw new InvalidOperationException($"Failed to find reverse proxy for site '{site.Name}'");
        }

        site.IdReverseProxy = proxy.UUID;
    }

    public async Task UpdateAsync(WebSiteConfiguration site)
    {
        logger.LogInformation("Updating reverse proxy for site {SiteName}", site.Name);

        var proxy = await FindAsync(p => p.UUID == site.IdReverseProxy);

        if (proxy is null)
        {
            logger.LogError("Failed to find reverse proxy for site {SiteName}", site.Name);

            throw new InvalidOperationException($"Failed to find reverse proxy for site '{site.Name}'");
        }

        var updateParams = new ReverseProxyUpdateParameters(dsmApiClient.ApiInformations, proxy);
        updateParams.Parameters.Description = GetDescription(site.Name);
        updateParams.Parameters.Frontend = new(site.HostName, site.PublicPort, (int)site.Protocol, new(site.EnableHSTS));
        updateParams.Parameters.Backend = new(NetworkConstants.Localhost, site.InternalPort, (int)ProtocolType.HTTP);

        var response = await dsmApiClient.ExecuteSimpleAsync(updateParams);

        if (!response.IsValid())
        {
            var errorMessage = $"Failed to update reverse proxy for site '{site.Name}'. API error code: {response?.Error?.Code}";
            throw new InvalidOperationException(errorMessage);
        }

        logger.LogInformation("reverse proxy for site {SiteName} updated successfully.", site.Name);
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

        logger.LogInformation("Deleting reverse proxy for site {SiteName}", site.Name);

        var deleteParams = new ReverseProxyDeleteParameters(dsmApiClient.ApiInformations);
        deleteParams.Parameters.Add(site.IdReverseProxy.Value);

        var deleteResponse = await dsmApiClient.ExecuteSimpleAsync(deleteParams);

        if (!deleteResponse.IsValid())
        {
            var errorMessage = $"Failed to delete reverse proxy for site '{site.Name}'. API error code: {deleteResponse?.Error?.Code}";
            throw new InvalidOperationException(errorMessage);
        }
    }

    private async Task<ReverseProxy?> FindAsync(Func<ReverseProxy, bool> predicate)
    {
        logger.LogInformation("Finding a reverse proxy...");

        var parameters = new ReverseProxyListParameters(dsmApiClient.ApiInformations);
        var list = await dsmApiClient.ExecuteAsync<ReverseProxyListResponse>(parameters);

        return list?.Data?.Entries?.FirstOrDefault(predicate);
    }
}
