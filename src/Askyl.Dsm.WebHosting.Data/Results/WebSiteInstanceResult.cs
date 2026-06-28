using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.Data.Domain.WebSites;

namespace Askyl.Dsm.WebHosting.Data.Results;

/// <summary>
/// Represents a single website instance result.
/// </summary>
public sealed class WebSiteInstanceResult(bool success, string? message, WebSiteInstance? value, string? warningMessage, ApiErrorCode errorCode = default)
    : ApiResultData<WebSiteInstance>(success, message, value, errorCode)
{
    [JsonConstructor]
    private WebSiteInstanceResult() : this(false, null, default!, null, ApiErrorCode.Failure) { }

    /// <summary>
    /// Gets an optional warning message (e.g., runtime incompatibility) when the operation succeeded.
    /// </summary>
    public string? WarningMessage { get; init; } = warningMessage;

    /// <summary>
    /// Creates a successful result with the website instance.
    /// </summary>
    /// <param name="value">The website instance.</param>
    /// <param name="message">Optional success message.</param>
    /// <param name="warningMessage">Optional warning message.</param>
    public static WebSiteInstanceResult CreateSuccess(WebSiteInstance value, string? message = null, string? warningMessage = null)
        => new(true, message, value, warningMessage, ApiErrorCode.None);

    /// <summary>
    /// Creates a failure result. The Value property will be null.
    /// </summary>
    /// <param name="message">Error message describing the failure.</param>
    public static WebSiteInstanceResult CreateFailure(string message)
        => new(false, message, null, null, ApiErrorCode.Failure);
}
