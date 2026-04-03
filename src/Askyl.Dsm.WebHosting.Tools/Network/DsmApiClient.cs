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
using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Tools.Network;

public class DsmApiClient(IHttpClientFactory HttpClientFactory, ILogger<DsmApiClient> log)
{
    private readonly HttpClient _httpClient = HttpClientFactory.CreateClient(ApplicationConstants.HttpClientName);
    private readonly ILogger<DsmApiClient> _log = log;

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
        _sid = String.Empty;
        _httpClient.DefaultRequestHeaders.Remove(NetworkConstants.CookieHeader);
    }

    public async Task<bool> ConnectAsync(LoginCredentials model)
    {
        if (!ReadSettings())
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

        return true;
    }

    private bool ReadSettings()
    {
        if (!File.Exists(SystemDefaults.ConfigurationFileName))
        {
            _log.LogCritical($"Configuration file \"{SystemDefaults.ConfigurationFileName}\" does not exists.");
            return false;
        }

        var settings = File.ReadAllLines(SystemDefaults.ConfigurationFileName)
                           .Where(x => x.Contains('='))
                           .ToDictionary(key => key.Split('=')[0], value => value.Split('=')[1].Replace("\"", String.Empty));

        _log.LogDebug("Configuration file loaded with {Count} parameters.", settings.Count);
        _server = settings[SystemDefaults.KeyExternalHostIp];

        if (!Int32.TryParse(settings[SystemDefaults.KeyExternalHttpsPort], out _port))
        {
            _port = SystemDefaults.DefaultHttpsPort;
        }

        return true;
    }

    private async Task<bool> HandShakeAsync()
    {
        var parameters = new InformationsQueryParameters(ApiInformations);

        var result = await ExecuteAsync<ApiInformationResponse>(parameters);

        if (result is null || !result.Success || result.Data is null || result.Data.Count == 0)
        {
            return false;
        }

        ApiInformations.Replace(result.Data);

        return true;
    }

    private async Task<bool> AuthenticateAsync(LoginCredentials model)
    {
        var parameters = new AuthenticationLoginParameters(ApiInformations);

        parameters.Parameters.Account = model.Login;
        parameters.Parameters.Password = model.Password;
        parameters.Parameters.OtpCode = model.OtpCode;

        var response = await ExecuteAsync<SynoLoginResponse>(parameters);

        if (response is null || !response.Success || response.Data is null)
        {
            return false;
        }

        _sid = response.Data.Sid;
        _httpClient.DefaultRequestHeaders.Add(NetworkConstants.CookieHeader, NetworkConstants.SsidCookiePrefix + response.Data.Sid);

        return true;
    }

    #region HTTP Request calls

    public async Task<R?> ExecuteAsync<R>(IApiParameters parameters)
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

        return result;
    }

    public async Task<ApiResponseBase<EmptyResponse>?> ExecuteSimpleAsync(IApiParameters parameters)
    {
        return await ExecuteAsync<ApiResponseBase<EmptyResponse>>(parameters);
    }

    private async Task<R?> ExecuteFormAsync<R>(string url, IApiParameters parameters)
        => await ExecutePostAsync<R>(url, parameters.ToForm());

    private async Task<R?> ExecuteJsonAsync<R>(string url, IApiParameters parameters)
        => await ExecutePostAsync<R>(url, parameters.ToJson());

    private async Task<R?> ExecutePostAsync<R>(string url, StringContent content)
    {
        var message = await _httpClient.PostAsync(url, content);

        if (message.StatusCode != System.Net.HttpStatusCode.OK)
        {
            return default;
        }

        var text = await message.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<R>(text);
    }

    #endregion
}
