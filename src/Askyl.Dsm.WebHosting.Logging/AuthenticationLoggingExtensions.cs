using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Logging;

/// <summary>
/// Structured logging extension methods for authentication-related events.
/// </summary>
public static partial class AuthenticationLoggingExtensions
{
    /// <summary>
    /// Logs a login failure for the specified user.
    /// </summary>
    [LoggerMessage(EventId = 1001, Level = LogLevel.Warning, Message = "Login failed for user: {Login}")]
    public static partial void LoginFailed(this ILogger logger, string login);

    /// <summary>
    /// Logs a successful login event for the specified user.
    /// </summary>
    [LoggerMessage(EventId = 1002, Level = LogLevel.Information, Message = "Login successful for user: {Login} - SID stored")]
    public static partial void LoginSuccessful(this ILogger logger, string login);

    /// <summary>
    /// Logs a user logout event.
    /// </summary>
    [LoggerMessage(EventId = 1003, Level = LogLevel.Information, Message = "User logged out")]
    public static partial void UserLoggedOut(this ILogger logger);

    /// <summary>
    /// Logs an error that occurred during logout.
    /// </summary>
    [LoggerMessage(EventId = 1004, Level = LogLevel.Error, Message = "Error during logout")]
    public static partial void LogoutError(this ILogger logger, Exception ex);
}
