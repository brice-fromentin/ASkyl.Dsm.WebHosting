using Microsoft.Deployment.DotNet.Releases;

namespace Askyl.Dsm.WebHosting.Tools.Runtime;

public static class Downloader
{
    // LTS detection now relies on Microsoft.Deployment.DotNet.Releases.ReleaseType instead of hard-coded channel list.

    public sealed class AspNetCoreChannelInfo
    {
        public required string ProductVersion { get; init; }
        public bool IsLts { get; init; }
        public ReleaseType ReleaseType { get; init; }
    }

    public static async Task<string> DownloadToAsync(bool skipDownloadIfExists = false)
    {
        var product = await GetProductAsync(Configuration.ChannelVersion, true).ConfigureAwait(false);
        var release = await GetLatestReleaseAsync(product).ConfigureAwait(false);
        var fileName = await DownloadReleaseToAsync(release, FileSystem.Downloads, skipDownloadIfExists).ConfigureAwait(false);

        return fileName;
    }

    /// <summary>
    /// Downloads a specific version of ASP.NET Core runtime.
    /// </summary>
    public static async Task<string> DownloadVersionToAsync(string version, string? channelVersion = null, bool skipDownloadIfExists = false)
    {
        var product = await GetProductAsync(channelVersion, false).ConfigureAwait(false);
        var release = await GetReleaseByVersionAsync(product, version).ConfigureAwait(false);
        var fileName = await DownloadReleaseToAsync(release, FileSystem.Downloads, skipDownloadIfExists).ConfigureAwait(false);

        return fileName;
    }

    /// <summary>
    /// Returns ASP.NET Core runtime releases for a channel (explicit, configured, or latest fallback).
    /// </summary>
    public static async Task<IReadOnlyList<AspNetCoreReleaseInfo>> GetAspNetCoreReleasesAsync(string? channelVersion = null)
    {
        var product = await GetProductAsync(channelVersion, false).ConfigureAwait(false);
        var releases = await product.GetReleasesAsync().ConfigureAwait(false);
        var isProductLts = product.ReleaseType == ReleaseType.LTS;

        return [.. releases.Where(r => r.AspNetCoreRuntime != null)
                           .Select(r =>
            {
                var version = r.AspNetCoreRuntime!.Version.ToString();

                return new AspNetCoreReleaseInfo
                {
                    Version = version,
                    ProductVersion = product.ProductVersion,
                    ReleaseDate = r.ReleaseDate,
                    IsLatest = String.Equals(version, product.LatestReleaseVersion?.ToString(), StringComparison.OrdinalIgnoreCase),
                    IsSecurity = r.IsSecurityUpdate,
                    IsLts = isProductLts
                };
            })
            .OrderByDescending(x => x.ReleaseDate)
            .ThenByDescending(x => x.Version, StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>
    /// Returns available ASP.NET Core channels (products) ordered by descending semantic Version.
    /// </summary>
    public static async Task<IReadOnlyList<AspNetCoreChannelInfo>> GetAspNetCoreChannelsAsync()
    {
        var products = await ProductCollection.GetAsync().ConfigureAwait(false) ?? throw new InvalidOperationException("Unable to retrieve products");
        if (products.Count == 0) { throw new InvalidOperationException("No products returned by ProductCollection"); }

        return [.. products
            .Select(p => (Product: p, Parsed: Version.TryParse(p.ProductVersion, out var v) ? v : new Version(0, 0)))
            .OrderByDescending(t => t.Parsed)
            .Select(t => new AspNetCoreChannelInfo
            {
                ProductVersion = t.Product.ProductVersion,
                IsLts = t.Product.ReleaseType == ReleaseType.LTS,
                ReleaseType = t.Product.ReleaseType
            })];
    }

    public sealed class AspNetCoreReleaseInfo
    {
        public required string Version { get; init; }
        public required string ProductVersion { get; init; }
        public DateTimeOffset? ReleaseDate { get; init; }
        public bool IsLatest { get; init; }
        public bool IsSecurity { get; init; }
        public bool IsLts { get; init; }
    }

    private static string ExtractChannel(string version)
    {
        var parts = version.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2) { return String.Join('.', parts[0], parts[1]); }
        return version;
    }

    private static async Task<Product> GetProductAsync(string? desiredChannelVersion, bool strictWhenConfigured)
    {
        var products = await ProductCollection.GetAsync().ConfigureAwait(false) ?? throw new InvalidOperationException("Unable to retrieve products");
        if (products.Count == 0) { throw new InvalidOperationException("No products returned by ProductCollection"); }

        // 1. Explicit requested channel
        if (!String.IsNullOrWhiteSpace(desiredChannelVersion))
        {
            var explicitMatch = products.FirstOrDefault(p => String.Equals(p.ProductVersion, desiredChannelVersion, StringComparison.OrdinalIgnoreCase));
            if (explicitMatch != null) { return explicitMatch; }
            if (strictWhenConfigured) { throw new InvalidOperationException($"Product Version {desiredChannelVersion} not found."); }
        }

        // 2. Configured channel (if not already tried and not empty)
        var configured = Configuration.ChannelVersion;
        if (!String.IsNullOrWhiteSpace(configured) && !String.Equals(configured, desiredChannelVersion, StringComparison.OrdinalIgnoreCase))
        {
            var configuredMatch = products.FirstOrDefault(p => String.Equals(p.ProductVersion, configured, StringComparison.OrdinalIgnoreCase));
            if (configuredMatch != null) { return configuredMatch; }
            if (strictWhenConfigured) { throw new InvalidOperationException($"Configured product Version {configured} not found."); }
        }

        // 3. Fallback latest
        return products
            .Select(p => (Product: p, Parsed: Version.TryParse(p.ProductVersion, out var v) ? v : new Version(0, 0)))
            .OrderByDescending(t => t.Parsed)
            .First().Product;
    }

    private static async Task<ProductRelease> GetLatestReleaseAsync(Product product)
    {
        var releases = await product.GetReleasesAsync().ConfigureAwait(false) ?? throw new InvalidOperationException("Unable to retrieve releases");
        if (releases.Count == 0) { throw new InvalidOperationException("No releases for product"); }

        var latest = releases.FirstOrDefault(r => r.Version == product.LatestReleaseVersion)
                     ?? releases.OrderByDescending(r => r.ReleaseDate).First();
        return latest;
    }

    private static async Task<ProductRelease> GetReleaseAsync(Product product)
    {
        var releases = await product.GetReleasesAsync().ConfigureAwait(false);

        if (releases == null || releases.Count == 0)
        {
            throw new InvalidOperationException("Unable to retrieve releases");
        }

        var release = releases.FirstOrDefault(x => x.Version == product.LatestReleaseVersion)
                        ?? throw new InvalidOperationException($"Release Version {product.LatestReleaseVersion} not found.");

        return release;
    }

    private static async Task<ProductRelease> GetReleaseByVersionAsync(Product product, string version)
    {
        var releases = await product.GetReleasesAsync().ConfigureAwait(false);

        if (releases == null || releases.Count == 0)
        {
            throw new InvalidOperationException("Unable to retrieve releases");
        }

        var release = releases.FirstOrDefault(r => r.AspNetCoreRuntime != null && 
                                                    String.Equals(r.AspNetCoreRuntime.Version.ToString(), version, StringComparison.OrdinalIgnoreCase))
                        ?? throw new InvalidOperationException($"ASP.NET Core runtime version {version} not found.");

        return release;
    }

    private static async Task<string> DownloadReleaseToAsync(ProductRelease release, string destinationPath, bool skipDownloadIfExists)
    {
        var rid = $"{Configuration.CurrentOS}-{Configuration.CurrentArchitecture}";
        var file = release.AspNetCoreRuntime.Files.FirstOrDefault(f => String.Equals(f.Rid, rid, StringComparison.OrdinalIgnoreCase))
                        ?? throw new FileNotFoundException($"No release file found for runtime identifier {rid}.");

        var fullDestinationPath = FileSystem.GetFullName(destinationPath, file.FileName);

        if (skipDownloadIfExists && File.Exists(fullDestinationPath))
        {
            return fullDestinationPath;
        }

        await file.DownloadAsync(fullDestinationPath).ConfigureAwait(false);

        return fullDestinationPath;
    }
}
