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
    [LoggerMessage(EventId = 2000001, Level = LogLevel.Critical, Message = "Configuration file \"{ConfigurationFileName}\" does not exist.")]
    public static partial void ConfigurationFileNotFound(this ILogger<ILogDsmApiClient> logger, string configurationFileName);

    /// <summary>
    /// Logs that configuration file was loaded with parameter count.
    /// </summary>
    [LoggerMessage(EventId = 2000002, Level = LogLevel.Debug, Message = "Configuration file loaded with {Count} parameters.")]
    public static partial void ConfigurationLoaded(this ILogger<ILogDsmApiClient> logger, int count);

    /// <summary>
    /// Logs that a mandatory configuration setting is missing or empty (critical).
    /// </summary>
    [LoggerMessage(EventId = 2000003, Level = LogLevel.Critical, Message = "Mandatory configuration setting \"{SettingKey}\" is missing or empty.")]
    public static partial void MandatorySettingMissing(this ILogger<ILogDsmApiClient> logger, string settingKey);

    /// <summary>
    /// Logs a DSM API request with method, URL, and duration.
    /// </summary>
    [LoggerMessage(EventId = 2000004, Level = LogLevel.Debug, Message = "DSM API request: {Method} {Url} - Status {StatusCode} ({Duration}ms)")]
    public static partial void ApiRequest(this ILogger<ILogDsmApiClient> logger, string method, string url, int statusCode, long duration);

    /// <summary>
    /// Logs an authentication failure or session expiration.
    /// </summary>
    [LoggerMessage(EventId = 2000005, Level = LogLevel.Warning, Message = "DSM API authentication failed: {ErrorMessage}")]
    public static partial void AuthenticationFailed(this ILogger<ILogDsmApiClient> logger, string errorMessage);

    /// <summary>
    /// Logs a DSM API error response with error code.
    /// </summary>
    [LoggerMessage(EventId = 2000006, Level = LogLevel.Error, Message = "DSM API error: {ErrorMessage} (Code: {ErrorCode})")]
    public static partial void ApiError(this ILogger<ILogDsmApiClient> logger, string errorMessage, int? errorCode);

    /// <summary>
    /// Logs the start of a connection to the DSM API.
    /// </summary>
    [LoggerMessage(EventId = 2000007, Level = LogLevel.Debug, Message = "Connecting to DSM API at {Server}:{Port}")]
    public static partial void Connecting(this ILogger<ILogDsmApiClient> logger, string server, int port);

    /// <summary>
    /// Logs successful connection to the DSM API.
    /// </summary>
    [LoggerMessage(EventId = 2000008, Level = LogLevel.Debug, Message = "Connected to DSM API")]
    public static partial void Connected(this ILogger<ILogDsmApiClient> logger);

    /// <summary>
    /// Logs the start of a disconnection from the DSM API.
    /// </summary>
    [LoggerMessage(EventId = 2000009, Level = LogLevel.Debug, Message = "Disconnecting from DSM API")]
    public static partial void Disconnecting(this ILogger<ILogDsmApiClient> logger);

    /// <summary>
    /// Logs successful disconnection from the DSM API.
    /// </summary>
    [LoggerMessage(EventId = 2000010, Level = LogLevel.Debug, Message = "Disconnected from DSM API")]
    public static partial void Disconnected(this ILogger<ILogDsmApiClient> logger);

    /// <summary>
    /// Logs the start of a handshake with the DSM API.
    /// </summary>
    [LoggerMessage(EventId = 2000011, Level = LogLevel.Debug, Message = "Starting DSM API handshake")]
    public static partial void HandshakeStarting(this ILogger<ILogDsmApiClient> logger);

    /// <summary>
    /// Logs successful handshake with the DSM API.
    /// </summary>
    [LoggerMessage(EventId = 2000012, Level = LogLevel.Debug, Message = "DSM API handshake completed successfully")]
    public static partial void HandshakeSuccess(this ILogger<ILogDsmApiClient> logger);

    /// <summary>
    /// Logs failed handshake with the DSM API.
    /// </summary>
    [LoggerMessage(EventId = 2000013, Level = LogLevel.Warning, Message = "DSM API handshake failed")]
    public static partial void HandshakeFailure(this ILogger<ILogDsmApiClient> logger);
}
