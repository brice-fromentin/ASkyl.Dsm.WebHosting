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

        // Day of week — PHP 'w' (0=Sun..6=Sat) and 'N' (1=Mon..7=Sun)
        // have no direct .NET custom format equivalent — passed through as literal

        // Day of year — PHP 'z' (0-365) vs .NET %j (1-366) — offset differs by 1
        // This is a known limitation; consumers should add 1 if exact day-of-year needed
        ['z'] = "%j",
    };

    /// <summary>
    /// Converts a PHP-style date format string to a .NET custom format string.
    /// </summary>
    /// <param name="phpFormat">PHP format string (e.g. "Y/m/d", "d/m/Y", "j M Y").</param>
    /// <returns>.NET format string (e.g. "yyyy/MM/dd", "dd/MM/yyyy", "d MMM yyyy"), or null if unrecognized.</returns>
    public static string? Convert(string? phpFormat)
    {
        if (String.IsNullOrWhiteSpace(phpFormat))
        {
            return null;
        }

        var result = new StringBuilder(phpFormat.Length * 2);

        for (var i = 0; i < phpFormat.Length; i++)
        {
            var ch = phpFormat[i];

            // Handle PHP escape mechanism: \x means literal 'x'
            if (ch == '\\')
            {
                if (i + 1 < phpFormat.Length)
                {
                    result.Append(phpFormat[++i]);
                }

                continue;
            }

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
