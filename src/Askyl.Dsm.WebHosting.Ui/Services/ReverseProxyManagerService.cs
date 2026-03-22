using Askyl.Dsm.WebHosting.Constants.API;
using Askyl.Dsm.WebHosting.Constants.Network;
using Askyl.Dsm.WebHosting.Constants.UI;

using Askyl.Dsm.WebHosting.Data.API.Definitions.ReverseProxy;
using Askyl.Dsm.WebHosting.Data.API.Parameters.ReverseProxyAPI;
using Askyl.Dsm.WebHosting.Data.API.Responses;
using Askyl.Dsm.WebHosting.Data.Exceptions;
using Askyl.Dsm.WebHosting.Data.Services;
using Askyl.Dsm.WebHosting.Data.WebSites;

using Askyl.Dsm.WebHosting.Tools.Extensions;
using Askyl.Dsm.WebHosting.Tools.Network;

namespace Askyl.Dsm.WebHosting.Ui.Services;

public class ReverseProxyManagerService(
    ILogger<ReverseProxyManagerService> logger,
    DsmApiClient dsmApiClient) : IReverseProxyManagerService
{
    #region Public API

    /// <summary>
    /// Creates a reverse proxy for the specified site with idempotency check.
    /// </summary>
    public async Task CreateAsync(WebSiteConfiguration site)
    {
        logger.LogInformation("Creating reverse proxy for site {SiteName}", site.Name);

        // Idempotency check: See if proxy already exists using composite key
        var existingProxy = await FindByCompositeKeyAsync(site);

        if (existingProxy is not null)
        {
            logger.LogWarning("Reverse proxy already exists for site {SiteName} with UUID {Uuid}.", site.Name, existingProxy.UUID);
            return;
        }

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

        // Verify creation by searching using composite key (more reliable than relying on response UUID)
        var createdProxy = await FindByCompositeKeyAsync(site);

        if (createdProxy is null)
        {
            logger.LogError("Reverse proxy creation succeeded but could not verify existence for site {SiteName}", site.Name);
            throw new InvalidOperationException($"Reverse proxy creation succeeded but could not verify existence for site '{site.Name}'");
        }

        logger.LogInformation("Reverse proxy created successfully for site {SiteName} with UUID {Uuid}", site.Name, createdProxy.UUID);
    }

    /// <summary>
    /// Updates a reverse proxy using composite key lookup.
    /// </summary>
    public async Task UpdateAsync(WebSiteConfiguration config)
    {
        logger.LogInformation("Updating reverse proxy for site {SiteName}", config.Name);

        // Find the proxy using composite key (backend port + frontend config)
        var proxy = await FindByCompositeKeyAsync(config) ?? throw new ReverseProxyNotFoundException($"Reverse proxy not found for site '{config.Name}'. You may need to recreate it.");

        // Update using the found proxy's UUID with NEW configuration values
        var updateParams = new ReverseProxyUpdateParameters(dsmApiClient.ApiInformations, proxy);
        updateParams.Parameters.Description = GetDescription(config.Name);
        updateParams.Parameters.Frontend = new(config.HostName, config.PublicPort, (int)config.Protocol, new(config.EnableHSTS));
        updateParams.Parameters.Backend = new(NetworkConstants.Localhost, config.InternalPort, (int)ProtocolType.HTTP);

        var response = await dsmApiClient.ExecuteSimpleAsync(updateParams);

        if (!response.IsValid())
        {
            throw new InvalidOperationException($"Failed to update reverse proxy for site '{config.Name}'. API error code: {response?.Error?.Code}");
        }

        logger.LogInformation("Reverse proxy updated successfully for site {SiteName}", config.Name);
    }

    /// <summary>
    /// Deletes a reverse proxy using composite key lookup.
    /// </summary>
    public async Task DeleteAsync(WebSiteConfiguration site)
    {
        if (site is null)
        {
            throw new ArgumentNullException(nameof(site));
        }

        // Find the proxy using composite key
        var proxy = await FindByCompositeKeyAsync(site);

        if (proxy is null || !proxy.UUID.HasValue)
        {
            logger.LogInformation("No reverse proxy found for site {SiteName}. Nothing to delete.", site.Name);
            return; // Graceful no-op
        }

        logger.LogInformation("Deleting reverse proxy {Uuid} for site {SiteName}", proxy.UUID, site.Name);

        try
        {
            await DeleteByUuidAsync(proxy.UUID.Value, site.Name);
        }
        catch (Exception ex) when (IsNotFoundError(ex.Message))
        {
            // Already deleted externally - graceful handling
            logger.LogWarning("Reverse proxy for site {SiteName} was already deleted.", site.Name);
            return; // Graceful no-op
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Finds a reverse proxy using backend port + full frontend configuration.
    /// This is the primary identification method (no UUID storage).
    /// </summary>
    private async Task<ReverseProxy?> FindByCompositeKeyAsync(WebSiteConfiguration config)
    {
        var allProxies = await GetAllReverseProxiesAsync();

        // MATCH: Backend port + complete frontend configuration
        // This combination uniquely identifies a reverse proxy entry
        return allProxies.FirstOrDefault(p =>
            p.Backend.Port == config.InternalPort &&
            String.Equals(p.Frontend.Fqdn, config.HostName, StringComparison.OrdinalIgnoreCase) &&
            p.Frontend.Port == config.PublicPort &&
            p.Frontend.Protocol == (int)config.Protocol);
    }

    /// <summary>
    /// Gets all reverse proxies from DSM.
    /// </summary>
    private async Task<List<ReverseProxy>> GetAllReverseProxiesAsync()
    {
        var parameters = new ReverseProxyListParameters(dsmApiClient.ApiInformations);
        var response = await dsmApiClient.ExecuteAsync<ReverseProxyListResponse>(parameters);

        return response?.Data?.Entries ?? [];
    }

    /// <summary>
    /// Deletes a reverse proxy by UUID.
    /// </summary>
    private async Task DeleteByUuidAsync(Guid uuid, string siteName)
    {
        var deleteParams = new ReverseProxyDeleteParameters(dsmApiClient.ApiInformations);
        deleteParams.Parameters.Add(uuid);

        var deleteResponse = await dsmApiClient.ExecuteSimpleAsync(deleteParams);

        if (!deleteResponse.IsValid())
        {
            if (IsNotFoundError(deleteResponse?.Error?.Code))
            {
                throw new InvalidOperationException($"Reverse proxy with UUID {uuid} not found (already deleted?)");
            }

            throw new InvalidOperationException($"Failed to delete reverse proxy for site '{siteName}'. API error code: {deleteResponse?.Error?.Code}");
        }

        logger.LogInformation("Deleted reverse proxy {Uuid} successfully", uuid);
    }

    /// <summary>
    /// Checks if an error code indicates a "not found" scenario.
    /// </summary>
    private static bool IsNotFoundError(int? errorCode)
    {
        if (!errorCode.HasValue)
        {
            return false;
        }

        // Common DSM API error codes for "not found"
        return errorCode.Value is ReverseProxyConstants.ErrorCodeNotFound or
               ReverseProxyConstants.ErrorCodeGenericNotFound or
               ReverseProxyConstants.ErrorCodeResourceNotFound;
    }

    /// <summary>
    /// Checks if an error message indicates a "not found" scenario.
    /// </summary>
    private static bool IsNotFoundError(string message)
    {
        if (String.IsNullOrEmpty(message))
        {
            return false;
        }

        return message.IndexOf("not found", StringComparison.OrdinalIgnoreCase) >= 0 ||
               message.IndexOf("does not exist", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>
    /// Gets the description for a reverse proxy entry.
    /// </summary>
    private static string GetDescription(string siteName) => $"{ReverseProxyConstants.DescriptionPrefix}{siteName}";

    #endregion
}
