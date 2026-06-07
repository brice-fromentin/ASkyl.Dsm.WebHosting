namespace Askyl.Dsm.WebHosting.Constants.Application;

/// <summary>
/// Defines constants specific to website configuration, process lifecycle, and hosting.
/// </summary>
public static class WebSiteConstants
{
    #region Configuration Files

    /// <summary>
    /// Default name for the websites configuration file.
    /// </summary>
    public const string ConfigurationFileName = "websites.json";

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

    #endregion

    #region Process Lifecycle

    /// <summary>
    /// Default process timeout in seconds for graceful shutdown operations. ASP.NET Core default shutdown timeout is 5 seconds; 10s provides headroom for custom background service cleanup after SIGTERM.
    /// </summary>
    public const int DefaultProcessTimeoutSeconds = 10;

    /// <summary>
    /// Minimum process timeout in seconds for graceful shutdown operations. Equals default — you can't go lower than default without explicit reason.
    /// </summary>
    public const int MinProcessTimeoutSeconds = DefaultProcessTimeoutSeconds;

    /// <summary>
    /// Maximum process timeout in seconds for graceful shutdown operations.
    /// </summary>
    public const int MaxProcessTimeoutSeconds = 120;

    /// <summary>
    /// Delay in milliseconds to wait after process kill for OS cleanup.
    /// </summary>
    public const int ProcessKillCleanupDelayMs = 500;

    #endregion

    #region Port Configuration

    /// <summary>
    /// Default public port for HTTPS web applications.
    /// </summary>
    public const int DefaultPublicPort = 443;

    /// <summary>
    /// Minimum port number for web applications (avoiding system services).
    /// </summary>
    public const int MinWebApplicationPort = 1024;

    /// <summary>
    /// Maximum port number for web applications.
    /// </summary>
    public const int MaxWebApplicationPort = 65535;

    #endregion

    #region File Extensions

    /// <summary>
    /// File extension for DLL files.
    /// </summary>
    public const string DllFileExtension = ".dll";

    #endregion
}
