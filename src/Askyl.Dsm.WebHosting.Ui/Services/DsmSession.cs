using Askyl.Dsm.WebHosting.Constants.Application;
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
using Askyl.Dsm.WebHosting.Data.Results;
using Askyl.Dsm.WebHosting.Logging;
using Askyl.Dsm.WebHosting.Tools.Network;

namespace Askyl.Dsm.WebHosting.Ui.Services;

/// <summary>
/// Per-user scoped session wrapper over DsmApiClient.
/// Manages SID persistence in ISession, owns per-user TTL cache and preferences.
/// </summary>
public sealed class DsmSession(DsmApiClient client, IHttpContextAccessor httpContextAccessor, ILogger<ILogDsmSession> logger) : IDsmSession, IAsyncDisposable
{
    private readonly ISession _session = httpContextAccessor.HttpContext!.Session;
    private readonly DsmApiClient _client = client;
    private readonly SemaphoreSlim _validationLock = new(1, 1);
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
    /// Rejects users without administrator rights.
    /// </summary>
    public async Task<ApiResult> ConnectAsync(LoginCredentials model, CancellationToken cancellationToken = default)
    {
        var sid = await AuthenticateAsync(model, cancellationToken);

        if (sid is null)
        {
            return new(false, null, ApiErrorCode.Unauthorized);
        }

        Sid = sid;
        Username = model.Login;

        Volatile.Write(ref _sessionValid, false);
        _lastSessionValidation = DateTime.MinValue;

        // DSM authenticates any user, including non-administrators. Validating here rejects them at
        // login rather than on the next request, and primes the TTL cache so this costs no extra call.
        if (!await ValidateSessionAsync(cancellationToken))
        {
            logger.NotAnAdministrator(model.Login);
            Disconnect();
            return new(false, null, ApiErrorCode.Forbidden);
        }

        await FetchUserPreferencesAsync(sid, cancellationToken);

        return ApiResult.CreateSuccess();
    }

    /// <summary>
    /// Validates whether the current DSM session is still active on the server, and doubles as the
    /// administrator check: SYNO.Core.User.get is admin-only, so a non-administrator gets a permission
    /// error and is rejected. Fails closed — anything other than an explicit success invalidates.
    /// Uses per-user TTL cache to avoid per-request API overhead.
    /// Serialized via semaphore to prevent concurrent duplicate API calls.
    /// </summary>
    public async Task<bool> ValidateSessionAsync(CancellationToken cancellationToken = default)
    {
        if (String.IsNullOrEmpty(Sid) || String.IsNullOrEmpty(Username))
        {
            return false;
        }

        if (Volatile.Read(ref _sessionValid) && IsWithinTtl())
        {
            return true;
        }

        await _validationLock.WaitAsync(cancellationToken);

        try
        {
            if (Volatile.Read(ref _sessionValid) && IsWithinTtl())
            {
                return true;
            }

            var parameters = new CoreUserGetParameters(new CoreUserGetEntry(Username));
            var response = await _client.ExecuteAsync<CoreUserGetResponse>(Sid, parameters, cancellationToken);

            // Fail closed: only an explicit success keeps the session alive. Treating every
            // non-`-4` outcome as valid admitted permission errors — precisely the non-administrator
            // sessions that should be rejected.
            if (response?.Success != true)
            {
                Volatile.Write(ref _sessionValid, false);
                _lastSessionValidation = DateTime.UtcNow;
                return false;
            }

            Volatile.Write(ref _sessionValid, true);
            _lastSessionValidation = DateTime.UtcNow;
            return true;
        }
        finally
        {
            _validationLock.Release();
        }
    }

    private bool IsWithinTtl()
        => (DateTime.UtcNow - _lastSessionValidation).TotalMinutes < ApplicationConstants.SessionValidationTtlMinutes;

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

    public ValueTask DisposeAsync()
    {
        _validationLock.Dispose();
        return default;
    }
}
