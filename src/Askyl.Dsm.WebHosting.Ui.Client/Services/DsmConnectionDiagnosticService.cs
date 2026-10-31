using System.Net;
using System.Text.Json;
using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Constants.JSON;
using Askyl.Dsm.WebHosting.Constants.WebApi;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Results;
using Askyl.Dsm.WebHosting.Globalization;
using Askyl.Dsm.WebHosting.Tools.Extensions;

namespace Askyl.Dsm.WebHosting.Ui.Client.Services;

/// <summary>
/// Proxy implementation of IDsmConnectionDiagnosticService that calls the REST API.
/// </summary>
/// <param name="httpClientFactory">HttpClientFactory to create the named client.</param>
/// <param name="localizer">Localizer for user-facing strings.</param>
public class DsmConnectionDiagnosticService(IHttpClientFactory httpClientFactory, ILocalizer localizer) : IDsmConnectionDiagnosticService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(ApplicationConstants.HttpClientName);

    /// <inheritdoc/>
    public async Task<DsmConnectionReportResult> GetReportAsync(CancellationToken cancellationToken = default)
        => await _httpClient.GetJsonOrDefaultAsync(DsmDiagnosticRoutes.ReportFullRoute, () => DsmConnectionReportResult.CreateFailure(localizer[LK.Diagnostic.Unavailable]), cancellationToken);

    /// <inheritdoc/>
    public async Task<DsmConnectionReportResult> RecheckAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsync(DsmDiagnosticRoutes.RecheckFullRoute, null, cancellationToken);

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            return DsmConnectionReportResult.CreateFailure(localizer[LK.Error.RateLimitExceeded]);
        }

        if (!response.IsSuccessStatusCode)
        {
            return DsmConnectionReportResult.CreateFailure(localizer[LK.Diagnostic.Unavailable]);
        }

        // Deserialized as the client AuthenticationService does: the SDK's implicit System.Net.Http.Json
        // makes ReadFromJsonAsync ambiguous with the Tools extension
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        return JsonSerializer.Deserialize<DsmConnectionReportResult>(json, JsonOptionsCache.Options)
            ?? DsmConnectionReportResult.CreateFailure(localizer[LK.Diagnostic.Unavailable]);
    }
}
