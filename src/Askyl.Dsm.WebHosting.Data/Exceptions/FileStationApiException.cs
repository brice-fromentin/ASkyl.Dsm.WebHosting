namespace Askyl.Dsm.WebHosting.Data.Exceptions;

/// <summary>
/// Exception thrown when a FileStation API call fails or returns unexpected results.
/// This typically occurs when the Synology FileStation API returns an error response
/// or when the response structure is invalid.
/// </summary>
public sealed class FileStationApiException : InvalidOperationException
{
    /// <summary>
    /// Gets the API error code if available.
    /// </summary>
    public int? ErrorCode { get; }

    /// <summary>
    /// Gets the API success status.
    /// </summary>
    public bool? Success { get; }

    /// <summary>
    /// Gets the path that was being accessed when the error occurred.
    /// </summary>
    public string? Path { get; }

    /// <summary>
    /// Gets the formatted error message including the error code if available.
    /// </summary>
    public string FormattedMessage =>
        ErrorCode.HasValue ? $"{Message} (Error Code: {ErrorCode})" : Message;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileStationApiException"/> class.
    /// </summary>
    public FileStationApiException()
        : base("FileStation API call failed.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileStationApiException"/> class with a custom message.
    /// </summary>
    /// <param name="message">The custom error message.</param>
    public FileStationApiException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileStationApiException"/> class with a custom message and inner exception.
    /// </summary>
    /// <param name="message">The custom error message.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public FileStationApiException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileStationApiException"/> class with detailed API error information.
    /// </summary>
    /// <param name="message">The custom error message.</param>
    /// <param name="success">The API success status.</param>
    /// <param name="errorCode">The API error code.</param>
    /// <param name="path">The path that was being accessed when the error occurred.</param>
    public FileStationApiException(string message, bool? success, int? errorCode, string? path = null)
        : base(message)
    {
        Success = success;
        ErrorCode = errorCode;
        Path = path;
    }
}
