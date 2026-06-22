using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Domain.Authentication;
using Askyl.Dsm.WebHosting.Data.Results;
using Askyl.Dsm.WebHosting.Globalization;
using Askyl.Dsm.WebHosting.Logging;
using Askyl.Dsm.WebHosting.Tools.Converters;

namespace Askyl.Dsm.WebHosting.Ui.Services;

/// <summary>
/// Implementation of IAuthenticationService that wraps DsmSession.
/// </summary>
/// <param name="dsmSession">The DSM session for making authentication calls.</param>
/// <param name="logger">Logger for tracking authentication operations.</param>
/// <param name="localizer">Localizer for user-facing strings.</param>
public class AuthenticationService(DsmSession dsmSession, ILogger<ILogAuthenticationService> logger, ILocalizer localizer) : IAuthenticationService
{
    /// <inheritdoc/>
    public async Task<AuthenticationResult> LoginAsync(string login, string password, string? otpCode)
    {
        var model = new LoginCredentials(login, password, otpCode);

        if (!await dsmSession.ConnectAsync(model))
        {
            logger.LoginFailed(login);
            return AuthenticationResult.CreateNotAuthenticated(localizer[LK.Error.AuthenticationFailed]);
        }

        var culture = DsmLanguageToCultureConverter.Convert(dsmSession.UserLanguage);
        var dateFormat = PhpFormatToDotNetConverter.Convert(dsmSession.UserDateFormat);
        var timeFormat = PhpFormatToDotNetConverter.Convert(dsmSession.UserTimeFormat);

        logger.LoginSuccessful(login);
        return AuthenticationResult.CreateAuthenticated(null, culture, dateFormat, timeFormat);
    }

    /// <inheritdoc/>
    public Task<ApiResult> LogoutAsync()
    {
        dsmSession.Disconnect();
        logger.UserLoggedOut();
        return Task.FromResult(ApiResult.CreateSuccess(localizer[LK.Success.LogoutSuccessful]));
    }

    /// <inheritdoc/>
    public async Task<ApiResultBool> IsAuthenticatedAsync()
    {
        if (!await dsmSession.ValidateSessionAsync())
        {
            logger.SessionValidationFailed();
            dsmSession.Disconnect();
            logger.SessionInvalidated();
            return ApiResultBool.CreateSuccess(false, localizer[LK.Error.SessionExpired]);
        }

        logger.SessionValidationSuccess(ApplicationConstants.SessionValidationTtlMinutes);
        return ApiResultBool.CreateSuccess(true);
    }
}
