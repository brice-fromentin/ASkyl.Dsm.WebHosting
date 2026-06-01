using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Domain.Authentication;
using Askyl.Dsm.WebHosting.Data.Results;
using Askyl.Dsm.WebHosting.Globalization;
using Askyl.Dsm.WebHosting.Globalization.Resources;
using Askyl.Dsm.WebHosting.Logging;
using Askyl.Dsm.WebHosting.Tools.Diagnostics;
using Askyl.Dsm.WebHosting.Tools.Network;
using Microsoft.Extensions.Localization;

namespace Askyl.Dsm.WebHosting.Ui.Services;

/// <summary>
/// Implementation of IAuthenticationService that wraps DsmApiClient.
/// </summary>
/// <param name="apiClient">The DSM API client for making authentication calls.</param>
/// <param name="httpContextAccessor">Access to current HTTP context for session management.</param>
/// <param name="logger">Logger for tracking authentication operations.</param>
/// <param name="localizer">Localizer for user-facing strings.</param>
public class AuthenticationService(DsmApiClient apiClient, IHttpContextAccessor httpContextAccessor, ILogger<ILogAuthenticationService> logger, IStringLocalizer<SharedResource> localizer) : IAuthenticationService
{
    /// <inheritdoc/>
    public async Task<AuthenticationResult> LoginAsync(string login, string password, string? otpCode)
    {
        using var timer = new OperationTimer(elapsed => logger.LoginDuration(elapsed, login));

        logger.LoginStarting(login);
        var model = new LoginCredentials(login, password, otpCode);

        if (!await apiClient.ConnectAsync(model))
        {
            logger.LoginFailed(login);
            return AuthenticationResult.CreateNotAuthenticated(localizer[L.Error.AuthenticationFailed]);
        }

        // Best-effort: fetch user preferences (language, date/time format) from SYNO.Core.UserSettings.get
        await apiClient.FetchUserLanguageAsync();

        // Server resolves culture (user preference or system fallback) and timezone
        var culture = CodepageToCultureConverter.Convert(apiClient.UserLanguage ?? apiClient.SystemPreferences.Codepage);
        var timezone = DsmTimezoneToIanaConverter.Convert(apiClient.SystemPreferences.Timezone);

        // Store DSM SID and username in server-side session for persistence
        httpContextAccessor.HttpContext?.Session.SetString(ApplicationConstants.DsmSessionKey, apiClient.Sid);
        httpContextAccessor.HttpContext?.Session.SetString(ApplicationConstants.DsmUsernameKey, login);
        logger.LoginSuccessful(login);
        return AuthenticationResult.CreateAuthenticated(null, culture, timezone);
    }

    /// <inheritdoc/>
    public async Task<ApiResult> LogoutAsync()
    {
        using var timer = new OperationTimer(elapsed => logger.LogoutDuration(elapsed));

        logger.LogoutStarting();

        try
        {
            httpContextAccessor.HttpContext?.Session.Remove(ApplicationConstants.DsmSessionKey);
            httpContextAccessor.HttpContext?.Session.Remove(ApplicationConstants.DsmUsernameKey);
            await apiClient.DisconnectAsync();
            logger.UserLoggedOut();
            return ApiResult.CreateSuccess(localizer[L.Success.LogoutSuccessful]);
        }
        catch (Exception ex)
        {
            logger.LogoutError(ex);
            return ApiResult.CreateFailure(localizer[L.Error.OperationFailed]);
        }
    }

    /// <inheritdoc/>
    public async Task<ApiResultBool> IsAuthenticatedAsync()
    {
        logger.SessionValidationStarting();

        var sessionId = httpContextAccessor.HttpContext?.Session.GetString(ApplicationConstants.DsmSessionKey);
        var username = httpContextAccessor.HttpContext?.Session.GetString(ApplicationConstants.DsmUsernameKey);

        if (String.IsNullOrEmpty(sessionId) || String.IsNullOrEmpty(username))
        {
            return ApiResultBool.CreateSuccess(false, localizer[L.Error.NoSessionFound]);
        }

        if (!await apiClient.ValidateSessionAsync(username))
        {
            logger.SessionValidationFailed();

            // Session is invalid on server - clear local session
            httpContextAccessor.HttpContext?.Session.Remove(ApplicationConstants.DsmSessionKey);
            httpContextAccessor.HttpContext?.Session.Remove(ApplicationConstants.DsmUsernameKey);
            logger.SessionInvalidated();

            return ApiResultBool.CreateSuccess(false, localizer[L.Error.SessionExpired]);
        }

        logger.SessionValidationSuccess(ApplicationConstants.SessionValidationTtlMinutes);
        return ApiResultBool.CreateSuccess(true);
    }
}
