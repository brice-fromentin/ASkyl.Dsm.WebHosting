using System.Collections.Concurrent;
using System.Diagnostics;

using Askyl.Dsm.WebHosting.Constants.Application;

using Askyl.Dsm.WebHosting.Data.Domain.WebSites;
using Askyl.Dsm.WebHosting.Data.Results;
using Askyl.Dsm.WebHosting.Data.Services;

namespace Askyl.Dsm.WebHosting.Ui.Services;

/// <summary>
/// Background service that manages website instances and their lifecycle.
/// </summary>
public class WebSiteHostingService(
    ILogger<WebSiteHostingService> logger,
    IWebSitesConfigurationService configService,
    IFileSystemService fileSystemService,
    IReverseProxyManagerService reverseProxyManager) : BackgroundService, IWebSiteHostingService
{
    #region Fields

    private readonly ConcurrentDictionary<Guid, WebSiteInstance> _instances = new();

    #endregion

    #region Public API

    /// <summary>
    /// Gets all website instances with their current runtime status.
    /// </summary>
    public Task<WebSiteInstancesResult> GetAllWebsitesAsync()
        => Task.FromResult(WebSiteInstancesResult.CreateSuccess(_instances.Values.Select(i => i.Clone()).ToList()));

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

            return WebSiteInstanceResult.CreateSuccess(existingInstance.Clone());
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
    /// Starts a website by ID.
    /// </summary>
    public async Task<ApiResult> StartWebsiteAsync(Guid id)
    {
        if (!_instances.TryGetValue(id, out var instance))
        {
            logger.LogWarning("Cannot start site: site with ID {InstanceId} not found", id);
            return ApiResult.CreateFailure($"Site with ID '{id}' not found");
        }

        return await StartSiteAsync(instance);
    }

    /// <summary>
    /// Stops a website by ID.
    /// </summary>
    public async Task<ApiResult> StopWebsiteAsync(Guid id)
    {
        if (!_instances.TryGetValue(id, out var instance))
        {
            logger.LogWarning("Cannot stop site: site with ID {InstanceId} not found", id);
            return ApiResult.CreateFailure($"Site with ID '{id}' not found");
        }

        return await StopSiteAsync(instance);
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
        await StopAllSitesAsync();
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
            logger.LogInformation("Instance created for site: {SiteName}", site.Name);
        }

        logger.LogInformation("All instances initialized: {Count} sites", _instances.Count);
    }

    private async Task StartEligibleSitesAsync()
    {
        var instancesToStart = _instances.Values.Where(i => i.Configuration.IsEnabled && i.Configuration.AutoStart);

        foreach (var instance in instancesToStart)
        {
            if (instance != null)
            {
                await StartSiteAsync(instance);
            }
        }
    }

    #endregion

    #region Instance Management

    public async Task<WebSiteInstance> AddInstanceAsync(WebSiteConfiguration configuration)
    {
        var instance = new WebSiteInstance(configuration.Clone());
        _instances[instance.Id] = instance;
        logger.LogInformation("Instance added for site: {SiteName}", configuration.Name);

        if (configuration.IsEnabled && configuration.AutoStart)
        {
            await StartSiteAsync(instance);
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
            await StopSiteAsync(existingInstance);
        }

        existingInstance.Configuration = newConfiguration.Clone();
        logger.LogInformation("Instance updated for site: {SiteName}", newConfiguration.Name);

        if (newConfiguration.IsEnabled && (wasRunning || newConfiguration.AutoStart))
        {
            await StartSiteAsync(existingInstance);
        }
    }

    private static bool ConfigurationRequiresRestart(WebSiteConfiguration oldConfig, WebSiteConfiguration newConfig)
    {
        return oldConfig.ApplicationPath != newConfig.ApplicationPath
            || oldConfig.InternalPort != newConfig.InternalPort
            || oldConfig.Environment != newConfig.Environment
            || !oldConfig.AdditionalEnvironmentVariables.SequenceEqual(newConfig.AdditionalEnvironmentVariables);
    }

    #endregion

    #region Instance Lifecycle Operations

    public async Task<ApiResult> RemoveInstanceAsync(Guid instanceId)
    {
        if (!_instances.TryRemove(instanceId, out var instance))
        {
            logger.LogWarning("Cannot remove instance: instance {InstanceId} not found", instanceId);
            return ApiResult.CreateFailure("Instance not found");
        }

        var siteName = instance.Configuration.Name;

        try
        {
            if (instance.IsRunning)
            {
                var stopResult = await StopSiteAsync(instance);

                if (!stopResult.Success)
                {
                    // Special case: NotFound is OK during deletion
                    // It means the instance was already removed from memory, nothing to stop
                    if (stopResult.ErrorCode != ApiErrorCode.NotFound)
                    {
                        _instances[instanceId] = instance;
                        return ApiResult.CreateFailure($"Failed to stop site before deletion: {stopResult.Message}");
                    }

                    logger.LogWarning("Site was already stopped or removed from memory. Continuing with cleanup.");
                }
            }

            // STEP 1: Delete reverse proxy rule (best effort - log but don't fail if it errors)
            var proxyDeleteResult = await DeleteReverseProxyRuleAsync(instance.Configuration);

            if (!proxyDeleteResult.Success)
            {
                logger.LogWarning("Reverse proxy deletion failed for '{SiteName}' (site will be removed anyway): {ErrorMessage}", siteName, proxyDeleteResult.Message);
                // Continue with removal even if proxy deletion fails
            }

            // STEP 2: Remove configuration (persistent storage)
            await configService.RemoveSiteAsync(instance.Configuration.Id);

            logger.LogInformation("Instance removed for site: {SiteName}", siteName);
            return ApiResult.CreateSuccess();
        }
        catch (Exception ex)
        {
            _instances[instanceId] = instance;
            logger.LogError(ex, "Failed to remove site: {SiteName}", siteName);
            return ApiResult.CreateFailure($"Failed to remove site: {ex.Message}");
        }
    }

    public async Task<ApiResult> StartSiteAsync(WebSiteInstance instance)
    {
        if (!_instances.TryGetValue(instance.Id, out var internalInstance))
        {
            logger.LogError("Instance not found: {InstanceId}", instance.Id);
            return ApiResult.CreateFailure("Instance not found");
        }

        var siteName = internalInstance.Configuration.Name;

        if (internalInstance.IsRunning)
        {
            instance.IsRunning = internalInstance.IsRunning;  // Sync to caller's instance
            logger.LogWarning("Site {SiteName} is already running", siteName);
            return ApiResult.CreateFailure($"Site '{siteName}' is already running");
        }

        logger.LogInformation("Starting site: {SiteName}", siteName);

        try
        {
            var site = internalInstance.Configuration;
            var executablePath = site.ApplicationRealPath;

            if (!File.Exists(executablePath))
            {
                logger.LogError("Application binary not found: {ApplicationPath}", executablePath);
                return ApiResult.CreateFailure($"Application binary not found: {executablePath}");
            }

            var startInfo = CreateProcessStartInfo(site, executablePath);
            var process = Process.Start(startInfo);

            if (process == null)
            {
                logger.LogError("Failed to start process for site: {SiteName}", siteName);
                return ApiResult.CreateFailure($"Failed to start process for site '{siteName}'");
            }

            internalInstance.Process = new ProcessInfo(process);
            internalInstance.IsRunning = true;  // Update serialized state
            instance.Process = internalInstance.Process;
            instance.IsRunning = internalInstance.IsRunning;  // Sync to caller's instance
            logger.LogInformation("Site {SiteName} started with PID {ProcessId}", siteName, process.Id);

            return ApiResult.CreateSuccess();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start site: {SiteName}", siteName);
            return ApiResult.CreateFailure($"Failed to start site: {ex.Message}");
        }
    }

    public async Task<ApiResult> StopSiteAsync(WebSiteInstance instance)
    {
        if (!_instances.TryGetValue(instance.Id, out var internalInstance))
        {
            logger.LogError("Instance not found: {InstanceId}", instance.Id);
            return ApiResult.CreateFailure(ApiErrorCode.NotFound, "Instance not found");
        }

        var siteName = internalInstance.Configuration.Name;

        // Idempotency check: already stopped
        if (!internalInstance.IsRunning)
        {
            SyncInstanceState(instance, internalInstance);
            logger.LogWarning("Site {SiteName} is already stopped (idempotent operation)", siteName);
            return ApiResult.CreateSuccess();
        }

        try
        {
            await StopProcessAsync(internalInstance, instance, siteName);

            CleanUpInstanceState(internalInstance, instance);
            logger.LogInformation("Site {SiteName} stopped successfully", siteName);

            return ApiResult.CreateSuccess();
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            // Process-related exceptions usually mean the process is already gone
            logger.LogWarning(ex, "Site {SiteName} process no longer exists. Cleaning up state.", siteName);
            CleanUpInstanceState(internalInstance, instance);
            return ApiResult.CreateSuccess();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to stop site: {SiteName}", siteName);
            return ApiResult.CreateFailure(ApiErrorCode.Failure, $"Failed to stop site: {ex.Message}");
        }
    }

    /// <summary>
    /// Stops the process associated with a website instance.
    /// </summary>
    private async Task StopProcessAsync(WebSiteInstance internalInstance, WebSiteInstance instance, string siteName)
    {
        var process = Process.GetProcessById(internalInstance.Process!.Id);

        // Check if process is already dead
        if (process.HasExited)
        {
            logger.LogWarning("Site {SiteName} process was already dead. Cleaning up state.", siteName);
            return;  // Process is gone, nothing to stop
        }

        process.CloseMainWindow();

        if (!process.WaitForExit(5000))
        {
            await ForceKillProcessAsync(process, siteName);
        }
    }

    /// <summary>
    /// Force kills a process that didn't stop gracefully.
    /// </summary>
    private async Task ForceKillProcessAsync(Process process, string siteName)
    {
        logger.LogWarning("Site {SiteName} did not stop gracefully. Force killing process.", siteName);

        try
        {
            process.Kill();
            await WaitForProcessExitAsync(process, 1000);
        }
        catch (Exception killEx)
        {
            logger.LogError(killEx, "Failed to force kill process for site {SiteName}. Process may still be running.", siteName);
            // Still consider it success - we did our best, OS will clean up orphaned process eventually
        }
    }

    /// <summary>
    /// Waits for a process to exit with timeout.
    /// </summary>
    private static async Task WaitForProcessExitAsync(Process process, int timeoutMilliseconds)
        => await Task.Run(() => process.WaitForExit(timeoutMilliseconds));

    /// <summary>
    /// Cleans up the instance state after stopping.
    /// </summary>
    private void CleanUpInstanceState(WebSiteInstance internalInstance, WebSiteInstance instance)
    {
        internalInstance.Process = null;
        internalInstance.IsRunning = false;  // Update serialized state
        SyncInstanceState(instance, internalInstance);
    }

    /// <summary>
    /// Synchronizes the caller's instance with the internal instance state.
    /// </summary>
    private void SyncInstanceState(WebSiteInstance target, WebSiteInstance source)
    {
        target.IsRunning = source.IsRunning;
        target.Process = source.Process;
    }

    private async Task StopAllSitesAsync()
    {
        var stopTasks = _instances.Values.Select(instance => Task.Run(() => StopSiteAsync(instance)));
        await Task.WhenAll(stopTasks);
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
        var isDirectory = !configuration.ApplicationRealPath.EndsWith(ApplicationConstants.DllFileExtension, StringComparison.OrdinalIgnoreCase);

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

    #region Process Management

    private static ProcessStartInfo CreateProcessStartInfo(WebSiteConfiguration site, string executablePath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = ApplicationConstants.DotnetExecutable,
            Arguments = executablePath,
            WorkingDirectory = Path.GetDirectoryName(executablePath),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        startInfo.Environment[ApplicationConstants.AspNetCoreUrlsEnvironmentVariable] = $"http://localhost:{site.InternalPort}";
        startInfo.Environment[ApplicationConstants.AspNetCoreEnvironmentVariable] = site.Environment;

        foreach (var envVar in site.AdditionalEnvironmentVariables)
        {
            startInfo.Environment[envVar.Key] = envVar.Value;
        }

        return startInfo;
    }

    #endregion
}
