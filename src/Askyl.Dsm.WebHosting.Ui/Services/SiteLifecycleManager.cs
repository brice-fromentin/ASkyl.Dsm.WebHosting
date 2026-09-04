using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Channels;
using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Domain.WebSites;
using Askyl.Dsm.WebHosting.Data.Results;
using Askyl.Dsm.WebHosting.Globalization;
using Askyl.Dsm.WebHosting.Logging;
using Askyl.Dsm.WebHosting.Tools.Infrastructure;

namespace Askyl.Dsm.WebHosting.Ui.Services;

/// <summary>
/// Manages the complete lifecycle of a single website instance.
/// Uses a channel-based command queue to serialize all operations — no semaphore needed.
/// Disposal is fire-and-forget: it queues a dispose command and returns, and the loop drains the
/// remaining commands in the background. Callers needing the process actually stopped must await
/// StopAsync first, as WebSiteHostingService.StopAllSitesAsync does.
/// </summary>
public sealed class SiteLifecycleManager(
    ILogger<ILogSiteLifecycleManager> logger,
    ILocalizer localizer,
    IProcessRunner processRunner,
    IAssemblyRuntimeDetector assemblyRuntimeDetector,
    WebSiteConfiguration configuration) : IDisposable
{
    private readonly Channel<LifecycleCommand> _channel = Channel.CreateBounded<LifecycleCommand>(new BoundedChannelOptions(WebSiteConstants.CommandChannelCapacity)
    {
        FullMode = BoundedChannelFullMode.Wait,
        SingleReader = true,
        SingleWriter = false
    });
    private readonly Lock _commandLoopLock = new();
    private volatile Task? _commandLoop;
    private IProcessHandle? _process;
    private volatile bool _isDisposing;

    /// <summary>
    /// Starts the website process with configured environment variables.
    /// Returns failure if already running, executable not found, or manager is disposing.
    /// </summary>
    public async Task<ApiResult> StartAsync()
    {
        if (_isDisposing)
        {
            logger.CannotStartSiteDisposing(configuration.Name);
            return ApiResult.CreateFailure(localizer[LK.Error.SiteConfigUpdating]);
        }

        EnsureLoopStarted();
        var tcs = new TaskCompletionSource<ApiResult>(TaskCreationOptions.RunContinuationsAsynchronously);

        if (!_channel.Writer.TryWrite(new StartCommand(tcs)))
        {
            return ApiResult.CreateFailure(localizer[LK.Error.FailedToQueueStart]);
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
            logger.CannotStopSiteDisposing(configuration.Name);
            return ApiResult.CreateFailure(localizer[LK.Error.SiteConfigUpdating]);
        }

        EnsureLoopStarted();
        var tcs = new TaskCompletionSource<ApiResult>(TaskCreationOptions.RunContinuationsAsynchronously);

        if (!_channel.Writer.TryWrite(new StopCommand(tcs, cancellationToken)))
        {
            return ApiResult.CreateFailure(localizer[LK.Error.FailedToQueueStop]);
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
            return new WebSiteRuntimeState(false, null);
        }

        EnsureLoopStarted();
        var tcs = new TaskCompletionSource<WebSiteRuntimeState>(TaskCreationOptions.RunContinuationsAsynchronously);

        if (!_channel.Writer.TryWrite(new GetStateCommand(tcs)))
        {
            return new WebSiteRuntimeState(false, null);
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
    /// Starts the single consumer loop on first use. Double-checked locking preserves the
    /// channel's SingleReader contract when concurrent callers reach this at the same time.
    /// </summary>
    private void EnsureLoopStarted()
    {
        if (_commandLoop is not null)
        {
            return;
        }

        lock (_commandLoopLock)
        {
            _commandLoop ??= ProcessSiteCommandsAsync();
        }
    }

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
                try
                {
                    if (await ExecuteCommandAsync(command).ConfigureAwait(false))
                    {
                        return;
                    }
                }
                catch (Exception ex)
                {
                    // A handler fault must never escape: it would kill this loop and leave every
                    // pending caller awaiting a TaskCompletionSource nobody will ever complete.
                    // Release the caller before logging so a failing logger cannot strand it.
                    command.Fail(localizer[LK.Error.OperationFailed]);
                    logger.LifecycleCommandFailed(ex, command.GetType().Name, configuration.Name);
                }
            }
        }
    }

    /// <summary>
    /// Executes a single command and completes its caller.
    /// Returns true when the loop must terminate (disposal).
    /// </summary>
    private async Task<bool> ExecuteCommandAsync(LifecycleCommand command)
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
                return true;

            default:
                break;
        }

        return false;
    }

    private ApiResult ProcessStartCommand()
    {
        if (_process?.HasExited == false)
        {
            logger.SiteAlreadyRunning(configuration.Name);
            return ApiResult.CreateFailure(localizer[LK.Error.SiteAlreadyRunning, configuration.Name]);
        }

        // Dispose stale process handle from a previously exited process
        DisposeStaleProcess();

        if (!File.Exists(configuration.ApplicationRealPath))
        {
            logger.ApplicationBinaryNotFound(configuration.ApplicationRealPath);
            return ApiResult.CreateFailure(localizer[LK.Error.ApplicationBinaryNotFound, configuration.ApplicationRealPath]);
        }

        // Detect and validate framework compatibility
        var runtimeInfo = assemblyRuntimeDetector.Detect(configuration.ApplicationRealPath);

        if (runtimeInfo is { IsCompatible: false })
        {
            // The result is for whoever is looking at the interface, so it is localized. The log is not:
            // it is read later, by whoever is diagnosing, and a NAS set to another language turned this
            // line into one nobody can grep for.
            logger.SiteStartBlockedIncompatible(configuration.Name, runtimeInfo.Channel);

            return ApiResult.CreateFailure(localizer[LK.Error.RuntimeNotInstalled, runtimeInfo.Channel]);
        }

        try
        {
            var startInfo = CreateProcessStartInfo();
            _process = processRunner.Start(startInfo);

            logger.SiteStarted(configuration.Name, _process.Id);
            return ApiResult.CreateSuccess();
        }
        catch (Exception ex)
        {
            logger.FailedToStartSite(ex, configuration.Name);
            return ApiResult.CreateFailure(localizer[LK.Error.OperationFailed]);
        }
    }

    private async Task<ApiResult> ProcessStopCommand(CancellationToken cancellationToken)
    {
        if (_process?.HasExited != false)
        {
            logger.SiteAlreadyStopped(configuration.Name);
            DisposeStaleProcess();
            return ApiResult.CreateSuccess();
        }

        var processToStop = _process;
        _process = null;

        try
        {
            await StopProcessAsync(processToStop, cancellationToken);
            logger.SiteStopped(configuration.Name);
            return ApiResult.CreateSuccess();
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or Win32Exception)
        {
            logger.SiteProcessNotFound(ex, configuration.Name);
            return ApiResult.CreateSuccess();
        }
        catch (Exception ex)
        {
            logger.FailedToStopSite(ex, configuration.Name);
            return ApiResult.CreateFailure(localizer[LK.Error.OperationFailed]);
        }
    }

    private WebSiteRuntimeState BuildRuntimeState()
    {
        if (_process?.HasExited != false)
        {
            return new WebSiteRuntimeState(false, null);
        }

        var processInfo = new ProcessInfo(_process.Id);

        return new WebSiteRuntimeState(true, processInfo);
    }

    private async Task ProcessDisposeCommand()
    {
        logger.DisposingLifecycleManager(configuration.Name);

        if (_process?.HasExited == false)
        {
            logger.ForceKillingProcessOnDispose(configuration.Name);

            try
            {
                _process.Kill();
                await Task.Delay(WebSiteConstants.ProcessKillCleanupDelayMs).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.FailedToKillProcessOnDispose(ex, configuration.Name);
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
            logger.ProcessAlreadyDead(configuration.Name);
            return;
        }

        process.SendGracefulShutdownSignal();

        int timeoutSeconds = configuration.ProcessTimeoutSeconds;
        logger.SentSigTerm(configuration.Name, timeoutSeconds);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeoutSeconds * WebSiteConstants.MillisecondsPerSecond);

        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
            {
                logger.ProcessWaitTimeout(configuration.Name, process.Id, timeoutSeconds * WebSiteConstants.MillisecondsPerSecond);
                await ForceKillProcessAsync(process, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Force kills a process that didn't stop gracefully.
    /// </summary>
    private async Task ForceKillProcessAsync(IProcessHandle process, CancellationToken cancellationToken)
    {
        logger.DidNotStopGracefully(configuration.Name);

        try
        {
            process.Kill();
            await Task.Delay(WebSiteConstants.ProcessKillCleanupDelayMs, cancellationToken);
        }
        catch (Exception killEx)
        {
            logger.FailedToForceKill(killEx, configuration.Name);
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
            Arguments = configuration.ApplicationRealPath,
            WorkingDirectory = Path.GetDirectoryName(configuration.ApplicationRealPath)!,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        startInfo.Environment[WebSiteConstants.AspNetCoreUrlsEnvironmentVariable] = $"http://localhost:{configuration.InternalPort}";
        startInfo.Environment[WebSiteConstants.AspNetCoreEnvironmentVariable] = configuration.Environment;

        foreach (var envVar in configuration.AdditionalEnvironmentVariables)
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

    private abstract record LifecycleCommand
    {
        /// <summary>
        /// Completes the waiting caller with a failure result when the handler throws.
        /// </summary>
        public abstract void Fail(string message);
    }

    private sealed record StartCommand(TaskCompletionSource<ApiResult> Result) : LifecycleCommand
    {
        public override void Fail(string message) => Result.TrySetResult(ApiResult.CreateFailure(message));
    }

    private sealed record StopCommand(TaskCompletionSource<ApiResult> Result, CancellationToken CancellationToken) : LifecycleCommand
    {
        public override void Fail(string message) => Result.TrySetResult(ApiResult.CreateFailure(message));
    }

    private sealed record GetStateCommand(TaskCompletionSource<WebSiteRuntimeState> Result) : LifecycleCommand
    {
        public override void Fail(string message) => Result.TrySetResult(new WebSiteRuntimeState(false, null));
    }

    private sealed record DisposeCommand : LifecycleCommand
    {
        public override void Fail(string message)
        {
            // Nothing awaits disposal.
        }
    }

    #endregion
}
