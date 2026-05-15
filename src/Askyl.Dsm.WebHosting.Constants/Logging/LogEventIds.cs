namespace Askyl.Dsm.WebHosting.Constants.Logging;

/// <summary>
/// Read-only EventId ranges for <see cref="Microsoft.Extensions.Logging.LoggerMessageAttribute"/> logging extensions.
/// These constants serve as a central registry for collision prevention and documentation — they are not referenced
/// in <c>[LoggerMessage]</c> attributes at runtime (the source generator inlines literal values at compile time).
/// </summary>
static class LogEventIds
{
    #region Authentication — 1000–1099

    /// <summary>Authentication service — IDs 1001–1004.</summary>
    public const int AuthenticationBase = 1000;

    #endregion

    #region File Management — 1100–1199

    /// <summary>FileSystemService — IDs 1100–1111.</summary>
    public const int FileServiceBase = 1100;

    /// <summary>FileManagerService — IDs 1112–1117.</summary>
    public const int FileManagerBase = 1112;

    /// <summary>LogDownloadService — IDs 1118–1124.</summary>
    public const int LogDownloadBase = 1118;

    #endregion

    #region Framework Management — 1200–1299

    /// <summary>FrameworkManagementService — IDs 1200–1206.</summary>
    public const int FrameworkManagementBase = 1200;

    /// <summary>DotnetVersionService — IDs 1207–1213.</summary>
    public const int DotnetVersionBase = 1207;

    #endregion

    #region Process Lifecycle — 1300–1399

    /// <summary>SiteLifecycleManager — IDs 1300–1316.</summary>
    public const int ProcessLifecycleBase = 1300;

    #endregion

    #region Reverse Proxy — 1400–1499

    /// <summary>ReverseProxyManagerService — IDs 1400–1410.</summary>
    public const int ReverseProxyBase = 1400;

    #endregion

    #region Website Hosting — 1500–1599

    /// <summary>WebSiteHostingService — IDs 1500–1533.</summary>
    public const int WebsiteHostingBase = 1500;

    #endregion

    #region Configuration — 1600–1699

    /// <summary>WebSitesConfigurationService — IDs 1600–1611.</summary>
    public const int ConfigurationBase = 1600;

    #endregion

    #region DSM API — 1700–1799

    /// <summary>DsmApiClient — IDs 1700–1704.</summary>
    public const int DsmApiBase = 1700;

    #endregion

    #region Infrastructure — 1800–1899

    /// <summary>ArchiveExtractorService — IDs 1800–1805.</summary>
    public const int ArchiveExtractorBase = 1800;

    /// <summary>VersionsDetectorService — IDs 1806–1809.</summary>
    public const int VersionsDetectorBase = 1806;

    /// <summary>PlatformInfoService — IDs 1810–1811.</summary>
    public const int PlatformInfoBase = 1810;

    /// <summary>DownloaderService — IDs 1812–1815.</summary>
    public const int DownloaderBase = 1812;

    /// <summary>SystemProcessRunner — ID 1816.</summary>
    public const int ProcessRunnerBase = 1816;

    /// <summary>SystemProcessHandle (includes ProcessTerminator) — IDs 1817–1821.</summary>
    public const int ProcessHandleBase = 1817;

    #endregion

    #region Client-side (WASM) — 1900–1999

    /// <summary>LicenseService (client) — ID 1900.</summary>
    public const int ClientBase = 1900;

    #endregion
}
