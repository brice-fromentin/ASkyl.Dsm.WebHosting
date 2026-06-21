namespace Askyl.Dsm.WebHosting.Constants.Logging;

/// <summary>
/// Service-level EventId bases for <see cref="Microsoft.Extensions.Logging.LoggerMessageAttribute"/> logging extensions.
/// Each service owns a dedicated range. These constants serve as a central registry for collision prevention and documentation — they
/// are not referenced in <c>[LoggerMessage]</c> attributes at runtime (the source generator inlines literal values).
/// </summary>
static class LogEventIds
{
    #region Authentication — 1000000–1000999

    /// <summary>Authentication service — IDs 1000001–1000007.</summary>
    public const int AuthenticationBase = 1000000;

    #endregion

    #region File Management — 1100000–1300999

    /// <summary>FileSystemService — IDs 1100001–1100012.</summary>
    public const int FileServiceBase = 1100000;

    /// <summary>FileManagerService — IDs 1200001–1200006.</summary>
    public const int FileManagerBase = 1200000;

    /// <summary>LogDownloadService — IDs 1300001–1300007.</summary>
    public const int LogDownloadBase = 1300000;

    #endregion

    #region Framework Management — 1400000–1500999

    /// <summary>FrameworkManagementService — IDs 1400001–1400007.</summary>
    public const int FrameworkManagementBase = 1400000;

    /// <summary>DotnetVersionService — IDs 1500001–1500007.</summary>
    public const int DotnetVersionBase = 1500000;

    #endregion

    #region Process Lifecycle — 1600000–1600999

    /// <summary>SiteLifecycleManager — IDs 1600001–1600019.</summary>
    public const int ProcessLifecycleBase = 1600000;

    #endregion

    #region Reverse Proxy — 1700000–1700999

    /// <summary>ReverseProxyManagerService — IDs 1700001–1700013.</summary>
    public const int ReverseProxyBase = 1700000;

    #endregion

    #region Website Hosting — 1800000–1800999

    /// <summary>WebSiteHostingService — IDs 1800001–1800031.</summary>
    public const int WebsiteHostingBase = 1800000;

    #endregion

    #region Configuration — 1900000–1900999

    /// <summary>WebSitesConfigurationService — IDs 1900001–1900012.</summary>
    public const int ConfigurationBase = 1900000;

    #endregion

    #region DSM API — 2000000–2000999

    /// <summary>DsmApiClient — IDs 2000001–2000013.</summary>
    public const int DsmApiBase = 2000000;

    #endregion

    #region Infrastructure — 2100000–2800999

    /// <summary>ArchiveExtractorService — IDs 2100001–2100006.</summary>
    public const int ArchiveExtractorBase = 2100000;

    /// <summary>VersionsDetectorService — IDs 2200001–2200004.</summary>
    public const int VersionsDetectorBase = 2200000;

    /// <summary>AssemblyRuntimeDetector — IDs 2250001–2250005.</summary>
    public const int AssemblyRuntimeDetectorBase = 2250000;

    /// <summary>PlatformInfoService — IDs 2300001–2300002.</summary>
    public const int PlatformInfoBase = 2300000;

    /// <summary>DownloaderService — IDs 2400001–2400004.</summary>
    public const int DownloaderBase = 2400000;

    /// <summary>SystemProcessRunner — ID 2500001.</summary>
    public const int ProcessRunnerBase = 2500000;

    /// <summary>SystemProcessHandle (incl. ProcessTerminator) — IDs 2600001–2600005.</summary>
    public const int ProcessHandleBase = 2600000;

    /// <summary>GlobalizationSettings — IDs 2700001–2700004.</summary>
    public const int GlobalizationSettingsBase = 2700000;

    /// <summary>DsmSettingsService — IDs 2800001–2800005.</summary>
    public const int DsmSettingsBase = 2800000;

    #endregion

    #region Client-side (WASM) — 7000000–7900000

    /// <summary>LicenseService (client) — ID 7000001.</summary>
    public const int ClientBase = 7000000;

    /// <summary>CultureManager (client) — IDs 7600001–7600010.</summary>
    public const int CultureManagerBase = 7600000;

    #endregion
}
