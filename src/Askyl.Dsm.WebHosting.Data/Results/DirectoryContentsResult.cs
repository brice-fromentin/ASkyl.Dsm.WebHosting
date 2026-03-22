using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.Results;

/// <summary>
/// Represents a directory contents result from FileStation with file system items.
/// </summary>
public sealed class DirectoryContentsResult(bool success, string? message, List<FsEntry>? value, ApiErrorCode errorCode = default)
    : ApiResultItems<FsEntry>(success, message, value, errorCode)
{
    [JsonConstructor]
    private DirectoryContentsResult() : this(false, null, default!, ApiErrorCode.Failure) { }

    /// <summary>
    /// Creates a successful result with the list of directory contents.
    /// </summary>
    /// <param name="value">The list of file system entries in the directory.</param>
    /// <param name="message">Optional success message.</param>
    public static DirectoryContentsResult CreateSuccess(List<FsEntry> value, string? message = null)
        => new(true, message, value, ApiErrorCode.None);

    /// <summary>
    /// Creates a failure result. The Value property will be null.
    /// </summary>
    /// <param name="message">Error message describing the failure.</param>
    public static DirectoryContentsResult CreateFailure(string message)
        => new(false, message, null, ApiErrorCode.Failure);

    /// <summary>
    /// Creates a failure result with a specific error code. The Value property will be null.
    /// </summary>
    /// <param name="errorCode">The API error code.</param>
    /// <param name="message">Error message describing the failure.</param>
    public static DirectoryContentsResult CreateFailure(ApiErrorCode errorCode, string message)
        => new(false, message, null, errorCode);
}
