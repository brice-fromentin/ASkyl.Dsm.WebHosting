namespace Askyl.Dsm.WebHosting.Constants.Application;

/// <summary>
/// Validation error messages for input sanitization.
/// </summary>
public static class ValidationConstants
{
    /// <summary>
    /// Error message when a path is required but not provided.
    /// </summary>
    public const string PathRequired = "Path is required";

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

    /// <summary>
    /// Maximum allowed length for an environment variable key name.
    /// </summary>
    public const int EnvVarKeyMaxLength = 256;

    /// <summary>
    /// Maximum allowed length for an environment variable value.
    /// </summary>
    public const int EnvVarValueMaxLength = 4096;

    /// <summary>
    /// Error message when an environment variable key exceeds the maximum length.
    /// </summary>
    public const string EnvVarKeyTooLong = "Environment variable key '{0}' exceeds maximum length of {1} characters";

    /// <summary>
    /// Error message when an environment variable value exceeds the maximum length.
    /// </summary>
    public const string EnvVarValueTooLong = "Environment variable '{0}' value exceeds maximum length of {1} characters";
}
