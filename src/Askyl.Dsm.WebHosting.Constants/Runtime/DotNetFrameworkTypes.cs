namespace Askyl.Dsm.WebHosting.Constants.Runtime;

/// <summary>
/// Defines the types of .NET framework components available for installation and management.
/// </summary>
public static class DotNetFrameworkTypes
{
    /// <summary>
    /// ASP.NET Core framework type for web applications.
    /// </summary>
    public const string AspNetCore = "ASP.NET Core";

    /// <summary>
    /// .NET SDK framework type for development.
    /// </summary>
    public const string Sdk = "SDK";

    /// <summary>
    /// Main .NET SDK framework type.
    /// </summary>
    public const string SdkMain = "SDK (Main)";

    /// <summary>
    /// .NET Runtime framework type for application execution.
    /// </summary>
    public const string Runtime = "Runtime";
}
