namespace Askyl.Dsm.WebHosting.Constants.Application;

/// <summary>
/// Validation error messages for input sanitization.
/// </summary>
public static class ValidationConstants
{
    /// <summary>
    /// Error message when path traversal is detected.
    /// </summary>
    public const string PathTraversalDetected = "Invalid path: traversal sequences are not allowed";

    /// <summary>
    /// Error message when the version format is invalid.
    /// </summary>
    public const string InvalidVersionFormat = "Invalid version format";

    /// <summary>
    /// Generic operation failed message (no internal details leaked).
    /// </summary>
    public const string OperationFailed = "The operation failed. Check the logs for details.";
}
