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
    /// Logs a DSM API request with method, URL, and duration.
    /// </summary>
    [LoggerMessage(EventId = 2000001, Level = LogLevel.Debug, Message = "DSM API request: {Method} {Url} - Status {StatusCode} ({Duration}ms)")]
    public static partial void ApiRequest(this ILogger<ILogDsmApiClient> logger, string method, string url, int statusCode, long duration);

    /// <summary>
    /// Logs that DSM refused a call. Debug, because each caller reports the outcome at the level it deserves: a wrong
    /// password, a non-administrator or an expired session is an expected refusal, not an application error.
    /// </summary>
    [LoggerMessage(EventId = 2000002, Level = LogLevel.Debug, Message = "DSM API {ApiName} refused the call with code {ErrorCode}: {ErrorMessage}")]
    public static partial void ApiError(this ILogger<ILogDsmApiClient> logger, string apiName, int? errorCode, string errorMessage);

    /// <summary>
    /// Logs successful handshake with the DSM API.
    /// </summary>
    [LoggerMessage(EventId = 2000003, Level = LogLevel.Debug, Message = "DSM API handshake completed successfully")]
    public static partial void HandshakeSuccess(this ILogger<ILogDsmApiClient> logger);

    /// <summary>
    /// Logs failed handshake with the DSM API.
    /// </summary>
    [LoggerMessage(EventId = 2000004, Level = LogLevel.Warning, Message = "DSM API handshake failed")]
    public static partial void HandshakeFailure(this ILogger<ILogDsmApiClient> logger);
}
