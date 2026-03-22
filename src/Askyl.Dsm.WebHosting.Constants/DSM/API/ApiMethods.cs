namespace Askyl.Dsm.WebHosting.Constants.DSM.API;

/// <summary>
/// Defines API method names used in DSM API calls.
/// </summary>
public static class ApiMethods
{
    #region CRUD Operations

    /// <summary>
    /// Method for creating new resources.
    /// </summary>
    public const string Create = "create";

    /// <summary>
    /// Method for adding resources.
    /// </summary>
    public const string Add = "add";

    /// <summary>
    /// Method for getting information or retrieving resources.
    /// </summary>
    public const string Get = "get";

    /// <summary>
    /// Method for listing resources.
    /// </summary>
    public const string List = "list";

    /// <summary>
    /// Method for updating existing resources.
    /// </summary>
    public const string Update = "update";

    /// <summary>
    /// Method for deleting resources.
    /// </summary>
    public const string Delete = "delete";

    #endregion

    #region Lifecycle Operations

    /// <summary>
    /// Method for starting operations or services.
    /// </summary>
    public const string Start = "start";

    /// <summary>
    /// Method for stopping operations or services.
    /// </summary>
    public const string Stop = "stop";

    #endregion

    #region Status Operations

    /// <summary>
    /// Method for getting status information.
    /// </summary>
    public const string Status = "status";

    #endregion
}
