using System.Text.Json;
using Askyl.Dsm.WebHosting.Constants.DSM.System;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Domain.DsmSystem;
using Askyl.Dsm.WebHosting.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Tools.Infrastructure;

/// <summary>
/// Reads DSM system preferences from /etc/synoinfo.conf at startup and again on <see cref="Reload"/>.
/// Throws if the file is missing or unreadable. When DSM's web configuration names its HTTPS port, DSM is
/// reached over loopback on that port; otherwise the hostname mode applies, and a missing hostname leaves
/// <see cref="Server"/> null.
/// </summary>
/// <param name="logger">Logger instance.</param>
/// <param name="fileReader">File system abstraction for reading configuration files.</param>
/// <param name="configuration">Application configuration for resolving the settings file path.</param>
public sealed class DsmSettingsService(ILogger<ILogDsmSettingsService> logger, IFileReader fileReader, IConfiguration configuration) : IDsmSettingsService
{
    // Swapped whole by Reload, so a reader sees one file's values, never a mix of two
    private volatile DsmSystemPreferences _preferences = ReadSettings(logger, fileReader, configuration);

    public string? Server => _preferences.Server;

    public int Port => _preferences.Port;

    public string Language => _preferences.Language;

    public void Reload() => _preferences = ReadSettings(logger, fileReader, configuration);

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

    // Null on a developer machine (no file) or when the file is unusable: the hostname mode then applies
    static int? ReadLoopbackPort(ILogger<ILogDsmSettingsService> logger, IFileReader fileReader, IConfiguration configuration)
    {
        var path = configuration.GetValue<string>(SystemDefaults.ConfigKeyWebConfigPath) ?? SystemDefaults.DsmWebConfigPath;

        if (!fileReader.FileExists(path))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(String.Join('\n', fileReader.ReadAllLines(path)));

            if (document.RootElement.TryGetProperty(SystemDefaults.KeyDsmWebConfigSsl, out var ssl)
                && ssl.TryGetProperty(SystemDefaults.KeyDsmWebConfigPort, out var port)
                && port.TryGetInt32(out var value)
                && value is >= 1 and <= 65535)
            {
                return value;
            }

            logger.WebConfigurationUnreadable(path, null);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            logger.WebConfigurationUnreadable(path, ex);
        }

        return null;
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

            var language = settings.TryGetValue(SystemDefaults.KeyLanguage, out var lang) && lang.Length > 0 ? lang : SystemDefaults.DefaultLanguage;

            if (ReadLoopbackPort(logger, fileReader, configuration) is { } loopbackPort)
            {
                logger.ReachingDsmOverLoopback(loopbackPort);

                return new DsmSystemPreferences(SystemDefaults.LoopbackAddress, loopbackPort, language);
            }

            // Left null rather than thrown: the connection diagnostic names what is missing
            var server = settings.TryGetValue(SystemDefaults.KeyExternalHostIp, out var host) && host.Trim() is { Length: > 0 } trimmed ? trimmed : null;

            if (server is null)
            {
                logger.MandatorySettingMissing(SystemDefaults.KeyExternalHostIp);
            }

            var port = Int32.TryParse(settings.TryGetValue(SystemDefaults.KeyExternalHttpsPort, out var p) ? p : null, out var parsedPort) ? parsedPort : SystemDefaults.DefaultHttpsPort;

            return new DsmSystemPreferences(server, port, language);
        }
        catch (Exception ex)
        {
            logger.SettingsReadFailed(ex);
            throw new InvalidOperationException($"Failed to read DSM settings from '{configPath}'.", ex);
        }
    }
}
