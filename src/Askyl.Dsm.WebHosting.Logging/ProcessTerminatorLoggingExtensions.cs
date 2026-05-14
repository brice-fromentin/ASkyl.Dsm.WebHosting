using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Logging;

/// <summary>
/// Structured logging extension methods for process termination operations.
/// </summary>
public static partial class ProcessTerminatorLoggingExtensions
{
    /// <summary>
    /// Logs SIGTERM signal sent to process.
    /// </summary>
    [LoggerMessage(EventId = 1819, Level = LogLevel.Information, Message = "Sending SIGTERM to process {ProcessId}")]
    public static partial void SigTermSent(this ILogger logger, int processId);

    /// <summary>
    /// Logs SIGKILL signal sent to process.
    /// </summary>
    [LoggerMessage(EventId = 1820, Level = LogLevel.Warning, Message = "Sending SIGKILL to process {ProcessId}")]
    public static partial void SigKillSent(this ILogger logger, int processId);

    /// <summary>
    /// Logs failure to terminate process.
    /// </summary>
    [LoggerMessage(EventId = 1821, Level = LogLevel.Error, Message = "Failed to terminate process {ProcessId}")]
    public static partial void FailedToTerminateProcess(this ILogger logger, Exception ex, int processId);
}
