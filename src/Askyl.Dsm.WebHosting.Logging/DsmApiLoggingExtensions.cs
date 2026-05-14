using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Logging;

/// <summary>Category marker for ILogger&lt;T&gt; — no implementation required.</summary>
public interface ILogDsmApiClient { }

/// <summary>
/// Structured logging extension methods for DSM API events.
/// </summary>
public static partial class DsmApiLoggingExtensions
{
    /// <summary>
    /// Logs that the configuration file does not exist (critical).
    /// </summary>
    [LoggerMessage(EventId = 1700, Level = LogLevel.Critical, Message = "Configuration file \"{ConfigurationFileName}\" does not exist.")]
    public static partial void ConfigurationFileNotFound(this ILogger<ILogDsmApiClient> logger, string configurationFileName);

    /// <summary>
    /// Logs that configuration file was loaded with parameter count.
    /// </summary>
    [LoggerMessage(EventId = 1701, Level = LogLevel.Debug, Message = "Configuration file loaded with {Count} parameters.")]
    public static partial void ConfigurationLoaded(this ILogger<ILogDsmApiClient> logger, int count);

    /// <summary>
    /// Logs a DSM API request with method, URL, and duration.
    /// </summary>
    [LoggerMessage(EventId = 1702, Level = LogLevel.Debug, Message = "DSM API request: {Method} {Url} - Status {StatusCode} ({Duration}ms)")]
    public static partial void ApiRequest(this ILogger<ILogDsmApiClient> logger, string method, string url, int statusCode, long duration);

    /// <summary>
    /// Logs an authentication failure or session expiration.
    /// </summary>
    [LoggerMessage(EventId = 1703, Level = LogLevel.Warning, Message = "DSM API authentication failed: {ErrorMessage}")]
    public static partial void AuthenticationFailed(this ILogger<ILogDsmApiClient> logger, string errorMessage);

    /// <summary>
    /// Logs a DSM API error response with error code.
    /// </summary>
    [LoggerMessage(EventId = 1704, Level = LogLevel.Error, Message = "DSM API error: {ErrorMessage} (Code: {ErrorCode})")]
    public static partial void ApiError(this ILogger<ILogDsmApiClient> logger, string errorMessage, int? errorCode);
}
