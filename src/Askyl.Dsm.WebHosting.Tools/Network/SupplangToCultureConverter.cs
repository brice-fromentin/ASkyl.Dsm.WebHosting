namespace Askyl.Dsm.WebHosting.Tools.Network;

/// <summary>
/// Converts DSM supplang values (e.g. "fre") to .NET culture names (e.g. "fr-FR").
/// Supplang codes are used in the "supplang" field of /etc/synoinfo.conf.
/// These differ from codepages for some languages (e.g. "fre" vs "fra").
/// </summary>
public static class SupplangToCultureConverter
{
    private static readonly Dictionary<string, string> SupplangMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "enu", "en-US" },
        { "fre", "fr-FR" },
        { "ger", "de-DE" },
        { "jpn", "ja-JP" },
        { "zht", "zh-TW" },
        { "zhs", "zh-CN" },
        { "cht", "zh-TW" },
        { "chs", "zh-CN" },
        { "krn", "ko-KR" },
        { "tha", "th-TH" },
        { "ita", "it-IT" },
        { "spn", "es-ES" },
        { "dan", "da-DK" },
        { "nor", "nb-NO" },
        { "sve", "sv-SE" },
        { "nld", "nl-NL" },
        { "rus", "ru-RU" },
        { "plk", "pl-PL" },
        { "ptb", "pt-PT" },
        { "ptg", "pt-BR" },
        { "hun", "hu-HU" },
        { "trk", "tr-TR" },
        { "csy", "cs-CZ" },
        { "heb", "he-IL" },
        { "ukr", "uk-UA" },
    };

    /// <summary>
    /// Converts a single DSM supplang code to a .NET culture name.
    /// Falls back to "en-US" if the code is empty or not recognized.
    /// </summary>
    /// <param name="supplang">DSM supplang code (e.g. "fre", "ger", "spn").</param>
    /// <returns>.NET culture name (e.g. "fr-FR", "de-DE", "es-ES").</returns>
    public static string Convert(string supplang)
    {
        if (String.IsNullOrWhiteSpace(supplang))
        {
            return "en-US";
        }

        return SupplangMap.TryGetValue(supplang.Trim(), out var cultureName) ? cultureName : "en-US";
    }

    /// <summary>
    /// Converts a comma-separated list of DSM supplang codes to an array of .NET culture names.
    /// Falls back to ["en-US"] if the input is empty.
    /// </summary>
    /// <param name="supplangCsv">Comma-separated supplang codes (e.g. "enu,fre,ger").</param>
    /// <returns>Array of .NET culture names (e.g. ["en-US", "fr-FR", "de-DE"]).</returns>
    public static string[] ConvertAll(string supplangCsv)
    {
        if (String.IsNullOrWhiteSpace(supplangCsv))
        {
            return ["en-US"];
        }

        var codes = supplangCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return [.. codes.Select(Convert)];
    }
}
