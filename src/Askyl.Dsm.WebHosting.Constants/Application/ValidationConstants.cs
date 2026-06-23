namespace Askyl.Dsm.WebHosting.Constants.Application;

/// <summary>
/// Validation limits for input sanitization (non-localizable numeric constants only).
/// User-facing messages have been migrated to Globalization resources.
/// </summary>
public static class ValidationConstants
{
    /// <summary>
    /// Maximum allowed length for an environment variable key name.
    /// </summary>
    public const int EnvVarKeyMaxLength = 256;

    /// <summary>
    /// Maximum allowed length for an environment variable value.
    /// </summary>
    public const int EnvVarValueMaxLength = 4096;

    #region Path Validation

    /// <summary>
    /// Literal path traversal segment used to detect directory escape attempts.
    /// </summary>
    public const string PathTraversalLiteral = "..";

    /// <summary>
    /// URL-encoded dot sequence used to detect obfuscated path traversal.
    /// </summary>
    public const string PathTraversalEncodedDot = "%2e";

    /// <summary>
    /// URL-encoded forward slash used to detect obfuscated path traversal.
    /// </summary>
    public const string PathTraversalEncodedSlash = "%2f";

    #endregion
}
