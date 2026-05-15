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
    /// Logs a login attempt for the specified user.
    /// </summary>
    [LoggerMessage(EventId = 1000005, Level = LogLevel.Debug, Message = "Login attempt for user: {Login}")]
    public static partial void LoginStarting(this ILogger<ILogAuthenticationService> logger, string login);

    /// <summary>
    /// Logs a logout initiation.
    /// </summary>
    [LoggerMessage(EventId = 1000006, Level = LogLevel.Debug, Message = "Logout initiated")]
    public static partial void LogoutStarting(this ILogger<ILogAuthenticationService> logger);

    /// <summary>
    /// Logs the duration of a login operation.
    /// </summary>
    [LoggerMessage(EventId = 1000007, Level = LogLevel.Debug, Message = "Login completed in {Duration}ms for user: {Login}")]
    public static partial void LoginDuration(this ILogger<ILogAuthenticationService> logger, long duration, string login);

    /// <summary>
    /// Logs the duration of a logout operation.
    /// </summary>
    [LoggerMessage(EventId = 1000008, Level = LogLevel.Debug, Message = "Logout completed in {Duration}ms")]
    public static partial void LogoutDuration(this ILogger<ILogAuthenticationService> logger, long duration);
}
