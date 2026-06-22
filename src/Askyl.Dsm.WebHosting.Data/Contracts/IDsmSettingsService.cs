namespace Askyl.Dsm.WebHosting.Data.Contracts;

/// <summary>
/// Reads DSM system preferences from /etc/synoinfo.conf once at startup.
/// Provides graceful fallback defaults if the configuration file is missing or malformed.
/// </summary>
public interface IDsmSettingsService
{
    /// <summary>
    /// The DSM server address (e.g. "127.0.0.1").
    /// </summary>
    string Server { get; }

    /// <summary>
    /// The DSM HTTPS port (e.g. 5001).
    /// </summary>
    int Port { get; }

    /// <summary>
    /// The system language in DSM format (e.g. "enu", "fra").
    /// </summary>
    string Language { get; }
}
