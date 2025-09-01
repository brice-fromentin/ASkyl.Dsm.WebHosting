namespace Askyl.Dsm.WebHosting.Constants.Application;

/// <summary>
/// Defines application-specific constants for configuration and settings.
/// </summary>
public static class ApplicationConstants
{
    /// <summary>
    /// Default name for the application settings file.
    /// </summary>
    public const string SettingsFileName = "appsettings.json";

    /// <summary>
    /// Configuration key for the download channel version setting.
    /// </summary>
    public const string ChannelVersionKey = "Download:ChannelVersion";

    /// <summary>
    /// HTTP client name identifier.
    /// </summary>
    public static readonly string HttpClientName = "brad babble diboo - " + Guid.NewGuid().ToString();
}
