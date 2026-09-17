namespace Askyl.Dsm.WebHosting.Constants.Logging;

/// <summary>
/// Service-level EventId ranges for <see cref="Microsoft.Extensions.Logging.LoggerMessageAttribute"/> logging extensions.
/// Each service owns one, from <c>Base</c> exclusive to <c>Last</c> inclusive. The source generator inlines literal values, so nothing
/// here is read at runtime — but <c>LogEventIdRegistryTests</c> reads it, and holds the assembly to it: every id must fall inside exactly
/// one range, ranges may not overlap, and each <c>Last</c> must be the highest id its range actually uses.
/// Before this was enforced the bounds lived in prose and drifted twice, unnoticed until someone read them against the source.
/// </summary>
static class LogEventIds
{
    #region Authentication — 1000000–1000999

    /// <summary>Authentication service.</summary>
    public const int AuthenticationBase = 1000000;
    public const int AuthenticationLast = 1000007;

    #endregion

    #region File Management — 1100000–1300999

    /// <summary>FileSystemService.</summary>
    public const int FileServiceBase = 1100000;
    public const int FileServiceLast = 1100012;

    /// <summary>FileManagerService.</summary>
    public const int FileManagerBase = 1200000;
    public const int FileManagerLast = 1200006;

    /// <summary>LogDownloadService.</summary>
    public const int LogDownloadBase = 1300000;
    public const int LogDownloadLast = 1300007;

    #endregion

    #region Framework Management — 1400000–1500999

    /// <summary>FrameworkManagementService.</summary>
    public const int FrameworkManagementBase = 1400000;
    public const int FrameworkManagementLast = 1400007;

    /// <summary>DotnetVersionService.</summary>
    public const int DotnetVersionBase = 1500000;
    public const int DotnetVersionLast = 1500007;

    #endregion

    #region Process Lifecycle — 1600000–1600999

    /// <summary>SiteLifecycleManager.</summary>
    public const int ProcessLifecycleBase = 1600000;
    public const int ProcessLifecycleLast = 1600020;

    #endregion

    #region Reverse Proxy — 1700000–1700999

    /// <summary>ReverseProxyManagerService.</summary>
    public const int ReverseProxyBase = 1700000;
    public const int ReverseProxyLast = 1700014;

    #endregion

    #region Website Hosting — 1800000–1800999

    /// <summary>WebSiteHostingService.</summary>
    public const int WebsiteHostingBase = 1800000;
    public const int WebsiteHostingLast = 1800032;

    #endregion

    #region Configuration — 1900000–1900999

    /// <summary>WebSitesConfigurationService.</summary>
    public const int ConfigurationBase = 1900000;
    public const int ConfigurationLast = 1900010;

    #endregion

    #region DSM API — 2000000–2000999

    /// <summary>DsmApiClient.</summary>
    public const int DsmApiBase = 2000000;
    public const int DsmApiLast = 2000013;

    #endregion

    #region Infrastructure — 2100000–2800999

    /// <summary>ArchiveExtractorService.</summary>
    public const int ArchiveExtractorBase = 2100000;
    public const int ArchiveExtractorLast = 2100006;

    /// <summary>VersionsDetectorService.</summary>
    public const int VersionsDetectorBase = 2200000;
    public const int VersionsDetectorLast = 2200005;

    /// <summary>AssemblyRuntimeDetector.</summary>
    public const int AssemblyRuntimeDetectorBase = 2250000;
    public const int AssemblyRuntimeDetectorLast = 2250005;

    /// <summary>PlatformInfoService.</summary>
    public const int PlatformInfoBase = 2300000;
    public const int PlatformInfoLast = 2300002;

    /// <summary>DownloaderService.</summary>
    public const int DownloaderBase = 2400000;
    public const int DownloaderLast = 2400004;

    /// <summary>SystemProcessRunner.</summary>
    public const int ProcessRunnerBase = 2500000;
    public const int ProcessRunnerLast = 2500003;

    /// <summary>SystemProcessHandle (incl. ProcessTerminator).</summary>
    public const int ProcessHandleBase = 2600000;
    public const int ProcessHandleLast = 2600005;

    /// <summary>GlobalizationSettings.</summary>
    public const int GlobalizationSettingsBase = 2700000;
    public const int GlobalizationSettingsLast = 2700004;

    /// <summary>DsmSettingsService.</summary>
    public const int DsmSettingsBase = 2800000;
    public const int DsmSettingsLast = 2800006;

    /// <summary>DsmSession.</summary>
    public const int DsmSessionBase = 2900000;
    public const int DsmSessionLast = 2900012;

    #endregion

    #region Client-side (WASM) — 7000000–7900000

    /// <summary>LicenseService (client).</summary>
    public const int ClientBase = 7000000;
    public const int ClientLast = 7000001;

    /// <summary>CultureManager (client).</summary>
    public const int CultureManagerBase = 7600000;
    public const int CultureManagerLast = 7600011;

    /// <summary>Client utilities (JS interop).</summary>
    public const int ClientUtilitiesBase = 7100000;
    public const int ClientUtilitiesLast = 7100001;

    #endregion
}
