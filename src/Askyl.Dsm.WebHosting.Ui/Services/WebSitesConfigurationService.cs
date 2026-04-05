using System.Text.Json;
using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Constants.JSON;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Domain.WebSites;
using Askyl.Dsm.WebHosting.Tools.Threading;

namespace Askyl.Dsm.WebHosting.Ui.Services;

public class WebSitesConfigurationService(ILogger<WebSitesConfigurationService> logger) : IWebSitesConfigurationService, ISemaphoreOwner
{
    #region ISemaphoreOwner Implementation

    public SemaphoreSlim Semaphore { get; } = new(1, 1);

    #endregion

    #region Fields

    private readonly string _configurationFilePath = Path.Combine(AppContext.BaseDirectory, ApplicationConstants.WebSitesConfigFileName);

    private WebSitesConfiguration? _cachedConfiguration;

    #endregion

    #region Configuration Management

    private async Task<WebSitesConfiguration> LoadConfigurationAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(_configurationFilePath))
            {
                logger.LogInformation("Configuration file not found, creating empty collection");

                return new();
            }

            var jsonContent = await File.ReadAllTextAsync(_configurationFilePath, cancellationToken);
            var collection = JsonSerializer.Deserialize<WebSitesConfiguration>(jsonContent, JsonOptionsCache.WriteIndented);

            return collection ?? new WebSitesConfiguration();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load configuration from {FilePath}", _configurationFilePath);

            return new();
        }
    }

    private async Task SaveConfigurationAsync(WebSitesConfiguration collection, CancellationToken cancellationToken)
    {
        try
        {
            collection.LastModified = DateTime.UtcNow;
            var jsonContent = JsonSerializer.Serialize(collection, JsonOptionsCache.WriteIndented);

            await File.WriteAllTextAsync(_configurationFilePath, jsonContent, cancellationToken);

            logger.LogInformation("Configuration saved successfully to {FilePath}", _configurationFilePath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save configuration to {FilePath}", _configurationFilePath);

            throw;
        }
    }

    private async Task EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (_cachedConfiguration == null)
        {
            _cachedConfiguration = await LoadConfigurationAsync(cancellationToken);
            logger.LogInformation("Configuration loaded and cached. Found {SiteCount} sites", _cachedConfiguration.Sites.Count);
        }
    }

    #endregion

    #region Site Retrieval

    public async Task<WebSiteConfiguration?> GetSiteAsync(Guid siteId, CancellationToken cancellationToken = default)
    {
        using (await SemaphoreLock.AcquireAsync(this, () => EnsureLoadedAsync(cancellationToken), cancellationToken))
        {
            return _cachedConfiguration!.Sites.FirstOrDefault(s => s.Id == siteId);
        }
    }

    public async Task<IEnumerable<WebSiteConfiguration>> GetAllSitesAsync(CancellationToken cancellationToken = default)
    {
        using (await SemaphoreLock.AcquireAsync(this, () => EnsureLoadedAsync(cancellationToken), cancellationToken))
        {
            return [.. _cachedConfiguration!.Sites];
        }
    }

    public async Task<IEnumerable<WebSiteConfiguration>> GetSitesToStartAsync(CancellationToken cancellationToken = default)
    {
        using (await SemaphoreLock.AcquireAsync(this, () => EnsureLoadedAsync(cancellationToken), cancellationToken))
        {
            return [.. _cachedConfiguration!.Sites.Where(site => site.IsEnabled && site.AutoStart)];
        }
    }

    #endregion

    #region Site Modification

    public async Task AddSiteAsync(WebSiteConfiguration site, CancellationToken cancellationToken = default)
    {
        using (await SemaphoreLock.AcquireAsync(this, () => EnsureLoadedAsync(cancellationToken), cancellationToken))
        {
            if (_cachedConfiguration!.Sites.Any(s => s.Name == site.Name))
            {
                throw new InvalidOperationException($"Site with name '{site.Name}' already exists");
            }

            site.Id = Guid.NewGuid();
            _cachedConfiguration.Sites.Add(site);

            await SaveConfigurationAsync(_cachedConfiguration, cancellationToken);
        }
    }

    public async Task UpdateSiteAsync(WebSiteConfiguration site, CancellationToken cancellationToken = default)
    {
        using (await SemaphoreLock.AcquireAsync(this, () => EnsureLoadedAsync(cancellationToken), cancellationToken))
        {
            var existingSiteIndex = _cachedConfiguration!.Sites.FindIndex(s => s.Id == site.Id);

            if (existingSiteIndex == -1)
            {
                throw new InvalidOperationException($"Site with Id '{site.Id}' not found");
            }

            if (_cachedConfiguration.Sites.Any(s => s.Name == site.Name && s.Id != site.Id))
            {
                throw new InvalidOperationException($"Site with name '{site.Name}' already exists");
            }

            _cachedConfiguration.Sites[existingSiteIndex] = site;

            await SaveConfigurationAsync(_cachedConfiguration, cancellationToken);
        }
    }

    public async Task RemoveSiteAsync(Guid siteId, CancellationToken cancellationToken = default)
    {
        using (await SemaphoreLock.AcquireAsync(this, () => EnsureLoadedAsync(cancellationToken), cancellationToken))
        {
            var site = _cachedConfiguration!.Sites.FirstOrDefault(s => s.Id == siteId) ?? throw new InvalidOperationException($"Site with Id '{siteId}' not found");

            _cachedConfiguration.Sites.Remove(site);

            await SaveConfigurationAsync(_cachedConfiguration, cancellationToken);
        }
    }

    #endregion
}
