namespace Askyl.Dsm.WebHosting.Constants.DSM.API;

/// <summary>
/// Defines constants for DSM's SYNO.Core.ACL API, as its own permission editor uses it (docs/ai/dsm-api-notes.md).
/// </summary>
public static class AclConstants
{
    /// <summary>
    /// ACL permission type for allowing access.
    /// </summary>
    public const string PermissionTypeAllow = "allow";

    /// <summary>
    /// ACL owner type value for group-based access control entries.
    /// </summary>
    public const string OwnerTypeGroup = "group";

    /// <summary>
    /// ACL owner name for DSM's web services group, which the package's account belongs to.
    /// </summary>
    public const string OwnerNameHttp = "http";

    /// <summary>
    /// The <c>type</c> of a <c>get</c> that returns explicit and inherited entries alike.
    /// </summary>
    public const string GetTypeAll = "all";

    /// <summary>
    /// The <c>level</c> of an entry set on the path itself; inherited entries count the folders above.
    /// </summary>
    public const int ExplicitLevel = 0;

    /// <summary>
    /// Interval between two <c>status</c> calls on a <c>set</c> task. DSM finished in 0.2 s on the Virtual DSM.
    /// </summary>
    public const int TaskPollIntervalMilliseconds = 200;

    /// <summary>
    /// How long a <c>set</c> task may run before the grant is reported as failed.
    /// </summary>
    public const int TaskTimeoutSeconds = 10;
}
