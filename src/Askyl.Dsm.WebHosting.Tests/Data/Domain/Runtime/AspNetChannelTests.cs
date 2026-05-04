using Askyl.Dsm.WebHosting.Data.Domain.Runtime;

namespace Askyl.Dsm.WebHosting.Tests.Data.Domain.Runtime;

public class AspNetChannelTests
{
    #region Constructor

    [Fact]
    public void Constructor_NullReleaseInfo_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new AspNetChannel(null!);

        // Assert
        var ex = Assert.Throws<ArgumentNullException>(act);
        Assert.Equal("releaseInfo", ex.ParamName);
    }

    [Fact]
    public void Constructor_ValidReleaseInfo_SetsReleaseInfo()
    {
        // Arrange
        var releaseInfo = CreateReleaseInfo("8.0.1", "8.0", isLts: true);

        // Act
        var channel = new AspNetChannel(releaseInfo);

        // Assert
        Assert.Same(releaseInfo, channel.ReleaseInfo);
    }

    #endregion

    #region FromReleaseInfo

    [Fact]
    public void FromReleaseInfo_ValidReleaseInfo_ReturnsChannel()
    {
        // Arrange
        var releaseInfo = CreateReleaseInfo("8.0.1", "8.0", isLts: true);

        // Act
        var channel = AspNetChannel.FromReleaseInfo(releaseInfo);

        // Assert
        Assert.Same(releaseInfo, channel.ReleaseInfo);
    }

    [Fact]
    public void FromReleaseInfo_NullReleaseInfo_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => AspNetChannel.FromReleaseInfo(null!);

        // Assert
        var ex = Assert.Throws<ArgumentNullException>(act);
        Assert.Equal("releaseInfo", ex.ParamName);
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_LtsChannel_AppendsLtsSuffix()
    {
        // Arrange
        var releaseInfo = CreateReleaseInfo("8.0.1", "8.0", isLts: true);
        var channel = new AspNetChannel(releaseInfo);

        // Act
        var result = channel.ToString();

        // Assert
        Assert.Equal("8.0 (LTS)", result);
    }

    [Fact]
    public void ToString_NonLtsChannel_OmitsLtsSuffix()
    {
        // Arrange
        var releaseInfo = CreateReleaseInfo("9.0.1", "9.0", isLts: false);
        var channel = new AspNetChannel(releaseInfo);

        // Act
        var result = channel.ToString();

        // Assert
        Assert.Equal("9.0", result);
    }

    #endregion

    #region Convenience Properties

    [Fact]
    public void ConvenienceProperties_DelegatesToReleaseInfo()
    {
        // Arrange
        var releaseInfo = CreateReleaseInfo("8.0.1", "8.0", isLts: true, isSecurity: true, releaseType: AspNetCoreReleaseType.LTS);
        var channel = new AspNetChannel(releaseInfo);

        // Assert
        Assert.Equal("8.0", channel.ProductVersion);
        Assert.Equal("8.0.1", channel.Version);
        Assert.True(channel.IsSecurity);
        Assert.True(channel.IsLts);
        Assert.Equal(AspNetCoreReleaseType.LTS, channel.ReleaseType);
        Assert.Equal(releaseInfo.ReleaseDate, channel.ReleaseDate);
    }

    #endregion

    #region Helpers

    private static AspNetCoreReleaseInfo CreateReleaseInfo(
        string version,
        string productVersion,
        bool isLts = false,
        bool isSecurity = false,
        AspNetCoreReleaseType releaseType = AspNetCoreReleaseType.Unknown)
    {
        return new(version, productVersion, DateTimeOffset.UtcNow, isSecurity, isLts, releaseType);
    }

    #endregion
}
