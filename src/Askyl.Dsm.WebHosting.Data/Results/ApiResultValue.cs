using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.Results;

/// <summary>
/// Generic result type for operations that return a single value (value types only).
/// The type parameter T represents the value type returned by the operation.
/// </summary>
/// <typeparam name="T">The type of value returned (must be a struct: bool, int, Guid, DateTime, etc.).</typeparam>
public abstract class ApiResultValue<T>(bool success, string? message, T? value, ApiErrorCode errorCode = default)
    where T : struct // Value types only!
{
    [JsonConstructor]
    protected ApiResultValue() : this(false, null, default!, ApiErrorCode.Failure) { }

    /// <summary>
    /// Indicates whether the operation succeeded (true) or failed (false).
    /// </summary>
    public bool Success { get; set; } = success;

    /// <summary>
    /// Message describing the result. Can be null or contain informational text in both
    /// successful and failed operations.
    /// </summary>
    public string? Message { get; set; } = message;

    /// <summary>
    /// Error code providing programmatic access to the type of error.
    /// Use ApiErrorCode.None for success.
    /// </summary>
    public ApiErrorCode ErrorCode { get; set; } = errorCode != default ? errorCode : (success ? ApiErrorCode.None : ApiErrorCode.Failure);

    /// <summary>
    /// The value returned by the operation. Null if operation failed.
    /// Only access this property when Success is true.
    /// </summary>
    public T? Value { get; set; } = value;
}
