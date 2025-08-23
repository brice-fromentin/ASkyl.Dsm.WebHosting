using Askyl.Dsm.WebHosting.Data.Runtime;
using Microsoft.Deployment.DotNet.Releases;

namespace Askyl.Dsm.WebHosting.Tools.Runtime;

public static class Downloader
{
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

                                        return new AspNetCoreReleaseInfo(version, product.ProductVersion, r.ReleaseDate, r.IsSecurityUpdate, isProductLts, ConvertReleaseType(product.ReleaseType));
                                    })
                           .OrderByDescending(x => x.ReleaseDate)
                           .ThenByDescending(x => x.Version, StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>
    /// Returns available ASP.NET Core channels (products) ordered by descending semantic Version.
    /// </summary>
    public static async Task<IReadOnlyList<AspNetCoreReleaseInfo>> GetAspNetCoreChannelsAsync()
    {
        var products = await ProductCollection.GetAsync().ConfigureAwait(false) ?? throw new InvalidOperationException("Unable to retrieve products");
        if (products.Count == 0) { throw new InvalidOperationException("No products returned by ProductCollection"); }

        return [.. products.Select(p => (Product: p, Parsed: Version.TryParse(p.ProductVersion, out var v) ? v : new Version(0, 0)))
                           .OrderByDescending(t => t.Parsed)
                           .Select(t => AspNetCoreReleaseInfo.CreateChannel(t.Product.ProductVersion, t.Product.ReleaseType == ReleaseType.LTS, ConvertReleaseType(t.Product.ReleaseType)))];
    }

    private static async Task<Product> GetProductAsync(string? desiredChannelVersion, bool strictWhenConfigured)
    {
        var products = await ProductCollection.GetAsync().ConfigureAwait(false) ?? throw new InvalidOperationException("Unable to retrieve products");
        if (products.Count == 0)
        {
            throw new InvalidOperationException("No products returned by ProductCollection");
        }

        // 1. Explicit requested channel
        if (!String.IsNullOrWhiteSpace(desiredChannelVersion))
        {
            if (TryGetProductByVersion(products, desiredChannelVersion, strictWhenConfigured, $"Product Version {desiredChannelVersion} not found.", out var result))
            {
                return result;
            }
        }

        // 2. Configured channel (if not already tried and not empty)
        var configured = Configuration.ChannelVersion;

        if (!String.IsNullOrWhiteSpace(configured) && !String.Equals(configured, desiredChannelVersion, StringComparison.OrdinalIgnoreCase))
        {
            if (TryGetProductByVersion(products, configured, strictWhenConfigured, $"Configured product Version {configured} not found.", out var result))
            {
                return result;
            }
        }

        // 3. Fallback latest
        return products.Select(p => (Product: p, Parsed: Version.TryParse(p.ProductVersion, out var v) ? v : new Version(0, 0)))
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

    private static async Task<ProductRelease> GetReleaseByVersionAsync(Product product, string version)
    {
        var releases = await product.GetReleasesAsync().ConfigureAwait(false);

        if (releases == null || releases.Count == 0)
        {
            throw new InvalidOperationException("Unable to retrieve releases");
        }

        var release = releases.FirstOrDefault(r => r.AspNetCoreRuntime != null &&
                                                   String.Equals(r.AspNetCoreRuntime.Version.ToString(), version, StringComparison.OrdinalIgnoreCase)) ?? throw new InvalidOperationException($"ASP.NET Core runtime version {version} not found.");

        return release;
    }

    private static async Task<string> DownloadReleaseToAsync(ProductRelease release, string destinationPath, bool skipDownloadIfExists)
    {
        var rid = $"{Configuration.CurrentOS}-{Configuration.CurrentArchitecture}";
        var file = release.AspNetCoreRuntime.Files.FirstOrDefault(f => String.Equals(f.Rid, rid, StringComparison.OrdinalIgnoreCase)) ?? throw new FileNotFoundException($"No release file found for runtime identifier {rid}.");

        var fullDestinationPath = FileSystem.GetFullName(destinationPath, file.FileName);

        if (skipDownloadIfExists && File.Exists(fullDestinationPath))
        {
            return fullDestinationPath;
        }

        await file.DownloadAsync(fullDestinationPath).ConfigureAwait(false);

        return fullDestinationPath;
    }

    private static AspNetCoreReleaseType ConvertReleaseType(ReleaseType releaseType)
        => (AspNetCoreReleaseType)releaseType;

    /// <summary>
    /// Helper method to find a product by version and optionally throw if not found.
    /// Uses the Try pattern to eliminate the need for null checking after the call.
    /// </summary>
    /// <param name="products">The collection of products to search</param>
    /// <param name="version">The version to search for</param>
    /// <param name="strictMode">Whether to throw an exception if not found</param>
    /// <param name="errorMessage">The error message to use in the exception</param>
    /// <param name="result">The found product if successful</param>
    /// <returns>True if the product was found, false otherwise (unless strict mode throws an exception)</returns>
    private static bool TryGetProductByVersion(IReadOnlyList<Product> products, string version, bool strictMode, string errorMessage, out Product result)
    {
        var product = products.FirstOrDefault(p => String.Equals(p.ProductVersion, version, StringComparison.OrdinalIgnoreCase));

        if (product != null)
        {
            result = product;
            return true;
        }

        if (strictMode)
        {
            throw new InvalidOperationException(errorMessage);
        }

        result = null!;
        return false;
    }
}
