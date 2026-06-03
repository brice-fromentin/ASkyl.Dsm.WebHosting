namespace Askyl.Dsm.WebHosting.Data.Domain.System;

/// <summary>
/// System-level DSM preferences extracted from /etc/synoinfo.conf.
/// Contains raw DSM codes (language) before conversion to .NET format.
/// </summary>
/// <param name="server">External host IP from configuration.</param>
/// <param name="port">External HTTPS port from configuration.</param>
/// <param name="language">System language (e.g. "def" for default browser language, or a specific code).</param>
public sealed class DsmSystemPreferences(
    string server,
    int port,
    string language)
{
    /// <summary>
    /// External host IP address from /etc/synoinfo.conf.
    /// </summary>
    public string Server { get; init; } = server;

    /// <summary>
    /// External HTTPS port from /etc/synoinfo.conf.
    /// </summary>
    public int Port { get; init; } = port;

    /// <summary>
    /// System language in DSM format (e.g. "def" for default browser language, or a specific code).
    /// When "def", the browser language should be used as fallback.
    /// </summary>
    public string Language { get; init; } = language;
}
