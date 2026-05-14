using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Logging;

/// <summary>
/// Structured logging extension methods for process spawning operations.
/// </summary>
public static partial class ProcessRunnerLoggingExtensions
{
    /// <summary>
    /// Logs process spawn with working directory and arguments.
    /// </summary>
    [LoggerMessage(EventId = 1816, Level = LogLevel.Debug, Message = "Spawning process: {FileName} {Arguments} (WorkingDirectory: {WorkingDirectory})")]
    public static partial void ProcessSpawned(this ILogger logger, string fileName, string arguments, string workingDirectory);
}
