namespace Askyl.Dsm.WebHosting.Data.Contracts;

/// <summary>
/// Provides platform information including operating system, architecture, and configured channel version.
/// </summary>
public interface IPlatformInfoService
{
    /// <summary>
    /// Gets the configured .NET channel version from application settings.
    /// </summary>
    string ChannelVersion { get; }

    /// <summary>
    /// Gets the current runtime architecture (e.g., "x64", "arm", "arm64").
    /// </summary>
    string CurrentArchitecture { get; }

    /// <summary>
    /// Gets the current operating system identifier (e.g., "linux", "osx", "windows").
    /// </summary>
    string CurrentOS { get; }
}
