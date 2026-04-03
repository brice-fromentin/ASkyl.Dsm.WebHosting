using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.Data.Domain.Runtime;

namespace Askyl.Dsm.WebHosting.Data.Results;

/// <summary>
/// Represents a list of ASP.NET Core release versions result.
/// </summary>
public sealed class ReleasesResult(bool success, string? message, List<AspNetRelease>? value, ApiErrorCode errorCode = default)
    : ApiResultItems<AspNetRelease>(success, message, value, errorCode)
{
    [JsonConstructor]
    private ReleasesResult() : this(false, null, default!, ApiErrorCode.Failure) { }

    /// <summary>
    /// Creates a successful result with the list of releases.
    /// </summary>
    /// <param name="value">The list of ASP.NET Core releases.</param>
    /// <param name="message">Optional success message.</param>
    public static ReleasesResult CreateSuccess(List<AspNetRelease> value, string? message = null)
        => new(true, message, value, ApiErrorCode.None);

    /// <summary>
    /// Creates a failure result. The Value property will be null.
    /// </summary>
    /// <param name="message">Error message describing the failure.</param>
    public static ReleasesResult CreateFailure(string message)
        => new(false, message, null, ApiErrorCode.Failure);

    /// <summary>
    /// Creates a failure result with a specific error code. The Value property will be null.
    /// </summary>
    /// <param name="errorCode">The API error code.</param>
    /// <param name="message">Error message describing the failure.</param>
    public static ReleasesResult CreateFailure(ApiErrorCode errorCode, string message)
        => new(false, message, null, errorCode);
}
