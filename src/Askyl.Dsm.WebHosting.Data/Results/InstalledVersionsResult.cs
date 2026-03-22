using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.Data.Domain.Runtime;

namespace Askyl.Dsm.WebHosting.Data.Results;

/// <summary>
/// Represents a list of installed .NET framework versions result.
/// </summary>
public sealed class InstalledVersionsResult(bool success, string? message, List<FrameworkInfo>? value, ApiErrorCode errorCode = default)
    : ApiResultItems<FrameworkInfo>(success, message, value, errorCode)
{
    [JsonConstructor]
    private InstalledVersionsResult() : this(false, null, default!, ApiErrorCode.Failure) { }

    /// <summary>
    /// Creates a successful result with the list of installed versions.
    /// </summary>
    /// <param name="value">The list of installed framework versions.</param>
    /// <param name="message">Optional success message.</param>
    public static InstalledVersionsResult CreateSuccess(List<FrameworkInfo> value, string? message = null)
        => new(true, message, value, ApiErrorCode.None);

    /// <summary>
    /// Creates a failure result. The Value property will be null.
    /// </summary>
    /// <param name="message">Error message describing the failure.</param>
    public static InstalledVersionsResult CreateFailure(string message)
        => new(false, message, null, ApiErrorCode.Failure);

    /// <summary>
    /// Creates a failure result with a specific error code. The Value property will be null.
    /// </summary>
    /// <param name="errorCode">The API error code.</param>
    /// <param name="message">Error message describing the failure.</param>
    public static InstalledVersionsResult CreateFailure(ApiErrorCode errorCode, string message)
        => new(false, message, null, errorCode);
}

    