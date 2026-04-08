using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Data.Domain.Licensing;
using Askyl.Dsm.WebHosting.Ui.Client.Interfaces;

namespace Askyl.Dsm.WebHosting.Ui.Client.Services;

/// <summary>
/// Client-side implementation of ILicenseService that fetches license files dynamically from wwwroot.
/// Licenses are loaded on-demand via parallel HTTP requests for better performance.
/// </summary>
/// <param name="httpClientFactory">HttpClientFactory to create the named client.</param>
/// <param name="logger">Logger instance for error reporting.</param>
public class LicenseService(IHttpClientFactory httpClientFactory, ILogger<LicenseService> logger) : ILicenseService
{
    private IReadOnlyList<LicenseInfo>? _licenses;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<LicenseInfo>> GetLicensesAsync()
    {
        if (_licenses is not null)
        {
            return _licenses;
        }

        var tasks = LicenseConstants.LicenseFileNames.Select(async fileName => await LoadLicenseAsync(fileName));
        var results = await Task.WhenAll(tasks);

        _licenses = results.Where(result => result is not null).Cast<LicenseInfo>().ToList().AsReadOnly();
        return _licenses;
    }

    private async Task<LicenseInfo?> LoadLicenseAsync(string fileName)
    {
        try
        {
            var content = await FetchLicenseContentAsync(fileName);

            if (!String.IsNullOrEmpty(content))
            {
                return new LicenseInfo(Path.GetFileNameWithoutExtension(fileName), content);
            }
        }
        catch (Exception exception)
        {
            // Skip licenses that fail to load silently in production
            logger.LogWarning(exception, "Failed to load license file: {FileName}", fileName);
        }

        return null;
    }

    private async Task<string> FetchLicenseContentAsync(string fileName)
    {
        // Build URL to licenses/filename.txt (relative to wwwroot)
        var url = $"licenses/{fileName}";

        using var httpClient = httpClientFactory.CreateClient(ApplicationConstants.HttpClientName);
        httpClient.Timeout = TimeSpan.FromSeconds(ApplicationConstants.HttpClientTimeoutSeconds);

        return await httpClient.GetStringAsync(url);
    }
}
