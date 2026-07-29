using Askyl.Dsm.WebHosting.Data.Domain.Authentication;
using Askyl.Dsm.WebHosting.Data.DsmApi.Parameters;
using Askyl.Dsm.WebHosting.Data.DsmApi.Responses;
using Askyl.Dsm.WebHosting.Data.Results;

namespace Askyl.Dsm.WebHosting.Data.Contracts;

/// <summary>
/// Per-user DSM session wrapper. Manages authentication, session validation, and API execution.
/// </summary>
public interface IDsmSession
{
    /// <summary>
    /// Whether a DSM session is currently established locally. Distinguishes a caller who never signed
    /// in from one whose session was rejected, which otherwise look identical to the caller.
    /// </summary>
    bool HasSession { get; }

    /// <summary>
    /// User's language in DSM format (e.g. "enu", "fra").
    /// </summary>
    string? UserLanguage { get; }

    /// <summary>
    /// User's date format in PHP-style format string.
    /// </summary>
    string? UserDateFormat { get; }

    /// <summary>
    /// User's time format in PHP-style format string.
    /// </summary>
    string? UserTimeFormat { get; }

    /// <summary>
    /// Authenticates against DSM, persists SID to session, and fetches user preferences.
    /// Rejects users without administrator rights.
    /// </summary>
    /// <param name="model">The login credentials.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A success result, or a failure carrying <see cref="ApiErrorCode.Unauthorized"/> for invalid
    /// credentials and <see cref="ApiErrorCode.Forbidden"/> for a non-administrator. Messages are
    /// localized by the caller, so no message is set here.
    /// </returns>
    Task<ApiResult> ConnectAsync(LoginCredentials model, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates whether the current DSM session is still active on the server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the session is valid.</returns>
    Task<bool> ValidateSessionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes the session on the NAS, then clears local state.
    /// Use whenever a live SID is abandoned, so it cannot be replayed if it was captured.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears session state and local cache without contacting the NAS.
    /// Only appropriate when the SID is already known to be dead.
    /// </summary>
    void Disconnect();

    /// <summary>
    /// Executes an API call with the session's SID attached.
    /// </summary>
    /// <param name="parameters">The API parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <typeparam name="R">The response type.</typeparam>
    /// <returns>The API response or null.</returns>
    Task<R?> ExecuteAsync<R>(IApiParameters parameters, CancellationToken cancellationToken = default) where R : IApiResponse;

    /// <summary>
    /// Executes a simple API call with the session's SID attached.
    /// </summary>
    /// <param name="parameters">The API parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The API response or null.</returns>
    Task<ApiResponseBase<object>?> ExecuteSimpleAsync(IApiParameters parameters, CancellationToken cancellationToken = default);
}
