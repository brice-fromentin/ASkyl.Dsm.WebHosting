using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.Results;

/// <summary>
/// Result of an authentication operation (login attempt).
/// Used by API endpoints to return success/failure status with message.
/// </summary>
public sealed class AuthenticationResult(bool success, string? message, ApiErrorCode errorCode = default)
    : ApiResult(success, message, errorCode)
{
    [JsonConstructor]
    private AuthenticationResult() : this(false, null, ApiErrorCode.Failure) { }

    /// <summary>
    /// Indicates whether the user is authenticated. This property is an alias for Success
    /// and provides semantic clarity in authentication contexts.
    /// </summary>
    public bool IsAuthenticated => Success;

    /// <summary>
    /// Creates a successful authentication result where the user is authenticated.
    /// </summary>
    /// <param name="message">Optional success message.</param>
    public static AuthenticationResult CreateAuthenticated(string? message = null)
        => new(true, message, ApiErrorCode.None);

    /// <summary>
    /// Creates an authentication failure result where credentials were invalid.
    /// </summary>
    /// <param name="message">Error message describing the authentication failure.</param>
    public static AuthenticationResult CreateNotAuthenticated(string message)
        => new(false, message, ApiErrorCode.Failure);

    /// <summary>
    /// Creates an authentication failure result with a specific error code.
    /// </summary>
    /// <param name="errorCode">The API error code.</param>
    /// <param name="message">Error message describing the authentication failure.</param>
    public static new AuthenticationResult CreateFailure(ApiErrorCode errorCode, string message)
        => new(false, message, errorCode);
}
