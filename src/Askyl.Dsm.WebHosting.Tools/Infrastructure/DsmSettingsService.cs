using System.Net;
using System.Text.Json;
using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Constants.DSM.System;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Domain.DsmSystem;
using Askyl.Dsm.WebHosting.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Tools.Infrastructure;

/// <summary>
/// Reads the DSM endpoint and system language at startup and again on <see cref="Reload"/>. On the NAS, DSM is
/// reached over loopback on the HTTPS port of its web configuration (DSM.json), or DSM's default port when that
/// file cannot give one. A developer machine names DSM's URL in ADWH_DSM_URL instead. The language comes from
/// /etc/synoinfo.conf, or the default when that file is missing or unreadable.
/// </summary>
/// <param name="logger">Logger instance.</param>
/// <param name="fileReader">File system abstraction for reading configuration files.</param>
/// <param name="configuration">Application configuration, which holds ADWH_DSM_URL and the file path overrides.</param>
public sealed class DsmSettingsService(ILogger<ILogDsmSettingsService> logger, IFileReader fileReader, IConfiguration configuration) : IDsmSettingsService
{
    // Swapped whole by Reload, so a reader sees one read's values, never a mix of two
    private volatile DsmSystemPreferences _preferences = ReadSettings(logger, fileReader, configuration);

    public string Server => _preferences.Server;

    public int Port => _preferences.Port;

    public string Language => _preferences.Language;

    public void Reload() => _preferences = ReadSettings(logger, fileReader, configuration);

    /// <summary>
    /// The path of DSM's web configuration (DSM.json), or the override in DsmSettings:WebConfigPath.
    /// </summary>
    public static string WebConfigPath(IConfiguration configuration) => configuration.GetValue<string>(SystemDefaults.ConfigKeyWebConfigPath) ?? SystemDefaults.DsmWebConfigPath;

    static DsmSystemPreferences ReadSettings(ILogger<ILogDsmSettingsService> logger, IFileReader fileReader, IConfiguration configuration)
    {
        var language = ReadLanguage(logger, fileReader, configuration);

        if (configuration[ApplicationConstants.DsmUrlEnvironmentVariable] is { Length: > 0 } url)
        {
            // A developer's mistake: stopping the start says so more plainly than a diagnostic could
            if (!Uri.TryCreate(url, UriKind.Absolute, out var dsm) || dsm.Scheme != Uri.UriSchemeHttps)
            {
                throw new InvalidOperationException($"{ApplicationConstants.DsmUrlEnvironmentVariable} must be an absolute https URL, such as https://nas.example:5001.");
            }

            // Punycode, as the HTTP client sends it; an IPv6 literal keeps the brackets a URL needs, which the certificate policy strips
            var host = dsm.HostNameType == UriHostNameType.IPv6 ? dsm.Host : dsm.IdnHost;

            logger.ReachingDsmFromUrl(host, dsm.Port);

            return new(host, dsm.Port, language);
        }

        var port = ReadLoopbackPort(logger, fileReader, configuration);

        logger.ReachingDsmOverLoopback(port);

        return new(SystemDefaults.LoopbackAddress, port, language);
    }

    static string ReadLanguage(ILogger<ILogDsmSettingsService> logger, IFileReader fileReader, IConfiguration configuration)
    {
        var configuredPath = configuration.GetValue<string>("DsmSettings:ConfigPath");
        var configPath = configuredPath ?? SystemDefaults.SynoInfoConfPath;

        logger.ResolvingConfigPath(configPath, configuredPath != null);

        // A developer machine has no synoinfo.conf, and the default language defers to the browser's
        if (!fileReader.FileExists(configPath))
        {
            logger.ConfigurationFileNotFound(configPath);
            return SystemDefaults.DefaultLanguage;
        }

        try
        {
            var settings = fileReader.ReadAllLines(configPath)
                                     .Where(x => x.Contains('='))
                                     .ToDictionary(k => k.Split(['='], 2)[0], v => v.Split(['='], 2)[1].Replace("\"", String.Empty));

            logger.ConfigurationLoaded(settings.Count);

            return settings.TryGetValue(SystemDefaults.KeyLanguage, out var language) && language.Length > 0 ? language : SystemDefaults.DefaultLanguage;
        }
        catch (Exception ex)
        {
            logger.SettingsReadFailed(ex);
            return SystemDefaults.DefaultLanguage;
        }
    }

    // synoinfo.conf is no fallback: moving DSM's port leaves it untouched, and its external_port_dsm_https is the
    // router's public port ("Router Port" in Control Panel). A wrong port fails closed: the certificate policy
    // accepts loopback only where root holds the listener
    static int ReadLoopbackPort(ILogger<ILogDsmSettingsService> logger, IFileReader fileReader, IConfiguration configuration)
    {
        var path = WebConfigPath(configuration);
        Exception? failure = null;

        try
        {
            if (fileReader.FileExists(path))
            {
                using var document = JsonDocument.Parse(String.Join('\n', fileReader.ReadAllLines(path)));

                if (document.RootElement.TryGetProperty(SystemDefaults.KeyDsmWebConfigSsl, out var ssl)
                    && ssl.TryGetProperty(SystemDefaults.KeyDsmWebConfigPort, out var port)
                    && port.TryGetInt32(out var value)
                    && value is > IPEndPoint.MinPort and <= IPEndPoint.MaxPort)
                {
                    return value;
                }
            }
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            failure = ex;
        }

        logger.WebConfigurationUnreadable(SystemDefaults.DefaultHttpsPort, path, failure);

        return SystemDefaults.DefaultHttpsPort;
    }
}
