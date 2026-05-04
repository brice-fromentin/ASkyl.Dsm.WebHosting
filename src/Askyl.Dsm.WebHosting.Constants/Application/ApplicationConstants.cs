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
    /// The sub-path alias for the application.
    /// </summary>
    public const string ApplicationSubPath = "adwh";

    /// <summary>
    /// The sub-path for the application URL, used for routing.
    /// </summary>
    public const string ApplicationUrlSubPath = "/" + ApplicationSubPath;

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
    /// Session idle timeout in minutes.
    /// </summary>
    public const int SessionTimeoutMinutes = 30;

    #endregion

    #region Status Messages

    /// <summary>
    /// Error message when platform is not supported.
    /// </summary>
    public const string PlatformNotSupportedErrorMessage = "The application can only run on Linux or MacOS";

    /// <summary>
    /// Error message for failed authentication.
    /// </summary>
    public const string AuthenticationFailedErrorMessage = "Authentication failed";

    /// <summary>
    /// Success message for successful authentication.
    /// </summary>
    public const string AuthenticationSuccessfulMessage = "Authentication successful";

    #endregion

    #region Loading Messages

    /// <summary>
    /// Error message when failing to load directory contents.
    /// </summary>
    public const string FailedToLoadDirectoryContentsErrorMessage = "Failed to load directory contents";

    /// <summary>
    /// Loading message for shared folders.
    /// </summary>
    public const string LoadingSharedFoldersMessage = "Loading shared folders...";

    /// <summary>
    /// Loading message for directory contents.
    /// </summary>
    public const string LoadingDirectoryContentsMessage = "Loading directory contents...";

    #endregion

    #region UI Interaction

    /// <summary>
    /// Double-click detection timeout in milliseconds for UI components.
    /// </summary>
    public const int DoubleClickTimeoutMilliseconds = 400;

    #endregion
}
