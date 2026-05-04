using Askyl.Dsm.WebHosting.Data.Domain.Runtime;

namespace Askyl.Dsm.WebHosting.Tests.Data.Domain.Runtime;

public class AspNetReleaseTests
{
    #region Create Factory

    [Fact]
    public void Create_MapsPropertiesFromReleaseInfo()
    {
        // Arrange
        var releaseDate = DateTimeOffset.UtcNow;
        var releaseInfo = new AspNetCoreReleaseInfo("8.0.1", "8.0", releaseDate, isSecurity: true, isLts: true, AspNetCoreReleaseType.LTS);

        // Act
        var release = AspNetRelease.Create(releaseInfo);

        // Assert
        Assert.Equal("8.0.1", release.Version);
        Assert.True(release.IsSecurity);
        Assert.Equal(releaseDate, release.ReleaseDate);
    }

    [Fact]
    public void Create_IsInstalled_DefaultsToFalse()
    {
        // Arrange
        var releaseInfo = new AspNetCoreReleaseInfo("8.0.1", "8.0");

        // Act
        var release = AspNetRelease.Create(releaseInfo);

        // Assert
        Assert.False(release.IsInstalled);
    }

    [Fact]
    public void Create_IsInstalled_True_WhenPassed()
    {
        // Arrange
        var releaseInfo = new AspNetCoreReleaseInfo("8.0.1", "8.0");

        // Act
        var release = AspNetRelease.Create(releaseInfo, isInstalled: true);

        // Assert
        Assert.True(release.IsInstalled);
    }

    #endregion
}
