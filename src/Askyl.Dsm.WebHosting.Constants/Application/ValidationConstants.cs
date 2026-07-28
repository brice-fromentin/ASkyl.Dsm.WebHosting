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

    /// <summary>
    /// Separator used when several validation failures are combined into one message.
    /// </summary>
    public const string MessageSeparator = "; ";

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

    /// <summary>
    /// Double-encoded dot sequence (%252e) that decodes to %2e then '.' after ASP.NET Core URL decoding.
    /// </summary>
    public const string PathTraversalDoubleEncodedDot = "%252e";

    /// <summary>
    /// Double-encoded forward slash (%252f) that decodes to %2f then '/' after ASP.NET Core URL decoding.
    /// </summary>
    public const string PathTraversalDoubleEncodedSlash = "%252f";

    #endregion
}
