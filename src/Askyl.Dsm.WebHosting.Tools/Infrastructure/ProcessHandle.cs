using System.Diagnostics;
using Askyl.Dsm.WebHosting.Logging;
using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Tools.Infrastructure;

/// <summary>
/// Handle to a running process, abstracting <see cref="Process"/> for testability.
/// </summary>
public interface IProcessHandle : IDisposable
{
    /// <summary>
    /// Gets the operating system process identifier.
    /// </summary>
    int Id { get; }

    /// <summary>
    /// Gets whether the process has terminated.
    /// </summary>
    bool HasExited { get; }

    /// <summary>
    /// Sends a graceful shutdown signal (SIGTERM on Unix, CloseMainWindow on Windows).
    /// </summary>
    void SendGracefulShutdownSignal();

    /// <summary>
    /// Waits asynchronously for the process to exit.
    /// </summary>
    Task WaitForExitAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Forcefully terminates the process.
    /// </summary>
    void Kill();
}

/// <summary>
/// Wraps <see cref="Process"/> to implement <see cref="IProcessHandle"/>.
/// Delegates graceful shutdown to <see cref="ProcessTerminator"/>.
/// </summary>
/// <param name="logger">Logger instance.</param>
/// <param name="process">The underlying process.</param>
internal sealed class SystemProcessHandle(ILogger<SystemProcessRunner> logger, Process process) : IProcessHandle
{
    private bool _isDisposed;

    public int Id => process.Id;
    public bool HasExited => process.HasExited;

    public void SendGracefulShutdownSignal()
    {
        if (process.HasExited)
        {
            return;
        }

        logger.SigTermSent(process.Id);

        try
        {
            ProcessTerminator.SendGracefulShutdownSignal(process);
        }
        catch (Exception ex)
        {
            logger.FailedToTerminateProcess(ex, process.Id);
            throw;
        }
    }

    public async Task WaitForExitAsync(CancellationToken cancellationToken)
    {
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        if (!process.HasExited)
        {
            return;
        }

        logger.ProcessExited(process.Id, process.ExitCode);
    }

    public void Kill()
    {
        logger.SigKillSent(process.Id);
        process.Kill();
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        process.Dispose();
        _isDisposed = true;
    }
}
