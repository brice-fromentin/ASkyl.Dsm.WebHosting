using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.Data.Runtime;

namespace Askyl.Dsm.WebHosting.Data.Results;

/// <summary>
/// Represents a list of ASP.NET Core channel versions result.
/// </summary>
public sealed class ChannelsResult(bool success, string? message, List<AspNetChannel>? value, ApiErrorCode errorCode = default)
    : ApiResultItems<AspNetChannel>(success, message, value, errorCode)
{
    [JsonConstructor]
    private ChannelsResult() : this(false, null, default!, ApiErrorCode.Failure) { }

    /// <summary>
    /// Creates a successful result with the list of channels.
    /// </summary>
    /// <param name="value">The list of ASP.NET Core channels.</param>
    /// <param name="message">Optional success message.</param>
    public static ChannelsResult CreateSuccess(List<AspNetChannel> value, string? message = null)
        => new(true, message, value, ApiErrorCode.None);

    /// <summary>
    /// Creates a failure result. The Value property will be null.
    /// </summary>
    /// <param name="message">Error message describing the failure.</param>
    public static ChannelsResult CreateFailure(string message)
        => new(false, message, null, ApiErrorCode.Failure);

    /// <summary>
    /// Creates a failure result with a specific error code. The Value property will be null.
    /// </summary>
    /// <param name="errorCode">The API error code.</param>
    /// <param name="message">Error message describing the failure.</param>
    public static ChannelsResult CreateFailure(ApiErrorCode errorCode, string message)
        => new(false, message, null, errorCode);
}
