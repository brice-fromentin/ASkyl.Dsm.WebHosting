using System.Collections.Concurrent;
using System.Diagnostics;
using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Data.WebSites;
using Askyl.Dsm.WebHosting.Ui.Models.Results;
using Askyl.Dsm.WebHosting.Ui.Models.WebSites;

namespace Askyl.Dsm.WebHosting.Ui.Services;

public class WebSiteHostingService(ILogger<WebSiteHostingService> logger, IWebSitesConfigurationService configService) : BackgroundService
{
    private readonly ILogger<WebSiteHostingService> _logger = logger;
    private readonly IWebSitesConfigurationService _configService = configService;
    private readonly ConcurrentDictionary<string, WebSiteInstance> _instances = new();
 
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("WebSite hosting service starting");

        await _configService.EnsureLoadedAsync();
        await InitializeAllInstancesAsync();
        await StartEligibleSitesAsync();

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("WebSite hosting service started");

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
        _logger.LogInformation("Stopping all websites");
        await StopAllSitesAsync();
        await base.StopAsync(stoppingToken);
    }

    private async Task InitializeAllInstancesAsync()
    {
        var allSites = await _configService.GetAllSitesAsync();

        foreach (var site in allSites)
        {
            var instance = new WebSiteInstance(site);
            _instances[site.Name] = instance;
            _logger.LogInformation("Instance created for site: {SiteName}", site.Name);
        }

        _logger.LogInformation("All instances initialized: {Count} sites", _instances.Count);
    }

    private async Task StartEligibleSitesAsync()
    {
        var sitesToStart = await _configService.GetSitesToStartAsync();

        foreach (var site in sitesToStart)
        {
            await StartSiteAsync(site.Name);
        }
    }

    public IEnumerable<WebSiteInstance> GetInstances()
    {
        return _instances.Values;
    }

    public Task<OperationResult> StartSiteAsync(string siteName)
    {
        if (!_instances.TryGetValue(siteName, out var instance))
        {
            _logger.LogError("Instance not found for site: {SiteName}", siteName);
            return Task.FromResult(OperationResult.CreateFailure($"Instance '{siteName}' not found"));
        }

        if (instance.IsRunning)
        {
            _logger.LogWarning("Site {SiteName} is already running", siteName);
            return Task.FromResult(OperationResult.CreateFailure($"Site '{siteName}' is already running"));
        }

        _logger.LogInformation("Starting site: {SiteName}", siteName);

        try
        {
            var site = instance.Configuration;

            if (!File.Exists(site.ApplicationPath))
            {
                _logger.LogError("Application binary not found: {ApplicationPath}", site.ApplicationPath);
                return Task.FromResult(OperationResult.CreateFailure($"Application binary not found: {site.ApplicationPath}"));
            }

            var startInfo = CreateProcessStartInfo(site);
            var process = Process.Start(startInfo);

            if (process == null)
            {
                _logger.LogError("Failed to start process for site: {SiteName}", siteName);
                return Task.FromResult(OperationResult.CreateFailure($"Failed to start process for site '{siteName}'"));
            }

            instance.Process = new ProcessInfo(process, site);
            _logger.LogInformation("Site {SiteName} started with PID {ProcessId}", siteName, process.Id);

            return Task.FromResult(OperationResult.CreateSuccess());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start site: {SiteName}", siteName);
            return Task.FromResult(OperationResult.CreateFailure($"Failed to start site: {ex.Message}"));
        }
    }

    public OperationResult StopSiteAsync(string siteName)
    {
        if (!_instances.TryGetValue(siteName, out var instance))
        {
            _logger.LogError("Instance not found for site: {SiteName}", siteName);
            return OperationResult.CreateFailure($"Instance '{siteName}' not found");
        }

        return StopSiteAsync(instance);
    }

    public OperationResult StopSiteAsync(WebSiteInstance instance)
    {
        var siteName = instance.Configuration.Name;

        if (!instance.IsRunning)
        {
            _logger.LogWarning("Site {SiteName} is not running", siteName);
            return OperationResult.CreateFailure($"Site '{siteName}' is not running");
        }

        try
        {
            var process = Process.GetProcessById(instance.Process!.ProcessId);
            process.CloseMainWindow();

            if (!process.WaitForExit(5000))
            {
                process.Kill();
            }

            instance.Process = null;
            _logger.LogInformation("Site {SiteName} stopped", siteName);
            return OperationResult.CreateSuccess();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop site: {SiteName}", siteName);
            return OperationResult.CreateFailure($"Failed to stop site: {ex.Message}");
        }
    }

    private OperationResult StopSite(WebSiteInstance instance) => StopSiteAsync(instance);


    private async Task StopAllSitesAsync()
    {
        var stopTasks = GetInstances().Select(instance => Task.Run(() => StopSite(instance)));

        await Task.WhenAll(stopTasks);
    }

    private static ProcessStartInfo CreateProcessStartInfo(WebSiteConfiguration site)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = ApplicationConstants.DotnetExecutable,
            Arguments = site.ApplicationPath,
            WorkingDirectory = Path.GetDirectoryName(site.ApplicationPath),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        startInfo.Environment[ApplicationConstants.AspNetCoreUrlsEnvironmentVariable] = $"http://localhost:{site.Port}";
        startInfo.Environment[ApplicationConstants.AspNetCoreEnvironmentVariable] = site.Environment;

        foreach (var envVar in site.AdditionalEnvironmentVariables)
        {
            startInfo.Environment[envVar.Key] = envVar.Value;
        }

        return startInfo;
    }
}