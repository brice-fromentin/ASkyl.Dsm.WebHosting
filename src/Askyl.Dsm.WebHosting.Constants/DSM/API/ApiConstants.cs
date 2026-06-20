namespace Askyl.Dsm.WebHosting.Constants.DSM.API;

/// <summary>
/// Contains all Synology DSM API constants: names, methods, versions, and composite values.
/// </summary>
public static class ApiConstants
{
    #region API Names — Core

    public const string Handshake = "query.cgi";
    public const string Info = "SYNO.API.Info";
    public const string Auth = "SYNO.API.Auth";

    #endregion

    #region API Names — FileStation

    public const string FileStationInfo = "SYNO.FileStation.Info";
    public const string FileStationList = "SYNO.FileStation.List";

    #endregion

    #region API Names — Core System

    public const string CoreAcl = "SYNO.Core.ACL";
    public const string CoreUser = "SYNO.Core.User";
    public const string CoreUserSettings = "SYNO.Core.UserSettings";

    #endregion

    #region API Names — AppPortal

    public const string AppPortalReverseProxy = "SYNO.Core.AppPortal.ReverseProxy";

    #endregion

    #region Methods

    public const string MethodGet = "get";
    public const string MethodCreate = "create";
    public const string MethodList = "list";
    public const string MethodUpdate = "update";
    public const string MethodDelete = "delete";

    #endregion

    #region Versions

    public const int MinVersion = 1;
    public const int MaxVersion = 7;

    #endregion

    #region Composite

    /// <summary>
    /// Comma-separated list of required API names for API handshake requests.
    /// </summary>
    public static readonly string RequiredApisJoined = $"{Auth},{FileStationInfo},{FileStationList},{CoreAcl},{CoreUser},{CoreUserSettings},{AppPortalReverseProxy}";

    #endregion
}
