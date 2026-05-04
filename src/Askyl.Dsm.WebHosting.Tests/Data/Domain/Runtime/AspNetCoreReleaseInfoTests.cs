using Askyl.Dsm.WebHosting.Data.Domain.Runtime;

namespace Askyl.Dsm.WebHosting.Tests.Data.Domain.Runtime;

public class AspNetCoreReleaseInfoTests
{
    #region Constructor Null Guards

    [Fact]
    public void Constructor_NullVersion_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new AspNetCoreReleaseInfo(null!, "8.0");

        // Assert
        var ex = Assert.Throws<ArgumentNullException>(act);
        Assert.Equal("version", ex.ParamName);
    }

    [Fact]
    public void Constructor_NullProductVersion_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new AspNetCoreReleaseInfo("8.0.1", null!);

        // Assert
        var ex = Assert.Throws<ArgumentNullException>(act);
        Assert.Equal("productVersion", ex.ParamName);
    }

    [Fact]
    public void Constructor_ValidArguments_SetsProperties()
    {
        // Arrange
        var version = "8.0.1";
        var productVersion = "8.0";
        var releaseDate = DateTimeOffset.UtcNow;

        // Act
        var info = new AspNetCoreReleaseInfo(version, productVersion, releaseDate, isSecurity: true, isLts: true, AspNetCoreReleaseType.LTS);

        // Assert
        Assert.Equal(version, info.Version);
        Assert.Equal(productVersion, info.ProductVersion);
        Assert.Equal(releaseDate, info.ReleaseDate);
        Assert.True(info.IsSecurity);
        Assert.True(info.IsLts);
        Assert.Equal(AspNetCoreReleaseType.LTS, info.ReleaseType);
    }

    [Fact]
    public void Constructor_DefaultValues_AppliedCorrectly()
    {
        // Act
        var info = new AspNetCoreReleaseInfo("8.0.1", "8.0");

        // Assert
        Assert.Null(info.ReleaseDate);
        Assert.False(info.IsSecurity);
        Assert.False(info.IsLts);
        Assert.Equal(AspNetCoreReleaseType.Unknown, info.ReleaseType);
    }

    #endregion

    #region CreateChannel

    [Fact]
    public void CreateChannel_SetsEmptyVersion()
    {
        // Act
        var info = AspNetCoreReleaseInfo.CreateChannel("8.0", isLts: true, AspNetCoreReleaseType.LTS);

        // Assert
        Assert.Equal("", info.Version);
    }

    [Fact]
    public void CreateChannel_SetsProductVersion()
    {
        // Act
        var info = AspNetCoreReleaseInfo.CreateChannel("8.0", isLts: true, AspNetCoreReleaseType.LTS);

        // Assert
        Assert.Equal("8.0", info.ProductVersion);
    }

    [Fact]
    public void CreateChannel_SetsIsLts()
    {
        // Act
        var info = AspNetCoreReleaseInfo.CreateChannel("8.0", isLts: true, AspNetCoreReleaseType.LTS);

        // Assert
        Assert.True(info.IsLts);
    }

    [Fact]
    public void CreateChannel_SetsNonLts()
    {
        // Act
        var info = AspNetCoreReleaseInfo.CreateChannel("9.0", isLts: false, AspNetCoreReleaseType.STS);

        // Assert
        Assert.False(info.IsLts);
        Assert.Equal(AspNetCoreReleaseType.STS, info.ReleaseType);
    }

    [Fact]
    public void CreateChannel_NullsReleaseDateAndSecurity()
    {
        // Act
        var info = AspNetCoreReleaseInfo.CreateChannel("8.0", isLts: true, AspNetCoreReleaseType.LTS);

        // Assert
        Assert.Null(info.ReleaseDate);
        Assert.False(info.IsSecurity);
    }

    #endregion
}
