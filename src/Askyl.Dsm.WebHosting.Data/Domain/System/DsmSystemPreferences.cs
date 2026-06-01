namespace Askyl.Dsm.WebHosting.Data.Domain.System;

/// <summary>
/// System-level DSM preferences extracted from /etc/synoinfo.conf.
/// Contains raw DSM codes (codepage, timezone, supported languages) before conversion to .NET format.
/// </summary>
/// <param name="server">External host IP from configuration.</param>
/// <param name="port">External HTTPS port from configuration.</param>
/// <param name="codepage">System codepage (e.g. "enu").</param>
/// <param name="timezone">System timezone (e.g. "Amsterdam").</param>
/// <param name="supportedLanguages">Comma-separated supported language codes (e.g. "enu,fra,deu").</param>
public sealed class DsmSystemPreferences(
    string server,
    int port,
    string codepage,
    string timezone,
    string supportedLanguages)
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
    /// System codepage in DSM format (e.g. "enu" = en-US, "fra" = fr-FR).
    /// </summary>
    public string Codepage { get; init; } = codepage;

    /// <summary>
    /// System timezone in DSM format (e.g. "Amsterdam", "New_York").
    /// </summary>
    public string Timezone { get; init; } = timezone;

    /// <summary>
    /// Comma-separated list of supported language codes in DSM format (e.g. "enu,cht,chs,krn,tha,ger,fre").
    /// </summary>
    public string SupportedLanguages { get; init; } = supportedLanguages;
}
