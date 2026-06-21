using System.Collections.Immutable;

namespace Askyl.Dsm.WebHosting.Constants.DSM.API;

/// <summary>
/// Maps PHP date/time format tokens to .NET custom format equivalents.
/// </summary>
/// <remarks>
/// PHP format reference: https://www.php.net/manual/en/datetime.format.php
/// .NET format reference: https://learn.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings
/// </remarks>
public static class PhpDotNetFormatTokens
{
    /// <summary>
    /// Unmapped PHP tokens with no .NET equivalent:
    /// 'e' — Timezone identifier (e.g. "Europe/Paris")
    /// 'T' — Timezone abbreviation (e.g. "CEST")
    /// 'O' — GMT offset without colon (e.g. "+0200")
    /// 'I' — DST indicator (0 or 1)
    /// 'Z' — Timezone offset in seconds (-43200 to 50400)
    /// 'w' — Day of week (0=Sun..6=Sat)
    /// 'N' — Day of week (1=Mon..7=Sun)
    /// 'S' — Ordinal suffix (st, nd, rd, th)
    /// 'c' — ISO 8601 (compound)
    /// 'r' — RFC 2822 (compound)
    /// 'u' — Unix timestamp (compound)
    /// </summary>
    public static readonly ImmutableDictionary<char, string> All = new Dictionary<char, string>()
    {
        // Year
        ['Y'] = "yyyy",
        ['y'] = "yy",

        // Month
        ['m'] = "MM",
        ['n'] = "M",
        ['M'] = "MMM",
        ['F'] = "MMMM",

        // Day
        ['d'] = "dd",
        ['j'] = "d",
        ['l'] = "dddd",
        ['D'] = "ddd",

        // Day of year — PHP 'z' (0-365) vs .NET %j (1-366) — offset differs by 1
        ['z'] = "%j",

        // 24-hour
        ['H'] = "HH",
        ['G'] = "H",

        // 12-hour
        ['h'] = "hh",
        ['g'] = "h",

        // Minutes / Seconds
        ['i'] = "mm",
        ['s'] = "ss",

        // AM/PM
        ['a'] = "tt",
        ['A'] = "tt",

        // Timezone
        ['P'] = "zzz",
    }.ToImmutableDictionary();
}
