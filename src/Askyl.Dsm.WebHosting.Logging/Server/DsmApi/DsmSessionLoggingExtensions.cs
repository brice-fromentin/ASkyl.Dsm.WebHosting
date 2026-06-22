using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Logging;

/// <summary>Category marker for ILogger&lt;T&gt; — no implementation required.</summary>
public interface ILogDsmSession { }

/// <summary>
/// Structured logging extension methods for DSM session management.
/// </summary>
public static partial class DsmSessionLoggingExtensions
{
    /// <summary>
    /// Logs that the DSM handshake succeeded.
    /// </summary>
    [LoggerMessage(EventId = 2900001, Level = LogLevel.Information, Message = "Handshake successful")]
    public static partial void HandshakeSuccess(this ILogger<ILogDsmSession> logger);

    /// <summary>
    /// Logs that the DSM handshake failed.
    /// </summary>
    [LoggerMessage(EventId = 2900002, Level = LogLevel.Warning, Message = "Handshake failed")]
    public static partial void HandshakeFailure(this ILogger<ILogDsmSession> logger);

    /// <summary>
    /// Logs that authentication succeeded for the given login.
    /// </summary>
    [LoggerMessage(EventId = 2900003, Level = LogLevel.Information, Message = "Authentication successful: {Login}")]
    public static partial void AuthenticationSuccess(this ILogger<ILogDsmSession> logger, string login);

    /// <summary>
    /// Logs that authentication failed with an error message.
    /// </summary>
    [LoggerMessage(EventId = 2900004, Level = LogLevel.Warning, Message = "Authentication failed: {ErrorMessage}")]
    public static partial void AuthenticationFailed(this ILogger<ILogDsmSession> logger, string errorMessage);

    /// <summary>
    /// Logs that fetching user preferences failed.
    /// </summary>
    [LoggerMessage(EventId = 2900005, Level = LogLevel.Debug, Message = "Failed to fetch user preferences: {Error}")]
    public static partial void FetchUserPreferencesFailed(this ILogger<ILogDsmSession> logger, string error);

    /// <summary>
    /// Logs that the session is being disconnected.
    /// </summary>
    [LoggerMessage(EventId = 2900006, Level = LogLevel.Information, Message = "Disconnecting from DSM")]
    public static partial void Disconnecting(this ILogger<ILogDsmSession> logger);

    /// <summary>
    /// Logs that the session has been disconnected.
    /// </summary>
    [LoggerMessage(EventId = 2900007, Level = LogLevel.Information, Message = "Disconnected from DSM")]
    public static partial void Disconnected(this ILogger<ILogDsmSession> logger);
}
