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

    private readonly string _configurationFilePath = Path.Combine(AppContext.BaseDirectory, WebSiteConstants.ConfigurationFileName);

    private WebSitesConfiguration? _cachedConfiguration;

    private bool _initialized = false;

    #endregion

    #region Initialization

    /// <summary>
    /// Ensures service is initialized and configuration is loaded. Called as callback after acquiring semaphore.
    /// </summary>
    private async Task EnsureInitializedAndLoadedAsync(CancellationToken cancellationToken)
    {
        // Initialize if not already done (thread-safe because called within semaphore lock)
        if (!_initialized)
        {
            await EnsureServiceInitializationAsync();
        }

        // Load configuration if not already loaded
        if (_cachedConfiguration == null)
        {
            _cachedConfiguration = await LoadConfigurationAsync(cancellationToken);
            logger.LogInformation("Configuration loaded and cached. Found {SiteCount} sites", _cachedConfiguration.Sites.Count);
        }
    }

    /// <summary>
    /// Validates critical paths and initializes the service.
    /// </summary>
    private async Task EnsureServiceInitializationAsync()
    {
        var baseDirectory = AppContext.BaseDirectory;

        if (!Directory.Exists(baseDirectory))
        {
            throw new DirectoryNotFoundException($"Application base directory does not exist: {baseDirectory}");
        }

        // Test write access to base directory (needed for configuration file)
        try
        {
            var testPath = Path.Combine(baseDirectory, ".write_test");
            File.WriteAllText(testPath, "");
            File.Delete(testPath);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new UnauthorizedAccessException($"Insufficient permissions to write to application directory: {baseDirectory}", ex);
        }

        logger.LogDebug("Service initialization completed successfully. Base directory: {BaseDirectory}", baseDirectory);
        _initialized = true;
    }

    #endregion

    #region Configuration Management

    private async Task<WebSitesConfiguration> LoadConfigurationAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_configurationFilePath))
        {
            logger.LogInformation("Configuration file not found, creating empty collection");
            return new();
        }

        try
        {
            var jsonContent = await File.ReadAllTextAsync(_configurationFilePath, cancellationToken);

            if (String.IsNullOrWhiteSpace(jsonContent))
            {
                logger.LogWarning("Configuration file is empty, creating new collection");
                return new();
            }

            var collection = JsonSerializer.Deserialize<WebSitesConfiguration>(jsonContent, JsonOptionsCache.WriteIndented);

            if (collection is null)
            {
                logger.LogWarning("Configuration deserialization returned null, creating new collection");
                return new();
            }

            logger.LogDebug("Configuration loaded successfully with {SiteCount} sites", collection.Sites.Count);
            return collection;
        }
        catch (JsonException jsonEx)
        {
            logger.LogError(jsonEx, "Configuration file is corrupted (invalid JSON). Backup created and new empty configuration initialized");

            await HandleCorruptedConfigurationAsync();
            return new();
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

    /// <summary>
    /// Handles corrupted configuration file by creating a backup with timestamp.
    /// </summary>
    private async Task HandleCorruptedConfigurationAsync()
    {
        try
        {
            var backupPath = $"{_configurationFilePath}.corrupted.{DateTime.UtcNow:yyyyMMddHHmmss}.bak";
            File.Move(_configurationFilePath, backupPath);
            logger.LogInformation("Corrupted configuration backed up to {BackupPath}", backupPath);
        }
        catch (Exception backupEx)
        {
            logger.LogWarning(backupEx, "Failed to create backup of corrupted configuration");
        }
    }

    #endregion

    #region Site Retrieval

    public async Task<WebSiteConfiguration?> GetSiteAsync(Guid siteId, CancellationToken cancellationToken = default)
    {
        using (await SemaphoreLock.AcquireAsync(this, () => EnsureInitializedAndLoadedAsync(cancellationToken), cancellationToken))
        {
            return _cachedConfiguration!.Sites.FirstOrDefault(s => s.Id == siteId);
        }
    }

    public async Task<IEnumerable<WebSiteConfiguration>> GetAllSitesAsync(CancellationToken cancellationToken = default)
    {
        using (await SemaphoreLock.AcquireAsync(this, () => EnsureInitializedAndLoadedAsync(cancellationToken), cancellationToken))
        {
            return [.. _cachedConfiguration!.Sites];
        }
    }

    public async Task<IEnumerable<WebSiteConfiguration>> GetSitesToStartAsync(CancellationToken cancellationToken = default)
    {
        using (await SemaphoreLock.AcquireAsync(this, () => EnsureInitializedAndLoadedAsync(cancellationToken), cancellationToken))
        {
            return [.. _cachedConfiguration!.Sites.Where(site => site.IsEnabled && site.AutoStart)];
        }
    }

    #endregion

    #region Site Modification

    public async Task AddSiteAsync(WebSiteConfiguration site, CancellationToken cancellationToken = default)
    {
        using (await SemaphoreLock.AcquireAsync(this, () => EnsureInitializedAndLoadedAsync(cancellationToken), cancellationToken))
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
        using (await SemaphoreLock.AcquireAsync(this, () => EnsureInitializedAndLoadedAsync(cancellationToken), cancellationToken))
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
        using (await SemaphoreLock.AcquireAsync(this, () => EnsureInitializedAndLoadedAsync(cancellationToken), cancellationToken))
        {
            var site = _cachedConfiguration!.Sites.FirstOrDefault(s => s.Id == siteId) ?? throw new InvalidOperationException($"Site with Id '{siteId}' not found");

            _cachedConfiguration.Sites.Remove(site);

            await SaveConfigurationAsync(_cachedConfiguration, cancellationToken);
        }
    }

    #endregion
}
