using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Logging;

/// <summary>Category marker for ILogger&lt;T&gt; — no implementation required.</summary>
public interface ILogSystemProcessRunner { }

/// <summary>
/// Structured logging extension methods for process spawning operations.
/// </summary>
public static partial class ProcessRunnerLoggingExtensions
{
    /// <summary>
    /// Logs process spawn with working directory and arguments.
    /// </summary>
    [LoggerMessage(EventId = 2500001, Level = LogLevel.Debug, Message = "Spawning process: {FileName} {Arguments} (WorkingDirectory: {WorkingDirectory})")]
    public static partial void ProcessSpawned(this ILogger<ILogSystemProcessRunner> logger, string fileName, string arguments, string workingDirectory);

    /// <summary>
    /// Logs a line written by a hosted process to its standard output.
    /// </summary>
    [LoggerMessage(EventId = 2500002, Level = LogLevel.Information, Message = "Process {ProcessId} stdout: {Line}")]
    public static partial void ProcessOutputReceived(this ILogger<ILogSystemProcessRunner> logger, int processId, string line);

    /// <summary>
    /// Logs a line written by a hosted process to its standard error.
    /// </summary>
    [LoggerMessage(EventId = 2500003, Level = LogLevel.Error, Message = "Process {ProcessId} stderr: {Line}")]
    public static partial void ProcessErrorReceived(this ILogger<ILogSystemProcessRunner> logger, int processId, string line);
}
