using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Constants.DSM.System;
using Askyl.Dsm.WebHosting.Logging;
using Askyl.Dsm.WebHosting.Tools.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Askyl.Dsm.WebHosting.Tests.Tools.Infrastructure;

/// <summary>
/// Builds a <see cref="DsmSettingsService"/> over an in-memory synoinfo.conf and a synthetic ADWH_DSM_URL.
/// Tests must never construct it with <see cref="SystemFileReader"/> or the process environment: those read
/// the machine's real files and a developer's real ADWH_DSM_URL, which carry live NAS details when present.
/// </summary>
internal static class FakeDsmSettings
{
    internal const string Server = "dsm.test";
    internal const int Port = 5001;
    internal const string Language = "enu";

    /// <summary>
    /// Creates a settings service returning fixed, synthetic DSM preferences.
    /// </summary>
    internal static DsmSettingsService Create()
    {
        var fileReader = new Mock<IFileReader>();
        fileReader.Setup(f => f.FileExists(SystemDefaults.SynoInfoConfPath)).Returns(true);
        fileReader.Setup(f => f.ReadAllLines(SystemDefaults.SynoInfoConfPath)).Returns([$"{SystemDefaults.KeyLanguage}=\"{Language}\""]);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [ApplicationConstants.DsmUrlEnvironmentVariable] = $"https://{Server}:{Port}" })
            .Build();

        return new DsmSettingsService(new Mock<ILogger<ILogDsmSettingsService>>().Object, fileReader.Object, configuration);
    }
}
