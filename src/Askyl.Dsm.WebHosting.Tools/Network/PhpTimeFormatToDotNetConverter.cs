using System.Text;

namespace Askyl.Dsm.WebHosting.Tools.Network;

/// <summary>
/// Converts PHP-style time format strings (used by DSM) to .NET format strings.
/// Example: <c>"H:i"</c> → <c>"H:mm"</c>, <c>"h:i a"</c> → <c>"h:mm tt"</c>.
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
        ['H'] = "H",  // Hour 0-23 without leading zero
        ['G'] = "H",  // Hour 0-23 without leading zero (same as H in PHP)

        // 12-hour
        ['h'] = "h",  // Hour 1-12 without leading zero
        ['g'] = "h",  // Hour 1-12 without leading zero (same as h in PHP)

        // Minutes
        ['i'] = "mm",  // Minutes with leading zero (00-59)

        // Seconds
        ['s'] = "ss",  // Seconds with leading zero (00-59)
        ['S'] = "\\th\\st\\nd\\rd\\th",  // English ordinal suffix (not directly mappable — placeholder)

        // AM/PM
        ['a'] = "tt",  // Lowercase am/pm
        ['A'] = "tt",  // Uppercase AM/PM
    };

    /// <summary>
    /// Converts a PHP-style time format string to a .NET custom format string.
    /// </summary>
    /// <param name="phpFormat">PHP format string (e.g. "H:i", "h:i a", "H:i:s").</param>
    /// <returns>.NET format string (e.g. "H:mm", "h:mm tt", "H:mm:ss"), or null if unrecognized.</returns>
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
