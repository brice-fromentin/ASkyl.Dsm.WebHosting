using Askyl.Dsm.WebHosting.Data.Domain.Authentication;
using Askyl.Dsm.WebHosting.Data.DsmApi.Parameters;
using Askyl.Dsm.WebHosting.Data.DsmApi.Responses;

namespace Askyl.Dsm.WebHosting.Data.Contracts;

/// <summary>
/// Per-user DSM session wrapper. Manages authentication, session validation, and API execution.
/// </summary>
public interface IDsmSession
{
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
    /// </summary>
    /// <param name="model">The login credentials.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if authentication succeeded.</returns>
    Task<bool> ConnectAsync(LoginCredentials model, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates whether the current DSM session is still active on the server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the session is valid.</returns>
    Task<bool> ValidateSessionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears session state and local cache.
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
