using System.Collections.Concurrent;
using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Domain.WebSites;
using Askyl.Dsm.WebHosting.Data.Results;

namespace Askyl.Dsm.WebHosting.Ui.Services;

/// <summary>
/// Background service that manages website instances and their lifecycle.
/// Orchestrates instance management by delegating to SiteLifecycleManager for process operations.
/// </summary>
public class WebSiteHostingService(
    ILogger<WebSiteHostingService> logger,
    ILoggerFactory loggerFactory,
    IWebSitesConfigurationService configService,
    IFileSystemService fileSystemService,
    IReverseProxyManagerService reverseProxyManager) : BackgroundService, IWebSiteHostingService
{
    #region Fields

    private readonly ConcurrentDictionary<Guid, WebSiteInstance> _instances = new();
    private readonly ConcurrentDictionary<Guid, SiteLifecycleManager> _lifecycleManagers = new();

    #endregion

    #region Public API

    /// <summary>
    /// Gets all website instances with their current runtime status.
    /// Updates runtime state from lifecycle managers before returning.
    /// </summary>
    public async Task<WebSiteInstancesResult> GetAllWebsitesAsync()
    {
        var instances = new List<WebSiteInstance>();

        foreach (var instance in _instances.Values)
        {
            if (_lifecycleManagers.TryGetValue(instance.Id, out var lifecycleManager))
            {
                var runtimeState = await lifecycleManager.GetRuntimeStateAsync();
                UpdateInstanceRuntimeState(instance, runtimeState);
            }

            instances.Add(instance);
        }

        return WebSiteInstancesResult.CreateSuccess(instances);
    }

    /// <summary>
    /// Adds a new website configuration and creates an instance.
    /// </summary>
    public async Task<WebSiteInstanceResult> AddWebsiteAsync(WebSiteConfiguration configuration)
    {
        try
        {
            // STEP 1: Set HTTP group permissions BEFORE adding website (CRITICAL - must succeed)
            var permissionResult = await SetHttpGroupPermissionsForApplicationAsync(configuration);

            if (!permissionResult.Success)
            {
                logger.LogError("Permission setting failed for '{SiteName}': {ErrorMessage}", configuration.Name, permissionResult.Message);
                return WebSiteInstanceResult.CreateFailure($"Failed to set permissions: {permissionResult.Message}");
            }

            // STEP 2: Create reverse proxy rule (CRITICAL - must succeed)
            var proxyResult = await CreateReverseProxyRuleAsync(configuration);

            if (!proxyResult.Success)
            {
                logger.LogError("Reverse proxy creation failed for '{SiteName}': {ErrorMessage}", configuration.Name, proxyResult.Message);
                return WebSiteInstanceResult.CreateFailure($"Failed to create reverse proxy: {proxyResult.Message}");
            }

            // STEP 3: Add website configuration (persistent storage)
            await configService.AddSiteAsync(configuration);

            // STEP 4: Create instance
            var instance = await AddInstanceAsync(configuration);

            return WebSiteInstanceResult.CreateSuccess(instance);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding website: {SiteName}", configuration.Name);
            return WebSiteInstanceResult.CreateFailure($"Failed to add website: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates an existing website configuration and refreshes the instance.
    /// </summary>
    public async Task<WebSiteInstanceResult> UpdateWebsiteAsync(WebSiteConfiguration configuration)
    {
        if (!_instances.TryGetValue(configuration.Id, out var existingInstance))
        {
            logger.LogError("Instance not found for site: {SiteName}", configuration.Name);
            return WebSiteInstanceResult.CreateFailure($"Instance not found for website '{configuration.Name}'");
        }

        try
        {
            // STEP 1: ALWAYS set HTTP group permissions (CRITICAL - must succeed, allows easy repairs)
            var permissionResult = await SetHttpGroupPermissionsForApplicationAsync(configuration);

            if (!permissionResult.Success)
            {
                logger.LogError("Permission setting failed for '{SiteName}': {ErrorMessage}", configuration.Name, permissionResult.Message);
                return WebSiteInstanceResult.CreateFailure($"Failed to set permissions: {permissionResult.Message}");
            }

            // STEP 2: Update reverse proxy rule (CRITICAL - must succeed)
            var proxyResult = await UpdateReverseProxyRuleAsync(configuration);

            if (!proxyResult.Success)
            {
                logger.LogError("Reverse proxy update failed for '{SiteName}': {ErrorMessage}", configuration.Name, proxyResult.Message);
                return WebSiteInstanceResult.CreateFailure($"Failed to update reverse proxy: {proxyResult.Message}");
            }

            // STEP 3: Update configuration (persistent storage)
            await configService.UpdateSiteAsync(configuration);

            // STEP 4: Update instance
            await UpdateInstanceAsync(existingInstance, configuration);

            return WebSiteInstanceResult.CreateSuccess(existingInstance);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating website: {SiteName}", configuration.Name);
            return WebSiteInstanceResult.CreateFailure($"Failed to update website: {ex.Message}");
        }
    }

    /// <summary>
    /// Removes a website by ID.
    /// </summary>
    public async Task<ApiResult> RemoveWebsiteAsync(Guid id)
        => await RemoveInstanceAsync(id);

    /// <summary>
    /// Starts a website by ID. Synchronous — waits for process to start and update runtime state.
    /// </summary>
    public async Task<ApiResult> StartWebsiteAsync(Guid id)
    {
        if (!_instances.TryGetValue(id, out var instance))
        {
            logger.LogWarning("Cannot start site: site with ID {InstanceId} not found", id);
            return ApiResult.CreateFailure($"Site with ID '{id}' not found");
        }

        if (!_lifecycleManagers.TryGetValue(id, out var lifecycleManager))
        {
            logger.LogError("Lifecycle manager not found for site: {SiteId}", id);
            return ApiResult.CreateFailure("Internal error: lifecycle manager not found");
        }

        var result = await lifecycleManager.StartAsync();

        if (result.Success)
        {
            var runtimeState = await lifecycleManager.GetRuntimeStateAsync();
            UpdateInstanceRuntimeState(instance, runtimeState);
        }

        return result;
    }

    /// <summary>
    /// Stops a website by ID. Synchronous — waits for SIGTERM signal and process exit (typically 1-3 seconds).
    /// </summary>
    public async Task<ApiResult> StopWebsiteAsync(Guid id)
    {
        if (!_instances.TryGetValue(id, out var instance))
        {
            logger.LogWarning("Cannot stop site: site with ID {InstanceId} not found", id);
            return ApiResult.CreateFailure($"Site with ID '{id}' not found");
        }

        if (!_lifecycleManagers.TryGetValue(id, out var lifecycleManager))
        {
            logger.LogError("Lifecycle manager not found for site: {SiteId}", id);
            return ApiResult.CreateFailure("Internal error: lifecycle manager not found");
        }

        var result = await lifecycleManager.StopAsync();

        if (result.Success)
        {
            var runtimeState = await lifecycleManager.GetRuntimeStateAsync();
            UpdateInstanceRuntimeState(instance, runtimeState);
        }

        return result;
    }

    #endregion

    #region Service Lifecycle

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("WebSite hosting service starting");

        await InitializeAllInstancesAsync();
        await StartEligibleSitesAsync();

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("WebSite hosting service started");

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Expected when service is stopping
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Stopping all websites");
        await StopAllSitesAsync(stoppingToken);
        await base.StopAsync(stoppingToken);
    }

    #endregion

    #region Initialization

    private async Task InitializeAllInstancesAsync()
    {
        var allSites = await configService.GetAllSitesAsync();

        foreach (var site in allSites)
        {
            var instance = new WebSiteInstance(site);
            _instances[instance.Id] = instance;

            var lifecycleManager = new SiteLifecycleManager(loggerFactory.CreateLogger<SiteLifecycleManager>(), site);
            _lifecycleManagers[instance.Id] = lifecycleManager;

            logger.LogInformation("Instance created for site: {SiteName}", site.Name);
        }

        logger.LogInformation("All instances initialized: {Count} sites", _instances.Count);
    }

    private async Task StartEligibleSitesAsync()
    {
        var results = await Task.WhenAll(
            _instances.Values
                .Where(i => i.Configuration.IsEnabled && i.Configuration.AutoStart)
                .Select(i => StartWebsiteAsync(i.Id)));

        var failures = results.Where(r => !r.Success).ToList();
        if (failures.Count > 0)
        {
            logger.LogWarning("{Count} site(s) failed to start: {Failures}", failures.Count, String.Join(", ", failures.Select(f => f.Message)));
        }
    }

    #endregion

    #region Instance Management

    public async Task<WebSiteInstance> AddInstanceAsync(WebSiteConfiguration configuration)
    {
        var instance = new WebSiteInstance(configuration);
        _instances[instance.Id] = instance;

        var lifecycleManager = new SiteLifecycleManager(loggerFactory.CreateLogger<SiteLifecycleManager>(), configuration);
        _lifecycleManagers[instance.Id] = lifecycleManager;

        logger.LogInformation("Instance added for site: {SiteName}", configuration.Name);

        if (configuration.IsEnabled && configuration.AutoStart)
        {
            await StartWebsiteAsync(instance.Id);
        }

        return instance;
    }

    public async Task UpdateInstanceAsync(WebSiteInstance instance, WebSiteConfiguration newConfiguration)
    {
        if (!_instances.TryGetValue(instance.Id, out var existingInstance))
        {
            logger.LogError("Instance of site: {SiteName} not found.", newConfiguration.Name);
            throw new ArgumentException("Instance not found.", nameof(instance));
        }

        var wasRunning = existingInstance.IsRunning;
        var oldConfiguration = existingInstance.Configuration;

        if (wasRunning && (!newConfiguration.IsEnabled || ConfigurationRequiresRestart(oldConfiguration, newConfiguration)))
        {
            logger.LogInformation("Stopping site: {SiteName} (disabled or restart required)", newConfiguration.Name);
            await StopWebsiteAsync(existingInstance.Id);
        }

        // Recreate lifecycle manager with new configuration to avoid stale config
        if (_lifecycleManagers.TryRemove(instance.Id, out var oldManager))
        {
            oldManager.Dispose();
        }

        existingInstance.Configuration = newConfiguration;
        _lifecycleManagers[instance.Id] = new SiteLifecycleManager(
            loggerFactory.CreateLogger<SiteLifecycleManager>(), newConfiguration);

        logger.LogInformation("Instance updated for site: {SiteName}", newConfiguration.Name);

        if (newConfiguration.IsEnabled && (wasRunning || newConfiguration.AutoStart))
        {
            await StartWebsiteAsync(existingInstance.Id);
        }
    }

    private static bool ConfigurationRequiresRestart(WebSiteConfiguration oldConfig, WebSiteConfiguration newConfig)
    {
        return oldConfig.ApplicationPath != newConfig.ApplicationPath
            || oldConfig.InternalPort != newConfig.InternalPort
            || oldConfig.Environment != newConfig.Environment
            || oldConfig.AdditionalEnvironmentVariables.Count != newConfig.AdditionalEnvironmentVariables.Count
            || !oldConfig.AdditionalEnvironmentVariables.All(
                kvp => newConfig.AdditionalEnvironmentVariables.TryGetValue(kvp.Key, out var value)
                    && value == kvp.Value);
    }

    #endregion

    #region Instance Lifecycle Operations

    public async Task<ApiResult> RemoveInstanceAsync(Guid instanceId)
    {
        if (!_instances.TryGetValue(instanceId, out var instance))
        {
            logger.LogWarning("Cannot remove instance: instance {InstanceId} not found", instanceId);
            return ApiResult.CreateFailure("Instance not found");
        }

        var siteName = instance.Configuration.Name;

        try
        {
            // Stop lifecycle manager if running (without removing from dictionary yet)
            if (_lifecycleManagers.TryGetValue(instanceId, out var lifecycleManager))
            {
                if (instance.IsRunning)
                {
                    var stopResult = await lifecycleManager.StopAsync();

                    if (!stopResult.Success && stopResult.ErrorCode != ApiErrorCode.NotFound)
                    {
                        logger.LogWarning("Failed to stop site before deletion: {ErrorMessage}", stopResult.Message);
                        // Continue with cleanup anyway
                    }
                }
            }

            // Delete reverse proxy rule (best effort - log but don't fail if it errors)
            var proxyDeleteResult = await DeleteReverseProxyRuleAsync(instance.Configuration);

            if (!proxyDeleteResult.Success)
            {
                logger.LogWarning("Reverse proxy deletion failed for '{SiteName}' (site will be removed anyway): {ErrorMessage}", siteName, proxyDeleteResult.Message);
            }

            // Remove configuration (persistent storage) — MUST succeed before removing from memory
            await configService.RemoveSiteAsync(instance.Configuration.Id);

            // Safe to remove from memory now — persistent config is gone
            _ = _instances.TryRemove(instanceId, out _);

            if (_lifecycleManagers.TryRemove(instanceId, out lifecycleManager))
            {
                lifecycleManager.Dispose();
            }

            logger.LogInformation("Instance removed for site: {SiteName}", siteName);
            return ApiResult.CreateSuccess();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to remove site: {SiteName}", siteName);
            return ApiResult.CreateFailure($"Failed to remove site: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates the runtime state of a WebSiteInstance from WebSiteRuntimeState.
    /// Helper method for synchronizing instance state with lifecycle manager state.
    /// </summary>
    private static void UpdateInstanceRuntimeState(WebSiteInstance instance, WebSiteRuntimeState runtimeState)
    {
        instance.IsRunning = runtimeState.IsRunning;
        instance.Process = runtimeState.ProcessDetails;
    }

    #endregion

    #region Bulk Operations

    private async Task StopAllSitesAsync(CancellationToken cancellationToken)
    {
        var stopTasks = _lifecycleManagers.Values.Select(
            async m =>
            {
                try
                {
                    await m.StopAsync(cancellationToken);
                }
                finally
                {
                    m.Dispose();
                }
            })
            .ToList();
        await Task.WhenAll(stopTasks);

        _lifecycleManagers.Clear();
        _instances.Clear();
    }

    #endregion

    #region Permission Management

    /// <summary>
    /// Sets HTTP group permissions for the application path in the configuration.
    /// </summary>
    private async Task<ApiResult> SetHttpGroupPermissionsForApplicationAsync(WebSiteConfiguration configuration)
    {
        if (String.IsNullOrEmpty(configuration.ApplicationRealPath))
        {
            logger.LogWarning("No application real path configured for '{SiteName}', skipping permission setting", configuration.Name);
            return ApiResult.CreateFailure("No application path configured");
        }

        // Determine if the target is a directory (if ApplicationPath ends with .dll, set permissions on parent directory)
        var isDirectory = !configuration.ApplicationRealPath.EndsWith(WebSiteConstants.DllFileExtension, StringComparison.OrdinalIgnoreCase);

        logger.LogDebug("Setting HTTP group permissions for '{SiteName}' at path: {Path} (IsDirectory: {IsDirectory})", configuration.Name, configuration.ApplicationRealPath, isDirectory);

        return await fileSystemService.SetHttpGroupPermissionsAsync(configuration.ApplicationRealPath, isDirectory);
    }

    #endregion

    #region Reverse Proxy Management

    /// <summary>
    /// Creates a reverse proxy rule for the specified website configuration.
    /// </summary>
    private async Task<ApiResult> CreateReverseProxyRuleAsync(WebSiteConfiguration configuration)
    {
        try
        {
            await reverseProxyManager.CreateAsync(configuration);
            logger.LogInformation("Reverse proxy rule created for site '{SiteName}'", configuration.Name);
            return ApiResult.CreateSuccess();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create reverse proxy rule for '{SiteName}'", configuration.Name);
            return ApiResult.CreateFailure($"Failed to create reverse proxy: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates a reverse proxy rule for the specified website configuration.
    /// </summary>
    private async Task<ApiResult> UpdateReverseProxyRuleAsync(WebSiteConfiguration configuration)
    {
        try
        {
            await reverseProxyManager.UpdateAsync(configuration);
            logger.LogInformation("Reverse proxy rule updated for '{SiteName}'", configuration.Name);
            return ApiResult.CreateSuccess();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update reverse proxy rule for '{SiteName}'", configuration.Name);
            return ApiResult.CreateFailure($"Failed to update reverse proxy: {ex.Message}");
        }
    }

    /// <summary>
    /// Deletes a reverse proxy rule for the specified website configuration.
    /// </summary>
    private async Task<ApiResult> DeleteReverseProxyRuleAsync(WebSiteConfiguration configuration)
    {
        try
        {
            await reverseProxyManager.DeleteAsync(configuration);
            logger.LogInformation("Reverse proxy rule deleted for '{SiteName}'", configuration.Name);
            return ApiResult.CreateSuccess();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete reverse proxy rule for '{SiteName}'", configuration.Name);
            return ApiResult.CreateFailure($"Failed to delete reverse proxy: {ex.Message}");
        }
    }

    #endregion
}
