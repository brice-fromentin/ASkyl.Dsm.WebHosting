using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Logging;

/// <summary>Category marker for ILogger&lt;T&gt; — no implementation required.</summary>
public interface ILogDsmConnectionDiagnosticService { }

/// <summary>
/// Structured logging extension methods for the DSM connection diagnostic.
/// </summary>
public static partial class DsmConnectionDiagnosticLoggingExtensions
{
    /// <summary>
    /// Logs that every check of the DSM connection passed.
    /// </summary>
    [LoggerMessage(EventId = 3100001, Level = LogLevel.Information, Message = "DSM connection healthy: {Host}:{Port}")]
    public static partial void DsmConnectionHealthy(this ILogger<ILogDsmConnectionDiagnosticService> logger, string host, int port);

    /// <summary>
    /// Logs the first failing check of the DSM connection and why it failed.
    /// </summary>
    [LoggerMessage(EventId = 3100002, Level = LogLevel.Warning, Message = "DSM connection check {Check} failed ({Failure}) for {Host}:{Port}")]
    public static partial void DsmConnectionFailed(this ILogger<ILogDsmConnectionDiagnosticService> logger, string check, string failure, string host, int port);
}
