using Askyl.Dsm.WebHosting.Tools.Network;
using Askyl.Dsm.WebHosting.Constants.Application;
using Microsoft.Extensions.Caching.Memory;

namespace Askyl.Dsm.WebHosting.Ui.Services;

public interface ITemporaryTokenService
{
    string GenerateToken();
    bool ValidateAndConsumeToken(string token);
}

public class TemporaryTokenService(DsmApiClient dsmApiClient, ILogger<TemporaryTokenService> logger, IMemoryCache cache) : ITemporaryTokenService
{

    #region Fields

    private readonly DsmApiClient _dsmApiClient = dsmApiClient;
    private readonly ILogger<TemporaryTokenService> _logger = logger;
    private readonly IMemoryCache _cache = cache;
    private readonly TimeSpan _tokenLifetime = TimeSpan.FromMinutes(LogConstants.TokenLifetimeMinutes);
    private const string TOKEN_PREFIX = LogConstants.TokenPrefix;

    #endregion

    #region Public Methods

    public string GenerateToken()
    {
        if (!_dsmApiClient.IsConnected)
        {
            _logger.LogWarning("Attempted to generate token without DSM connection");
            return "";
        }

        var token = Guid.NewGuid().ToString("N");
        var cacheKey = TOKEN_PREFIX + token;

        _cache.Set(cacheKey, DateTime.UtcNow, _tokenLifetime);

        _logger.LogDebug("Generated temporary token: {Token}", token[..8] + "...");

        return token;
    }

    public bool ValidateAndConsumeToken(string token)
    {
        if (String.IsNullOrEmpty(token))
        {
            return false;
        }

        var cacheKey = TOKEN_PREFIX + token;

        if (_cache.TryGetValue(cacheKey, out var createdAt) && createdAt is DateTime tokenCreatedAt)
        {
            // Remove token immediately (one-time use)
            _cache.Remove(cacheKey);

            var age = DateTime.UtcNow - tokenCreatedAt;
            var isValid = age <= _tokenLifetime;

            _logger.LogDebug("Token validation: {Token} - Valid: {IsValid}, Age: {Age}", token[..8] + "...", isValid, age);

            return isValid;
        }

        _logger.LogWarning("Invalid or already used token: {Token}", token[..8] + "...");
        return false;
    }

    #endregion

}