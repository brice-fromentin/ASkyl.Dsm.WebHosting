using Askyl.Dsm.WebHosting.Constants.DSM.System;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Logging;
using Askyl.Dsm.WebHosting.Tools.Infrastructure;
using Askyl.Dsm.WebHosting.Ui.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Askyl.Dsm.WebHosting.Tests.Ui.Services;

public sealed class DsmWebConfigurationWatcherTests : IDisposable
{
    readonly string _directory = Directory.CreateTempSubdirectory("adwh-dsm-web-config-").FullName;

    string WebConfigPath => Path.Combine(_directory, "DSM.json");

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    static string WebConfig(int port) => $$"""{ "ssl": { "port": {{port}} } }""";

    IConfiguration Configuration(string webConfigPath) => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        [SystemDefaults.ConfigKeyWebConfigPath] = webConfigPath,
        [ApplicationHostFactory.ConfigPathSettingKey] = Path.Combine(_directory, "synoinfo.conf")
    }).Build();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AChangedFile_ReloadsTheSettings(bool replaced)
    {
        File.WriteAllText(WebConfigPath, WebConfig(5001));

        var configuration = Configuration(WebConfigPath);
        var settings = new DsmSettingsService(NullLogger<ILogDsmSettingsService>.Instance, new SystemFileReader(), configuration);
        using var watcher = new DsmWebConfigurationWatcher(settings, configuration, NullLogger<ILogDsmSettingsService>.Instance);

        await watcher.StartAsync(CancellationToken.None);
        Assert.Equal(5001, settings.Port);

        if (replaced)
        {
            var staged = WebConfigPath + ".new";

            File.WriteAllText(staged, WebConfig(5443));
            File.Move(staged, WebConfigPath, overwrite: true);
        }
        else
        {
            File.WriteAllText(WebConfigPath, WebConfig(5443));
        }

        // The watcher reports asynchronously: wait for the reload, bounded, rather than for a fixed delay
        var deadline = DateTime.UtcNow.AddSeconds(10);

        while (settings.Port != 5443 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50);
        }

        Assert.Equal(5443, settings.Port);
    }

    [Fact]
    public async Task AMissingDirectory_WatchesNothing()
    {
        using var watcher = new DsmWebConfigurationWatcher(Mock.Of<IDsmSettingsService>(), Configuration(Path.Combine(_directory, "missing", "DSM.json")), NullLogger<ILogDsmSettingsService>.Instance);

        // A FileSystemWatcher throws on a missing directory, which would stop the host on a developer machine
        await watcher.StartAsync(CancellationToken.None);
    }
}
