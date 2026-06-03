using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Constants.DSM.API;
using Askyl.Dsm.WebHosting.Constants.DSM.System;
using Askyl.Dsm.WebHosting.Constants.Network;
using Askyl.Dsm.WebHosting.Data.Domain.Authentication;
using Askyl.Dsm.WebHosting.Data.Domain.System;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.Auth;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core.User;
using Askyl.Dsm.WebHosting.Data.DsmApi.Parameters;
using Askyl.Dsm.WebHosting.Data.DsmApi.Parameters.Auth;
using Askyl.Dsm.WebHosting.Data.DsmApi.Parameters.Core.User;
using Askyl.Dsm.WebHosting.Data.DsmApi.Parameters.Core.UserSettings;
using Askyl.Dsm.WebHosting.Data.DsmApi.Parameters.Info;
using Askyl.Dsm.WebHosting.Data.DsmApi.Responses;
using Askyl.Dsm.WebHosting.Data.DsmApi.Responses.Auth;
using Askyl.Dsm.WebHosting.Data.DsmApi.Responses.Core.User;
using Askyl.Dsm.WebHosting.Data.DsmApi.Responses.Core.UserSettings;
using Askyl.Dsm.WebHosting.Data.Exceptions;
using Askyl.Dsm.WebHosting.Logging;
using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Tools.Network;

public class DsmApiClient(IHttpClientFactory httpClientFactory, ILogger<ILogDsmApiClient> logger)
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(ApplicationConstants.HttpClientName);

    // Session validation cache
    private bool _sessionValid;
    private DateTime _lastSessionValidation;

    public ApiInformationCollection ApiInformations { get; } = new();

    /// <summary>
    /// System-level DSM preferences extracted from /etc/synoinfo.conf.
    /// Populated at construction time by reading the configuration file synchronously.
    /// </summary>
    public DsmSystemPreferences SystemPreferences { get; } = ReadSettings(logger);

    /// <summary>
    /// Session ID (SID) for the current DSM session.
    /// </summary>
    public string Sid { get; private set; } = String.Empty;

    /// <summary>
    /// User's language in DSM format (e.g. "enu", "fra").
    /// Populated after authentication via SYNO.Core.UserSettings.get (best-effort).
    /// Returns "def" if the user has not set a personal language preference.
    /// Null if the API call failed or returned no language.
    /// </summary>
    public string? UserLanguage { get; private set; }

    /// <summary>
    /// User's date format in PHP-style format string (e.g. "Y/m/d", "d/m/Y").
    /// Populated after authentication via SYNO.Core.UserSettings.get (best-effort).
    /// Null if the API call failed or returned no date format.
    /// </summary>
    public string? UserDateFormat { get; private set; }

    /// <summary>
    /// User's time format in PHP-style format string (e.g. "H:i", "h:i a").
    /// Populated after authentication via SYNO.Core.UserSettings.get (best-effort).
    /// Null if the API call failed or returned no time format.
    /// </summary>
    public string? UserTimeFormat { get; private set; }

    /// <summary>
    /// Sets the session ID directly (for restoring from persisted state).
    /// Also updates the HTTP client cookie header to ensure API calls use the restored SID.
    /// </summary>
    public void SetSid(string sid)
    {
        Sid = sid;
        _httpClient.DefaultRequestHeaders.Remove(NetworkConstants.CookieHeader);

        if (sid.Length > 0)
        {
            _httpClient.DefaultRequestHeaders.Add(NetworkConstants.CookieHeader, NetworkConstants.SsidCookiePrefix + sid);
        }
    }

    /// <summary>
    /// Disconnects and clears the session.
    /// </summary>
    public async Task DisconnectAsync()
    {
        logger.Disconnecting();

        SetSid(String.Empty);

        // Clear session validation cache
        _sessionValid = false;
        _lastSessionValidation = DateTime.MinValue;

        // Clear user preferences on disconnect
        UserLanguage = null;
        UserDateFormat = null;
        UserTimeFormat = null;

        logger.Disconnected();
    }

    public async Task<bool> ConnectAsync(LoginCredentials model)
    {
        logger.Connecting(SystemPreferences.Server, SystemPreferences.Port);

        if (!await HandShakeAsync())
        {
            return false;
        }

        if (!await AuthenticateAsync(model))
        {
            return false;
        }

        // Invalidate session cache on new connection
        _sessionValid = false;
        _lastSessionValidation = DateTime.MinValue;
        logger.Connected();

        return true;
    }

    /// <summary>
    /// Validates whether the current DSM session is still active on the server.
    /// Uses a lightweight SYNO.Core.User.get call to verify the SID and user existence.
    /// Results are cached for the configured TTL to avoid per-request API overhead.
    /// </summary>
    /// <param name="username">The logged-in username to validate against.</param>
    /// <returns>True if the session is valid, false if expired or invalid.</returns>
    public async Task<bool> ValidateSessionAsync(string username)
    {
        if (String.IsNullOrEmpty(Sid))
        {
            return false;
        }

        // Check cache - skip API call if validation is fresh
        if (_sessionValid && (DateTime.UtcNow - _lastSessionValidation).TotalMinutes < ApplicationConstants.SessionValidationTtlMinutes)
        {
            return true;
        }

        // Use Core.User.get as a lightweight session validation probe
        // If the SID is invalid, DSM returns error -4 (authentication failure)
        var parameters = new CoreUserGetParameters(ApiInformations, new CoreUserGetEntry(username));

        var response = await ExecuteAsync<CoreUserGetResponse>(parameters);

        // Session is invalid if: no response, or auth error (code -4)
        // We accept any non-auth failure as valid (user-specific errors still mean SID is alive)
        if (response is null || response.Error?.Code == DsmConstants.ErrorCodeAuthenticationFailed)
        {
            _sessionValid = false;
            _lastSessionValidation = DateTime.UtcNow;
            return false;
        }

        _sessionValid = true;
        _lastSessionValidation = DateTime.UtcNow;
        return true;
    }

    private static DsmSystemPreferences ReadSettings(ILogger<ILogDsmApiClient> logger)
    {
        if (!File.Exists(SystemDefaults.ConfigurationFileName))
        {
            logger.ConfigurationFileNotFound(SystemDefaults.ConfigurationFileName);
            throw new FileNotFoundException("DSM configuration file not found", SystemDefaults.ConfigurationFileName);
        }

        var lines = File.ReadAllLines(SystemDefaults.ConfigurationFileName);
        var settings = lines.Where(x => x.Contains('='))
                            .ToDictionary(key => key.Split('=')[0], value => value.Split('=')[1].Replace("\"", String.Empty));

        logger.ConfigurationLoaded(settings.Count);

        var server = GetMandatorySetting(settings, SystemDefaults.KeyExternalHostIp, logger);
        var language = settings.TryGetValue(SystemDefaults.KeyLanguage, out var lang) && lang.Length > 0 ? lang : "def";
        var port = Int32.TryParse(settings.TryGetValue(SystemDefaults.KeyExternalHttpsPort, out var p) ? p : null, out var parsedPort) ? parsedPort : SystemDefaults.DefaultHttpsPort;

        return new DsmSystemPreferences(server, port, language);
    }

    private static string GetMandatorySetting(Dictionary<string, string> settings, string key, ILogger<ILogDsmApiClient> logger)
    {
        if (!settings.TryGetValue(key, out var value) || value.Length == 0)
        {
            logger.MandatorySettingMissing(key);
            throw new MandatorySettingMissingException(key);
        }

        return value;
    }

    /// <summary>
    /// Fetches the user's personal preferences from SYNO.Core.UserSettings.get (best-effort, non-blocking).
    /// Called after authentication; failure silently falls back to system preferences.
    /// </summary>
    /// <remarks>
    /// The API returns all user settings (~1400 lines). We extract Personal.lang, dateFormat, and timeFormat.
    /// </remarks>
    /// <returns>The user's language code in DSM format (e.g. "enu"), or null if unavailable.</returns>
    public async Task<string?> FetchUserLanguageAsync()
    {
        try
        {
            var parameters = new CoreUserSettingsParameters(ApiInformations);
            var response = await ExecuteAsync<CoreUserSettingsResponse>(parameters);

            var personal = response?.Data?.Personal;

            if (personal?.Lang is { Length: > 0 } lang)
            {
                UserLanguage = lang;
            }

            if (personal?.DateFormat is { Length: > 0 } dateFormat)
            {
                UserDateFormat = dateFormat;
            }

            if (personal?.TimeFormat is { Length: > 0 } timeFormat)
            {
                UserTimeFormat = timeFormat;
            }

            return UserLanguage;
        }
        catch
        {
            // Best-effort: silently ignore failures; system preferences are used as fallback
        }

        return null;
    }

    private async Task<bool> HandShakeAsync()
    {
        logger.HandshakeStarting();

        var parameters = new InformationsQueryParameters(ApiInformations);
        var result = await ExecuteAsync<ApiInformationResponse>(parameters);

        if (result?.Success != true || result.Data is null || result.Data.Count == 0)
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
        var login = new AuthenticateLogin(model.Login, model.Password, model.OtpCode);
        var parameters = new AuthLoginParameters(ApiInformations, login);
        var response = await ExecuteAsync<AuthLoginResponse>(parameters);

        if (response?.Success != true || response.Data is null)
        {
            var errorMessage = response?.Error?.Errors?.Reason ?? "Authentication failed";
            logger.AuthenticationFailed(errorMessage);
            return false;
        }

        SetSid(response.Data.Sid);

        return true;
    }

    #region HTTP Request calls

    public async Task<R?> ExecuteAsync<R>(IApiParameters parameters)
        where R : IApiResponse
    {
        var url = parameters.BuildUrl(SystemPreferences.Server, SystemPreferences.Port);
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

        var text = await response.Content.ReadAsStringAsync();

        if (response.StatusCode != HttpStatusCode.OK)
        {
            return default;
        }

        return JsonSerializer.Deserialize<R>(text);
    }

    #endregion
}
