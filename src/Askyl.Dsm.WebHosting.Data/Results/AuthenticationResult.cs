using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.Results;

/// <summary>
/// Result of an authentication operation (login attempt).
/// Used by API endpoints to return success/failure status with message.
/// </summary>
public sealed class AuthenticationResult : ApiResult
{
    [JsonConstructor]
    private AuthenticationResult() : base() { }

    private AuthenticationResult(bool success, string? message, string? culture, ApiErrorCode errorCode)
        : base(success, message, errorCode)
    {
        Culture = culture;
    }

    /// <summary>
    /// The resolved culture in .NET format (e.g. "en-US").
    /// The server chooses between user preference and system fallback.
    /// Only populated on successful authentication.
    /// </summary>
    public string? Culture { get; set; }

    /// <summary>
    /// Indicates whether the user is authenticated. This property is an alias for Success
    /// and provides semantic clarity in authentication contexts. Not serialized to JSON.
    /// </summary>
    [JsonIgnore]
    public bool IsAuthenticated => Success;

    /// <summary>
    /// Creates a successful authentication result where the user is authenticated.
    /// </summary>
    /// <param name="message">Optional success message.</param>
    /// <param name="culture">The resolved culture (user preference or system fallback), or null to let client use browser/system fallback.</param>
    public static AuthenticationResult CreateAuthenticated(string? message, string? culture)
        => new(true, message, culture, ApiErrorCode.None);

    /// <summary>
    /// Creates an authentication failure result where credentials were invalid.
    /// </summary>
    /// <param name="message">Error message describing the authentication failure.</param>
    public static AuthenticationResult CreateNotAuthenticated(string message)
        => new(false, message, null, ApiErrorCode.Failure);
}
