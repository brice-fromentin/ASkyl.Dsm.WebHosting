using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Logging;

/// <summary>Category marker for ILogger&lt;T&gt; — no implementation required.</summary>
public interface ILogAuthenticationService { }

/// <summary>
/// Structured logging extension methods for authentication-related events.
/// </summary>
public static partial class AuthenticationLoggingExtensions
{
    /// <summary>
    /// Logs a login failure for the specified user.
    /// </summary>
    [LoggerMessage(EventId = 1000001, Level = LogLevel.Warning, Message = "Login failed for user: {Login}")]
    public static partial void LoginFailed(this ILogger<ILogAuthenticationService> logger, string login);

    /// <summary>
    /// Logs a successful login event for the specified user.
    /// </summary>
    [LoggerMessage(EventId = 1000002, Level = LogLevel.Information, Message = "Login successful for user: {Login} - SID stored")]
    public static partial void LoginSuccessful(this ILogger<ILogAuthenticationService> logger, string login);

    /// <summary>
    /// Logs a user logout event.
    /// </summary>
    [LoggerMessage(EventId = 1000003, Level = LogLevel.Information, Message = "User logged out")]
    public static partial void UserLoggedOut(this ILogger<ILogAuthenticationService> logger);

    /// <summary>
    /// Logs an error that occurred during logout.
    /// </summary>
    [LoggerMessage(EventId = 1000004, Level = LogLevel.Error, Message = "Error during logout")]
    public static partial void LogoutError(this ILogger<ILogAuthenticationService> logger, Exception ex);

    /// <summary>
    /// Logs that the DSM session was validated successfully against the server.
    /// </summary>
    [LoggerMessage(EventId = 1000005, Level = LogLevel.Debug, Message = "DSM session validated successfully (cached for {TtlMinutes} minutes)")]
    public static partial void SessionValidationSuccess(this ILogger<ILogAuthenticationService> logger, int ttlMinutes);

    /// <summary>
    /// Logs that the DSM session validation failed — session is expired or invalid on the server.
    /// </summary>
    [LoggerMessage(EventId = 1000006, Level = LogLevel.Warning, Message = "DSM session validation failed — session expired or invalid on server")]
    public static partial void SessionValidationFailed(this ILogger<ILogAuthenticationService> logger);

    /// <summary>
    /// Logs that an invalid DSM session was detected and cleared from the local session store.
    /// </summary>
    [LoggerMessage(EventId = 1000007, Level = LogLevel.Information, Message = "Invalid DSM session detected — cleared from local session")]
    public static partial void SessionInvalidated(this ILogger<ILogAuthenticationService> logger);
}
