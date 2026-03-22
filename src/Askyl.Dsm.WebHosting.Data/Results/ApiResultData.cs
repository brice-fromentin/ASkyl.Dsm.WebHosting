using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.Results;

/// <summary>
/// Generic result type for operations that return a single reference type value.
/// The type parameter T represents the reference type returned by the operation.
/// </summary>
/// <typeparam name="T">The type of reference type data returned (must be a class).</typeparam>
public abstract class ApiResultData<T>(bool success, string? message, T? value, ApiErrorCode errorCode = default)
    where T : class // Reference types only!
{
    [JsonConstructor]
    protected ApiResultData() : this(false, null, default!, ApiErrorCode.Failure) { }

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
