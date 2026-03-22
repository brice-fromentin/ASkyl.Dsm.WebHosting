using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.Results;

/// <summary>
/// Non-generic result type for operations that don't return data.
/// </summary>
public class ApiResult(bool success, string? message, ApiErrorCode errorCode = default)
{
    [JsonConstructor]
    private ApiResult() : this(false, null, ApiErrorCode.Failure) { }

    /// <summary>
    /// Indicates whether the operation succeeded (true) or failed (false).
    /// </summary>
    public bool Success { get; set; } = success;

    /// <summary>
    /// Message describing the result. Can be null or contain informational text in both
    /// successful and failed operations. For example: "Installation completed successfully."
    /// or "Invalid credentials".
    /// </summary>
    public string? Message { get; set; } = message;

    /// <summary>
    /// Error code providing programmatic access to the type of error.
    /// Use ApiErrorCode.None for success.
    /// </summary>
    public ApiErrorCode ErrorCode { get; set; } = errorCode != default ? errorCode : (success ? ApiErrorCode.None : ApiErrorCode.Failure);

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="message">Optional success message.</param>
    public static ApiResult CreateSuccess(string? message = null)
        => new(true, message, ApiErrorCode.None);

    /// <summary>
    /// Creates a failure result with an error message.
    /// </summary>
    /// <param name="message">Error message describing what went wrong.</param>
    public static ApiResult CreateFailure(string message)
        => new(false, message, ApiErrorCode.Failure);

    /// <summary>
    /// Creates a failure result with a specific error code.
    /// </summary>
    /// <param name="errorCode">The error code.</param>
    /// <param name="message">Error message describing what went wrong.</param>
    public static ApiResult CreateFailure(ApiErrorCode errorCode, string message)
        => new(false, message, errorCode);
}
