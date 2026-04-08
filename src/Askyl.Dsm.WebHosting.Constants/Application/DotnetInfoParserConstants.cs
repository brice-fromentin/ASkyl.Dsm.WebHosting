namespace Askyl.Dsm.WebHosting.Constants.Application;

/// <summary>
/// Defines constants for parsing dotnet --info output.
/// Used by VersionsDetectorService to identify sections and framework types.
/// </summary>
public static class DotnetInfoParserConstants
{
    #region Section Headers

    /// <summary>
    /// Section header for .NET SDKs installed list.
    /// Example: ".NET SDKs installed:"
    /// </summary>
    public const string SdkSectionHeader = ".NET SDKs installed:";

    /// <summary>
    /// Section header for .NET runtimes installed list.
    /// Example: ".NET runtimes installed:"
    /// </summary>
    public const string RuntimeSectionHeader = ".NET runtimes installed:";

    /// <summary>
    /// Section header for main .NET SDK information.
    /// Example: ".NET SDK:"
    /// </summary>
    public const string MainSdkSectionHeader = ".NET SDK:";

    #endregion

    #region Framework Type Identifiers

    /// <summary>
    /// Framework type identifier for main SDK version.
    /// Used in ordering and display (highest priority).
    /// </summary>
    public const string FrameworkTypeMainSdk = "SDK (Main)";

    /// <summary>
    /// Framework type identifier for SDK versions from installed list.
    /// </summary>
    public const string FrameworkTypeSdk = "SDK";

    /// <summary>
    /// Framework type identifier for runtime versions (Microsoft.NETCore.App).
    /// </summary>
    public const string FrameworkTypeRuntime = "Runtime";

    /// <summary>
    /// Framework type identifier for ASP.NET Core versions (Microsoft.AspNetCore.App).
    /// </summary>
    public const string FrameworkTypeAspNetCore = "ASP.NET Core";

    #endregion

    #region Product Name Identifiers

    /// <summary>
    /// Product name for ASP.NET Core runtime in dotnet --info output.
    /// Example: "Microsoft.AspNetCore.App 9.0.5"
    /// </summary>
    public const string AspNetCoreProductName = "Microsoft.AspNetCore.App";

    /// <summary>
    /// Product name for .NET Core runtime in dotnet --info output.
    /// Example: "Microsoft.NETCore.App 9.0.5"
    /// </summary>
    public const string NetCoreProductName = "Microsoft.NETCore.App";

    #endregion

    #region Version Line Prefixes

    /// <summary>
    /// Prefix for version line in main SDK section.
    /// Example: " Version:           9.0.301"
    /// </summary>
    public const string VersionLinePrefix = "Version:";

    #endregion
}
