using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Constants.DSM.System;
using Askyl.Dsm.WebHosting.Data.DsmApi.Parameters.Info;
using Askyl.Dsm.WebHosting.Logging;
using Askyl.Dsm.WebHosting.Tools.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Askyl.Dsm.WebHosting.Tests.Tools.Infrastructure;

public class DsmSettingsServiceTests
{
    readonly Mock<ILogger<ILogDsmSettingsService>> _logger = new();
    readonly Mock<IFileReader> _fileReader = new();

    DsmSettingsService Create(string? dsmUrl = null)
        => new(_logger.Object, _fileReader.Object, new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [ApplicationConstants.DsmUrlEnvironmentVariable] = dsmUrl })
            .Build());

    void WithSynoInfo(params string[] lines)
    {
        _fileReader.Setup(f => f.FileExists(SystemDefaults.SynoInfoConfPath)).Returns(true);
        _fileReader.Setup(f => f.ReadAllLines(SystemDefaults.SynoInfoConfPath)).Returns(lines);
    }

    void WithWebConfig(string json)
    {
        _fileReader.Setup(f => f.FileExists(SystemDefaults.DsmWebConfigPath)).Returns(true);
        _fileReader.Setup(f => f.ReadAllLines(SystemDefaults.DsmWebConfigPath)).Returns([json]);
    }

    #region Language

    [Fact]
    public void Constructor_MissingSynoInfo_UsesTheDefaultLanguage()
    {
        // A developer machine has no synoinfo.conf; the host used to refuse to start without one
        var service = Create();

        Assert.Equal(SystemDefaults.DefaultLanguage, service.Language);
    }

    [Fact]
    public void Constructor_UnreadableSynoInfo_UsesTheDefaultLanguage()
    {
        _fileReader.Setup(f => f.FileExists(SystemDefaults.SynoInfoConfPath)).Returns(true);
        _fileReader.Setup(f => f.ReadAllLines(SystemDefaults.SynoInfoConfPath)).Throws(new IOException("Simulated read failure"));

        var service = Create();

        Assert.Equal(SystemDefaults.DefaultLanguage, service.Language);
    }

    [Fact]
    public void Constructor_SynoInfoLanguage_IsReadWithoutQuotes()
    {
        WithSynoInfo("no_equals_here", "language=\"fra\"", "other_setting=value");

        Assert.Equal("fra", Create().Language);
    }

    [Fact]
    public void Constructor_SynoInfoWithoutLanguage_UsesTheDefaultLanguage()
    {
        WithSynoInfo("other_setting=value");

        Assert.Equal(SystemDefaults.DefaultLanguage, Create().Language);
    }

    #endregion

    #region Loopback

    [Fact]
    public void Constructor_WebConfigPresent_ReachesDsmOverLoopbackOnItsPort()
    {
        WithSynoInfo("language=fra");
        _fileReader.Setup(f => f.FileExists(SystemDefaults.DsmWebConfigPath)).Returns(true);
        _fileReader.Setup(f => f.ReadAllLines(SystemDefaults.DsmWebConfigPath)).Returns(["{\"port\":5000,", "\"ssl\":{\"port\":5443}}"]);

        var service = Create();

        Assert.Equal("127.0.0.1", service.Server);
        Assert.Equal(5443, service.Port);
        Assert.Equal("fra", service.Language);
    }

    [Fact]
    public void Constructor_WebConfigAbsent_UsesDsmsDefaultPort()
    {
        var service = Create();

        Assert.Equal("127.0.0.1", service.Server);
        Assert.Equal(5001, service.Port);
    }

    [Theory]
    [InlineData("{")]
    [InlineData("[]")]
    [InlineData("{\"ssl\":5443}")]
    [InlineData("{\"ssl\":{\"port\":0}}")]
    [InlineData("{\"ssl\":{\"port\":70000}}")]
    [InlineData("{\"ssl\":{\"port\":\"abc\"}}")]
    [InlineData("{\"port\":5000}")]
    public void Constructor_UnusableWebConfig_UsesDsmsDefaultPort(string json)
    {
        WithWebConfig(json);

        var service = Create();

        Assert.Equal("127.0.0.1", service.Server);
        Assert.Equal(5001, service.Port);
    }

    [Fact]
    public void Reload_PicksUpAPortChangedInDsm()
    {
        _fileReader.Setup(f => f.FileExists(SystemDefaults.DsmWebConfigPath)).Returns(true);
        _fileReader.SetupSequence(f => f.ReadAllLines(SystemDefaults.DsmWebConfigPath))
                   .Returns(["{\"ssl\":{\"port\":5001}}"])
                   .Returns(["{\"ssl\":{\"port\":5443}}"]);

        var service = Create();

        service.Reload();

        Assert.Equal(5443, service.Port);
    }

    #endregion

    #region ADWH_DSM_URL

    [Fact]
    public void Constructor_DsmUrl_ReachesThatHostAndPort()
    {
        // The URL wins even on a machine whose DSM.json would give loopback
        WithWebConfig("{\"ssl\":{\"port\":5001}}");

        var service = Create("https://dsm.test:5443");

        Assert.Equal("dsm.test", service.Server);
        Assert.Equal(5443, service.Port);
    }

    [Fact]
    public void Constructor_DsmUrlWithAnIpv6Literal_BuildsAValidApiUrl()
    {
        var service = Create("https://[::1]:5443");

        var url = new Uri(new InformationsQueryParameters().BuildUrl(service.Server, service.Port, "query.cgi"));

        Assert.Equal("[::1]", url.Host);
        Assert.Equal(5443, url.Port);
    }

    [Theory]
    [InlineData("http://dsm.test:5000")]
    [InlineData("dsm.test:5001")]
    [InlineData("/webapi")]
    [InlineData("not a url")]
    public void Constructor_DsmUrlThatIsNotAnAbsoluteHttpsUrl_Throws(string url)
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Create(url));

        Assert.Contains(ApplicationConstants.DsmUrlEnvironmentVariable, ex.Message, StringComparison.Ordinal);
    }

    #endregion
}
