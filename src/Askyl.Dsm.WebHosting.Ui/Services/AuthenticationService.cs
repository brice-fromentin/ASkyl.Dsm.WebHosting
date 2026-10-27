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
/// <param name="diagnosticService">Decides whether a failed login means DSM is unreachable.</param>
/// <param name="timeProvider">Times the login's DSM budget.</param>
/// <param name="logger">Logger for tracking authentication operations.</param>
/// <param name="localizer">Localizer for user-facing strings.</param>
public class AuthenticationService(
    IDsmSession dsmSession,
    IDsmConnectionDiagnosticService diagnosticService,
    IValidator<LoginCredentials> credentialsValidator,
    TimeProvider timeProvider,
    ILogger<ILogAuthenticationService> logger,
    ILocalizer localizer) : IAuthenticationService
{
    static readonly TimeSpan DsmTimeout = TimeSpan.FromSeconds(ApplicationConstants.DsmLoginTimeoutSeconds);

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

        ApiResult connection;

        try
        {
            // Bounded, so the diagnostic below still answers before the browser stops waiting
            using var timeout = new CancellationTokenSource(DsmTimeout, timeProvider);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

            connection = await dsmSession.ConnectAsync(model, linked.Token);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            // Whatever broke on the way to DSM, the diagnostic decides whether DSM itself is the problem
            logger.LoginTransportFailed(login, ex);

            var report = await diagnosticService.RecheckAsync(CancellationToken.None);

            return report.Value is { IsHealthy: false }
                ? AuthenticationResult.CreateDsmUnreachable(localizer[LK.Diagnostic.DsmUnreachable])
                : AuthenticationResult.CreateNotAuthenticated(localizer[LK.Error.AuthenticationFailed]);
        }

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
    public async Task<ApiResult> LogoutAsync(CancellationToken cancellationToken = default)
    {
        // Revokes the SID on the NAS as well as clearing it locally: a captured SID would otherwise
        // stay usable until DSM expired it, which is the whole point of logging out.
        await dsmSession.DisconnectAsync(cancellationToken);

        logger.UserLoggedOut();

        return ApiResult.CreateSuccess(localizer[LK.Success.LogoutSuccessful]);
    }

    /// <inheritdoc/>
    public async Task<ApiResultBool> IsAuthenticatedAsync(CancellationToken cancellationToken = default)
    {
        // Captured before validating: a caller who never signed in is not a failure, and warning that
        // their session "expired or is invalid on server" would be false as well as noisy.
        var hadSession = dsmSession.HasSession;

        if (!await dsmSession.ValidateSessionAsync(cancellationToken))
        {
            if (hadSession)
            {
                logger.SessionValidationFailed();
                dsmSession.Disconnect();
                logger.SessionInvalidated();
            }

            return ApiResultBool.CreateSuccess(false, localizer[LK.Error.SessionExpired]);
        }

        logger.SessionValidationSuccess();
        return ApiResultBool.CreateSuccess(true);
    }
}
