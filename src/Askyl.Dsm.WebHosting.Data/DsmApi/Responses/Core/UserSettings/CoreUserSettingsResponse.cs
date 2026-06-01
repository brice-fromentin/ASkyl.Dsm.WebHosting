using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Responses.Core.UserSettings;

/// <summary>
/// Response for SYNO.Core.UserSettings.get — returns all user settings.
/// We only extract Personal.lang; the rest of the massive ~1400-line response is ignored.
/// </summary>
public class CoreUserSettingsResponse : ApiResponseBase<CoreUserSettingsData>
{
}

/// <summary>
/// Minimal data model for SYNO.Core.UserSettings response — only extracts the Personal section.
/// The full response contains 100+ app-specific settings which we intentionally ignore.
/// </summary>
public class CoreUserSettingsData
{
    [JsonPropertyName("Personal")]
    public CoreUserSettingsPersonal? Personal { get; set; }
}

/// <summary>
/// Personal user settings section containing language, date, and time preferences.
/// </summary>
public class CoreUserSettingsPersonal
{
    /// <summary>
    /// User's preferred language in DSM codepage format (e.g. "enu" = en-US, "fra" = fr-FR).
    /// </summary>
    [JsonPropertyName("lang")]
    public string? Lang { get; set; }

    /// <summary>
    /// User's preferred date format using PHP-style format strings (e.g. "Y/m/d", "d/m/Y").
    /// </summary>
    [JsonPropertyName("dateFormat")]
    public string? DateFormat { get; set; }

    /// <summary>
    /// User's preferred time format using PHP-style format strings (e.g. "H:i", "h:i a").
    /// </summary>
    [JsonPropertyName("timeFormat")]
    public string? TimeFormat { get; set; }
}
