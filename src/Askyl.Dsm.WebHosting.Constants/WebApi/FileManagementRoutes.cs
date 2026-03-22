namespace Askyl.Dsm.WebHosting.Constants.WebApi;

/// <summary>
/// Defines file management API route constants for the application's REST endpoints.
/// </summary>
public static class FileManagementRoutes
{
    /// <summary>
    /// Base route prefix for the File Management controller (versioned).
    /// </summary>
    public const string ControllerBaseRoute = "api/v1/files";

    #region Shared Folders

    /// <summary>
    /// Route segment to get shared folders.
    /// </summary>
    public const string SharedFoldersRoute = "shared-folders";

    /// <summary>
    /// Full route for the shared folders endpoint.
    /// </summary>
    public static string SharedFoldersFullRoute => String.Join("/", ControllerBaseRoute, SharedFoldersRoute);

    #endregion

    #region Directory Contents

    /// <summary>
    /// Route segment to get directory contents.
    /// </summary>
    public const string DirectoryContentsRoute = "directory";

    /// <summary>
    /// Full route for the directory contents endpoint.
    /// </summary>
    public static string DirectoryContentsFullRoute => String.Join("/", ControllerBaseRoute, DirectoryContentsRoute);

    #endregion
}
