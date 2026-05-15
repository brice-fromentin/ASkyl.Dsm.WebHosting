using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Constants.DSM.API;
using Askyl.Dsm.WebHosting.Constants.DSM.System;
using Askyl.Dsm.WebHosting.Constants.Network;
using Askyl.Dsm.WebHosting.Data.Domain.Authentication;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core;
using Askyl.Dsm.WebHosting.Data.DsmApi.Parameters;
using Askyl.Dsm.WebHosting.Data.DsmApi.Parameters.Core;
using Askyl.Dsm.WebHosting.Data.DsmApi.Parameters.CoreInformations;
using Askyl.Dsm.WebHosting.Data.DsmApi.Responses;
using Askyl.Dsm.WebHosting.Logging;
using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Tools.Network;

public class DsmApiClient(IHttpClientFactory httpClientFactory, ILogger<ILogDsmApiClient> logger)
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(ApplicationConstants.HttpClientName);

    private string _server = String.Empty;
    private int _port = SystemDefaults.DefaultHttpsPort;
    private string _sid = String.Empty;

    public ApiInformationCollection ApiInformations { get; } = new();

    public string Sid => _sid;

    public bool IsConnected => !String.IsNullOrEmpty(_sid);

    /// <summary>
    /// Sets the session ID directly (for restoring from persisted state).
    /// </summary>
    public void SetSid(string sid) => _sid = sid;

    /// <summary>
    /// Disconnects and clears the session.
    /// </summary>
    public async Task DisconnectAsync()
    {
        logger.Disconnecting();

        _sid = String.Empty;
        _httpClient.DefaultRequestHeaders.Remove(NetworkConstants.CookieHeader);

        logger.Disconnected();
    }

    public async Task<bool> ConnectAsync(LoginCredentials model)
    {
        logger.Connecting(_server, _port);

        if (!await ReadSettingsAsync())
        {
            return false;
        }

        if (!await HandShakeAsync())
        {
            return false;
        }

        if (!await AuthenticateAsync(model))
        {
            return false;
        }

        logger.Connected();

        return true;
    }

    private async Task<bool> ReadSettingsAsync()
    {
        if (!File.Exists(SystemDefaults.ConfigurationFileName))
        {
            logger.ConfigurationFileNotFound(SystemDefaults.ConfigurationFileName);
            return false;
        }

        var lines = await File.ReadAllLinesAsync(SystemDefaults.ConfigurationFileName);
        var settings = lines.Where(x => x.Contains('='))
                           .ToDictionary(key => key.Split('=')[0], value => value.Split('=')[1].Replace("\"", String.Empty));

        logger.ConfigurationLoaded(settings.Count);
        _server = settings[SystemDefaults.KeyExternalHostIp];

        if (!Int32.TryParse(settings[SystemDefaults.KeyExternalHttpsPort], out _port))
        {
            _port = SystemDefaults.DefaultHttpsPort;
        }

        return true;
    }

    private async Task<bool> HandShakeAsync()
    {
        logger.HandshakeStarting();

        var parameters = new InformationsQueryParameters(ApiInformations);

        var result = await ExecuteAsync<ApiInformationResponse>(parameters);

        if (result is null || !result.Success || result.Data is null || result.Data.Count == 0)
        {
            logger.HandshakeFailure();
            return false;
        }

        ApiInformations.Replace(result.Data);

        logger.HandshakeSuccess();

        return true;
    }

    private async Task<bool> AuthenticateAsync(LoginCredentials model)
    {
        var login = new AuthenticateLogin
        {
            Account = model.Login,
            Password = model.Password,
            OtpCode = model.OtpCode
        };

        var parameters = new AuthenticationLoginParameters(ApiInformations, login);

        var response = await ExecuteAsync<SynoLoginResponse>(parameters);

        if (response?.Success != true || response.Data is null)
        {
            var errorMessage = response?.Error?.Errors?.Reason ?? "Authentication failed";
            logger.AuthenticationFailed(errorMessage);
            return false;
        }

        _sid = response.Data.Sid;
        _httpClient.DefaultRequestHeaders.Add(NetworkConstants.CookieHeader, NetworkConstants.SsidCookiePrefix + response.Data.Sid);

        return true;
    }

    #region HTTP Request calls

    public async Task<R?> ExecuteAsync<R>(IApiParameters parameters)
        where R : IApiResponse
    {
        var url = parameters.BuildUrl(_server, _port);
        var result = parameters.SerializationFormat switch
        {
            SerializationFormats.Form
                => await ExecuteFormAsync<R>(url, parameters),
            SerializationFormats.Json
                => await ExecuteJsonAsync<R>(url, parameters),
            _
                => throw new NotSupportedException($"SerializationFormat : {parameters.SerializationFormat} not supported.")
        };

        LogApiErrorIfFailed(result, logger);

        return result;
    }

    public async Task<ApiResponseBase<EmptyResponse>?> ExecuteSimpleAsync(IApiParameters parameters)
        => await ExecuteAsync<ApiResponseBase<EmptyResponse>>(parameters);

    private static void LogApiErrorIfFailed<R>(R? result, ILogger<ILogDsmApiClient> logger)
        where R : IApiResponse
    {
        if (result is { Success: false, Error: { } error })
        {
            logger.ApiError(error.Errors?.Reason ?? "Unknown error", error.Code);
        }
    }

    private async Task<R?> ExecuteFormAsync<R>(string url, IApiParameters parameters)
        where R : IApiResponse
        => await ExecutePostAsync<R>(url, parameters.ToForm());

    private async Task<R?> ExecuteJsonAsync<R>(string url, IApiParameters parameters)
        where R : IApiResponse
        => await ExecutePostAsync<R>(url, parameters.ToJson());

    private async Task<R?> ExecutePostAsync<R>(string url, StringContent content)
        where R : IApiResponse
    {
        var stopwatch = Stopwatch.StartNew();
        var response = await _httpClient.PostAsync(url, content);
        stopwatch.Stop();

        logger.ApiRequest("POST", url, (int)response.StatusCode, stopwatch.ElapsedMilliseconds);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            return default;
        }

        var text = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<R>(text);
    }

    #endregion
}
