using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Logging;
using Askyl.Dsm.WebHosting.Tools.Infrastructure;

namespace Askyl.Dsm.WebHosting.Ui.Services;

/// <summary>
/// Reloads the DSM settings whenever DSM's web configuration (DSM.json) changes. Control Panel moves DSM's port
/// with an nginx reload, which restarts no package, so the file is the only signal; without it the old port stays
/// in use until a sign-in fails and the diagnostic reloads. Nothing is watched where the file's directory does
/// not exist, as on a developer machine.
/// </summary>
/// <param name="settings">The settings to reload.</param>
/// <param name="configuration">Application configuration, which may override the file's path.</param>
/// <param name="logger">Logger instance.</param>
public sealed class DsmWebConfigurationWatcher(IDsmSettingsService settings, IConfiguration configuration, ILogger<ILogDsmSettingsService> logger) : IHostedService, IDisposable
{
    readonly string _path = DsmSettingsService.WebConfigPath(configuration);

    FileSystemWatcher? _watcher;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (Path.GetDirectoryName(_path) is { Length: > 0 } directory && Directory.Exists(directory))
        {
            // Whether DSM rewrites the file in place or replaces it, one of these fires
            _watcher = new(directory, Path.GetFileName(_path));
            _watcher.Changed += OnChanged;
            _watcher.Created += OnChanged;
            _watcher.Renamed += OnChanged;
            _watcher.Error += (_, e) => logger.WebConfigurationWatchFailed(_path, e.GetException());
            _watcher.EnableRaisingEvents = true;
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public void Dispose() => _watcher?.Dispose();

    // Raised on a thread-pool thread, where an exception would end the process
    void OnChanged(object sender, FileSystemEventArgs e)
    {
        try
        {
            settings.Reload();
        }
        catch (Exception ex)
        {
            logger.WebConfigurationWatchFailed(_path, ex);
        }
    }
}
