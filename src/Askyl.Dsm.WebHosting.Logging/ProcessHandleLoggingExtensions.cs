using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Logging;

/// <summary>
/// Structured logging extension methods for process handle operations.
/// </summary>
public static partial class ProcessHandleLoggingExtensions
{
    /// <summary>
    /// Logs process exit with exit code.
    /// </summary>
    [LoggerMessage(EventId = 1817, Level = LogLevel.Information, Message = "Process {ProcessId} exited with code {ExitCode}")]
    public static partial void ProcessExited(this ILogger logger, int processId, int exitCode);

    /// <summary>
    /// Logs process wait timeout.
    /// </summary>
    [LoggerMessage(EventId = 1818, Level = LogLevel.Warning, Message = "Process {ProcessId} did not exit within {TimeoutMs}ms")]
    public static partial void ProcessWaitTimeout(this ILogger logger, int processId, long timeoutMs);
}
