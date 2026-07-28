using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Domain.Authentication;
using Askyl.Dsm.WebHosting.Data.Results;
using Askyl.Dsm.WebHosting.Globalization;
using Askyl.Dsm.WebHosting.Globalization.Validators;
using Askyl.Dsm.WebHosting.Logging;
using Askyl.Dsm.WebHosting.Tools.Converters;
using FluentValidation;

namespace Askyl.Dsm.WebHosting.Ui.Services;

/// <summary>
/// Implementation of IAuthenticationService that wraps DsmSession.
/// </summary>
/// <param name="dsmSession">The DSM session for making authentication calls.</param>
/// <param name="logger">Logger for tracking authentication operations.</param>
/// <param name="localizer">Localizer for user-facing strings.</param>
public class AuthenticationService(
    IDsmSession dsmSession,
    IValidator<LoginCredentials> credentialsValidator,
    ILogger<ILogAuthenticationService> logger,
    ILocalizer localizer) : IAuthenticationService
{
    /// <inheritdoc/>
    public async Task<AuthenticationResult> LoginAsync(string login, string password, string? otpCode, CancellationToken cancellationToken = default)
    {
        var model = new LoginCredentials(login, password, otpCode);

        // Validated here rather than by a model-binding filter, so a rejection is an ordinary
        // failure result carrying the localized reason instead of a short-circuited 400.
        var validation = await credentialsValidator.ValidateAsync(model, cancellationToken);

        if (!validation.IsValid)
        {
            logger.LoginFailed(login);
            return AuthenticationResult.CreateNotAuthenticated(validation.ToMessage());
        }

        var connection = await dsmSession.ConnectAsync(model, cancellationToken);

        if (!connection.Success)
        {
            logger.LoginFailed(login);

            var reason = connection.ErrorCode == ApiErrorCode.Forbidden
                ? LK.Error.AdministratorRequired
                : LK.Error.AuthenticationFailed;

            return AuthenticationResult.CreateNotAuthenticated(localizer[reason]);
        }

        var culture = DsmLanguageToCultureConverter.Convert(dsmSession.UserLanguage);
        var dateFormat = PhpFormatToDotNetConverter.Convert(dsmSession.UserDateFormat);
        var timeFormat = PhpFormatToDotNetConverter.Convert(dsmSession.UserTimeFormat);

        logger.LoginSuccessful(login);
        return AuthenticationResult.CreateAuthenticated(null, culture, dateFormat, timeFormat);
    }

    /// <inheritdoc/>
    public Task<ApiResult> LogoutAsync(CancellationToken cancellationToken = default)
    {
        dsmSession.Disconnect();
        logger.UserLoggedOut();
        return Task.FromResult(ApiResult.CreateSuccess(localizer[LK.Success.LogoutSuccessful]));
    }

    /// <inheritdoc/>
    public async Task<ApiResultBool> IsAuthenticatedAsync(CancellationToken cancellationToken = default)
    {
        if (!await dsmSession.ValidateSessionAsync(cancellationToken))
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
