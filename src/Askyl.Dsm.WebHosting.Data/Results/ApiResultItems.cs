using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.Results;

/// <summary>
/// Generic result type for operations that return a list of items.
/// The type parameter TItem represents the item type, not the collection type.
/// </summary>
/// <typeparam name="TItem">The type of items in the list.</typeparam>
public abstract class ApiResultItems<TItem>(bool success, string? message, List<TItem>? value, ApiErrorCode errorCode = default)
    where TItem : class
{
    [JsonConstructor]
    protected ApiResultItems() : this(false, null, default!, ApiErrorCode.Failure) { }

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
    /// The list of items returned by the operation. Null if operation failed.
    /// Only access this property when Success is true.
    /// </summary>
    public List<TItem>? Value { get; set; } = value;
}
