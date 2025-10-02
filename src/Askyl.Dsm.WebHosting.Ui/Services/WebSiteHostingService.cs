using System.Collections.Concurrent;
using System.Diagnostics;
using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Data.WebSites;
using Askyl.Dsm.WebHosting.Ui.Models;

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

    private async Task StartEligibleSitesAsync()
    {
        if (!_instances.IsEmpty)
        {
            _logger.LogWarning("Some processes are already running at startup - this should not happen");
            return;
        }

        var sitesToStart = await _configService.GetSitesToStartAsync();

        foreach (var site in sitesToStart)
        {
            await StartSiteAsync(site);
        }
    }

    public IEnumerable<WebSiteInstance> GetInstances()
    {
        return _instances.Values;
    }

    private Task<bool> StartSiteAsync(WebSiteConfiguration site)
    {
        _logger.LogInformation("Starting site: {SiteName}", site.Name);

        try
        {
            if (!File.Exists(site.ApplicationPath))
            {
                _logger.LogError("Application binary not found: {ApplicationPath}", site.ApplicationPath);
                return Task.FromResult(false);
            }

            var startInfo = CreateProcessStartInfo(site);
            var process = Process.Start(startInfo);
            if (process == null)
            {
                _logger.LogError("Failed to start process for site: {SiteName}", site.Name);
                return Task.FromResult(false);
            }

            var instance = new WebSiteInstance(site)
            {
                Process = new ProcessInfo(process, site)
            };

            _instances[site.Name] = instance;
            _logger.LogInformation("Site {SiteName} started with PID {ProcessId}", site.Name, process.Id);

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start site: {SiteName}", site.Name);
            return Task.FromResult(false);
        }
    }

    private bool StopSite(WebSiteInstance instance)
    {
        var siteName = instance.Configuration.Name;

        try
        {
            if (instance.Process == null)
            {
                _logger.LogWarning("Site {SiteName} has no running process", siteName);
                return false;
            }

            var process = Process.GetProcessById(instance.Process.ProcessId);
            process.CloseMainWindow();

            if (!process.WaitForExit(5000))
            {
                process.Kill();
            }

            _instances.TryRemove(siteName, out _);
            _logger.LogInformation("Site {SiteName} stopped", siteName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop site: {SiteName}", siteName);
            return false;
        }
    }


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