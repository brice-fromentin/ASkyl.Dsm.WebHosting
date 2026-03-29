using Askyl.Dsm.WebHosting.Data.Domain.Runtime;

namespace Askyl.Dsm.WebHosting.Data.Contracts;

/// <summary>
/// Service for downloading and managing .NET runtime releases.
/// </summary>
public interface IDownloaderService
{
    /// <summary>
    /// Downloads the latest ASP.NET Core runtime release to the downloads directory.
    /// </summary>
    /// <param name="skipDownloadIfExists">If true, skips download if file already exists.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The path to the downloaded file.</returns>
    Task<string> DownloadToAsync(bool skipDownloadIfExists = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a specific version of ASP.NET Core runtime.
    /// </summary>
    /// <param name="version">The version to download (e.g., "8.0.5").</param>
    /// <param name="channelVersion">Optional channel version filter.</param>
    /// <param name="skipDownloadIfExists">If true, skips download if file already exists.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The path to the downloaded file.</returns>
    Task<string> DownloadVersionToAsync(string version, string? channelVersion = null, bool skipDownloadIfExists = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available ASP.NET Core runtime releases for a specific product.
    /// </summary>
    /// <param name="channel">Optional channel filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of available releases.</returns>
    Task<IReadOnlyList<AspNetCoreReleaseInfo>> GetAspNetCoreReleasesAsync(string? channel = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available ASP.NET Core runtime channels.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of available channels.</returns>
    Task<IReadOnlyList<AspNetCoreReleaseInfo>> GetAspNetCoreChannelsAsync(CancellationToken cancellationToken = default);
}
