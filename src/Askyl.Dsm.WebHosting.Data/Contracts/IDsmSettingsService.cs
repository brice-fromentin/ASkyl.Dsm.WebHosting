namespace Askyl.Dsm.WebHosting.Data.Contracts;

/// <summary>
/// DSM system preferences from /etc/synoinfo.conf, read at startup and again on <see cref="Reload"/>.
/// </summary>
public interface IDsmSettingsService
{
    /// <summary>
    /// The DSM hostname (external_host_ip), or null when DSM has none configured.
    /// </summary>
    string? Server { get; }

    /// <summary>
    /// The DSM HTTPS port (e.g. 5001).
    /// </summary>
    int Port { get; }

    /// <summary>
    /// The system language in DSM format (e.g. "enu", "fra").
    /// </summary>
    string Language { get; }

    /// <summary>
    /// Re-reads the settings file, so a change made in DSM applies without a restart.
    /// Throws <see cref="InvalidOperationException"/> when the file is missing or unreadable, keeping the previous values.
    /// </summary>
    void Reload();
}
