namespace Askyl.Dsm.WebHosting.Constants.Application;

/// <summary>
/// Defines application-specific constants for configuration and settings.
/// For website hosting constants, see <see cref="WebSiteConstants"/>.
/// </summary>
public static class ApplicationConstants
{
    #region Configuration Files

    /// <summary>
    /// Default name for the application settings file.
    /// </summary>
    public const string SettingsFileName = "appsettings.json";

    /// <summary>
    /// Configuration key for the download channel version setting.
    /// </summary>
    public const string ChannelVersionKey = "Download:ChannelVersion";

    /// <summary>
    /// Default configuration version.
    /// </summary>
    public const string DefaultConfigurationVersion = "1.0";

    #endregion

    #region HTTP Client

    /// <summary>
    /// HTTP client name identifier.
    /// </summary>
    public const string HttpClientName = "UiClient";

    /// <summary>
    /// HTTP client timeout in seconds. Must exceed <see cref="WebSiteConstants.DefaultProcessTimeoutSeconds"/> to avoid client-side timeouts during stop operations. SIGTERM + ASP.NET Core drain completes in ~1-3 seconds; 15s provides headroom for the full request/response roundtrip.
    /// </summary>
    public const int HttpClientTimeoutSeconds = 15;

    #endregion

    #region Runtime Path

    /// <summary>
    /// The root path for the .NET runtimes directory.
    /// </summary>
    public const string RuntimesRootPath = "../runtimes";

    #endregion

    #region Application Paths & Routing

    /// <summary>
    /// The sub-path for the application URL, used for routing.
    /// </summary>
    public const string ApplicationUrlSubPath = "/adwh";

    /// <summary>
    /// The home page path.
    /// </summary>
    public const string HomePagePath = "";

    /// <summary>
    /// The login page path.
    /// </summary>
    public const string LoginPagePath = "login";

    #endregion

    #region Session & Authentication

    /// <summary>
    /// Session key for DSM authentication SID.
    /// </summary>
    public const string DsmSessionKey = "DsmSid";

    /// <summary>
    /// Session key for the logged-in DSM username. Used by session validation to fetch user info via SYNO.Core.User.get.
    /// </summary>
    public const string DsmUsernameKey = "DsmUsername";

    /// <summary>
    /// Session idle timeout in minutes.
    /// </summary>
    public const int SessionTimeoutMinutes = 30;

    /// <summary>
    /// TTL for cached DSM session validation results in minutes. Matches the minimum DSM session timeout (1 minute) to prevent validating stale sessions. Prevents per-request API overhead while detecting expired sessions promptly.
    /// </summary>
    public const int SessionValidationTtlMinutes = 1;

    #endregion

    #region UI Interaction

    /// <summary>
    /// Double-click detection timeout in milliseconds for UI components.
    /// </summary>
    public const int DoubleClickTimeoutMilliseconds = 400;

    /// <summary>
    /// Environment variable name for supported cultures passed from server to WASM client via <c>Blazor.start()</c>.
    /// </summary>
    public const string SupportedCulturesEnvironmentVariable = "ADWH_SUPPORTED_CULTURES";

    /// <summary>
    /// Environment variable name for DSM system culture passed from server to WASM client via <c>Blazor.start()</c>.
    /// </summary>
    public const string SystemCultureEnvironmentVariable = "ADWH_SYSTEM_CULTURE";

    #endregion
}
