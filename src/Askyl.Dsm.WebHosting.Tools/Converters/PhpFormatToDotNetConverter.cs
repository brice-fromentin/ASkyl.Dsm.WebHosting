using System.Text;
using Askyl.Dsm.WebHosting.Constants.DSM.API;

namespace Askyl.Dsm.WebHosting.Tools.Converters;

/// <summary>
/// Converts PHP-style date/time format strings (used by DSM) to .NET format strings.
/// Example: <c>"Y/m/d"</c> → <c>"yyyy/MM/dd"</c>, <c>"H:i"</c> → <c>"HH:mm"</c>.
/// </summary>
/// <remarks>
/// PHP format reference: https://www.php.net/manual/en/datetime.format.php
/// .NET format reference: https://learn.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings
/// </remarks>
public static class PhpFormatToDotNetConverter
{
    /// <summary>
    /// Converts a PHP-style date/time format string to a .NET custom format string.
    /// </summary>
    /// <param name="phpFormat">PHP format string (e.g. "Y/m/d", "H:i", "d/m/Y H:i").</param>
    /// <returns>.NET format string, or <c>null</c> if input is null/whitespace.</returns>
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

            if (PhpDotNetFormatTokens.All.TryGetValue(ch, out var replacement))
            {
                result.Append(replacement);
            }
            else
            {
                result.Append(ch);
            }
        }

        return result.ToString();
    }
}
