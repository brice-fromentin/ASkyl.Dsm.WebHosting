namespace Askyl.Dsm.WebHosting.Globalization;

/// <summary>
/// Strongly-typed localization key constants, organized by domain.
/// Keys are flat strings that match the &lt;data name="..."&gt; entries in SharedResource.resx.
/// Use with <see cref="ILocalizer"/> for compile-time safety.
/// </summary>
public static class L
{
    /// <summary>Common UI strings shared across the application.</summary>
    public static class Common
    {
        public const string OK = "OK";
        public const string Cancel = "Cancel";
        public const string Close = "Close";
        public const string Save = "Save";
        public const string Delete = "Delete";
        public const string Refresh = "Refresh";
        public const string Loading = "Loading";
        public const string Size = "Common_Size";
        public const string Type = "Common_Type";
        public const string Modified = "Common_Modified";
        public const string ApplicationTitle = "Common_ApplicationTitle";
        public const string UnhandledError = "Common_UnhandledError";
        public const string Dash = "Common_Dash";
        public const string CheckMark = "Common_CheckMark";
        public const string WarningIcon = "Common_WarningIcon";
    }

    /// <summary>Authentication and login page strings.</summary>
    public static class Login
    {
        public const string PageTitle = "Login_PageTitle";
        public const string DialogTitle = "Login_DialogTitle";
        public const string LoginLabel = "Login_LoginLabel";
        public const string PasswordLabel = "Login_PasswordLabel";
        public const string OTPLabel = "Login_OTPLabel";
        public const string Authenticating = "Login_Authenticating";
    }

    /// <summary>Home page strings (toolbar, grid columns, toasts).</summary>
    public static class Home
    {
        public const string PageTitle = "Home_PageTitle";
        public const string AddButton = "Home_AddButton";
        public const string EditButton = "Home_EditButton";
        public const string DotnetVersionButton = "Home_DotnetVersionButton";
        public const string AspNetOnlineButton = "Home_AspNetOnlineButton";
        public const string LicensesButton = "Home_LicensesButton";
        public const string DownloadLogsButton = "Home_DownloadLogsButton";
        public const string LogoutButton = "Home_LogoutButton";
        public const string GridColumnName = "Home_GridColumnName";
        public const string GridColumnPath = "Home_GridColumnPath";
        public const string GridColumnFramework = "Home_GridColumnFramework";
        public const string GridColumnInternalPort = "Home_GridColumnInternalPort";
        public const string GridColumnState = "Home_GridColumnState";
        public const string LoadingWebsites = "Home_LoadingWebsites";
        public const string DeleteConfirmation = "Home_DeleteConfirmation";
        public const string ErrorDeleting = "Home_ErrorDeleting";
        public const string ErrorStarting = "Home_ErrorStarting";
        public const string ErrorStopping = "Home_ErrorStopping";
        public const string ErrorLoggingOut = "Home_ErrorLoggingOut";
    }

    /// <summary>Not found page strings.</summary>
    public static class NotFound
    {
        public const string PageTitle = "NotFound_PageTitle";
        public const string Content = "NotFound_Content";
        public const string GoHome = "NotFound_GoHome";
    }

    /// <summary>Website configuration dialog strings.</summary>
    public static class WebsiteConfig
    {
        public const string EditTitle = "WebsiteConfig_EditTitle";
        public const string AddTitle = "WebsiteConfig_AddTitle";
        public const string NameLabel = "WebsiteConfig_NameLabel";
        public const string AppSettingsSection = "WebsiteConfig_AppSettingsSection";
        public const string AppPathLabel = "WebsiteConfig_AppPathLabel";
        public const string EnvironmentLabel = "WebsiteConfig_EnvironmentLabel";
        public const string InternalPortLabel = "WebsiteConfig_InternalPortLabel";
        public const string ShutdownTimeoutLabel = "WebsiteConfig_ShutdownTimeoutLabel";
        public const string EnabledLabel = "WebsiteConfig_EnabledLabel";
        public const string AutoStartLabel = "WebsiteConfig_AutoStartLabel";
        public const string ReverseProxySection = "WebsiteConfig_ReverseProxySection";
        public const string HostnameLabel = "WebsiteConfig_HostnameLabel";
        public const string ProtocolLabel = "WebsiteConfig_ProtocolLabel";
        public const string PublicPortLabel = "WebsiteConfig_PublicPortLabel";
        public const string EnableHSTSLabel = "WebsiteConfig_EnableHSTSLabel";
        public const string UpdateButton = "WebsiteConfig_UpdateButton";
        public const string CreateButton = "WebsiteConfig_CreateButton";
        public const string FrameworkNotInstalled = "WebsiteConfig_FrameworkNotInstalled";
        public const string ErrorModifying = "WebsiteConfig_ErrorModifying";
        public const string ActionUpdating = "WebsiteConfig_ActionUpdating";
        public const string ActionCreating = "WebsiteConfig_ActionCreating";
        public const string ProtocolHttp = "WebsiteConfig_ProtocolHttp";
        public const string ProtocolHttps = "WebsiteConfig_ProtocolHttps";
    }

    /// <summary>ASP.NET releases dialog strings.</summary>
    public static class AspNetReleases
    {
        public const string DialogTitle = "AspNetReleases_DialogTitle";
        public const string ChannelLabel = "AspNetReleases_ChannelLabel";
        public const string SelectVersion = "AspNetReleases_SelectVersion";
        public const string InstallVersion = "AspNetReleases_InstallVersion";
        public const string UninstallVersion = "AspNetReleases_UninstallVersion";
        public const string GridColumnVersion = "AspNetReleases_GridColumnVersion";
        public const string GridColumnInstalled = "AspNetReleases_GridColumnInstalled";
        public const string GridColumnSecurity = "AspNetReleases_GridColumnSecurity";
        public const string GridColumnRelease = "AspNetReleases_GridColumnRelease";
        public const string InstallationError = "AspNetReleases_InstallationError";
        public const string UninstallConfirmation = "AspNetReleases_UninstallConfirmation";
        public const string UninstallationError = "AspNetReleases_UninstallationError";
    }

    /// <summary>.NET versions dialog strings.</summary>
    public static class DotnetVersions
    {
        public const string DialogTitle = "DotnetVersions_DialogTitle";
        public const string Searching = "DotnetVersions_Searching";
        public const string NotFound = "DotnetVersions_NotFound";
        public const string FailedToLoad = "DotnetVersions_FailedToLoad";
        public const string ErrorSearching = "DotnetVersions_ErrorSearching";
    }

    /// <summary>Licenses dialog strings.</summary>
    public static class Licenses
    {
        public const string DialogTitle = "Licenses_DialogTitle";
        public const string Loading = "Licenses_Loading";
    }

    /// <summary>File selection dialog strings.</summary>
    public static class FileSelection
    {
        public const string DialogTitle = "FileSelection_DialogTitle";
        public const string SelectFile = "FileSelection_SelectFile";
        public const string NoFilesFound = "FileSelection_NoFilesFound";
        public const string SelectFolder = "FileSelection_SelectFolder";
        public const string Directory = "FileSelection_Directory";
        public const string File = "FileSelection_File";
    }

    /// <summary>AutoDataGrid control strings.</summary>
    public static class AutoDataGrid
    {
        public const string Loading = "AutoDataGrid_Loading";
        public const string Empty = "AutoDataGrid_Empty";
        public const string ItemsCount = "AutoDataGrid_ItemsCount";
    }

    /// <summary>Loading/working state messages.</summary>
    public static class Loading
    {
        public const string SharedFolders = "Loading_SharedFolders";
        public const string DirectoryContents = "Loading_DirectoryContents";
        public const string Installing = "Loading_Installing";
        public const string Channels = "Loading_Channels";
        public const string Releases = "Loading_Releases";
        public const string Uninstalling = "Loading_Uninstalling";
        public const string DeletingWebsite = "Loading_DeletingWebsite";
        public const string StartingWebsite = "Loading_StartingWebsite";
        public const string StoppingWebsite = "Loading_StoppingWebsite";
        public const string UpdatingWebsite = "Loading_UpdatingWebsite";
        public const string CreatingWebsite = "Loading_CreatingWebsite";
    }

    /// <summary>Error messages displayed to the user (toasts, dialogs, API responses).</summary>
    public static class Error
    {
        public const string PlatformNotSupported = "Error_PlatformNotSupported";
        public const string AuthenticationFailed = "Error_AuthenticationFailed";
        public const string OperationFailed = "Error_OperationFailed";
        public const string RateLimitExceeded = "Error_RateLimitExceeded";
        public const string FailedToLoadWebsites = "Error_FailedToLoadWebsites";
        public const string FailedToAddWebsite = "Error_FailedToAddWebsite";
        public const string FailedToUpdateWebsite = "Error_FailedToUpdateWebsite";
        public const string FailedToDeleteWebsite = "Error_FailedToDeleteWebsite";
        public const string FailedToRemoveWebsite = "Error_FailedToRemoveWebsite";
        public const string FailedToStartWebsite = "Error_FailedToStartWebsite";
        public const string FailedToStopWebsite = "Error_FailedToStopWebsite";
        public const string FailedToLogout = "Error_FailedToLogout";
        public const string FailedToLoadSharedFolders = "Error_FailedToLoadSharedFolders";
        public const string FailedToLoadDirectoryContents = "Error_FailedToLoadDirectoryContents";
        public const string FailedToLoadDirectoryContentsWithPath = "Error_FailedToLoadDirectoryContentsWithPath";
        public const string FailedToLoadInstalledVersions = "Error_FailedToLoadInstalledVersions";
        public const string FailedToCheckChannelInstalled = "Error_FailedToCheckChannelInstalled";
        public const string FailedToCheckVersionInstalled = "Error_FailedToCheckVersionInstalled";
        public const string FailedToLoadChannels = "Error_FailedToLoadChannels";
        public const string FailedToLoadReleases = "Error_FailedToLoadReleases";
        public const string FailedToLoadReleasesForChannel = "Error_FailedToLoadReleasesForChannel";
        public const string FailedToInstallFramework = "Error_FailedToInstallFramework";
        public const string FailedToLogin = "Error_FailedToLogin";
        public const string FailedToCheckAuthStatus = "Error_FailedToCheckAuthStatus";
        public const string Unknown = "Error_Unknown";
        public const string ApplicationPathRequired = "Error_ApplicationPathRequired";
        public const string InstallationFailed = "Error_InstallationFailed";
        public const string UninstallationFailed = "Error_UninstallationFailed";
        public const string SiteConfigUpdating = "Error_SiteConfigUpdating";
        public const string FailedToQueueStart = "Error_FailedToQueueStart";
        public const string FailedToQueueStop = "Error_FailedToQueueStop";
        public const string SiteAlreadyRunning = "Error_SiteAlreadyRunning";
        public const string ApplicationBinaryNotFound = "Error_ApplicationBinaryNotFound";
        public const string IncompatibleFramework = "Error_IncompatibleFramework";
        public const string InstanceNotFound = "Error_InstanceNotFound";
        public const string NoApplicationPath = "Error_NoApplicationPath";
        public const string SiteNotFound = "Error_SiteNotFound";
        public const string FailedToSetACL = "Error_FailedToSetACL";
        public const string NoSessionFound = "Error_NoSessionFound";
        public const string SessionExpired = "Error_SessionExpired";
        public const string FailedToSetPermissions = "Error_FailedToSetPermissions";
        public const string FailedToCreateReverseProxy = "Error_FailedToCreateReverseProxy";
        public const string FailedToUpdateReverseProxy = "Error_FailedToUpdateReverseProxy";
    }

    /// <summary>Success messages displayed to the user.</summary>
    public static class Success
    {
        public const string AuthenticationSuccessful = "Success_AuthenticationSuccessful";
        public const string InstallationCompleted = "Success_InstallationCompleted";
        public const string LogoutSuccessful = "Success_LogoutSuccessful";
        public const string UninstallationCompleted = "Success_UninstallationCompleted";
    }

    /// <summary>Validation error messages from server-side services (not model-specific).</summary>
    public static class Validation
    {
        public const string PathRequired = "Validation_PathRequired";
        public const string PathTraversalDetected = "Validation_PathTraversalDetected";
        public const string InvalidVersionFormat = "Validation_InvalidVersionFormat";
        public const string VersionRequired = "Validation_VersionRequired";
        public const string EnvVarKeyTooLong = "Validation_EnvVarKeyTooLong";
        public const string EnvVarValueTooLong = "Validation_EnvVarValueTooLong";
    }

    /// <summary>Validation messages for WebSiteConfiguration model.</summary>
    public static class WebSiteConfiguration
    {
        public const string NameRequired = "WebSiteConfiguration_NameRequired";
        public const string NameLength = "WebSiteConfiguration_NameLength";
        public const string ApplicationPathRequired = "WebSiteConfiguration_ApplicationPathRequired";
        public const string InternalPortRequired = "WebSiteConfiguration_InternalPortRequired";
        public const string PublicPortRequired = "WebSiteConfiguration_PublicPortRequired";
        public const string InternalPortRange = "WebSiteConfiguration_InternalPortRange";
        public const string PublicPortRange = "WebSiteConfiguration_PublicPortRange";
        public const string EnvironmentRequired = "WebSiteConfiguration_EnvironmentRequired";
        public const string ProcessTimeoutRange = "WebSiteConfiguration_ProcessTimeoutRange";
        public const string HostNameRequired = "WebSiteConfiguration_HostNameRequired";
    }

    /// <summary>Validation messages for LoginCredentials model.</summary>
    public static class LoginCredentials
    {
        public const string LoginRequired = "LoginCredentials_LoginRequired";
        public const string PasswordRequired = "LoginCredentials_PasswordRequired";
    }
}
