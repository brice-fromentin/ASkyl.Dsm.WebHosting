using System.Security.Cryptography;
using System.Text;
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
using Microsoft.Extensions.Caching.Memory;

namespace Askyl.Dsm.WebHosting.Ui.Services;

/// <summary>
/// Per-user scoped session wrapper over DsmApiClient.
/// Manages SID persistence in ISession, owns per-user TTL cache and preferences.
/// </summary>
public sealed class DsmSession(DsmApiClient client, IHttpContextAccessor httpContextAccessor, IMemoryCache validationCache, ILogger<ILogDsmSession> logger) : IDsmSession, IAsyncDisposable
{
    private readonly HttpContext _httpContext = httpContextAccessor.HttpContext!;
    private readonly ISession _session = httpContextAccessor.HttpContext!.Session;
    private readonly DsmApiClient _client = client;
    private readonly SemaphoreSlim _validationLock = new(1, 1);

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

    private string? Binding
    {
        get => _session.GetString(ApplicationConstants.SessionBindingKey);
        set => UpdateSessionValue(ApplicationConstants.SessionBindingKey, value);
    }

    /// <summary>
    /// Whether a DSM session is currently established locally.
    /// </summary>
    public bool HasSession => !String.IsNullOrEmpty(Sid);

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

        // Force a fresh check at login: a cached entry for a recycled SID must never skip the
        // administrator gate below.
        validationCache.Remove(BuildValidationCacheKey(sid));

        // DSM authenticates any user, including non-administrators. Validating here rejects them at
        // login rather than on the next request, and primes the TTL cache so this costs no extra call.
        // The binding is not checked: it does not exist until this check passes.
        if (!await ConfirmWithDsmAsync(sid, model.Login, cancellationToken))
        {
            // The SID is live — DSM authenticated the user, we are refusing them — so revoke it rather
            // than abandoning a usable session on the NAS.
            logger.NotAnAdministrator(model.Login);
            await DisconnectAsync(cancellationToken);
            return new(false, null, ApiErrorCode.Forbidden);
        }

        BindToThisBrowser();

        await FetchUserPreferencesAsync(sid, cancellationToken);

        return ApiResult.CreateSuccess();
    }

    /// <summary>
    /// Validates the current session: the request must present the binding issued at login, then DSM must
    /// confirm the SID. Fails closed — anything other than an explicit success invalidates.
    /// </summary>
    public async Task<bool> ValidateSessionAsync(CancellationToken cancellationToken = default)
    {
        if (String.IsNullOrEmpty(Sid) || String.IsNullOrEmpty(Username))
        {
            return false;
        }

        // Before the cache, which is keyed by SID: a fixated session shares its victim's SID, so a cached
        // entry would admit it.
        if (!IsBoundToThisBrowser())
        {
            logger.SessionBindingRejected();
            return false;
        }

        return await ConfirmWithDsmAsync(Sid, Username, cancellationToken);
    }

    /// <summary>
    /// Confirms with DSM that the SID is still active, which doubles as the administrator check:
    /// SYNO.Core.User.get is admin-only, so a non-administrator gets a permission error and is rejected.
    /// Results are cached per SID in a shared memory cache, so validity outlives the request that
    /// established it. Instance state cannot do this: IDsmSession is Scoped, so fields reset every
    /// request and every request would pay a DSM round trip.
    /// </summary>
    /// <param name="sid">The DSM session identifier.</param>
    /// <param name="username">The DSM user the session belongs to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task<bool> ConfirmWithDsmAsync(string sid, string username, CancellationToken cancellationToken)
    {
        var cacheKey = BuildValidationCacheKey(sid);

        if (validationCache.TryGetValue(cacheKey, out _))
        {
            return true;
        }

        await _validationLock.WaitAsync(cancellationToken);

        try
        {
            if (validationCache.TryGetValue(cacheKey, out _))
            {
                return true;
            }

            var parameters = new CoreUserGetParameters(new CoreUserGetEntry(username));
            var response = await _client.ExecuteAsync<CoreUserGetResponse>(sid, parameters, cancellationToken);

            // Fail closed: only an explicit success keeps the session alive. Treating every
            // non-`-4` outcome as valid admitted permission errors — precisely the non-administrator
            // sessions that should be rejected.
            if (response?.Success != true)
            {
                validationCache.Remove(cacheKey);
                return false;
            }

            // Absolute, not sliding: an active user must still be re-checked every TTL, otherwise a
            // session revoked on the NAS would stay accepted here for as long as they kept clicking.
            validationCache.Set(cacheKey, true, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(ApplicationConstants.SessionValidationTtlMinutes)
            });

            // Only this branch talks to DSM. The caller logs every successful validation, hit or miss,
            // so without this line the two are indistinguishable in a log — and the cache defect fixed
            // in PR #39 was found by reading exactly that.
            logger.SessionValidatedAgainstDsm(ApplicationConstants.SessionValidationTtlMinutes);

            return true;
        }
        finally
        {
            _validationLock.Release();
        }
    }

    /// <summary>
    /// Builds the per-SID cache key. Keying on the SID keeps one user's validity from answering for
    /// another's, which instance-scoped state gave for free and a shared cache must do deliberately.
    /// </summary>
    /// <param name="sid">The DSM session identifier.</param>
    private static string BuildValidationCacheKey(string sid)
        => ApplicationConstants.SessionValidationCacheKeyPrefix + sid;

    /// <summary>
    /// Whether the request presents the binding issued at this session's login. The session cookie alone
    /// proves nothing: ASP.NET Core accepts any validly protected session key, including one an attacker
    /// planted in the browser before its owner signed in.
    /// </summary>
    private bool IsBoundToThisBrowser()
        => Binding is { Length: > 0 } expected
            && _httpContext.Request.Cookies[ApplicationConstants.SessionBindingCookieName] is { } presented
            && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(presented));

    /// <summary>
    /// Issues a fresh binding, kept in the session and handed to this browser alone. Replacing it on every
    /// login is the rotation the session key itself cannot have.
    /// </summary>
    private void BindToThisBrowser()
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(ApplicationConstants.SessionBindingTokenBytes));

        Binding = token;
        _httpContext.Response.Cookies.Append(ApplicationConstants.SessionBindingCookieName, token, BuildBindingCookieOptions());
    }

    /// <summary>
    /// Same attributes as the session cookie configured in Program.cs, so the binding travels wherever the
    /// session does and nowhere else.
    /// </summary>
    private static CookieOptions BuildBindingCookieOptions()
        => new() { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict };

    /// <summary>
    /// Revokes the session on the NAS, then clears local state. Use this whenever a SID is abandoned
    /// while it is still live; <see cref="Disconnect"/> alone leaves it usable until DSM expires it.
    /// </summary>
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await RevokeSessionAsync(cancellationToken);

        Disconnect();
    }

    /// <summary>
    /// Calls SYNO.API.Auth.logout for the current SID. Failures are logged and swallowed: clearing the
    /// local session must succeed even when the NAS is unreachable, so logout can never leave the user
    /// stuck signed in. The log states which of the two happened.
    /// </summary>
    private async Task RevokeSessionAsync(CancellationToken cancellationToken)
    {
        if (String.IsNullOrEmpty(Sid))
        {
            return;
        }

        try
        {
            var response = await _client.ExecuteAsync<ApiResponseBase<object>>(Sid, new AuthLogoutParameters(), cancellationToken);

            if (response?.Success == true)
            {
                logger.SessionRevoked();
            }
            else
            {
                logger.SessionRevocationRefused(response?.Error?.Code ?? 0);
            }
        }
        catch (Exception ex)
        {
            logger.SessionRevocationFailed(ex);
        }
    }

    /// <summary>
    /// Clears session state and local cache without contacting the NAS. Only appropriate when the SID
    /// is already known to be dead; otherwise prefer <see cref="DisconnectAsync"/>.
    /// </summary>
    public void Disconnect()
    {
        // Evict first: the key derives from the SID that is about to be cleared, and leaving the entry
        // behind would keep a signed-out session passing validation until the TTL lapsed.
        if (Sid is { Length: > 0 } sid)
        {
            validationCache.Remove(BuildValidationCacheKey(sid));
        }

        Sid = null;
        Username = null;
        Binding = null;
        _httpContext.Response.Cookies.Delete(ApplicationConstants.SessionBindingCookieName, BuildBindingCookieOptions());

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
