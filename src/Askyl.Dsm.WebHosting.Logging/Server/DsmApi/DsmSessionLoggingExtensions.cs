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
    [LoggerMessage(EventId = 2900005, Level = LogLevel.Debug, Message = "Failed to fetch user preferences")]
    public static partial void FetchUserPreferencesFailed(this ILogger<ILogDsmSession> logger, Exception exception);

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

    /// <summary>
    /// Logs that a user authenticated against DSM but lacks administrator rights.
    /// </summary>
    [LoggerMessage(EventId = 2900008, Level = LogLevel.Warning, Message = "Login rejected, not a DSM administrator: {Login}")]
    public static partial void NotAnAdministrator(this ILogger<ILogDsmSession> logger, string login);

    /// <summary>
    /// Logs that the DSM session was revoked server-side, so the SID is no longer usable on the NAS.
    /// </summary>
    [LoggerMessage(EventId = 2900009, Level = LogLevel.Information, Message = "DSM session revoked")]
    public static partial void SessionRevoked(this ILogger<ILogDsmSession> logger);

    /// <summary>
    /// Logs that DSM refused the logout call, leaving the SID valid on the NAS until it expires.
    /// </summary>
    [LoggerMessage(EventId = 2900010, Level = LogLevel.Warning, Message = "DSM refused the session revocation (error {ErrorCode}); the SID stays valid on the NAS until it expires")]
    public static partial void SessionRevocationRefused(this ILogger<ILogDsmSession> logger, int errorCode);

    /// <summary>
    /// Logs that the logout call could not be made at all, for example because the NAS was unreachable.
    /// </summary>
    [LoggerMessage(EventId = 2900011, Level = LogLevel.Warning, Message = "DSM session revocation call failed; the SID stays valid on the NAS until it expires")]
    public static partial void SessionRevocationFailed(this ILogger<ILogDsmSession> logger, Exception exception);

    /// <summary>
    /// Logs a session validated by an actual call to DSM, as opposed to one served from the cache.
    /// </summary>
    [LoggerMessage(EventId = 2900012, Level = LogLevel.Debug, Message = "DSM session validated against SYNO.Core.User and cached for {TtlMinutes} minutes")]
    public static partial void SessionValidatedAgainstDsm(this ILogger<ILogDsmSession> logger, int ttlMinutes);
}
