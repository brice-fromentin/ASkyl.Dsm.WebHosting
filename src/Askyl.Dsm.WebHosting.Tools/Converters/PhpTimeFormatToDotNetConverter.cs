using System.Text;

namespace Askyl.Dsm.WebHosting.Tools.Converters;

/// <summary>
/// Converts PHP-style time format strings (used by DSM) to .NET format strings.
/// Example: <c>"H:i"</c> → <c>"HH:mm"</c>, <c>"h:i a"</c> → <c>"hh:mm tt"</c>.
/// </summary>
/// <remarks>
/// PHP format reference: https://www.php.net/manual/en/datetime.format.php
/// .NET format reference: https://learn.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings
/// </remarks>
public static class PhpTimeFormatToDotNetConverter
{
    private static readonly Dictionary<char, string> TokenMap = new()
    {
        // 24-hour
        ['H'] = "HH",  // Hour 00-23 with leading zero
        ['G'] = "H",   // Hour 0-23 without leading zero

        // 12-hour
        ['h'] = "hh",  // Hour 01-12 with leading zero
        ['g'] = "h",   // Hour 1-12 without leading zero

        // Minutes
        ['i'] = "mm",  // Minutes with leading zero (00-59)

        // Seconds
        ['s'] = "ss",  // Seconds with leading zero (00-59)

        // PHP 'S' (ordinal suffix: st, nd, rd, th) has no .NET equivalent — omitted
        // AM/PM
        ['a'] = "tt",  // Lowercase am/pm
        ['A'] = "tt",  // Uppercase AM/PM
    };

    /// <summary>
    /// Converts a PHP-style time format string to a .NET custom format string.
    /// </summary>
    /// <param name="phpFormat">PHP format string (e.g. "H:i", "h:i a", "H:i:s").</param>
    /// <returns>.NET format string (e.g. "HH:mm", "hh:mm tt", "HH:mm:ss"), or null if unrecognized.</returns>
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
