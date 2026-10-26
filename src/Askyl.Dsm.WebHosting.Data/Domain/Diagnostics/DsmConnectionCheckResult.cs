using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.Domain.Diagnostics;

/// <summary>
/// The outcome of one diagnostic check.
/// </summary>
/// <param name="Check">Which check.</param>
/// <param name="Status">Passed, failed or skipped.</param>
/// <param name="Failure">Why it failed; <see cref="DsmCheckFailure.None"/> otherwise.</param>
/// <param name="HttpStatus">The HTTP status SYNO.API.Info answered with, when it answered.</param>
public record DsmConnectionCheckResult(
    DsmConnectionCheck Check,
    DsmCheckStatus Status,
    DsmCheckFailure Failure = DsmCheckFailure.None,
    int? HttpStatus = null)
{
    /// <summary>
    /// Initializes an empty result for deserialization.
    /// </summary>
    [JsonConstructor]
    public DsmConnectionCheckResult() : this(default, default) { }
}
