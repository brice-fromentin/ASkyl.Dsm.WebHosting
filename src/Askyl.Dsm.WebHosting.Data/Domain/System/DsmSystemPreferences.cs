namespace Askyl.Dsm.WebHosting.Data.Domain.DsmSystem;

/// <summary>
/// The DSM endpoint the application uses, and the system language in raw DSM format before conversion to .NET format.
/// </summary>
/// <param name="server">The address the application connects to.</param>
/// <param name="port">DSM's HTTPS port at that address.</param>
/// <param name="language">System language (e.g. "def" for default browser language, or a specific code).</param>
public sealed class DsmSystemPreferences(
    string server,
    int port,
    string language)
{
    /// <summary>
    /// The address the application connects to: 127.0.0.1 on the NAS, the host of ADWH_DSM_URL on a developer machine.
    /// </summary>
    public string Server { get; init; } = server;

    /// <summary>
    /// DSM's HTTPS port at <see cref="Server"/>.
    /// </summary>
    public int Port { get; init; } = port;

    /// <summary>
    /// System language in DSM format (e.g. "def" for default browser language, or a specific code).
    /// When "def", the browser language should be used as fallback.
    /// </summary>
    public string Language { get; init; } = language;
}
