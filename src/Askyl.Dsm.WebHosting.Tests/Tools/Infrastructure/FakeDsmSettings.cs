using Askyl.Dsm.WebHosting.Constants.DSM.System;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Logging;
using Askyl.Dsm.WebHosting.Tools.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Askyl.Dsm.WebHosting.Tests.Tools.Infrastructure;

/// <summary>
/// Builds a <see cref="DsmSettingsService"/> over an in-memory synoinfo.conf.
/// Tests must never construct it with <see cref="SystemFileReader"/>: that reads the host's real
/// /etc/synoinfo.conf, which is absent on clean machines and CI, and carries live NAS details when present.
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
        fileReader.Setup(f => f.ReadAllLines(SystemDefaults.SynoInfoConfPath))
                  .Returns([
                      $"{SystemDefaults.KeyExternalHostIp}=\"{Server}\"",
                      $"{SystemDefaults.KeyExternalHttpsPort}=\"{Port}\"",
                      $"{SystemDefaults.KeyLanguage}=\"{Language}\""
                  ]);

        return new DsmSettingsService(new Mock<ILogger<ILogDsmSettingsService>>().Object, fileReader.Object, new ConfigurationBuilder().Build());
    }
}
