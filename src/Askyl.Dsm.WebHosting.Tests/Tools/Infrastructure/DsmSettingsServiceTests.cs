using Askyl.Dsm.WebHosting.Constants.DSM.System;
using Askyl.Dsm.WebHosting.Constants.Network;
using Askyl.Dsm.WebHosting.Logging;
using Askyl.Dsm.WebHosting.Tools.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;

namespace Askyl.Dsm.WebHosting.Tests.Tools.Infrastructure;

public class DsmSettingsServiceTests
{
    readonly Mock<ILogger<ILogDsmSettingsService>> _logger;
    readonly Mock<IFileReader> _fileReader;

    public DsmSettingsServiceTests()
    {
        _logger = new Mock<ILogger<ILogDsmSettingsService>>();
        _fileReader = new Mock<IFileReader>();
    }

    #region Service Construction

    [Fact]
    public void Constructor_ServiceInitializes_WithoutException()
    {
        // Arrange — config file doesn't exist, service should use defaults
        _fileReader.Setup(f => f.FileExists(SystemDefaults.SynoInfoConfPath)).Returns(false);

        // Act
        var service = new DsmSettingsService(_logger.Object, _fileReader.Object);

        // Assert — defaults
        Assert.Equal(NetworkConstants.Localhost, service.Server);
        Assert.Equal(SystemDefaults.DefaultHttpsPort, service.Port);
        Assert.Equal(SystemDefaults.DefaultLanguage, service.Language);
    }

    #endregion

    #region Valid Configuration File

    [Fact]
    public void Constructor_ValidConfig_ReturnsParsedSettings()
    {
        // Arrange
        _fileReader.Setup(f => f.FileExists(SystemDefaults.SynoInfoConfPath)).Returns(true);
        _fileReader.Setup(f => f.ReadAllLines(SystemDefaults.SynoInfoConfPath))
                   .Returns([
                       "external_host_ip=192.168.1.100",
                       "external_port_dsm_https=9001",
                       "language=fra",
                       "other_setting=value",
                   ]);

        // Act
        var service = new DsmSettingsService(_logger.Object, _fileReader.Object);

        // Assert
        Assert.Equal("192.168.1.100", service.Server);
        Assert.Equal(9001, service.Port);
        Assert.Equal("fra", service.Language);
    }

    [Fact]
    public void Constructor_ConfigWithQuotedValues_RemovesQuotes()
    {
        // Arrange
        _fileReader.Setup(f => f.FileExists(SystemDefaults.SynoInfoConfPath)).Returns(true);
        _fileReader.Setup(f => f.ReadAllLines(SystemDefaults.SynoInfoConfPath))
                   .Returns([
                       $"external_host_ip=\"192.168.1.200\"",
                       "external_port_dsm_https=443",
                       "language=\"enu\"",
                   ]);

        // Act
        var service = new DsmSettingsService(_logger.Object, _fileReader.Object);

        // Assert
        Assert.Equal("192.168.1.200", service.Server);
        Assert.Equal(443, service.Port);
        Assert.Equal("enu", service.Language);
    }

    #endregion

    #region Missing Optional Settings

    [Fact]
    public void Constructor_MissingLanguage_UsesDefault()
    {
        // Arrange
        _fileReader.Setup(f => f.FileExists(SystemDefaults.SynoInfoConfPath)).Returns(true);
        _fileReader.Setup(f => f.ReadAllLines(SystemDefaults.SynoInfoConfPath))
                   .Returns([
                       "external_host_ip=10.0.0.1",
                       "external_port_dsm_https=5001",
                   ]);

        // Act
        var service = new DsmSettingsService(_logger.Object, _fileReader.Object);

        // Assert
        Assert.Equal("10.0.0.1", service.Server);
        Assert.Equal(5001, service.Port);
        Assert.Equal(SystemDefaults.DefaultLanguage, service.Language);
    }

    [Fact]
    public void Constructor_InvalidPort_UsesDefault()
    {
        // Arrange
        _fileReader.Setup(f => f.FileExists(SystemDefaults.SynoInfoConfPath)).Returns(true);
        _fileReader.Setup(f => f.ReadAllLines(SystemDefaults.SynoInfoConfPath))
                   .Returns([
                       "external_host_ip=10.0.0.1",
                       "external_port_dsm_https=not_a_number",
                   ]);

        // Act
        var service = new DsmSettingsService(_logger.Object, _fileReader.Object);

        // Assert
        Assert.Equal(SystemDefaults.DefaultHttpsPort, service.Port);
    }

    #endregion

    #region Missing Mandatory Settings

    [Fact]
    public void Constructor_MissingServerIp_ThrowsInvalidOperationException()
    {
        // Arrange
        _fileReader.Setup(f => f.FileExists(SystemDefaults.SynoInfoConfPath)).Returns(true);
        _fileReader.Setup(f => f.ReadAllLines(SystemDefaults.SynoInfoConfPath))
                   .Returns([
                       "external_port_dsm_https=5001",
                   ]);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => new DsmSettingsService(_logger.Object, _fileReader.Object));
        Assert.Contains(SystemDefaults.KeyExternalHostIp, ex.Message);
    }

    [Fact]
    public void Constructor_EmptyServerIp_ThrowsInvalidOperationException()
    {
        // Arrange
        _fileReader.Setup(f => f.FileExists(SystemDefaults.SynoInfoConfPath)).Returns(true);
        _fileReader.Setup(f => f.ReadAllLines(SystemDefaults.SynoInfoConfPath))
                   .Returns([
                       "external_host_ip=",
                       "external_port_dsm_https=5001",
                   ]);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => new DsmSettingsService(_logger.Object, _fileReader.Object));
        Assert.Contains(SystemDefaults.KeyExternalHostIp, ex.Message);
    }

    #endregion

    #region Malformed Configuration

    [Fact]
    public void Constructor_ConfigReadThrowsException_UsesDefaults()
    {
        // Arrange
        _fileReader.Setup(f => f.FileExists(SystemDefaults.SynoInfoConfPath)).Returns(true);
        _fileReader.Setup(f => f.ReadAllLines(SystemDefaults.SynoInfoConfPath))
                   .Throws(new System.IO.IOException("Simulated read failure"));

        // Act
        var service = new DsmSettingsService(_logger.Object, _fileReader.Object);

        // Assert — falls back to defaults
        Assert.Equal(NetworkConstants.Localhost, service.Server);
        Assert.Equal(SystemDefaults.DefaultHttpsPort, service.Port);
        Assert.Equal(SystemDefaults.DefaultLanguage, service.Language);
    }

    [Fact]
    public void Constructor_MalformedLines_IgnoresLinesWithoutEquals()
    {
        // Arrange — lines without = are ignored by the parsing logic
        _fileReader.Setup(f => f.FileExists(SystemDefaults.SynoInfoConfPath)).Returns(true);
        _fileReader.Setup(f => f.ReadAllLines(SystemDefaults.SynoInfoConfPath))
                   .Returns([
                       "no_equals_here",
                       "external_host_ip=10.0.0.5",
                       "another_bad_line",
                       "external_port_dsm_https=8443",
                   ]);

        // Act
        var service = new DsmSettingsService(_logger.Object, _fileReader.Object);

        // Assert
        Assert.Equal("10.0.0.5", service.Server);
        Assert.Equal(8443, service.Port);
    }

    #endregion
}
