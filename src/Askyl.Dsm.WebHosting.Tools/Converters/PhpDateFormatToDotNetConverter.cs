using System.Text;

namespace Askyl.Dsm.WebHosting.Tools.Converters;

/// <summary>
/// Converts PHP-style date format strings (used by DSM) to .NET format strings.
/// Example: <c>"Y/m/d"</c> → <c>"yyyy/MM/dd"</c>, <c>"d/m/Y"</c> → <c>"dd/MM/yyyy"</c>.
/// </summary>
/// <remarks>
/// PHP format reference: https://www.php.net/manual/en/datetime.format.php
/// .NET format reference: https://learn.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings
/// </remarks>
public static class PhpDateFormatToDotNetConverter
{
    private static readonly Dictionary<char, string> TokenMap = new()
    {
        // Year
        ['Y'] = "yyyy",  // Full year (e.g. 2026)
        ['y'] = "yy",    // Two-digit year (e.g. 26)

        // Month
        ['m'] = "MM",    // Numeric with leading zero (01-12)
        ['n'] = "M",     // Numeric without leading zero (1-12)
        ['M'] = "MMM",   // Abbreviated name (Jan-Dec)
        ['F'] = "MMMM",  // Full name (January-December)

        // Day
        ['d'] = "dd",    // Two-digit with leading zero (01-31)
        ['j'] = "d",     // Day without leading zero (1-31)
        ['l'] = "dddd",  // Full weekday name (Monday-Sunday)
        ['D'] = "ddd",   // Abbreviated weekday name (Mon-Sun)
        ['w'] = "%u",    // Day of week as number (0=Sun, 6=Sat) — not directly mappable
        ['N'] = "%u",    // ISO-8601 day of week (1=Mon, 7=Sun) — not directly mappable

        // Day of year
        ['z'] = "%j",    // Day of year (0-366)
    };

    /// <summary>
    /// Converts a PHP-style date format string to a .NET custom format string.
    /// </summary>
    /// <param name="phpFormat">PHP format string (e.g. "Y/m/d", "d/m/Y", "j M Y").</param>
    /// <returns>.NET format string (e.g. "yyyy/MM/dd", "dd/MM/yyyy", "d MMM yyyy"), or null if unrecognized.</returns>
    public static string? Convert(string? phpFormat)
    {
        if (string.IsNullOrWhiteSpace(phpFormat))
        {
            return null;
        }

        var result = new StringBuilder(phpFormat.Length * 2);

        for (var i = 0; i < phpFormat.Length; i++)
        {
            var ch = phpFormat[i];

            if (TokenMap.TryGetValue(ch, out var replacement))
            {
                result.Append(replacement);
            }
            else
            {
                // Keep separators and unknown characters as-is
                result.Append(ch);
            }
        }

        return result.ToString();
    }
}
