using System.Text.Json;
using System.Text.RegularExpressions;
using Askyl.Dsm.WebHosting.Constants.DSM.API;
using Askyl.Dsm.WebHosting.Constants.Network;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Domain.WebSites;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.ReverseProxy;
using Askyl.Dsm.WebHosting.Data.DsmApi.Parameters.Core.AppPortal.ReverseProxy;
using Askyl.Dsm.WebHosting.Data.DsmApi.Responses.Core.AppPortal.ReverseProxy;
using Askyl.Dsm.WebHosting.Data.Exceptions;
using Askyl.Dsm.WebHosting.Logging;
using Askyl.Dsm.WebHosting.Tools.Extensions;

namespace Askyl.Dsm.WebHosting.Ui.Services;

public partial class ReverseProxyManagerService(
    ILogger<ILogReverseProxyManagerService> logger,
    IDsmSession dsmSession) : IReverseProxyManagerService
{
    #region Public API

    /// <summary>
    /// Creates a reverse proxy for the specified site with idempotency check.
    /// </summary>
    public async Task CreateAsync(WebSiteConfiguration site, CancellationToken cancellationToken = default)
    {
        logger.CreatingReverseProxy(site.Name);

        // Idempotency check: See if proxy already exists using composite key
        var existingProxy = await FindByCompositeKeyAsync(site, cancellationToken);

        if (existingProxy is not null)
        {
            logger.ReverseProxyAlreadyExists(site.Name, existingProxy.UUID);
            return;
        }

        var description = GetDescription(site.Name);
        var proxy = new ReverseProxy
        {
            Description = description,
            Frontend = new(site.HostName, site.PublicPort, (int)site.Protocol, new(site.EnableHSTS)),
            Backend = new(NetworkConstants.Localhost, site.InternalPort, (int)ProtocolType.HTTP)
        };

        var createParams = new ReverseProxyCreateParameters(proxy);

        var response = await dsmSession.ExecuteSimpleAsync(createParams, cancellationToken);

        if (!response.IsValid())
        {
            logger.FailedToCreateReverseProxy(site.Name, response?.Error?.Code);
            throw new InvalidOperationException($"Failed to create reverse proxy for site '{site.Name}'");
        }

        // Verify creation by searching using composite key (more reliable than relying on response UUID)
        var createdProxy = await FindByCompositeKeyAsync(site, cancellationToken);

        if (createdProxy is null)
        {
            logger.ReverseProxyCreationNotVerified(site.Name);
            throw new InvalidOperationException($"Reverse proxy creation succeeded but could not verify existence for site '{site.Name}'");
        }

        logger.ReverseProxyCreated(site.Name, createdProxy.UUID);
    }

    /// <summary>
    /// Updates a reverse proxy using composite key lookup.
    /// </summary>
    public async Task UpdateAsync(WebSiteConfiguration config, CancellationToken cancellationToken = default)
    {
        logger.UpdatingReverseProxy(config.Name);

        // Find the proxy using composite key (backend port + frontend config)
        var proxy = await FindByCompositeKeyAsync(config, cancellationToken) ?? throw new ReverseProxyNotFoundException($"Reverse proxy not found for site '{config.Name}'. You may need to recreate it.");

        // Update using record 'with' expression — preserves all existing properties
        var updatedProxy = proxy with
        {
            Description = GetDescription(config.Name),
            Frontend = new(config.HostName, config.PublicPort, (int)config.Protocol, new(config.EnableHSTS)),
            Backend = new(NetworkConstants.Localhost, config.InternalPort, (int)ProtocolType.HTTP)
        };

        var updateParams = new ReverseProxyUpdateParameters(updatedProxy);

        var response = await dsmSession.ExecuteSimpleAsync(updateParams, cancellationToken);

        if (!response.IsValid())
        {
            var exception = new InvalidOperationException($"Failed to update reverse proxy for site '{config.Name}'. API error code: {response?.Error?.Code}");
            logger.FailedToUpdateReverseProxy(exception, config.Name);
            throw exception;
        }

        logger.ReverseProxyUpdated(config.Name);
    }

    /// <summary>
    /// Deletes a reverse proxy using composite key lookup.
    /// </summary>
    public async Task DeleteAsync(WebSiteConfiguration site, CancellationToken cancellationToken = default)
    {
        if (site is null)
        {
            throw new ArgumentNullException(nameof(site));
        }

        // Find the proxy using composite key
        var proxy = await FindByCompositeKeyAsync(site, cancellationToken);

        if (proxy is null || !proxy.UUID.HasValue)
        {
            logger.NoReverseProxyToDelete(site.Name);
            return; // Graceful no-op
        }

        logger.DeletingReverseProxy(proxy.UUID, site.Name);

        try
        {
            await DeleteByUuidAsync((Guid)proxy.UUID, site.Name, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.FailedToDeleteReverseProxy(ex, proxy.UUID, site.Name);
            throw;
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Finds a reverse proxy using composite key (backend port + frontend configuration).
    /// This is the primary identification method (no UUID storage).
    /// </summary>
    private async Task<ReverseProxy?> FindByCompositeKeyAsync(WebSiteConfiguration config, CancellationToken cancellationToken)
    {
        var allProxies = await GetAllReverseProxiesAsync(cancellationToken);

        return allProxies.FirstOrDefault(p =>
            p.Backend.Port == config.InternalPort &&
            String.Equals(p.Frontend.Fqdn, config.HostName, StringComparison.OrdinalIgnoreCase) &&
            p.Frontend.Port == config.PublicPort &&
            p.Frontend.Protocol == (int)config.Protocol);
    }

    /// <summary>
    /// Gets all reverse proxies from DSM.
    /// </summary>
    private async Task<List<ReverseProxy>> GetAllReverseProxiesAsync(CancellationToken cancellationToken)
    {
        var parameters = new ReverseProxyListParameters();
        var response = await dsmSession.ExecuteAsync<ReverseProxyListResponse>(parameters, cancellationToken);

        return response?.Data?.Entries ?? [];
    }

    /// <summary>
    /// Deletes a reverse proxy by UUID.
    /// </summary>
    private async Task DeleteByUuidAsync(Guid uuid, string siteName, CancellationToken cancellationToken)
    {
        var deleteParams = new ReverseProxyDeleteParameters();
        deleteParams.Parameters.Add(uuid);

        var deleteResponse = await dsmSession.ExecuteSimpleAsync(deleteParams, cancellationToken);

        if (!deleteResponse.IsValid())
        {
            throw new InvalidOperationException($"Failed to delete reverse proxy for site '{siteName}'. API error code: {deleteResponse?.Error?.Code}");
        }

        logger.ReverseProxyDeleted(uuid);
    }

    /// <summary>
    /// Gets the description for a reverse proxy entry.
    /// </summary>
    private static string GetDescription(string siteName) => $"{ReverseProxyConstants.DescriptionPrefix}{siteName}";

    #endregion
}
