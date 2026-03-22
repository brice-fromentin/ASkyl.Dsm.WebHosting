namespace Askyl.Dsm.WebHosting.Data.Results;

/// <summary>
/// Standard error codes for API operations.
/// </summary>
public enum ApiErrorCode
{
    /// <summary>
    /// No error (success).
    /// </summary>
    None = 0,

    /// <summary>
    /// The requested resource was not found.
    /// </summary>
    NotFound = 404,

    /// <summary>
    /// General failure (default error code).
    /// </summary>
    Failure = 500,

    /// <summary>
    /// Operation failed due to invalid state or precondition.
    /// </summary>
    InvalidState = 1001,

    /// <summary>
    /// Authentication or authorization failed.
    /// </summary>
    Unauthorized = 401,

    /// <summary>
    /// Request was invalid (bad parameters).
    /// </summary>
    BadRequest = 400
}
