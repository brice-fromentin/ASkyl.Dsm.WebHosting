namespace Askyl.Dsm.WebHosting.Constants.Application;

public static class LogConstants
{

    #region Directory and Path Constants

    public const string LogsDirectoryName = "logs";
    public const string PackageLogDirectoryPath = "/var/packages/AskylWebHosting/var/logs";
    public const string DebugLogFilePath = "/tmp/adwh-debug.log";

    #endregion

    #region Date Format Constants

    public const string ArchiveDateTimeFormat = "yyyyMMdd-HHmmss";

    #endregion

    #region File and Media Type Constants

    public const string ZipFileExtension = ".zip";
    public const string ZipMediaType = "application/zip";

    #endregion

    #region Token Constants

    public const string TokenPrefix = "auth_token_";
    public const int TokenLifetimeMinutes = 5;

    #endregion

    #region API Endpoints

    public const string LogDownloadEndpoint = ApplicationConstants.ApplicationSubPath + "/api/LogDownload/logs";
    
    public const string TokenQueryParameter = "token";

    #endregion

}