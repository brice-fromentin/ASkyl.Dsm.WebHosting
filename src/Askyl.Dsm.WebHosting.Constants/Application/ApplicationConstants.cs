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

    /// <summary>
    /// Default name for the websites configuration file.
    /// </summary>
    public const string WebSitesConfigFileName = "websites.json";

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
    /// Default configuration version.
    /// </summary>
    public const string DefaultConfigurationVersion = "1.0";

    /// <summary>
    /// Minimum port number for web applications (avoiding system services).
    /// </summary>
    public const int MinWebApplicationPort = 1024;

    /// <summary>
    /// Maximum port number for web applications.
    /// </summary>
    public const int MaxWebApplicationPort = 65535;

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
}
