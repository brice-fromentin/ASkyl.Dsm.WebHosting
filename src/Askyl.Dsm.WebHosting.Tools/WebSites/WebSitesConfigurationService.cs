using System.Text.Json;
using Microsoft.Extensions.Logging;
using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Data.WebSites;

namespace Askyl.Dsm.WebHosting.Tools.WebSites;

public class WebSitesConfigurationService(ILogger<WebSitesConfigurationService> logger) : IWebSitesConfigurationService
{
    private readonly string _configurationFilePath = InitializeConfigurationPath();
    private readonly ILogger<WebSitesConfigurationService> _logger = logger;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private WebSitesConfiguration? _cachedConfiguration;

    private static string InitializeConfigurationPath()
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, ApplicationConstants.WebSitesConfigFileName);
        var configDirectory = Path.GetDirectoryName(configPath);
        if (!String.IsNullOrEmpty(configDirectory))
        {
            Directory.CreateDirectory(configDirectory);
        }
        return configPath;
    }

    public async Task<WebSitesConfiguration> LoadConfigurationAsync()
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

    public async Task SaveConfigurationAsync(WebSitesConfiguration collection)
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

    public async Task EnsureLoadedAsync()
    {
        if (_cachedConfiguration == null)
        {
            _cachedConfiguration = await LoadConfigurationAsync();
            _logger.LogInformation("Configuration loaded and cached. Found {SiteCount} sites", _cachedConfiguration.Sites.Count);
        }
    }

    public async Task<WebSiteConfiguration?> GetSiteAsync(string siteName)
    {
        await EnsureLoadedAsync();
        return _cachedConfiguration!.Sites.FirstOrDefault(s => s.Name == siteName);
    }

    public async Task<IEnumerable<WebSiteConfiguration>> GetAllSitesAsync()
    {
        await EnsureLoadedAsync();
        return _cachedConfiguration!.Sites;
    }

    public async Task<IEnumerable<WebSiteConfiguration>> GetSitesToStartAsync()
    {
        await EnsureLoadedAsync();
        return _cachedConfiguration!.Sites.Where(site => site.IsEnabled && site.AutoStart);
    }

    public async Task AddSiteAsync(WebSiteConfiguration site)
    {
        await EnsureLoadedAsync();

        if (_cachedConfiguration!.Sites.Any(s => s.Name == site.Name))
        {
            throw new InvalidOperationException($"Site with name '{site.Name}' already exists");
        }

        _cachedConfiguration.Sites.Add(site);
        await SaveConfigurationAsync(_cachedConfiguration);
    }

    public async Task UpdateSiteAsync(WebSiteConfiguration site)
    {
        await EnsureLoadedAsync();
        var existingSiteIndex = _cachedConfiguration!.Sites.FindIndex(s => s.Name == site.Name);

        if (existingSiteIndex == -1)
        {
            throw new InvalidOperationException($"Site with name '{site.Name}' not found");
        }

        _cachedConfiguration.Sites[existingSiteIndex] = site;
        await SaveConfigurationAsync(_cachedConfiguration);
    }

    public async Task RemoveSiteAsync(string siteName)
    {
        await EnsureLoadedAsync();
        var site = _cachedConfiguration!.Sites.FirstOrDefault(s => s.Name == siteName);

        if (site == null)
        {
            throw new InvalidOperationException($"Site with name '{siteName}' not found");
        }

        _cachedConfiguration.Sites.Remove(site);
        await SaveConfigurationAsync(_cachedConfiguration);
    }

    public async Task<bool> SiteExistsAsync(string siteName)
    {
        await EnsureLoadedAsync();
        return _cachedConfiguration!.Sites.Any(s => s.Name == siteName);
    }
}