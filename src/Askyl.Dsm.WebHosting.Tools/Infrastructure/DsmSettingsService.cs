using Askyl.Dsm.WebHosting.Constants.DSM.System;
using Askyl.Dsm.WebHosting.Constants.Network;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Domain.DsmSystem;
using Askyl.Dsm.WebHosting.Logging;
using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Tools.Infrastructure;

/// <summary>
/// Reads DSM system preferences from /etc/synoinfo.conf once at startup.
/// Provides graceful fallback defaults if the configuration file is missing or malformed.
/// </summary>
public sealed class DsmSettingsService(ILogger<ILogDsmSettingsService> logger) : IDsmSettingsService
{
    private readonly DsmSystemPreferences _preferences = ReadSettings(logger);

    public string Server => _preferences.Server;

    public int Port => _preferences.Port;

    public string Language => _preferences.Language;

    static DsmSystemPreferences ReadSettings(ILogger<ILogDsmSettingsService> logger)
    {
        if (!File.Exists(SystemDefaults.ConfigurationFileName))
        {
            logger.ConfigurationFileNotFound(SystemDefaults.ConfigurationFileName);
            return CreateDefaults(logger);
        }

        try
        {
            var lines = File.ReadAllLines(SystemDefaults.ConfigurationFileName);
            var settings = lines.Where(x => x.Contains('='))
                                .ToDictionary(k => k.Split(['='], 2)[0], v => v.Split(['='], 2)[1].Replace("\"", String.Empty));

            logger.ConfigurationLoaded(settings.Count);

            var server = GetMandatorySetting(settings, SystemDefaults.KeyExternalHostIp, logger);
            var language = settings.TryGetValue(SystemDefaults.KeyLanguage, out var lang) && lang.Length > 0 ? lang : SystemDefaults.DefaultLanguage;
            var port = Int32.TryParse(settings.TryGetValue(SystemDefaults.KeyExternalHttpsPort, out var p) ? p : null, out var parsedPort) ? parsedPort : SystemDefaults.DefaultHttpsPort;

            return new DsmSystemPreferences(server, port, language);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.SettingsReadFailed(ex.Message);
            return CreateDefaults(logger);
        }
    }

    static DsmSystemPreferences CreateDefaults(ILogger<ILogDsmSettingsService> logger)
    {
        logger.UsingDefaults();
        return new DsmSystemPreferences(NetworkConstants.Localhost, SystemDefaults.DefaultHttpsPort, SystemDefaults.DefaultLanguage);
    }

    static string GetMandatorySetting(Dictionary<string, string> settings, string key, ILogger<ILogDsmSettingsService> logger)
    {
        if (!settings.TryGetValue(key, out var value) || value.Length == 0)
        {
            logger.MandatorySettingMissing(key);
            throw new InvalidOperationException($"Mandatory setting '{key}' is missing from '{SystemDefaults.ConfigurationFileName}'.");
        }

        return value;
    }
}
