namespace Askyl.Dsm.WebHosting.Constants.Application;

public static class LogConstants
{

    #region Directory and Path Constants

    public const string LogsDirectoryName = "logs";

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

    public const string LogDownloadEndpoint = "/api/LogDownload/logs";
    public const string TokenQueryParameter = "token";

    #endregion

}