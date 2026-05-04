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
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(ApplicationConstants.HttpClientName);

    private Task<IReadOnlyList<LicenseInfo>>? _loadLicensesTask;

    private IReadOnlyList<LicenseInfo>? _licenses;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<LicenseInfo>> GetLicensesAsync()
        => _licenses ?? await LazyInitializeLicensesAsync();

    private async Task<IReadOnlyList<LicenseInfo>> LazyInitializeLicensesAsync()
    {
        var loadTask = _loadLicensesTask ??= LoadLicensesInternalAsync();
        _licenses = await loadTask;
        return _licenses;
    }

    private async Task<IReadOnlyList<LicenseInfo>> LoadLicensesInternalAsync()
    {
        var tasks = LicenseConstants.LicenseFileNames.Select(async fileName => await LoadLicenseAsync(fileName));
        var results = await Task.WhenAll(tasks);

        return results.Where(result => result is not null).Cast<LicenseInfo>().ToList().AsReadOnly();
    }

    private async Task<LicenseInfo?> LoadLicenseAsync(string fileName)
    {
        try
        {
            var content = await FetchLicenseContentAsync(fileName);

            if (!String.IsNullOrEmpty(content))
            {
                return new(Path.GetFileNameWithoutExtension(fileName), content);
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
        => await _httpClient.GetStringAsync($"licenses/{fileName}");
}
