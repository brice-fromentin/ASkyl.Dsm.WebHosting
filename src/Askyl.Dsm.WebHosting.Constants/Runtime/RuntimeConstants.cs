namespace Askyl.Dsm.WebHosting.Constants.Runtime;

/// <summary>
/// Contains runtime architecture and operating system identifier constants.
/// </summary>
public static class RuntimeConstants
{
    #region Architecture Identifiers

    /// <summary>
    /// 64-bit x86 architecture identifier.
    /// </summary>
    public const string ArchitectureX64 = "x64";

    #endregion

    #region Operating System Identifiers

    /// <summary>
    /// ARM 32-bit architecture identifier.
    /// </summary>
    public const string ArchitectureArm = "arm";

    /// <summary>
    /// ARM 64-bit architecture identifier.
    /// </summary>
    public const string ArchitectureArm64 = "arm64";

    /// <summary>
    /// Linux operating system identifier.
    /// </summary>
    public const string OsLinux = "linux";

    /// <summary>
    /// macOS operating system identifier.
    /// </summary>
    public const string OsOsx = "osx";

    /// <summary>
    /// Windows operating system identifier.
    /// </summary>
    public const string OsWindows = "windows";

    #endregion

    #region Error Messages

    /// <summary>
    /// Error message when unable to retrieve products from ProductCollection.
    /// </summary>
    public const string UnableToRetrieveProductsErrorMessage = "Unable to retrieve products";

    /// <summary>
    /// Error message when no products are returned by ProductCollection.
    /// </summary>
    public const string NoProductsReturnedErrorMessage = "No products returned by ProductCollection";

    /// <summary>
    /// Error message when unable to retrieve releases for a product.
    /// </summary>
    public const string UnableToRetrieveReleasesErrorMessage = "Unable to retrieve releases";

    /// <summary>
    /// Error message when no releases are found for a product.
    /// </summary>
    public const string NoReleasesForProductErrorMessage = "No releases for product";

    /// <summary>
    /// Error message format when ASP.NET Core runtime version is not found.
    /// Usage: String.Format(AspNetCoreRuntimeVersionNotFoundErrorMessage, version)
    /// </summary>
    public const string AspNetCoreRuntimeVersionNotFoundErrorMessage = "ASP.NET Core runtime version {0} not found.";

    /// <summary>
    /// Error message format when product version is not found.
    /// Usage: String.Format(ProductVersionNotFoundErrorMessage, version)
    /// </summary>
    public const string ProductVersionNotFoundErrorMessage = "Product Version {0} not found.";

    /// <summary>
    /// Error message format when configured product version is not found.
    /// Usage: String.Format(ConfiguredProductVersionNotFoundErrorMessage, version)
    /// </summary>
    public const string ConfiguredProductVersionNotFoundErrorMessage = "Configured product Version {0} not found.";

    /// <summary>
    /// Error message format when no release file is found for a runtime identifier.
    /// Usage: String.Format(NoReleaseFileForRuntimeIdentifierErrorMessage, rid)
    /// </summary>
    public const string NoReleaseFileForRuntimeIdentifierErrorMessage = "No release file found for runtime identifier {0}.";

    #endregion
}
