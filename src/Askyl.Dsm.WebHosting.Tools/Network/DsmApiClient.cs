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
using Askyl.Dsm.WebHosting.Tools.Infrastructure;
using Askyl.Dsm.WebHosting.Tools.Threading;
using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Tools.Network;

public class DsmApiClient(IHttpClientFactory httpClientFactory, IDsmSettingsService settingsService, ILogger<ILogDsmApiClient> logger)
    : ISemaphoreOwner
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(ApplicationConstants.HttpClientName);

    public ApiInformationCollection ApiInformations { get; } = new();

    public SemaphoreSlim Semaphore { get; } = new(1, 1);

    #region HTTP Request calls

    public async Task<R?> ExecuteAsync<R>(string? sid, IApiParameters parameters)
        where R : IApiResponse
    {
        await EnsureInitializedAsync();

        var path = (parameters.Name == ApiConstants.Info)
            ? ApiConstants.Handshake
            : ApiInformations.Get(parameters.Name)?.Path
                ?? throw new InvalidOperationException($"Unknown API: {parameters.Name}");

        var url = parameters.BuildUrl(settingsService.Server, settingsService.Port, path);

        var result = parameters.SerializationFormat switch
        {
            SerializationFormats.Form
                => await ExecuteFormAsync<R>(sid, url, parameters),
            SerializationFormats.Json
                => await ExecuteJsonAsync<R>(sid, url, parameters),
            _
                => throw new NotSupportedException($"SerializationFormat : {parameters.SerializationFormat} not supported.")
        };

        LogApiErrorIfFailed(result, logger);

        return result;
    }

    public async Task<ApiResponseBase<object>?> ExecuteSimpleAsync(string? sid, IApiParameters parameters)
        => await ExecuteAsync<ApiResponseBase<object>>(sid, parameters);

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
        var url = parameters.BuildUrl(settingsService.Server, settingsService.Port, ApiConstants.Handshake);

        var result = await ExecuteFormAsync<ApiInformationResponse>(null, url, parameters);

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

    private static void LogApiErrorIfFailed<R>(R? result, ILogger<ILogDsmApiClient> logger)
        where R : IApiResponse
    {
        if (result is { Success: false, Error: { } error })
        {
            logger.ApiError(error.Errors?.Reason ?? "Unknown error", error.Code);
        }
    }

    private async Task<R?> ExecuteFormAsync<R>(string? sid, string url, IApiParameters parameters)
        where R : IApiResponse
        => await ExecutePostAsync<R>(sid, url, parameters.ToForm());

    private async Task<R?> ExecuteJsonAsync<R>(string? sid, string url, IApiParameters parameters)
        where R : IApiResponse
        => await ExecutePostAsync<R>(sid, url, parameters.ToJson());

    private async Task<R?> ExecutePostAsync<R>(string? sid, string url, StringContent content)
        where R : IApiResponse
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };

        if (sid is { Length: > 0 })
        {
            request.Headers.Add(NetworkConstants.CookieHeader, NetworkConstants.SsidCookiePrefix + sid);
        }

        HttpResponseMessage? response = null;

        using var timer = new OperationTimer(elapsed => logger.ApiRequest(HttpMethod.Post.ToString(), url, (int)response!.StatusCode, elapsed));

        response = await _httpClient.SendAsync(request);

        var text = await response.Content.ReadAsStringAsync();

        if (response.StatusCode != HttpStatusCode.OK)
        {
            return default;
        }

        return JsonSerializer.Deserialize<R>(text);
    }

    #endregion
}
