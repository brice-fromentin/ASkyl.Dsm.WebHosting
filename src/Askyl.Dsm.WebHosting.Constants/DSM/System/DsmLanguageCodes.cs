using System.Collections.Immutable;

namespace Askyl.Dsm.WebHosting.Constants.DSM.System;

/// <summary>
/// Maps DSM language codes (both user language and supplang variants) to .NET culture names.
/// User language codes: "fra", "deu", "spa", etc.
/// Supplang codes: "fre", "ger", "spn", etc. (differ for some languages).
/// </summary>
public static class DsmLanguageCodes
{
    /// <summary>
    /// Value indicating the DSM language is set to browser default.
    /// </summary>
    public const string DefaultBrowser = "def";

    /// <summary>
    /// All DSM language codes mapped to .NET culture names (sorted alphabetically by code).
    /// Includes both user language codes and supplang variants.
    /// </summary>
    public static readonly ImmutableDictionary<string, string> All = new Dictionary<string, string>()
    {
        { "ces", "cs-CZ" },
        { "chs", "zh-CN" },
        { "cht", "zh-TW" },
        { "csy", "cs-CZ" },
        { "dan", "da-DK" },
        { "deu", "de-DE" },
        { "enu", "en-US" },
        { "fra", "fr-FR" },
        { "fre", "fr-FR" },
        { "ger", "de-DE" },
        { "heb", "he-IL" },
        { "hun", "hu-HU" },
        { "ita", "it-IT" },
        { "jpn", "ja-JP" },
        { "kor", "ko-KR" },
        { "krn", "ko-KR" },
        { "nld", "nl-NL" },
        { "nor", "nb-NO" },
        { "pol", "pl-PL" },
        { "plk", "pl-PL" },
        { "ptb", "pt-PT" },
        { "ptg", "pt-BR" },
        { "rus", "ru-RU" },
        { "spa", "es-ES" },
        { "spn", "es-ES" },
        { "sve", "sv-SE" },
        { "swe", "sv-SE" },
        { "tha", "th-TH" },
        { "trk", "tr-TR" },
        { "tur", "tr-TR" },
        { "ukr", "uk-UA" },
        { "zhs", "zh-CN" },
        { "zht", "zh-TW" },
        { "zhcn", "zh-CN" },
        { "zhtw", "zh-TW" },
    }.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Converts a DSM language code to a .NET culture name.
    /// Returns <c>null</c> if the language code is "def" (browser default), null, empty, or not recognized.
    /// </summary>
    /// <param name="languageCode">DSM language code (e.g. "enu", "fra", "deu", "def").</param>
    /// <returns>.NET culture name (e.g. "en-US", "fr-FR", "de-DE"), or <c>null</c> if unrecognized.</returns>
    public static string? Convert(string? languageCode)
    {
        if (String.IsNullOrWhiteSpace(languageCode) || String.Equals(languageCode, DefaultBrowser, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return All.TryGetValue(languageCode.Trim(), out var cultureName) ? cultureName : null;
    }
}
