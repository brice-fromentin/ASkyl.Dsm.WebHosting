using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.Results;

/// <summary>
/// Represents a boolean API operation result with success status, optional error message, and data value.
/// </summary>
public sealed class ApiResultBool(bool success, string? message, bool value, ApiErrorCode errorCode = default)
    : ApiResultValue<bool>(success, message, value, errorCode)
{
    [JsonConstructor]
    private ApiResultBool() : this(false, null, false, ApiErrorCode.Failure) { }

    /// <summary>
    /// Creates a successful result with a boolean value.
    /// </summary>
    /// <param name="value">The boolean value to return.</param>
    /// <param name="message">Optional success message.</param>
    public static ApiResultBool CreateSuccess(bool value, string? message = null)
        => new(true, message, value, ApiErrorCode.None);

    /// <summary>
    /// Creates a failure result. The Value property will be false.
    /// </summary>
    /// <param name="message">Error message describing what went wrong.</param>
    public static ApiResultBool CreateFailure(string message)
        => new(false, message, false, ApiErrorCode.Failure);

    /// <summary>
    /// Creates a failure result with a specific error code.
    /// </summary>
    /// <param name="errorCode">The error code.</param>
    /// <param name="message">Error message describing what went wrong.</param>
    public static ApiResultBool CreateFailure(ApiErrorCode errorCode, string message)
        => new(false, message, false, errorCode);
}

