using System.Threading;
using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Constants.DSM.API;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Domain.Authentication;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.Auth;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core.User;
using Askyl.Dsm.WebHosting.Data.DsmApi.Parameters;
using Askyl.Dsm.WebHosting.Data.DsmApi.Parameters.Auth;
using Askyl.Dsm.WebHosting.Data.DsmApi.Parameters.Core.User;
using Askyl.Dsm.WebHosting.Data.DsmApi.Parameters.Core.UserSettings;
using Askyl.Dsm.WebHosting.Data.DsmApi.Responses;
using Askyl.Dsm.WebHosting.Data.DsmApi.Responses.Auth;
using Askyl.Dsm.WebHosting.Data.DsmApi.Responses.Core.User;
using Askyl.Dsm.WebHosting.Data.DsmApi.Responses.Core.UserSettings;
using Askyl.Dsm.WebHosting.Logging;
using Askyl.Dsm.WebHosting.Tools.Network;

namespace Askyl.Dsm.WebHosting.Ui.Services;

/// <summary>
/// Per-user scoped session wrapper over DsmApiClient.
/// Manages SID persistence in ISession, owns per-user TTL cache and preferences.
/// </summary>
public sealed class DsmSession(DsmApiClient client, IHttpContextAccessor httpContextAccessor, ILogger<ILogDsmSession> logger) : IDsmSession
{
    private readonly ISession _session = httpContextAccessor.HttpContext!.Session;
    private readonly DsmApiClient _client = client;
    private bool _sessionValid;
    private DateTime _lastSessionValidation = DateTime.MinValue;

    private string? Sid
    {
        get => _session.GetString(ApplicationConstants.DsmSessionKey);
        set => UpdateSessionValue(ApplicationConstants.DsmSessionKey, value);
    }

    private string? Username
    {
        get => _session.GetString(ApplicationConstants.DsmUsernameKey);
        set => UpdateSessionValue(ApplicationConstants.DsmUsernameKey, value);
    }

    /// <summary>
    /// User's language in DSM format (e.g. "enu", "fra").
    /// </summary>
    public string? UserLanguage { get; private set; }

    /// <summary>
    /// User's date format in PHP-style format string.
    /// </summary>
    public string? UserDateFormat { get; private set; }

    /// <summary>
    /// User's time format in PHP-style format string.
    /// </summary>
    public string? UserTimeFormat { get; private set; }

    /// <summary>
    /// Authenticates against DSM, persists SID to session, and fetches user preferences.
    /// </summary>
    public async Task<bool> ConnectAsync(LoginCredentials model, CancellationToken cancellationToken = default)
    {
        var sid = await AuthenticateAsync(model, cancellationToken);

        if (sid is null)
        {
            return false;
        }

        Sid = sid;
        Username = model.Login;

        Volatile.Write(ref _sessionValid, false);
        _lastSessionValidation = DateTime.MinValue;

        await FetchUserPreferencesAsync(sid, cancellationToken);

        return true;
    }

    /// <summary>
    /// Validates whether the current DSM session is still active on the server.
    /// Uses per-user TTL cache to avoid per-request API overhead.
    /// </summary>
    public async Task<bool> ValidateSessionAsync(CancellationToken cancellationToken = default)
    {
        if (String.IsNullOrEmpty(Sid) || String.IsNullOrEmpty(Username))
        {
            return false;
        }

        if (Volatile.Read(ref _sessionValid) && (DateTime.UtcNow - _lastSessionValidation).TotalMinutes < ApplicationConstants.SessionValidationTtlMinutes)
        {
            return true;
        }

        var parameters = new CoreUserGetParameters(new CoreUserGetEntry(Username));
        var response = await _client.ExecuteAsync<CoreUserGetResponse>(Sid, parameters, cancellationToken);

        if (response is null || response.Error?.Code == DsmConstants.ErrorCodeAuthenticationFailed)
        {
            Volatile.Write(ref _sessionValid, false);
            _lastSessionValidation = DateTime.UtcNow;
            return false;
        }

        Volatile.Write(ref _sessionValid, true);
        _lastSessionValidation = DateTime.UtcNow;
        return true;
    }

    /// <summary>
    /// Clears session state and local cache.
    /// </summary>
    public void Disconnect()
    {
        Sid = null;
        Username = null;

        Volatile.Write(ref _sessionValid, false);
        _lastSessionValidation = DateTime.MinValue;

        UserLanguage = null;
        UserDateFormat = null;
        UserTimeFormat = null;
    }

    /// <summary>
    /// Executes an API call with the session's SID attached.
    /// </summary>
    public Task<R?> ExecuteAsync<R>(IApiParameters parameters, CancellationToken cancellationToken = default) where R : IApiResponse
        => _client.ExecuteAsync<R>(Sid, parameters, cancellationToken);

    /// <summary>
    /// Executes a simple API call with the session's SID attached.
    /// </summary>
    public Task<ApiResponseBase<object>?> ExecuteSimpleAsync(IApiParameters parameters, CancellationToken cancellationToken = default)
        => _client.ExecuteAsync<ApiResponseBase<object>>(Sid, parameters, cancellationToken);

    private void UpdateSessionValue(string key, string? value)
    {
        if (value is { Length: > 0 })
        {
            _session.SetString(key, value);
        }
        else
        {
            _session.Remove(key);
        }
    }

    private async Task<string?> AuthenticateAsync(LoginCredentials model, CancellationToken cancellationToken)
    {
        var login = new AuthenticateLogin(model.Login, model.Password, model.OtpCode);
        var parameters = new AuthLoginParameters(login);
        var response = await _client.ExecuteAsync<AuthLoginResponse>(null, parameters, cancellationToken);

        if (response?.Success != true || response.Data is null)
        {
            var errorMessage = response?.Error?.Errors?.Reason ?? "Authentication failed";
            logger.AuthenticationFailed(errorMessage);
            return null;
        }

        logger.AuthenticationSuccess(model.Login);
        return response.Data.Sid;
    }

    private async Task FetchUserPreferencesAsync(string sid, CancellationToken cancellationToken)
    {
        try
        {
            var parameters = new CoreUserSettingsParameters();
            var response = await _client.ExecuteAsync<CoreUserSettingsResponse>(sid, parameters, cancellationToken);

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
        }
        catch (Exception ex)
        {
            logger.FetchUserPreferencesFailed(ex);
        }
    }
}
