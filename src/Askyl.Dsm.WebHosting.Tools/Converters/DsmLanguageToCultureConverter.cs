using Askyl.Dsm.WebHosting.Constants.DSM.System;

namespace Askyl.Dsm.WebHosting.Tools.Converters;

/// <summary>
/// Converts DSM language codes (e.g. "enu") to .NET culture names (e.g. "en-US").
/// DSM uses 3-letter language identifiers (e.g. "enu", "fra", "deu").
/// </summary>
public static class DsmLanguageToCultureConverter
{
    /// <summary>
    /// Converts a DSM language code to a .NET culture name.
    /// Falls back to "en-US" if the language code is null, empty, or not recognized.
    /// </summary>
    /// <param name="languageCode">DSM language code (e.g. "enu", "fra", "deu"), or null to use default.</param>
    /// <returns>.NET culture name (e.g. "en-US", "fr-FR", "de-DE"). Never null.</returns>
    public static string Convert(string? languageCode)
    {
        return String.IsNullOrWhiteSpace(languageCode)
            ? "en-US"
            : DsmLanguageCodes.All.TryGetValue(languageCode.Trim(), out var cultureName) ? cultureName : "en-US";
    }
}
