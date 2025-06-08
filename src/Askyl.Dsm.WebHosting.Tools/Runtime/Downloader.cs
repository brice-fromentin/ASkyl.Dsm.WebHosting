using Microsoft.Deployment.DotNet.Releases;

namespace Askyl.Dsm.WebHosting.Tools.Runtime;

public static class Downloader
{
    public static async Task<string> DownloadToAsync(bool skipDownloadIfExists = false)
    {
        var product = await GetProductsAsync().ConfigureAwait(false);
        var release = await GetReleaseAsync(product).ConfigureAwait(false);
        var fileName = await DownloadReleaseToAsync(release, FileSystem.Downloads, skipDownloadIfExists).ConfigureAwait(false);

        return fileName;
    }

    private static async Task<Product> GetProductsAsync()
    {
        var products = await ProductCollection.GetAsync().ConfigureAwait(false);

        if (products == null || products.Count == 0)
        {
            throw new InvalidOperationException("Unable to retrieve products");
        }

        var product = products.FirstOrDefault(x => x.ProductVersion == Configuration.ChannelVersion)
                        ?? throw new InvalidOperationException($"Product Version {Configuration.ChannelVersion} not found.");

        return product;
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

    private static async Task<string> DownloadReleaseToAsync(ProductRelease release, string destinationPath, bool skipDownloadIfExists)
    {
        var rid = $"{Configuration.CurrentOS}-{Configuration.CurrentArchitecture}";
        var file = release.AspNetCoreRuntime.Files.FirstOrDefault(f => string.Equals(f.Rid, rid, StringComparison.OrdinalIgnoreCase))
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
