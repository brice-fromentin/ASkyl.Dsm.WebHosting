using System.Text.Json;
using Microsoft.Extensions.Logging;
using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Data.WebSites;
using Askyl.Dsm.WebHosting.Tools.Threading;

namespace Askyl.Dsm.WebHosting.Tools.WebSites;

using Askyl.Dsm.WebHosting.Tools.Network;

public class WebSitesConfigurationService(ILogger<WebSitesConfigurationService> logger, DsmApiClient dsmApiClient) : IWebSitesConfigurationService
{
    #region Fields

    private readonly string _configurationFilePath = Path.Combine(AppContext.BaseDirectory, ApplicationConstants.WebSitesConfigFileName);
    private readonly ILogger<WebSitesConfigurationService> _logger = logger;
    private readonly DsmApiClient _dsmApiClient = dsmApiClient;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private WebSitesConfiguration? _cachedConfiguration;

    #endregion

    #region Configuration Management

    private async Task<WebSitesConfiguration> LoadConfigurationAsync()
    {
        try
        {
            if (!File.Exists(_configurationFilePath))
            {
                _logger.LogInformation("Configuration file not found, creating empty collection");
                return new();
            }

            var jsonContent = await File.ReadAllTextAsync(_configurationFilePath);
            var collection = JsonSerializer.Deserialize<WebSitesConfiguration>(jsonContent, _jsonOptions);

            return collection ?? new WebSitesConfiguration();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load configuration from {FilePath}", _configurationFilePath);
            return new();
        }
    }

    private async Task SaveConfigurationAsync(WebSitesConfiguration collection)
    {
        try
        {
            collection.LastModified = DateTime.UtcNow;
            var jsonContent = JsonSerializer.Serialize(collection, _jsonOptions);
            await File.WriteAllTextAsync(_configurationFilePath, jsonContent);

            _logger.LogInformation("Configuration saved successfully to {FilePath}", _configurationFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save configuration to {FilePath}", _configurationFilePath);
            throw;
        }
    }

    private async Task EnsureLoadedAsync()
    {
        if (_cachedConfiguration == null)
        {
            _cachedConfiguration = await LoadConfigurationAsync();
            _logger.LogInformation("Configuration loaded and cached. Found {SiteCount} sites", _cachedConfiguration.Sites.Count);
        }
    }

    #endregion

    #region Site Retrieval

    public async Task<WebSiteConfiguration?> GetSiteAsync(Guid siteId)
    {
        using (await SemaphoreLock.AcquireAsync(_semaphore, EnsureLoadedAsync))
        {
            return _cachedConfiguration!.Sites.FirstOrDefault(s => s.Id == siteId);
        }
    }

    public async Task<IEnumerable<WebSiteConfiguration>> GetAllSitesAsync()
    {
        using (await SemaphoreLock.AcquireAsync(_semaphore, EnsureLoadedAsync))
        {
            return [.. _cachedConfiguration!.Sites];
        }
    }

    public async Task<IEnumerable<WebSiteConfiguration>> GetSitesToStartAsync()
    {
        using (await SemaphoreLock.AcquireAsync(_semaphore, EnsureLoadedAsync))
        {
            return [.. _cachedConfiguration!.Sites.Where(site => site.IsEnabled && site.AutoStart)];
        }
    }

    #endregion

    #region Site Modification

    public async Task AddSiteAsync(WebSiteConfiguration site)
    {
        using (await SemaphoreLock.AcquireAsync(_semaphore, EnsureLoadedAsync))
        {
            if (_cachedConfiguration!.Sites.Any(s => s.Name == site.Name))
            {
                throw new InvalidOperationException($"Site with name '{site.Name}' already exists");
            }

            site.Id = Guid.NewGuid();
            _cachedConfiguration.Sites.Add(site);
            await SaveConfigurationAsync(_cachedConfiguration);
        }
    }

    public async Task UpdateSiteAsync(WebSiteConfiguration site)
    {
        using (await SemaphoreLock.AcquireAsync(_semaphore, EnsureLoadedAsync))
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
            await SaveConfigurationAsync(_cachedConfiguration);
        }
    }

    public async Task RemoveSiteAsync(Guid siteId)
    {
        using (await SemaphoreLock.AcquireAsync(_semaphore, EnsureLoadedAsync))
        {
            var site = _cachedConfiguration!.Sites.FirstOrDefault(s => s.Id == siteId) ?? throw new InvalidOperationException($"Site with Id '{siteId}' not found");
            _cachedConfiguration.Sites.Remove(site);
            await SaveConfigurationAsync(_cachedConfiguration);
        }
    }

    #endregion
}