using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.Data.WebSites;

namespace Askyl.Dsm.WebHosting.Data.Results;

/// <summary>
/// Represents a list of website instances result.
/// </summary>
public sealed class WebSiteInstancesResult(bool success, string? message, List<WebSiteInstance>? value, ApiErrorCode errorCode = default)
    : ApiResultItems<WebSiteInstance>(success, message, value, errorCode)
{
    [JsonConstructor]
    private WebSiteInstancesResult() : this(false, null, default!, ApiErrorCode.Failure) { }

    /// <summary>
    /// Creates a successful result with the list of websites.
    /// </summary>
    /// <param name="value">The list of website instances.</param>
    /// <param name="message">Optional success message.</param>
    public static WebSiteInstancesResult CreateSuccess(List<WebSiteInstance> value, string? message = null)
        => new(true, message, value, ApiErrorCode.None);

    /// <summary>
    /// Creates a failure result. The Value property will be null.
    /// </summary>
    /// <param name="message">Error message describing the failure.</param>
    public static WebSiteInstancesResult CreateFailure(string message)
        => new(false, message, null, ApiErrorCode.Failure);

    /// <summary>
    /// Creates a failure result with a specific error code.
    /// </summary>
    /// <param name="errorCode">The error code.</param>
    /// <param name="message">Error message describing the failure.</param>
    public static WebSiteInstancesResult CreateFailure(ApiErrorCode errorCode, string message)
        => new(false, message, null, errorCode);
}
