namespace Askyl.Dsm.WebHosting.Tools.Network;

/// <summary>
/// Converts DSM codepage values (e.g. "enu") to .NET culture names (e.g. "en-US").
/// Codepages are 3-letter identifiers used in /etc/synoinfo.conf.
/// </summary>
public static class CodepageToCultureConverter
{
    private static readonly Dictionary<string, string> CodepageMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "enu", "en-US" },
        { "fra", "fr-FR" },
        { "deu", "de-DE" },
        { "jpn", "ja-JP" },
        { "zht", "zh-TW" },
        { "zhs", "zh-CN" },
        { "zhtw", "zh-TW" },
        { "zhcn", "zh-CN" },
        { "kor", "ko-KR" },
        { "tha", "th-TH" },
        { "ita", "it-IT" },
        { "spa", "es-ES" },
        { "dan", "da-DK" },
        { "nor", "nb-NO" },
        { "swe", "sv-SE" },
        { "nld", "nl-NL" },
        { "rus", "ru-RU" },
        { "pol", "pl-PL" },
        { "ptb", "pt-PT" },
        { "ptg", "pt-BR" },
        { "hun", "hu-HU" },
        { "tur", "tr-TR" },
        { "ces", "cs-CZ" },
        { "heb", "he-IL" },
        { "ukr", "uk-UA" },
        { "cht", "zh-TW" },
        { "chs", "zh-CN" },
        { "ger", "de-DE" },
        { "fre", "fr-FR" },
        { "spn", "es-ES" },
        { "sve", "sv-SE" },
        { "plk", "pl-PL" },
        { "trk", "tr-TR" },
        { "csy", "cs-CZ" },
        { "krn", "ko-KR" },
    };

    /// <summary>
    /// Converts a DSM codepage to a .NET culture name.
    /// Falls back to "en-US" if the codepage is empty or not recognized.
    /// </summary>
    /// <param name="codepage">DSM codepage (e.g. "enu", "fra", "deu"). Must not be null.</param>
    /// <returns>.NET culture name (e.g. "en-US", "fr-FR", "de-DE"). Never null.</returns>
    public static string Convert(string codepage)
    {
        return String.IsNullOrWhiteSpace(codepage)
            ? "en-US"
            : CodepageMap.TryGetValue(codepage.Trim(), out var cultureName) ? cultureName : "en-US";
    }
}
