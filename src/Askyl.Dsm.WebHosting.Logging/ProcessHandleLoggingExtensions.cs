using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Logging;

/// <summary>Category marker for ILogger&lt;T&gt; — no implementation required.</summary>
public interface ILogSystemProcessHandle { }

/// <summary>
/// Structured logging extension methods for process handle operations.
/// </summary>
public static partial class ProcessHandleLoggingExtensions
{
    /// <summary>
    /// Logs SIGTERM signal sent to process.
    /// </summary>
    [LoggerMessage(EventId = 2600001, Level = LogLevel.Information, Message = "Sending SIGTERM to process {ProcessId}")]
    public static partial void SigTermSent(this ILogger<ILogSystemProcessHandle> logger, int processId);

    /// <summary>
    /// Logs failure to terminate process.
    /// </summary>
    [LoggerMessage(EventId = 2600002, Level = LogLevel.Error, Message = "Failed to terminate process {ProcessId}")]
    public static partial void FailedToTerminateProcess(this ILogger<ILogSystemProcessHandle> logger, Exception ex, int processId);

    /// <summary>
    /// Logs process exit with exit code.
    /// </summary>
    [LoggerMessage(EventId = 2600003, Level = LogLevel.Information, Message = "Process {ProcessId} exited with code {ExitCode}")]
    public static partial void ProcessExited(this ILogger<ILogSystemProcessHandle> logger, int processId, int exitCode);

    /// <summary>
    /// Logs process wait timeout.
    /// </summary>
    [LoggerMessage(EventId = 2600004, Level = LogLevel.Warning, Message = "Process {ProcessId} did not exit within {TimeoutMs}ms")]
    public static partial void ProcessWaitTimeout(this ILogger<ILogSystemProcessHandle> logger, int processId, long timeoutMs);

    /// <summary>
    /// Logs SIGKILL signal sent to process.
    /// </summary>
    [LoggerMessage(EventId = 2600005, Level = LogLevel.Warning, Message = "Sending SIGKILL to process {ProcessId}")]
    public static partial void SigKillSent(this ILogger<ILogSystemProcessHandle> logger, int processId);
}
