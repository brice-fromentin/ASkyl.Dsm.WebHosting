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
    [LoggerMessage(EventId = 1816, Level = LogLevel.Debug, Message = "Spawning process: {FileName} {Arguments} (WorkingDirectory: {WorkingDirectory})")]
    public static partial void ProcessSpawned(this ILogger<ILogSystemProcessRunner> logger, string fileName, string arguments, string workingDirectory);
}
