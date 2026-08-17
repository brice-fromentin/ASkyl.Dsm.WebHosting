using Askyl.Dsm.WebHosting.Constants.DSM.System;
using Askyl.Dsm.WebHosting.Logging;
using Askyl.Dsm.WebHosting.Tests.Tools.Infrastructure;
using Askyl.Dsm.WebHosting.Ui.Components;
using Askyl.Dsm.WebHosting.Ui.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Tests.Ui;

/// <summary>
/// Boots the real <c>Program.cs</c> in memory: the actual service registrations, the actual middleware
/// pipeline, the actual configuration providers. Every other gate in this repository is syntactic and
/// passes whatever the application does at startup; this one fails when the host cannot come up.
/// </summary>
/// <remarks>
/// All mutable state is redirected into one throwaway directory, so a test run neither reads the host's
/// real <c>/etc/synoinfo.conf</c> — absent on CI, and carrying live NAS details when present — nor writes
/// a <c>websites.json</c> next to the test binaries.
/// </remarks>
/// <remarks>
/// The type argument is <see cref="App"/> rather than <c>Program</c> because both the host and the
/// WebAssembly client declare a top-level <c>Program</c>, which is ambiguous from this assembly.
/// <see cref="WebApplicationFactory{TEntryPoint}"/> only uses the argument to locate the entry-point
/// assembly, so any public type from the host project serves.
/// </remarks>
public sealed class ApplicationHostFactory : WebApplicationFactory<App>
{
    #region Constants

    /// <summary>
    /// Deliberately not Development: that branch calls UseWebAssemblyDebugging, which starts a debug
    /// proxy that has no business running inside a test process.
    /// </summary>
    const string TestingEnvironmentName = "Testing";

    const string ConfigPathSettingKey = "DsmSettings:ConfigPath";

    #endregion

    #region Fields

    readonly string _stateDirectory = Path.Combine(Path.GetTempPath(), $"adwh-runtime-gate-{Guid.NewGuid():N}");

    #endregion

    #region Properties

    /// <summary>
    /// Absolute path of the synthetic DSM settings file this factory writes for the host to read.
    /// </summary>
    public string SynoInfoPath => Path.Combine(_stateDirectory, Path.GetFileName(SystemDefaults.SynoInfoConfPath));

    #endregion

    #region Host Configuration

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        WriteSyntheticDsmSettings();

        builder.UseEnvironment(TestingEnvironmentName);

        // Added last so it wins over appsettings.json, which points at /etc/synoinfo.conf.
        builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(
            new Dictionary<string, string?> { [ConfigPathSettingKey] = SynoInfoPath }));

        builder.ConfigureServices(services =>
        {
            // The hosted WebSiteHostingService starts every site the configuration lists. Pointing it at
            // an empty directory keeps the gate from spawning real processes.
            services.RemoveAll<WebSitesConfigurationService>();
            services.AddSingleton(provider => new WebSitesConfigurationService(
                provider.GetRequiredService<ILogger<ILogWebSitesConfigurationService>>(), _stateDirectory));
        });
    }

    void WriteSyntheticDsmSettings()
    {
        Directory.CreateDirectory(_stateDirectory);

        File.WriteAllLines(SynoInfoPath,
        [
            $"{SystemDefaults.KeyExternalHostIp}=\"{FakeDsmSettings.Server}\"",
            $"{SystemDefaults.KeyExternalHttpsPort}=\"{FakeDsmSettings.Port}\"",
            $"{SystemDefaults.KeyLanguage}=\"{FakeDsmSettings.Language}\""
        ]);
    }

    #endregion

    #region Cleanup

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && Directory.Exists(_stateDirectory))
        {
            Directory.Delete(_stateDirectory, recursive: true);
        }
    }

    #endregion
}
