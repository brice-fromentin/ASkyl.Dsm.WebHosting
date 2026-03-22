namespace Askyl.Dsm.WebHosting.Constants.UI;

/// <summary>
/// Constants for file size calculations and display formatting.
/// </summary>
public static class FileSizeConstants
{
    /// <summary>
    /// Number of bytes in a kibibyte (1024 bytes).
    /// </summary>
    public const long BytesPerKibibyte = 1024L;

    /// <summary>
    /// Number of bytes in a mebibyte (1024² bytes).
    /// </summary>
    public const long BytesPerMebibyte = BytesPerKibibyte * 1024L;

    /// <summary>
    /// Number of bytes in a gibibyte (1024³ bytes).
    /// </summary>
    public const long BytesPerGibibyte = BytesPerMebibyte * 1024L;

    /// <summary>
    /// Unit suffix for bytes.
    /// </summary>
    public const string BytesSuffix = "B";

    /// <summary>
    /// Unit suffix for kibibytes (1024 bytes).
    /// </summary>
    public const string KibibytesSuffix = "KiB";

    /// <summary>
    /// Unit suffix for mebibytes (1024² bytes).
    /// </summary>
    public const string MebibytesSuffix = "MiB";

    /// <summary>
    /// Unit suffix for gibibytes (1024³ bytes).
    /// </summary>
    public const string GibibytesSuffix = "GiB";

    /// <summary>
    /// Decimal places to display for fractional file sizes.
    /// </summary>
    public const string DecimalFormat = "F2";
}