namespace Askyl.Dsm.WebHosting.Data.Contracts;

/// <summary>
/// The DSM endpoint and system language, read at startup and again on <see cref="Reload"/>. On the NAS, DSM is
/// reached over loopback on the HTTPS port of its web configuration (DSM.json); a developer machine names DSM's
/// URL in the ADWH_DSM_URL environment variable instead.
/// </summary>
public interface IDsmSettingsService
{
    /// <summary>
    /// The address the application connects to: 127.0.0.1, or the host of ADWH_DSM_URL when it is set.
    /// </summary>
    string Server { get; }

    /// <summary>
    /// The DSM HTTPS port (e.g. 5001).
    /// </summary>
    int Port { get; }

    /// <summary>
    /// The system language in DSM format (e.g. "enu", "fra"), or the default when synoinfo.conf cannot be read.
    /// </summary>
    string Language { get; }

    /// <summary>
    /// Re-reads the settings, so a port changed in DSM applies without a restart.
    /// Throws <see cref="InvalidOperationException"/> when ADWH_DSM_URL is not an absolute https URL, keeping the previous values.
    /// </summary>
    void Reload();
}
