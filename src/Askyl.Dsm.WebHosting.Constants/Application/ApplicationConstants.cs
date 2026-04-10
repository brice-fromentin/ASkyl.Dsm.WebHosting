namespace Askyl.Dsm.WebHosting.Constants.Application;

/// <summary>
/// Defines application-specific constants for configuration and settings.
/// </summary>
public static class ApplicationConstants
{
    #region Configuration Files

    /// <summary>
    /// Default name for the application settings file.
    /// </summary>
    public const string SettingsFileName = "appsettings.json";

    /// <summary>
    /// Default name for the websites configuration file.
    /// </summary>
    public const string WebSitesConfigFileName = "websites.json";

    /// <summary>
    /// Configuration key for the download channel version setting.
    /// </summary>
    public const string ChannelVersionKey = "Download:ChannelVersion";

    /// <summary>
    /// Default configuration version.
    /// </summary>
    public const string DefaultConfigurationVersion = "1.0";

    #endregion

    #region Environment & Runtime

    /// <summary>
    /// Default environment for ASP.NET Core applications.
    /// </summary>
    public const string DefaultEnvironment = "Production";

    /// <summary>
    /// Environment variable for ASP.NET Core URLs.
    /// </summary>
    public const string AspNetCoreUrlsEnvironmentVariable = "ASPNETCORE_URLS";

    /// <summary>
    /// Environment variable for ASP.NET Core environment.
    /// </summary>
    public const string AspNetCoreEnvironmentVariable = "ASPNETCORE_ENVIRONMENT";

    /// <summary>
    /// .NET CLI executable name.
    /// </summary>
    public const string DotnetExecutable = "dotnet";

    /// <summary>
    /// The root path for the .NET runtimes directory.
    /// </summary>
    public const string RuntimesRootPath = "../runtimes";

    #endregion

    #region HTTP Client

    /// <summary>
    /// HTTP client name identifier.
    /// </summary>
    public const string HttpClientName = "UiClient";

    /// <summary>
    /// HTTP client timeout in seconds.
    /// </summary>
    public const int HttpClientTimeoutSeconds = 10;

    /// <summary>
    /// Default process timeout in seconds for graceful shutdown operations.
    /// </summary>
    public const int DefaultProcessTimeoutSeconds = 60;

    /// <summary>
    /// Delay in milliseconds to wait after process kill for OS cleanup.
    /// </summary>
    public const int ProcessKillCleanupDelayMs = 500;

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

    #region Port Configuration

    /// <summary>
    /// Minimum port number for web applications (avoiding system services).
    /// </summary>
    public const int MinWebApplicationPort = 1024;

    /// <summary>
    /// Maximum port number for web applications.
    /// </summary>
    public const int MaxWebApplicationPort = 65535;

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

    #region File Extensions

    /// <summary>
    /// File extension for DLL files.
    /// </summary>
    public const string DllFileExtension = ".dll";

    #endregion

    #region Validation Error Messages

    /// <summary>
    /// Validation error message for required site name.
    /// </summary>
    public const string SiteNameRequiredErrorMessage = "Site name is required.";

    /// <summary>
    /// Validation error message for required application path.
    /// </summary>
    public const string ApplicationPathRequiredErrorMessage = "Application path is required.";

    /// <summary>
    /// Validation error message for required port.
    /// </summary>
    public const string PortRequiredErrorMessage = "Port is required.";

    /// <summary>
    /// Validation error message for port range.
    /// </summary>
    public const string PortRangeErrorMessage = "Port must be between 1024 and 65535.";

    /// <summary>
    /// Validation error message for required environment.
    /// </summary>
    public const string EnvironmentRequiredErrorMessage = "Environment is required.";

    /// <summary>
    /// Validation error message for required host name.
    /// </summary>
    public const string HostNameRequiredErrorMessage = "Host name is required.";

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
