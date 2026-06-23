namespace Askyl.Dsm.WebHosting.Constants.Application;

public static class LogConstants
{
    #region Directory and Path Constants

    public const string LogsDirectoryName = "logs";
    public const string PackageLogDirectoryPath = "/var/packages/AskylWebHosting/var/logs";
    public const string DebugLogFilePath = "/tmp/adwh-debug.log";

    #endregion

    #region File Type Constants

    public const string ZipFileExtension = ".zip";

    #endregion

    #region Archive Entry Paths

    /// <summary>
    /// Archive folder prefix for package-level log entries.
    /// </summary>
    public const string LogArchivePackagePrefix = "package-logs";

    /// <summary>
    /// Display name for package logs in the download UI.
    /// </summary>
    public const string LogArchivePackageDisplayName = "Package logs";

    /// <summary>
    /// Archive entry path for the debug log file.
    /// </summary>
    public const string LogArchiveDebugEntryPath = "debug-logs/adwh-debug.log";

    /// <summary>
    /// Archive folder prefix for application-level log entries.
    /// </summary>
    public const string LogArchiveAppPrefix = "application-logs";

    /// <summary>
    /// Display name for application logs in the download UI.
    /// </summary>
    public const string LogArchiveAppDisplayName = "Application logs";

    #endregion
}
