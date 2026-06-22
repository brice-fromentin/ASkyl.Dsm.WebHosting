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
    Task<bool> ConnectAsync(LoginCredentials model);

    /// <summary>
    /// Validates whether the current DSM session is still active on the server.
    /// </summary>
    Task<bool> ValidateSessionAsync();

    /// <summary>
    /// Clears session state and local cache.
    /// </summary>
    void Disconnect();

    /// <summary>
    /// Executes an API call with the session's SID attached.
    /// </summary>
    Task<R?> ExecuteAsync<R>(IApiParameters parameters) where R : IApiResponse;

    /// <summary>
    /// Executes a simple API call with the session's SID attached.
    /// </summary>
    Task<ApiResponseBase<object>?> ExecuteSimpleAsync(IApiParameters parameters);
}
