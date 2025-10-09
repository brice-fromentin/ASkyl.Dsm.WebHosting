using System.Collections.Concurrent;
using System.Diagnostics;
using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Data.WebSites;
using Askyl.Dsm.WebHosting.Ui.Models.Results;

namespace Askyl.Dsm.WebHosting.Ui.Services;

public class WebSiteHostingService(ILogger<WebSiteHostingService> logger, IWebSitesConfigurationService configService) : BackgroundService
{
    #region Fields

    private readonly ConcurrentDictionary<Guid, WebSiteInstance> _instances = new();

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

    public IEnumerable<WebSiteInstance> GetInstances() => [.. _instances.Values.Select(i => i.Clone())];

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

    public async Task<OperationResult> RemoveInstanceAsync(Guid instanceId)
    {
        if (!_instances.TryRemove(instanceId, out var instance))
        {
            logger.LogWarning("Cannot remove instance: instance {InstanceId} not found", instanceId);
            return OperationResult.CreateFailure($"Instance not found");
        }

        var siteName = instance.Configuration.Name;

        try
        {
            if (instance.IsRunning)
            {
                var stopResult = await StopSiteAsync(instance);

                if (!stopResult.Success)
                {
                    _instances[instanceId] = instance;
                    return OperationResult.CreateFailure($"Failed to stop site before deletion: {stopResult.ErrorMessage}");
                }
            }

            await configService.RemoveSiteAsync(instance.Configuration.Id);
            logger.LogInformation("Instance removed for site: {SiteName}", siteName);
            return OperationResult.CreateSuccess();
        }
        catch (Exception ex)
        {
            _instances[instanceId] = instance;
            logger.LogError(ex, "Failed to remove site: {SiteName}", siteName);
            return OperationResult.CreateFailure($"Failed to remove site: {ex.Message}");
        }
    }

    public Task<OperationResult> StartSiteAsync(WebSiteInstance instance)
    {
        if (!_instances.TryGetValue(instance.Id, out var internalInstance))
        {
            logger.LogError("Instance not found: {InstanceId}", instance.Id);
            return Task.FromResult(OperationResult.CreateFailure($"Instance not found"));
        }

        var siteName = internalInstance.Configuration.Name;

        if (internalInstance.IsRunning)
        {
            instance.Process = internalInstance.Process;
            logger.LogWarning("Site {SiteName} is already running", siteName);
            return Task.FromResult(OperationResult.CreateFailure($"Site '{siteName}' is already running"));
        }

        logger.LogInformation("Starting site: {SiteName}", siteName);

        try
        {
            var site = internalInstance.Configuration;
            var executablePath = String.IsNullOrEmpty(site.ApplicationRealPath) ? site.ApplicationPath : site.ApplicationRealPath;

            if (!File.Exists(executablePath))
            {
                logger.LogError("Application binary not found: {ApplicationPath}", executablePath);
                return Task.FromResult(OperationResult.CreateFailure($"Application binary not found: {executablePath}"));
            }

            var startInfo = CreateProcessStartInfo(site, executablePath);
            var process = Process.Start(startInfo);

            if (process == null)
            {
                logger.LogError("Failed to start process for site: {SiteName}", siteName);
                return Task.FromResult(OperationResult.CreateFailure($"Failed to start process for site '{siteName}'"));
            }

            internalInstance.Process = new ProcessInfo(process);
            instance.Process = internalInstance.Process;
            logger.LogInformation("Site {SiteName} started with PID {ProcessId}", siteName, process.Id);

            return Task.FromResult(OperationResult.CreateSuccess());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start site: {SiteName}", siteName);
            return Task.FromResult(OperationResult.CreateFailure($"Failed to start site: {ex.Message}"));
        }
    }

    public Task<OperationResult> StopSiteAsync(WebSiteInstance instance)
    {
        if (!_instances.TryGetValue(instance.Id, out var internalInstance))
        {
            logger.LogError("Instance not found: {InstanceId}", instance.Id);
            return Task.FromResult(OperationResult.CreateFailure($"Instance not found"));
        }

        var siteName = internalInstance.Configuration.Name;

        if (!internalInstance.IsRunning)
        {
            instance.Process = null;
            logger.LogWarning("Site {SiteName} is not running", siteName);
            return Task.FromResult(OperationResult.CreateFailure($"Site '{siteName}' is not running"));
        }

        try
        {
            var process = Process.GetProcessById(internalInstance.Process!.Id);
            process.CloseMainWindow();

            if (!process.WaitForExit(5000))
            {
                process.Kill();
            }

            internalInstance.Process = null;
            instance.Process = null;
            logger.LogInformation("Site {SiteName} stopped", siteName);
            return Task.FromResult(OperationResult.CreateSuccess());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to stop site: {SiteName}", siteName);
            return Task.FromResult(OperationResult.CreateFailure($"Failed to stop site: {ex.Message}"));
        }
    }

    private async Task StopAllSitesAsync()
    {
        var stopTasks = _instances.Values.Select(instance => Task.Run(() => StopSiteAsync(instance)));
        await Task.WhenAll(stopTasks);
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