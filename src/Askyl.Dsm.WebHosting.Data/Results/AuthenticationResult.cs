using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.Results;

/// <summary>
/// Result of an authentication operation (login attempt).
/// Used by API endpoints to return success/failure status with message.
/// </summary>
public sealed class AuthenticationResult(bool success, string? message, string? culture = null, string? timezone = null, ApiErrorCode errorCode = default)
    : ApiResult(success, message, errorCode)
{
    [JsonConstructor]
    private AuthenticationResult() : this(false, null, null, null, ApiErrorCode.Failure) { }

    /// <summary>
    /// Indicates whether the user is authenticated. This property is an alias for Success
    /// and provides semantic clarity in authentication contexts.
    /// </summary>
    public bool IsAuthenticated => Success;

    /// <summary>
    /// The resolved culture in .NET format (e.g. "en-US").
    /// The server chooses between user preference and system fallback.
    /// Only populated on successful authentication.
    /// </summary>
    [JsonPropertyName("culture")]
    public string? Culture { get; } = culture;

    /// <summary>
    /// The system timezone in IANA format (e.g. "Europe/Amsterdam").
    /// Converted on the server from the DSM timezone.
    /// Only populated on successful authentication.
    /// </summary>
    [JsonPropertyName("timezone")]
    public string? Timezone { get; } = timezone;

    /// <summary>
    /// Creates a successful authentication result where the user is authenticated.
    /// </summary>
    /// <param name="message">Optional success message.</param>
    /// <param name="culture">The resolved culture (user preference or system fallback).</param>
    /// <param name="timezone">The system timezone in IANA format.</param>
    public static AuthenticationResult CreateAuthenticated(string? message, string culture, string timezone)
        => new(true, message, culture, timezone, ApiErrorCode.None);

    /// <summary>
    /// Creates an authentication failure result where credentials were invalid.
    /// </summary>
    /// <param name="message">Error message describing the authentication failure.</param>
    public static AuthenticationResult CreateNotAuthenticated(string message)
        => new(false, message, null, null, ApiErrorCode.Failure);
}
