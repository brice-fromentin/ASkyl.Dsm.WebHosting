using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Channels;
using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Data.Domain.WebSites;
using Askyl.Dsm.WebHosting.Data.Results;
using Askyl.Dsm.WebHosting.Tools.Infrastructure;

namespace Askyl.Dsm.WebHosting.Ui.Services;

/// <summary>
/// Manages the complete lifecycle of a single website instance.
/// Uses a channel-based command queue to serialize all operations — no semaphore needed.
/// Disposal waits for pending commands to drain before cleaning up resources.
/// </summary>
public sealed class SiteLifecycleManager : IDisposable
{
    private readonly ILogger<SiteLifecycleManager> _logger;
    private readonly WebSiteConfiguration _configuration;
    private readonly Channel<LifecycleCommand> _channel = Channel.CreateBounded<LifecycleCommand>(new BoundedChannelOptions(16)
    {
        FullMode = BoundedChannelFullMode.Wait,
        SingleReader = true,
        SingleWriter = false
    });
    private readonly Task _loopTask;
    private Process? _process;
    private volatile bool _isDisposing;

    public SiteLifecycleManager(ILogger<SiteLifecycleManager> logger, WebSiteConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _loopTask = ProcessSiteCommandsAsync();
    }

    /// <summary>
    /// Starts the website process with configured environment variables.
    /// Returns failure if already running, executable not found, or manager is disposing.
    /// </summary>
    public async Task<ApiResult> StartAsync()
    {
        if (_isDisposing)
        {
            _logger.LogWarning("Cannot start site '{SiteName}': lifecycle manager is disposing", _configuration.Name);
            return ApiResult.CreateFailure("Site configuration is being updated");
        }

        var tcs = new TaskCompletionSource<ApiResult>();
        if (!_channel.Writer.TryWrite(new StartCommand(tcs)))
        {
            return ApiResult.CreateFailure("Failed to queue start command");
        }

        return await tcs.Task;
    }

    /// <summary>
    /// Stops the website process with graceful shutdown and force kill fallback.
    /// Idempotent — returns success if already stopped.
    /// </summary>
    public async Task<ApiResult> StopAsync(CancellationToken cancellationToken = default)
    {
        if (_isDisposing)
        {
            _logger.LogWarning("Cannot stop site '{SiteName}': lifecycle manager is disposing", _configuration.Name);
            return ApiResult.CreateFailure("Site configuration is being updated");
        }

        var tcs = new TaskCompletionSource<ApiResult>();
        if (!_channel.Writer.TryWrite(new StopCommand(tcs, cancellationToken)))
        {
            return ApiResult.CreateFailure("Failed to queue stop command");
        }

        return await tcs.Task;
    }

    /// <summary>
    /// Gets the current runtime state of this instance.
    /// </summary>
    public async Task<WebSiteRuntimeState> GetRuntimeStateAsync(CancellationToken cancellationToken = default)
    {
        if (_isDisposing)
        {
            return WebSiteRuntimeState.Stopped;
        }

        var tcs = new TaskCompletionSource<WebSiteRuntimeState>();
        if (!_channel.Writer.TryWrite(new GetStateCommand(tcs)))
        {
            return WebSiteRuntimeState.Stopped;
        }

        return await tcs.Task.WaitAsync(cancellationToken);
    }

    /// <summary>
    /// Disposes managed resources. Blocks until all pending commands complete.
    /// Commands queued after this call are rejected by the _isDisposing check.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposing)
        {
            return;
        }

        _isDisposing = true;

        // Queue dispose command — executes after all previously queued commands
        _ = _channel.Writer.TryWrite(new DisposeCommand());
        _channel.Writer.Complete();

        // Wait for loop to drain pending commands and clean up
        _loopTask.WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
    }

    #region Command Loop

    /// <summary>
    /// Single consumer loop — all state mutation happens here.
    /// Commands execute sequentially, eliminating TOCTOU races.
    /// </summary>
    private async Task ProcessSiteCommandsAsync()
    {
        await foreach (var command in _channel.Reader.ReadAllAsync())
        {
            switch (command)
            {
                case StartCommand start:
                    start.Result.SetResult(ProcessStartCommand());
                    break;

                case StopCommand stop:
                    stop.Result.SetResult(await ProcessStopCommand(stop.CancellationToken));
                    break;

                case GetStateCommand state:
                    state.Result.SetResult(BuildRuntimeState());
                    break;

                case DisposeCommand:
                    await ProcessDisposeCommand();
                    return;

                default:
                    break;
            }
        }
    }

    private ApiResult ProcessStartCommand()
    {
        if (_process?.HasExited == false)
        {
            _logger.LogWarning("Site '{SiteName}' is already running", _configuration.Name);
            return ApiResult.CreateFailure($"Site '{_configuration.Name}' is already running");
        }

        // Dispose stale process handle from a previously exited process
        DisposeStaleProcess();

        if (!File.Exists(_configuration.ApplicationRealPath))
        {
            _logger.LogError("Application binary not found: {ApplicationPath}", _configuration.ApplicationRealPath);
            return ApiResult.CreateFailure($"Application binary not found: {_configuration.ApplicationRealPath}");
        }

        try
        {
            var startInfo = CreateProcessStartInfo();
            _process = Process.Start(startInfo)!;

            _logger.LogInformation("Site '{SiteName}' started with PID {ProcessId}", _configuration.Name, _process.Id);
            return ApiResult.CreateSuccess();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start site: {SiteName}", _configuration.Name);
            return ApiResult.CreateFailure($"Failed to start site: {ex.Message}");
        }
    }

    private async Task<ApiResult> ProcessStopCommand(CancellationToken cancellationToken)
    {
        if (_process?.HasExited != false)
        {
            _logger.LogWarning("Site '{SiteName}' is already stopped (idempotent operation)", _configuration.Name);
            DisposeStaleProcess();
            return ApiResult.CreateSuccess();
        }

        var processToStop = _process;
        _process = null;

        try
        {
            await StopProcessAsync(processToStop, cancellationToken);
            _logger.LogInformation("Site '{SiteName}' stopped successfully", _configuration.Name);
            return ApiResult.CreateSuccess();
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or Win32Exception)
        {
            _logger.LogWarning(ex, "Site '{SiteName}' process no longer exists.", _configuration.Name);
            return ApiResult.CreateSuccess();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop site: {SiteName}", _configuration.Name);
            return ApiResult.CreateFailure($"Failed to stop site: {ex.Message}");
        }
    }

    private WebSiteRuntimeState BuildRuntimeState()
    {
        if (_process?.HasExited != false)
        {
            return WebSiteRuntimeState.Stopped;
        }

        var processInfo = new ProcessInfo(_process.Id, !_process.HasExited);

        return processInfo.IsResponding
            ? WebSiteRuntimeState.Running(processInfo)
            : WebSiteRuntimeState.NotResponding(processInfo);
    }

    private async Task ProcessDisposeCommand()
    {
        _logger.LogInformation("Disposing lifecycle manager for site '{SiteName}'", _configuration.Name);

        if (_process?.HasExited == false)
        {
            _logger.LogWarning("Force killing process during dispose for site '{SiteName}'", _configuration.Name);
            try
            {
                _process.Kill();
                await Task.Delay(ApplicationConstants.ProcessKillCleanupDelayMs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to kill process during dispose for site '{SiteName}'", _configuration.Name);
            }
        }

        DisposeStaleProcess();
    }

    #endregion

    #region Process Management

    /// <summary>
    /// Stops the process with graceful shutdown and timeout-based force kill.
    /// </summary>
    private async Task StopProcessAsync(Process? process, CancellationToken cancellationToken)
    {
        if (process?.HasExited != false)
        {
            _logger.LogWarning("Site '{SiteName}' process was already dead. Cleaning up state.", _configuration.Name);
            return;
        }

        ProcessTerminator.SendGracefulShutdownSignal(process);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(ApplicationConstants.DefaultProcessTimeoutSeconds * 1000);

        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
            {
                await ForceKillProcessAsync(process, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Force kills a process that didn't stop gracefully.
    /// </summary>
    private async Task ForceKillProcessAsync(Process process, CancellationToken cancellationToken)
    {
        _logger.LogWarning("Site '{SiteName}' did not stop gracefully. Force killing process.", _configuration.Name);

        try
        {
            process.Kill();
            await Task.Delay(ApplicationConstants.ProcessKillCleanupDelayMs, cancellationToken);
        }
        catch (Exception killEx)
        {
            _logger.LogError(killEx, "Failed to force kill process for site '{SiteName}'. Process may still be running.", _configuration.Name);
        }
    }

    /// <summary>
    /// Creates the process start info with environment variables.
    /// </summary>
    private ProcessStartInfo CreateProcessStartInfo()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = ApplicationConstants.DotnetExecutable,
            Arguments = _configuration.ApplicationRealPath,
            WorkingDirectory = Path.GetDirectoryName(_configuration.ApplicationRealPath)!,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        startInfo.Environment[ApplicationConstants.AspNetCoreUrlsEnvironmentVariable] = $"http://localhost:{_configuration.InternalPort}";
        startInfo.Environment[ApplicationConstants.AspNetCoreEnvironmentVariable] = _configuration.Environment;

        foreach (var envVar in _configuration.AdditionalEnvironmentVariables)
        {
            startInfo.Environment[envVar.Key] = envVar.Value;
        }

        return startInfo;
    }

    /// <summary>
    /// Disposes a stale process handle and clears the reference.
    /// </summary>
    private void DisposeStaleProcess()
    {
        _process?.Dispose();
        _process = null;
    }

    #endregion

    #region Command Types

    private abstract record LifecycleCommand;
    private sealed record StartCommand(TaskCompletionSource<ApiResult> Result) : LifecycleCommand;
    private sealed record StopCommand(TaskCompletionSource<ApiResult> Result, CancellationToken CancellationToken) : LifecycleCommand;
    private sealed record GetStateCommand(TaskCompletionSource<WebSiteRuntimeState> Result) : LifecycleCommand;
    private sealed record DisposeCommand : LifecycleCommand;

    #endregion
}
