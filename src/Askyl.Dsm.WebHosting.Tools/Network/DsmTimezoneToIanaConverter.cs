namespace Askyl.Dsm.WebHosting.Tools.Network;

/// <summary>
/// Converts DSM timezone names (e.g. "Amsterdam") to IANA timezone identifiers (e.g. "Europe/Amsterdam").
/// DSM uses simplified timezone names that need to be mapped to full IANA identifiers.
/// </summary>
public static class DsmTimezoneToIanaConverter
{
    private static readonly Dictionary<string, string> TimezoneMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Amsterdam", "Europe/Amsterdam" },
        { "London", "Europe/London" },
        { "Paris", "Europe/Paris" },
        { "Berlin", "Europe/Berlin" },
        { "Rome", "Europe/Rome" },
        { "Moscow", "Europe/Moscow" },
        { "Athens", "Europe/Athens" },
        { "Helsinki", "Europe/Helsinki" },
        { "Stockholm", "Europe/Stockholm" },
        { "Oslo", "Europe/Oslo" },
        { "Copenhagen", "Europe/Copenhagen" },
        { "Madrid", "Europe/Madrid" },
        { "Lisbon", "Europe/Lisbon" },
        { "Dublin", "Europe/Dublin" },
        { "Warsaw", "Europe/Warsaw" },
        { "Prague", "Europe/Prague" },
        { "Budapest", "Europe/Budapest" },
        { "Bucharest", "Europe/Bucharest" },
        { "Sofia", "Europe/Sofia" },
        { "Kyiv", "Europe/Kyiv" },
        { "Minsk", "Europe/Minsk" },
        { "Vienna", "Europe/Vienna" },
        { "Brussels", "Europe/Brussels" },
        { "Zurich", "Europe/Zurich" },
        { "New_York", "America/New_York" },
        { "Chicago", "America/Chicago" },
        { "Denver", "America/Denver" },
        { "Los_Angeles", "America/Los_Angeles" },
        { "Anchorage", "America/Anchorage" },
        { "Halifax", "America/Halifax" },
        { "Montreal", "America/Montreal" },
        { "Toronto", "America/Toronto" },
        { "Mexico_City", "America/Mexico_City" },
        { "Bogota", "America/Bogota" },
        { "Lima", "America/Lima" },
        { "Santiago", "America/Santiago" },
        { "Buenos_Aires", "America/Argentina/Buenos_Aires" },
        { "Sao_Paulo", "America/Sao_Paulo" },
        { "Tokyo", "Asia/Tokyo" },
        { "Seoul", "Asia/Seoul" },
        { "Shanghai", "Asia/Shanghai" },
        { "Beijing", "Asia/Shanghai" },
        { "Hong_Kong", "Asia/Hong_Kong" },
        { "Singapore", "Asia/Singapore" },
        { "Bangkok", "Asia/Bangkok" },
        { "Kuala_Lumpur", "Asia/Kuala_Lumpur" },
        { "Manila", "Asia/Manila" },
        { "Jakarta", "Asia/Jakarta" },
        { "Mumbai", "Asia/Kolkata" },
        { "Dubai", "Asia/Dubai" },
        { "Sydney", "Australia/Sydney" },
        { "Melbourne", "Australia/Melbourne" },
        { "Brisbane", "Australia/Brisbane" },
        { "Auckland", "Pacific/Auckland" },
        { "Honolulu", "Pacific/Honolulu" },
        { "Johannesburg", "Africa/Johannesburg" },
        { "Cairo", "Africa/Cairo" },
        { "Lagos", "Africa/Lagos" },
        { "Casablanca", "Africa/Casablanca" },
        { "Tehran", "Asia/Tehran" },
        { "Istanbul", "Europe/Istanbul" },
        { "Jerusalem", "Asia/Jerusalem" },
    };

    /// <summary>
    /// Converts a DSM timezone name to an IANA timezone identifier.
    /// Falls back to "UTC" if the timezone is empty or not recognized.
    /// </summary>
    /// <param name="dsmTimezone">DSM timezone name (e.g. "Amsterdam", "New_York").</param>
    /// <returns>IANA timezone identifier (e.g. "Europe/Amsterdam", "America/New_York").</returns>
    public static string Convert(string dsmTimezone)
    {
        if (String.IsNullOrWhiteSpace(dsmTimezone))
        {
            return "UTC";
        }

        return TimezoneMap.TryGetValue(dsmTimezone.Trim(), out var ianaName) ? ianaName : "UTC";
    }
}
