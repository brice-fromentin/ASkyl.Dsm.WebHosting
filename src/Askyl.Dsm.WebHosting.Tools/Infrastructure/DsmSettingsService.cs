using Askyl.Dsm.WebHosting.Constants.DSM.System;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Domain.DsmSystem;
using Askyl.Dsm.WebHosting.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Tools.Infrastructure;

/// <summary>
/// Reads DSM system preferences from /etc/synoinfo.conf once at startup.
/// Throws if the configuration file is missing or malformed.
/// </summary>
/// <param name="logger">Logger instance.</param>
/// <param name="fileReader">File system abstraction for reading configuration files.</param>
/// <param name="configuration">Application configuration for resolving the settings file path.</param>
public sealed class DsmSettingsService(ILogger<ILogDsmSettingsService> logger, IFileReader fileReader, IConfiguration configuration) : IDsmSettingsService
{
    private readonly DsmSystemPreferences _preferences = ReadSettings(logger, fileReader, configuration);

    public string Server => _preferences.Server;

    public int Port => _preferences.Port;

    public string Language => _preferences.Language;

    static string ResolveAndValidateConfigPath(ILogger<ILogDsmSettingsService> logger, IFileReader fileReader, IConfiguration configuration)
    {
        var configuredPath = configuration.GetValue<string>("DsmSettings:ConfigPath");
        var configPath = configuredPath ?? SystemDefaults.SynoInfoConfPath;
        var hasOverride = configuredPath != null;

        logger.ResolvingConfigPath(configPath, hasOverride);

        if (!fileReader.FileExists(configPath))
        {
            logger.ConfigurationFileNotFound(configPath);
            throw new InvalidOperationException(
                $"DSM settings file not found at '{configPath}'. " +
                (hasOverride
                    ? $"This is expected in local development. Create the mock configuration by copying the template: cp dev-mock/synoinfo.conf.template {configPath}"
                    : "Ensure DSM /etc/synoinfo.conf exists and is readable."));
        }

        return configPath;
    }

    static DsmSystemPreferences ReadSettings(ILogger<ILogDsmSettingsService> logger, IFileReader fileReader, IConfiguration configuration)
    {
        var configPath = ResolveAndValidateConfigPath(logger, fileReader, configuration);

        try
        {
            var lines = fileReader.ReadAllLines(configPath);
            var settings = lines.Where(x => x.Contains('='))
                                .ToDictionary(k => k.Split(['='], 2)[0], v => v.Split(['='], 2)[1].Replace("\"", String.Empty));

            logger.ConfigurationLoaded(settings.Count);

            var server = GetMandatorySetting(settings, SystemDefaults.KeyExternalHostIp, configPath, logger);
            var language = settings.TryGetValue(SystemDefaults.KeyLanguage, out var lang) && lang.Length > 0 ? lang : SystemDefaults.DefaultLanguage;
            var port = Int32.TryParse(settings.TryGetValue(SystemDefaults.KeyExternalHttpsPort, out var p) ? p : null, out var parsedPort) ? parsedPort : SystemDefaults.DefaultHttpsPort;

            return new DsmSystemPreferences(server, port, language);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex) when (!(ex is InvalidOperationException))
        {
            logger.SettingsReadFailed(ex);
            throw new InvalidOperationException($"Failed to read DSM settings from '{configPath}'.", ex);
        }
    }

    static string GetMandatorySetting(Dictionary<string, string> settings, string key, string configPath, ILogger<ILogDsmSettingsService> logger)
    {
        if (!settings.TryGetValue(key, out var value) || value.Length == 0)
        {
            logger.MandatorySettingMissing(key);
            throw new InvalidOperationException($"Mandatory setting '{key}' is missing from '{configPath}'.");
        }

        return value;
    }
}
