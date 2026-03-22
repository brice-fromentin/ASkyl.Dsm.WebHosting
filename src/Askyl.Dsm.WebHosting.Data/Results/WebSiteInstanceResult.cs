using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.Data.WebSites;

namespace Askyl.Dsm.WebHosting.Data.Results;

/// <summary>
/// Represents a single website instance result.
/// </summary>
public sealed class WebSiteInstanceResult(bool success, string? message, WebSiteInstance? value, ApiErrorCode errorCode = default)
    : ApiResultData<WebSiteInstance>(success, message, value, errorCode)
{
    [JsonConstructor]
    private WebSiteInstanceResult() : this(false, null, default!, ApiErrorCode.Failure) { }

    /// <summary>
    /// Creates a successful result with the website instance.
    /// </summary>
    /// <param name="value">The website instance.</param>
    /// <param name="message">Optional success message.</param>
    public static WebSiteInstanceResult CreateSuccess(WebSiteInstance value, string? message = null)
        => new(true, message, value, ApiErrorCode.None);

    /// <summary>
    /// Creates a failure result. The Value property will be null.
    /// </summary>
    /// <param name="message">Error message describing the failure.</param>
    public static WebSiteInstanceResult CreateFailure(string message)
        => new(false, message, null, ApiErrorCode.Failure);

    /// <summary>
    /// Creates a failure result with a specific error code.
    /// </summary>
    /// <param name="errorCode">The error code.</param>
    /// <param name="message">Error message describing the failure.</param>
    public static WebSiteInstanceResult CreateFailure(ApiErrorCode errorCode, string message)
        => new(false, message, null, errorCode);
}
