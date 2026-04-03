using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.Data.Domain.FileSystem;

namespace Askyl.Dsm.WebHosting.Data.Results;

/// <summary>
/// Represents a list of file system items in a directory result from FileStation.
/// </summary>
public sealed class DirectoryFilesResult(bool success, string? message, List<FsEntry>? value, ApiErrorCode errorCode = default)
    : ApiResultItems<FsEntry>(success, message, value, errorCode)
{
    [JsonConstructor]
    private DirectoryFilesResult() : this(false, null, default!, ApiErrorCode.Failure) { }

    /// <summary>
    /// Creates a successful result with the list of files.
    /// </summary>
    /// <param name="value">The list of file system entries.</param>
    /// <param name="message">Optional success message.</param>
    public static DirectoryFilesResult CreateSuccess(List<FsEntry> value, string? message = null)
        => new(true, message, value, ApiErrorCode.None);

    /// <summary>
    /// Creates a failure result. The Value property will be null.
    /// </summary>
    /// <param name="message">Error message describing the failure.</param>
    public static DirectoryFilesResult CreateFailure(string message)
        => new(false, message, null, ApiErrorCode.Failure);

    /// <summary>
    /// Creates a failure result with a specific error code. The Value property will be null.
    /// </summary>
    /// <param name="errorCode">The API error code.</param>
    /// <param name="message">Error message describing the failure.</param>
    public static DirectoryFilesResult CreateFailure(ApiErrorCode errorCode, string message)
        => new(false, message, null, errorCode);
}
