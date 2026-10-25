using System.Net;
using System.Text.Json;
using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Constants.DSM.API;
using Askyl.Dsm.WebHosting.Constants.Network;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core;
using Askyl.Dsm.WebHosting.Data.DsmApi.Parameters;
using Askyl.Dsm.WebHosting.Data.DsmApi.Parameters.Info;
using Askyl.Dsm.WebHosting.Data.DsmApi.Responses;
using Askyl.Dsm.WebHosting.Logging;
using Askyl.Dsm.WebHosting.Tools.Diagnostics;
using Askyl.Dsm.WebHosting.Tools.Threading;
using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Tools.Network;

public class DsmApiClient(IHttpClientFactory httpClientFactory, IDsmSettingsService settingsService, ILogger<ILogDsmApiClient> logger)
    : ISemaphoreOwner, IAsyncDisposable
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(ApplicationConstants.HttpClientName);

    public ApiInformationCollection ApiInformations { get; } = new();

    public SemaphoreSlim Semaphore { get; } = new(1, 1);

    public ValueTask DisposeAsync()
    {
        Semaphore.Dispose();
        return default;
    }

    #region HTTP Request calls

    public async Task<R?> ExecuteAsync<R>(string? sid, IApiParameters parameters, CancellationToken cancellationToken = default)
        where R : IApiResponse
    {
        await EnsureInitializedAsync();

        var path = (parameters.Name == ApiConstants.Info)
            ? ApiConstants.Handshake
            : ApiInformations.Get(parameters.Name)?.Path
                ?? throw new InvalidOperationException($"Unknown API: {parameters.Name}");

        var url = parameters.BuildUrl(RequireServer(), settingsService.Port, path);

        var result = parameters.SerializationFormat switch
        {
            SerializationFormats.Form
                => await ExecuteFormAsync<R>(sid, url, parameters, cancellationToken),
            SerializationFormats.Json
                => await ExecuteJsonAsync<R>(sid, url, parameters, cancellationToken),
            _
                => throw new NotSupportedException($"SerializationFormat : {parameters.SerializationFormat} not supported.")
        };

        LogApiErrorIfFailed(result, logger);

        return result;
    }

    public async Task<ApiResponseBase<object>?> ExecuteSimpleAsync(string? sid, IApiParameters parameters, CancellationToken cancellationToken = default)
        => await ExecuteAsync<ApiResponseBase<object>>(sid, parameters, cancellationToken);

    /// <summary>
    /// Sends the SYNO.API.Info request for the connection diagnostic, leaving the handshake cache alone.
    /// </summary>
    /// <returns>Whether DSM answered with its API list, and the HTTP status it answered with.</returns>
    public async Task<(bool Answered, int StatusCode)> ProbeAsync(CancellationToken cancellationToken)
    {
        var parameters = new InformationsQueryParameters();
        var url = parameters.BuildUrl(RequireServer(), settingsService.Port, ApiConstants.Handshake);

        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = parameters.ToForm() };
        using var response = await _httpClient.SendAsync(request, cancellationToken);

        var statusCode = (int)response.StatusCode;

        if (response.StatusCode != HttpStatusCode.OK)
        {
            return (false, statusCode);
        }

        try
        {
            var result = JsonSerializer.Deserialize<ApiInformationResponse>(await response.Content.ReadAsStringAsync(cancellationToken));

            return (result is { Success: true, Data.Count: > 0 }, statusCode);
        }
        catch (JsonException)
        {
            // Something other than DSM answered on that port
            return (false, statusCode);
        }
    }

    private async Task EnsureInitializedAsync()
    {
        if (ApiInformations.Get(ApiConstants.Auth) is not null)
        {
            return;
        }

        using var @lock = await SemaphoreLock.AcquireAsync(this);

        if (ApiInformations.Get(ApiConstants.Auth) is not null)
        {
            return;
        }

        if (!await HandShakeAsync())
        {
            throw new InvalidOperationException("DSM API handshake failed");
        }
    }

    private async Task<bool> HandShakeAsync()
    {
        var parameters = new InformationsQueryParameters();
        var url = parameters.BuildUrl(RequireServer(), settingsService.Port, ApiConstants.Handshake);

        var result = await ExecuteFormAsync<ApiInformationResponse>(null, url, parameters, CancellationToken.None);

        if (result?.Success != true || result.Data is null || result.Data.Count == 0)
        {
            logger.HandshakeFailure();
            return false;
        }

        ApiInformations.Replace(result.Data);
        logger.HandshakeSuccess();

        return true;
    }

    #endregion

    #region HTTP Helpers

    // Fails before building "https://:5001/…", so a missing hostname surfaces as itself
    private string RequireServer()
        => settingsService.Server ?? throw new InvalidOperationException("DSM hostname is not configured");

    private static void LogApiErrorIfFailed<R>(R? result, ILogger<ILogDsmApiClient> logger)
        where R : IApiResponse
    {
        if (result is { Success: false, Error: { } error })
        {
            logger.ApiError(error.Errors?.Reason ?? "Unknown error", error.Code);
        }
    }

    private async Task<R?> ExecuteFormAsync<R>(string? sid, string url, IApiParameters parameters, CancellationToken cancellationToken)
        where R : IApiResponse
        => await ExecutePostAsync<R>(sid, url, parameters.ToForm(), cancellationToken);

    private async Task<R?> ExecuteJsonAsync<R>(string? sid, string url, IApiParameters parameters, CancellationToken cancellationToken)
        where R : IApiResponse
        => await ExecutePostAsync<R>(sid, url, parameters.ToJson(), cancellationToken);

    private async Task<R?> ExecutePostAsync<R>(string? sid, string url, StringContent content, CancellationToken cancellationToken)
        where R : IApiResponse
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };

        if (sid is { Length: > 0 })
        {
            request.Headers.Add(NetworkConstants.CookieHeader, NetworkConstants.SsidCookiePrefix + sid);
        }

        // The timer must be started before the request: SendAsync buffers the whole body by default, so
        // it is the measurement. It reports on disposal, which is why it reads a captured status rather
        // than the response — the response is disposed first, and dereferencing it here would replace a
        // failed send's own exception with a NullReferenceException.
        var statusCode = 0;

        using var timer = new OperationTimer(elapsed => logger.ApiRequest(request.Method.Method, url, statusCode, elapsed));

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        statusCode = (int)response.StatusCode;

        var text = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            return default;
        }

        return JsonSerializer.Deserialize<R>(text);
    }

    #endregion
}
