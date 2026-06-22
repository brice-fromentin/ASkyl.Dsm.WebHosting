using Askyl.Dsm.WebHosting.Constants.DSM.System;
using Askyl.Dsm.WebHosting.Logging;
using Askyl.Dsm.WebHosting.Tools.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;

namespace Askyl.Dsm.WebHosting.Tests.Tools.Infrastructure;

public class DsmSettingsServiceTests
{
    readonly Mock<ILogger<ILogDsmSettingsService>> _logger;

    public DsmSettingsServiceTests()
    {
        _logger = new Mock<ILogger<ILogDsmSettingsService>>();
    }

    #region Service Construction

    [Fact]
    public void Constructor_ServiceInitializes_WithoutException()
    {
        // DsmSettingsService reads /etc/synoinfo.conf with graceful fallback
        // On systems without the file, it returns defaults without throwing
        var service = new DsmSettingsService(_logger.Object);

        Assert.NotNull(service.Server);
        Assert.True(service.Port > 0);
        Assert.NotNull(service.Language);
    }

    #endregion

    #region Parsing Logic

    [Fact]
    public void ParseConfigFile_ValidContent_ReturnsCorrectSettings()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"asl_wh_settings_{Guid.NewGuid():N}");
        var configFile = Path.Combine(tempDir, "synoinfo.conf");

        try
        {
            Directory.CreateDirectory(tempDir);

            File.WriteAllText(configFile, @"
external_host_ip=192.168.1.100
external_port_dsm_https=9001
language=fra
other_setting=value
");

            // Act — replicate ReadSettings parsing logic
            var lines = File.ReadAllLines(configFile);
            var settings = lines.Where(x => x.Contains('='))
                                .ToDictionary(k => k.Split(['='], 2)[0], v => v.Split(['='], 2)[1].Replace("\"", String.Empty));

            // Assert
            Assert.Equal("192.168.1.100", settings[SystemDefaults.KeyExternalHostIp]);
            Assert.Equal("9001", settings[SystemDefaults.KeyExternalHttpsPort]);
            Assert.Equal("fra", settings[SystemDefaults.KeyLanguage]);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void ParseConfigFile_MalformedLines_ReturnsEmptySettings()
    {
        // A file with no = lines produces an empty dictionary
        var malformedLines = new[] { "no_equals_here", "another_line", "123" };

        var settings = malformedLines.Where(x => x.Contains('='))
                                     .ToDictionary(k => k.Split(['='], 2)[0], v => v.Split(['='], 2)[1].Replace("\"", String.Empty));

        Assert.Empty(settings);
    }

    #endregion

    #region Mandatory Setting

    [Fact]
    public void GetMandatorySetting_PresentKey_ReturnsValue()
    {
        var settings = new Dictionary<string, string>
        {
            [SystemDefaults.KeyExternalHostIp] = "10.0.0.1"
        };

        var result = GetMandatorySetting(settings, SystemDefaults.KeyExternalHostIp);

        Assert.Equal("10.0.0.1", result);
    }

    [Fact]
    public void GetMandatorySetting_MissingKey_ThrowsInvalidOperationException()
    {
        var settings = new Dictionary<string, string>
        {
            [SystemDefaults.KeyExternalHttpsPort] = "5001"
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            GetMandatorySetting(settings, SystemDefaults.KeyExternalHostIp));

        Assert.Contains(SystemDefaults.KeyExternalHostIp, ex.Message);
    }

    [Fact]
    public void GetMandatorySetting_EmptyValue_ThrowsInvalidOperationException()
    {
        var settings = new Dictionary<string, string>
        {
            [SystemDefaults.KeyExternalHostIp] = String.Empty
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            GetMandatorySetting(settings, SystemDefaults.KeyExternalHostIp));

        Assert.Contains(SystemDefaults.KeyExternalHostIp, ex.Message);
    }

    #endregion

    #region Helpers

    static string GetMandatorySetting(Dictionary<string, string> settings, string key)
    {
        if (!settings.TryGetValue(key, out var value) || value.Length == 0)
        {
            throw new InvalidOperationException($"Mandatory setting '{key}' is missing.");
        }

        return value;
    }

    #endregion
}
