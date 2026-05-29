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
}
