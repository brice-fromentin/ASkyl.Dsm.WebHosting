using System.Diagnostics.CodeAnalysis;

namespace Askyl.Dsm.WebHosting.Data.Runtime;

/// <summary>
/// Represents the release type of an ASP.NET Core channel. Mirrors exactly the ReleaseType enum from Microsoft.Deployment.DotNet.Releases.
/// This avoids a direct dependency on the external library in the data model while allowing direct casting.
/// </summary>
public enum AspNetCoreReleaseType
{
    /// <summary>
    /// Indicates a release is supported for the Long Term Support (LTS) timeframe (3 years).
    /// </summary>
    LTS = 0,

    /// <summary>
    /// Indicates a release is supported for the Standard Term Support (STS) timeframe (18 months).
    /// </summary>
    STS = 1,

    /// <summary>
    /// The release type is unknown and could not be parsed.
    /// </summary>
    Unknown = 99
}

/// <summary>
/// Represents information about an ASP.NET Core release, including both channel and version details.
/// </summary>
public sealed class AspNetCoreReleaseInfo
{
    /// <summary>
    /// The specific version of the ASP.NET Core release (e.g., "8.0.1").
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// The product version/channel (e.g., "8.0").
    /// </summary>
    public required string ProductVersion { get; init; }

    /// <summary>
    /// The release date of this version.
    /// </summary>
    public DateTimeOffset? ReleaseDate { get; init; }

    /// <summary>
    /// Indicates whether this release is a security update.
    /// </summary>
    public bool IsSecurity { get; init; }

    /// <summary>
    /// Indicates whether this release is part of a Long Term Support (LTS) channel.
    /// </summary>
    public bool IsLts { get; init; }

    /// <summary>
    /// The release type of the channel (LTS, Current, etc.).
    /// </summary>
    public AspNetCoreReleaseType ReleaseType { get; init; }

    [SetsRequiredMembers]
    [SuppressMessage("Style", "IDE0290", Justification = "Explicit constructor call improves clarity and documents SetsRequiredMembers initialization.")]
    public AspNetCoreReleaseInfo(string version, string productVersion, DateTimeOffset? releaseDate = null, bool isSecurity = false, bool isLts = false, AspNetCoreReleaseType releaseType = AspNetCoreReleaseType.Unknown)
    {
        Version = version ?? throw new ArgumentNullException(nameof(version));
        ProductVersion = productVersion ?? throw new ArgumentNullException(nameof(productVersion));
        ReleaseDate = releaseDate;
        IsSecurity = isSecurity;
        IsLts = isLts;
        ReleaseType = releaseType;
    }

    /// <summary>
    /// Creates a channel-only instance for representing available channels.
    /// </summary>
    public static AspNetCoreReleaseInfo CreateChannel(string productVersion, bool isLts, AspNetCoreReleaseType releaseType)
        => new(String.Empty, productVersion, null, false, isLts, releaseType);
}
