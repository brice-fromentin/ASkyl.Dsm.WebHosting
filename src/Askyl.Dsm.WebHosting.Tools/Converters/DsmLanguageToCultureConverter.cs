using System.Diagnostics;
using Askyl.Dsm.WebHosting.Constants.DSM.System;

namespace Askyl.Dsm.WebHosting.Tools.Converters;

/// <summary>
/// Converts DSM language codes (e.g. "enu") to .NET culture names (e.g. "en-US").
/// DSM uses 3-letter language identifiers (e.g. "enu", "fra", "deu").
/// </summary>
public static class DsmLanguageToCultureConverter
{
    /// <summary>
    /// Value indicating the DSM language is set to browser default.
    /// </summary>
    public const string DefaultBrowser = "def";

    /// <summary>
    /// Converts a DSM language code to a .NET culture name.
    /// Returns <c>null</c> if the language code is "def" (browser default), null, empty, or not recognized.
    /// </summary>
    /// <param name="languageCode">DSM language code (e.g. "enu", "fra", "deu", "def").</param>
    /// <returns>.NET culture name (e.g. "en-US", "fr-FR", "de-DE"), or <c>null</c> if the code means "use browser default".</returns>
    public static string? Convert(string? languageCode)
    {
        if (String.IsNullOrWhiteSpace(languageCode) || String.Equals(languageCode, DefaultBrowser, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var trimmed = languageCode.Trim();

        if (trimmed != languageCode)
        {
            Debug.WriteLine($"DsmLanguageToCultureConverter: input was trimmed from '{languageCode}' to '{trimmed}'");
        }

        return DsmLanguageCodes.All.TryGetValue(trimmed, out var cultureName) ? cultureName : null;
    }
}
