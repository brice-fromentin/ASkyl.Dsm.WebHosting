using System.Net;
using System.Text.Json;
using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Constants.DSM.API;
using Askyl.Dsm.WebHosting.Constants.Network;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core;
using Askyl.Dsm.WebHosting.Data.DsmApi.Parameters;
using Askyl.Dsm.WebHosting.Data.DsmApi.Responses;
using Askyl.Dsm.WebHosting.Logging;
using Askyl.Dsm.WebHosting.Tools.Diagnostics;
using Askyl.Dsm.WebHosting.Tools.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Tools.Network;

public class DsmApiClient(IHttpClientFactory httpClientFactory, DsmSettingsService settingsService, ILogger<ILogDsmApiClient> logger)
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(ApplicationConstants.HttpClientName);

    public ApiInformationCollection ApiInformations { get; } = new();

    #region HTTP Request calls

    public async Task<R?> ExecuteAsync<R>(string? sid, IApiParameters parameters)
        where R : IApiResponse
    {
        var url = parameters.BuildUrl(settingsService.Server, settingsService.Port);
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

        using var timer = new OperationTimer(elapsed => logger.ApiRequest("POST", url, (int)response!.StatusCode, elapsed));

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
