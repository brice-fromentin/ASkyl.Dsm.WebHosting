using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Channels;
using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Data.Domain.WebSites;
using Askyl.Dsm.WebHosting.Data.Results;
using Askyl.Dsm.WebHosting.Logging;
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
    private readonly IProcessRunner _processRunner;
    private readonly WebSiteConfiguration _configuration;
    private readonly Channel<LifecycleCommand> _channel = Channel.CreateBounded<LifecycleCommand>(new BoundedChannelOptions(16)
    {
        FullMode = BoundedChannelFullMode.Wait,
        SingleReader = true,
        SingleWriter = false
    });
    private readonly Task _loopTask;
    private IProcessHandle? _process;
    private volatile bool _isDisposing;

    public SiteLifecycleManager(ILogger<SiteLifecycleManager> logger, IProcessRunner processRunner, WebSiteConfiguration configuration)
    {
        _logger = logger;
        _processRunner = processRunner;
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
            _logger.CannotStartSiteDisposing(_configuration.Name);
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
            _logger.CannotStopSiteDisposing(_configuration.Name);
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
    /// Disposes managed resources. Fires dispose command and completes the channel.
    /// The command loop drains pending commands and cleans up in the background.
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
    }

    #region Command Loop

    /// <summary>
    /// Single consumer loop — all state mutation happens here.
    /// Commands execute sequentially, eliminating TOCTOU races.
    /// Uses ConfigureAwait(false) to avoid capturing synchronization context
    /// (this runs on background thread pool, no UI/context dependencies).
    /// </summary>
    private async Task ProcessSiteCommandsAsync()
    {
        while (await _channel.Reader.WaitToReadAsync().ConfigureAwait(false))
        {
            while (_channel.Reader.TryRead(out var command))
            {
                switch (command)
                {
                    case StartCommand start:
                        start.Result.SetResult(ProcessStartCommand());
                        break;

                    case StopCommand stop:
                        stop.Result.SetResult(await ProcessStopCommand(stop.CancellationToken).ConfigureAwait(false));
                        break;

                    case GetStateCommand state:
                        state.Result.SetResult(BuildRuntimeState());
                        break;

                    case DisposeCommand:
                        await ProcessDisposeCommand().ConfigureAwait(false);
                        return;

                    default:
                        break;
                }
            }
        }
    }

    private ApiResult ProcessStartCommand()
    {
        if (_process?.HasExited == false)
        {
            _logger.SiteAlreadyRunning(_configuration.Name);
            return ApiResult.CreateFailure($"Site '{_configuration.Name}' is already running");
        }

        // Dispose stale process handle from a previously exited process
        DisposeStaleProcess();

        if (!File.Exists(_configuration.ApplicationRealPath))
        {
            _logger.ApplicationBinaryNotFound(_configuration.ApplicationRealPath);
            return ApiResult.CreateFailure($"Application binary not found: {_configuration.ApplicationRealPath}");
        }

        try
        {
            var startInfo = CreateProcessStartInfo();
            _process = _processRunner.Start(startInfo);

            _logger.SiteStarted(_configuration.Name, _process.Id);
            return ApiResult.CreateSuccess();
        }
        catch (Exception ex)
        {
            _logger.FailedToStartSite(ex, _configuration.Name);
            return ApiResult.CreateFailure($"Failed to start site: {ex.Message}");
        }
    }

    private async Task<ApiResult> ProcessStopCommand(CancellationToken cancellationToken)
    {
        if (_process?.HasExited != false)
        {
            _logger.SiteAlreadyStopped(_configuration.Name);
            DisposeStaleProcess();
            return ApiResult.CreateSuccess();
        }

        var processToStop = _process;
        _process = null;

        try
        {
            await StopProcessAsync(processToStop, cancellationToken);
            _logger.SiteStopped(_configuration.Name);
            return ApiResult.CreateSuccess();
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or Win32Exception)
        {
            _logger.SiteProcessNotFound(ex, _configuration.Name);
            return ApiResult.CreateSuccess();
        }
        catch (Exception ex)
        {
            _logger.FailedToStopSite(ex, _configuration.Name);
            return ApiResult.CreateFailure($"Failed to stop site: {ex.Message}");
        }
    }

    private WebSiteRuntimeState BuildRuntimeState()
    {
        if (_process?.HasExited != false)
        {
            return WebSiteRuntimeState.Stopped;
        }

        var processInfo = new ProcessInfo(_process.Id);

        return WebSiteRuntimeState.Running(processInfo);
    }

    private async Task ProcessDisposeCommand()
    {
        _logger.DisposingLifecycleManager(_configuration.Name);

        if (_process?.HasExited == false)
        {
            _logger.ForceKillingProcessOnDispose(_configuration.Name);
            try
            {
                _process.Kill();
                await Task.Delay(WebSiteConstants.ProcessKillCleanupDelayMs).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.FailedToKillProcessOnDispose(ex, _configuration.Name);
            }
        }

        DisposeStaleProcess();
    }

    #endregion

    #region Process Management

    /// <summary>
    /// Stops the process with graceful shutdown and timeout-based force kill.
    /// </summary>
    private async Task StopProcessAsync(IProcessHandle? process, CancellationToken cancellationToken)
    {
        if (process?.HasExited != false)
        {
            _logger.ProcessAlreadyDead(_configuration.Name);
            return;
        }

        process.SendGracefulShutdownSignal();

        int timeoutSeconds = _configuration.ProcessTimeoutSeconds;
        _logger.SentSigTerm(_configuration.Name, timeoutSeconds);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeoutSeconds * 1000);

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
    private async Task ForceKillProcessAsync(IProcessHandle process, CancellationToken cancellationToken)
    {
        _logger.DidNotStopGracefully(_configuration.Name);

        try
        {
            process.Kill();
            await Task.Delay(WebSiteConstants.ProcessKillCleanupDelayMs, cancellationToken);
        }
        catch (Exception killEx)
        {
            _logger.FailedToForceKill(killEx, _configuration.Name);
        }
    }

    /// <summary>
    /// Creates the process start info with environment variables.
    /// </summary>
    private ProcessStartInfo CreateProcessStartInfo()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = WebSiteConstants.DotnetExecutable,
            Arguments = _configuration.ApplicationRealPath,
            WorkingDirectory = Path.GetDirectoryName(_configuration.ApplicationRealPath)!,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        startInfo.Environment[WebSiteConstants.AspNetCoreUrlsEnvironmentVariable] = $"http://localhost:{_configuration.InternalPort}";
        startInfo.Environment[WebSiteConstants.AspNetCoreEnvironmentVariable] = _configuration.Environment;

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
