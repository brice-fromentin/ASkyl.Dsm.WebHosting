using System.Collections.Concurrent;
using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Constants.Runtime;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Domain.WebSites;
using Askyl.Dsm.WebHosting.Data.Results;
using Askyl.Dsm.WebHosting.Globalization;
using Askyl.Dsm.WebHosting.Logging;
using Askyl.Dsm.WebHosting.Tools.Diagnostics;
using Askyl.Dsm.WebHosting.Tools.Infrastructure;

namespace Askyl.Dsm.WebHosting.Ui.Services;

/// <summary>
/// Background service that manages website instances and their lifecycle.
/// Orchestrates instance management by delegating to SiteLifecycleManager for process operations.
/// </summary>
public class WebSiteHostingService(
    ILogger<ILogWebSiteHostingService> logger,
    ILoggerFactory loggerFactory,
    IProcessRunner processRunner,
    IWebSitesConfigurationService configService,
    IFileSystemService fileSystemService,
    IReverseProxyManagerService reverseProxyManager,
    IAssemblyRuntimeDetector assemblyRuntimeDetector,
    IVersionsDetectorService versionsDetector,
    ILocalizer localizer) : BackgroundService, IWebSiteHostingService
{
    #region Fields

    private readonly ConcurrentDictionary<Guid, SiteEntry> _sites = new();

    #endregion

    #region Nested Types

    /// <summary>
    /// Pairs a website instance with its lifecycle manager, eliminating parallel dictionary synchronization.
    /// </summary>
    private sealed class SiteEntry(WebSiteInstanceDetails instance, SiteLifecycleManager lifecycleManager)
    {
        public WebSiteInstanceDetails Instance { get; } = instance;
        public SiteLifecycleManager LifecycleManager { get; set; } = lifecycleManager;
    }

    #endregion

    #region Public API

    /// <summary>
    /// Gets all website instances with their current runtime status.
    /// Updates runtime state from lifecycle managers before returning.
    /// </summary>
    public async Task<WebSiteInstancesResult> GetAllWebsitesAsync()
    {
        var instances = new List<WebSiteInstance>();

        foreach (var entry in _sites.Values)
        {
            var runtimeState = await entry.LifecycleManager.GetRuntimeStateAsync();
            UpdateInstanceRuntimeState(entry.Instance, runtimeState);
            instances.Add(entry.Instance); // Serialized as base type — Process excluded
        }

        return WebSiteInstancesResult.CreateSuccess(instances);
    }

    /// <summary>
    /// Adds a new website configuration and creates an instance.
    /// </summary>
    public async Task<WebSiteInstanceResult> AddWebsiteAsync(WebSiteConfiguration configuration)
    {
        using var timer = new OperationTimer(elapsed => logger.AddWebsiteDuration(elapsed, configuration.Name));

        logger.AddWebsiteStarting(configuration.Name);

        // Validate environment variables before any side effects
        var envVarResult = ValidateEnvironmentVariables(configuration.AdditionalEnvironmentVariables);
        if (envVarResult is not null)
        {
            return envVarResult;
        }

        try
        {
            // STEP 1: Set HTTP group permissions BEFORE adding website (CRITICAL - must succeed)
            var permissionResult = await SetHttpGroupPermissionsForApplicationAsync(configuration);

            if (!permissionResult.Success)
            {
                logger.PermissionSettingFailedAdd(configuration.Name, permissionResult.Message);
                return WebSiteInstanceResult.CreateFailure(localizer[L.Error.FailedToSetPermissions, permissionResult.Message ?? localizer[L.Error.Unknown]]);
            }

            // STEP 2: Create reverse proxy rule (CRITICAL - must succeed)
            var proxyResult = await CreateReverseProxyRuleAsync(configuration);

            if (!proxyResult.Success)
            {
                logger.ReverseProxyCreationFailedAdd(configuration.Name, proxyResult.Message);
                return WebSiteInstanceResult.CreateFailure(localizer[L.Error.FailedToCreateReverseProxy, proxyResult.Message ?? localizer[L.Error.Unknown]]);
            }

            // STEP 3: Add website configuration (persistent storage)
            await configService.AddSiteAsync(configuration);

            // STEP 4: Create instance
            var instance = await AddInstanceAsync(configuration);

            // STEP 5: Detect framework from assembly and warn if incompatible
            var result = AttachRuntimeInfo(instance, configuration.ApplicationRealPath);

            return result;
        }
        catch (Exception ex)
        {
            logger.ErrorAddingWebsite(ex, configuration.Name);
            return WebSiteInstanceResult.CreateFailure(localizer[L.Error.OperationFailed]);
        }
    }

    /// <summary>
    /// Updates an existing website configuration and refreshes the instance.
    /// </summary>
    public async Task<WebSiteInstanceResult> UpdateWebsiteAsync(WebSiteConfiguration configuration)
    {
        if (!_sites.TryGetValue(configuration.Id, out var entry))
        {
            logger.InstanceNotFoundUpdate(configuration.Name);
            return WebSiteInstanceResult.CreateFailure(localizer[L.Error.InstanceNotFound]);
        }

        var existingInstance = entry.Instance;

        using var timer = new OperationTimer(elapsed => logger.UpdateWebsiteDuration(elapsed, configuration.Name));

        logger.UpdateWebsiteStarting(configuration.Name);

        // Validate environment variables before any side effects
        var envVarResult = ValidateEnvironmentVariables(configuration.AdditionalEnvironmentVariables);
        if (envVarResult is not null)
        {
            return envVarResult;
        }

        try
        {
            // STEP 1: ALWAYS set HTTP group permissions (CRITICAL - must succeed, allows easy repairs)
            var permissionResult = await SetHttpGroupPermissionsForApplicationAsync(configuration);

            if (!permissionResult.Success)
            {
                logger.PermissionSettingFailedUpdate(configuration.Name, permissionResult.Message);
                return WebSiteInstanceResult.CreateFailure(localizer[L.Error.FailedToSetPermissions, permissionResult.Message ?? localizer[L.Error.Unknown]]);
            }

            // STEP 2: Update reverse proxy rule (CRITICAL - must succeed)
            var proxyResult = await UpdateReverseProxyRuleAsync(configuration);

            if (!proxyResult.Success)
            {
                logger.ReverseProxyUpdateFailed(configuration.Name, proxyResult.Message);
                return WebSiteInstanceResult.CreateFailure(localizer[L.Error.FailedToUpdateReverseProxy, proxyResult.Message ?? localizer[L.Error.Unknown]]);
            }

            // STEP 3: Update configuration (persistent storage)
            await configService.UpdateSiteAsync(configuration);

            // STEP 4: Update instance
            await UpdateInstanceAsync(entry, configuration);

            // STEP 5: Detect framework from assembly and warn if incompatible
            return AttachRuntimeInfo(existingInstance, configuration.ApplicationRealPath);
        }
        catch (Exception ex)
        {
            logger.ErrorUpdatingWebsite(ex, configuration.Name);
            return WebSiteInstanceResult.CreateFailure(localizer[L.Error.OperationFailed]);
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
        if (!_sites.TryGetValue(id, out var entry))
        {
            logger.CannotStartSiteNotFound(id);
            return ApiResult.CreateFailure(localizer[L.Error.SiteNotFound, id]);
        }

        using var timer = new OperationTimer(elapsed => logger.StartWebsiteDuration(elapsed, entry.Instance.Configuration.Name));

        logger.StartWebsiteStarting(entry.Instance.Configuration.Name);

        var result = await entry.LifecycleManager.StartAsync();

        if (result.Success)
        {
            var runtimeState = await entry.LifecycleManager.GetRuntimeStateAsync();
            UpdateInstanceRuntimeState(entry.Instance, runtimeState);
        }

        return result;
    }

    /// <summary>
    /// Stops a website by ID. Synchronous — waits for SIGTERM signal and process exit (typically 1-3 seconds).
    /// </summary>
    public async Task<ApiResult> StopWebsiteAsync(Guid id)
    {
        if (!_sites.TryGetValue(id, out var entry))
        {
            logger.CannotStopSiteNotFound(id);
            return ApiResult.CreateFailure(localizer[L.Error.SiteNotFound, id]);
        }

        using var timer = new OperationTimer(elapsed => logger.StopWebsiteDuration(elapsed, entry.Instance.Configuration.Name));

        logger.StopWebsiteStarting(entry.Instance.Configuration.Name);

        var result = await entry.LifecycleManager.StopAsync();

        if (result.Success)
        {
            var runtimeState = await entry.LifecycleManager.GetRuntimeStateAsync();
            UpdateInstanceRuntimeState(entry.Instance, runtimeState);
        }

        return result;
    }

    #endregion

    #region Service Lifecycle

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.HostingServiceStarting();

        await versionsDetector.RefreshCacheAsync(cancellationToken);
        await InitializeAllInstancesAsync();
        await StartEligibleSitesAsync();

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.HostingServiceStarted();

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
        logger.StoppingAllWebsites();
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
            var instance = new WebSiteInstanceDetails(site);

            // Detect framework from assembly
            if (!String.IsNullOrEmpty(site.ApplicationRealPath))
            {
                var runtimeInfo = assemblyRuntimeDetector.Detect(site.ApplicationRealPath);
                instance.RequiredFramework = runtimeInfo?.Channel;
            }

            var lifecycleManager = new SiteLifecycleManager(loggerFactory.CreateLogger<ILogSiteLifecycleManager>(), localizer, processRunner, assemblyRuntimeDetector, site);
            _sites[instance.Id] = new SiteEntry(instance, lifecycleManager);

            logger.InstanceCreated(site.Name);
        }

        logger.AllInstancesInitialized(_sites.Count);
    }

    private async Task StartEligibleSitesAsync()
    {
        var results = await Task.WhenAll(
            _sites.Values
                .Where(e => e.Instance.Configuration.IsEnabled && e.Instance.Configuration.AutoStart)
                .Select(e => StartWebsiteAsync(e.Instance.Id)));

        var failures = results.Where(r => !r.Success).ToList();
        if (failures.Count != 0)
        {
            logger.SitesFailedToStart(failures.Count, String.Join(", ", failures.Select(f => f.Message)));
        }
    }

    #endregion

    #region Instance Management

    public async Task<WebSiteInstance> AddInstanceAsync(WebSiteConfiguration configuration)
    {
        var instance = new WebSiteInstanceDetails(configuration);
        var lifecycleManager = new SiteLifecycleManager(loggerFactory.CreateLogger<ILogSiteLifecycleManager>(), localizer, processRunner, assemblyRuntimeDetector, configuration);
        _sites[instance.Id] = new SiteEntry(instance, lifecycleManager);

        logger.InstanceAdded(configuration.Name);

        if (configuration.IsEnabled && configuration.AutoStart)
        {
            await StartWebsiteAsync(instance.Id);
        }

        return instance;
    }

    private async Task UpdateInstanceAsync(SiteEntry entry, WebSiteConfiguration newConfiguration)
    {
        var existingInstance = entry.Instance;

        var wasRunning = existingInstance.IsRunning;
        var oldConfiguration = existingInstance.Configuration;

        if (wasRunning && (!newConfiguration.IsEnabled || ConfigurationRequiresRestart(oldConfiguration, newConfiguration)))
        {
            logger.StoppingSiteDisabledOrRestart(newConfiguration.Name);
            await StopWebsiteAsync(existingInstance.Id);
        }

        // Recreate lifecycle manager with new configuration to avoid stale config
        entry.LifecycleManager.Dispose();
        existingInstance.Configuration = newConfiguration;
        entry.LifecycleManager = new SiteLifecycleManager(loggerFactory.CreateLogger<ILogSiteLifecycleManager>(), localizer, processRunner, assemblyRuntimeDetector, newConfiguration);

        logger.InstanceUpdated(newConfiguration.Name);

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
        if (!_sites.TryGetValue(instanceId, out var entry))
        {
            logger.CannotRemoveInstanceNotFound(instanceId);
            return ApiResult.CreateFailure(localizer[L.Error.InstanceNotFound]);
        }

        var instance = entry.Instance;
        var siteName = instance.Configuration.Name;

        using var timer = new OperationTimer(elapsed => logger.RemoveWebsiteDuration(elapsed, siteName));

        logger.RemoveWebsiteStarting(siteName);

        try
        {
            // Stop lifecycle manager if running (without removing from dictionary yet)
            if (instance.IsRunning)
            {
                var stopResult = await entry.LifecycleManager.StopAsync();

                if (!stopResult.Success && stopResult.ErrorCode != ApiErrorCode.NotFound)
                {
                    logger.FailedToStopBeforeDeletion(stopResult.Message);
                    // Continue with cleanup anyway
                }
            }

            // Delete reverse proxy rule (best effort - log but don't fail if it errors)
            var proxyDeleteResult = await DeleteReverseProxyRuleAsync(instance.Configuration);

            if (!proxyDeleteResult.Success)
            {
                logger.ReverseProxyDeletionFailed(siteName, proxyDeleteResult.Message);
            }

            // Remove configuration (persistent storage) — MUST succeed before removing from memory
            await configService.RemoveSiteAsync(instance.Configuration.Id);

            // Safe to remove from memory now — persistent config is gone
            _ = _sites.TryRemove(instanceId, out var removedEntry);
            removedEntry?.LifecycleManager.Dispose();

            logger.InstanceRemoved(siteName);
            return ApiResult.CreateSuccess();
        }
        catch (Exception ex)
        {
            logger.FailedToRemoveSite(ex, siteName);
            return ApiResult.CreateFailure(localizer[L.Error.OperationFailed]);
        }
    }

    /// <summary>
    /// Updates the runtime state of a WebSiteInstance from WebSiteRuntimeState.
    /// Helper method for synchronizing instance state with lifecycle manager state.
    /// </summary>
    private static void UpdateInstanceRuntimeState(WebSiteInstanceDetails instance, WebSiteRuntimeState runtimeState)
    {
        instance.IsRunning = runtimeState.IsRunning;
        instance.Process = runtimeState.ProcessDetails;
    }

    #endregion

    #region Bulk Operations

    private async Task StopAllSitesAsync(CancellationToken cancellationToken)
    {
        var stopTasks = _sites.Values.Select(
            async entry =>
            {
                try
                {
                    await entry.LifecycleManager.StopAsync(cancellationToken);
                }
                finally
                {
                    entry.LifecycleManager.Dispose();
                }
            })
            .ToList();
        await Task.WhenAll(stopTasks);

        _sites.Clear();
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
            logger.NoApplicationRealPath(configuration.Name);
            return ApiResult.CreateFailure(localizer[L.Error.NoApplicationPath]);
        }

        // Determine if the target is a directory (if ApplicationPath ends with .dll, set permissions on parent directory)
        var isDirectory = !configuration.ApplicationRealPath.EndsWith(WebSiteConstants.DllFileExtension, StringComparison.OrdinalIgnoreCase);

        logger.SettingHttpGroupPermissions(configuration.Name, configuration.ApplicationRealPath, isDirectory);

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
            logger.ReverseProxyRuleCreated(configuration.Name);
            return ApiResult.CreateSuccess();
        }
        catch (Exception ex)
        {
            logger.FailedToCreateReverseProxyRule(ex, configuration.Name);
            return ApiResult.CreateFailure(localizer[L.Error.OperationFailed]);
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
            logger.ReverseProxyRuleUpdated(configuration.Name);
            return ApiResult.CreateSuccess();
        }
        catch (Exception ex)
        {
            logger.FailedToUpdateReverseProxyRule(ex, configuration.Name);
            return ApiResult.CreateFailure(localizer[L.Error.OperationFailed]);
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
            logger.ReverseProxyRuleDeleted(configuration.Name);
            return ApiResult.CreateSuccess();
        }
        catch (Exception ex)
        {
            logger.FailedToDeleteReverseProxyRule(ex, configuration.Name);
            return ApiResult.CreateFailure(localizer[L.Error.OperationFailed]);
        }
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Detects the required .NET runtime from the assembly and attaches a warning if incompatible or undetectable.
    /// </summary>
    private WebSiteInstanceResult AttachRuntimeInfo(WebSiteInstance instance, string applicationPath)
    {
        var result = WebSiteInstanceResult.CreateSuccess(instance);
        var runtimeInfo = assemblyRuntimeDetector.Detect(applicationPath);

        if (runtimeInfo is null)
        {
            result.WarningMessage = RuntimeConstants.RuntimeDetectionFailedWarningMessage;
            return result;
        }

        instance.RequiredFramework = runtimeInfo.Channel;

        if (!runtimeInfo.IsCompatible)
        {
            result.WarningMessage = runtimeInfo.MissingMessage;
        }

        return result;
    }

    /// <summary>
    /// Validates environment variable keys and values to prevent resource exhaustion.
    /// Returns a failure result if validation fails, or null if all checks pass.
    /// </summary>
    private WebSiteInstanceResult? ValidateEnvironmentVariables(Dictionary<string, string> environmentVariables)
    {
        if (environmentVariables is null || environmentVariables.Count == 0)
        {
            return null;
        }

        foreach (var kvp in environmentVariables)
        {
            if (String.IsNullOrWhiteSpace(kvp.Key) || kvp.Key.Length > ValidationConstants.EnvVarKeyMaxLength)
            {
                return WebSiteInstanceResult.CreateFailure(localizer[L.Validation.EnvVarKeyTooLong, kvp.Key, ValidationConstants.EnvVarKeyMaxLength]);
            }

            if (kvp.Value?.Length > ValidationConstants.EnvVarValueMaxLength)
            {
                return WebSiteInstanceResult.CreateFailure(localizer[L.Validation.EnvVarValueTooLong, kvp.Key, ValidationConstants.EnvVarValueMaxLength]);
            }
        }

        return null;
    }

    #endregion
}
