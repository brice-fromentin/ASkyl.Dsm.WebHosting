using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.Data.Domain.Diagnostics;

namespace Askyl.Dsm.WebHosting.Data.Results;

/// <summary>
/// Result carrying a DSM connection diagnostic report.
/// </summary>
public sealed class DsmConnectionReportResult(bool success, string? message, DsmConnectionReport? value, ApiErrorCode errorCode = default)
    : ApiResultData<DsmConnectionReport>(success, message, value, errorCode)
{
    [JsonConstructor]
    private DsmConnectionReportResult() : this(false, null, default!, ApiErrorCode.Failure) { }

    /// <summary>
    /// Creates a successful result with the report.
    /// </summary>
    /// <param name="value">The report.</param>
    public static DsmConnectionReportResult CreateSuccess(DsmConnectionReport value)
        => new(true, null, value, ApiErrorCode.None);

    /// <summary>
    /// Creates a failure result. The Value property will be null.
    /// </summary>
    /// <param name="message">Error message describing the failure.</param>
    public static DsmConnectionReportResult CreateFailure(string message)
        => new(false, message, null, ApiErrorCode.Failure);
}
