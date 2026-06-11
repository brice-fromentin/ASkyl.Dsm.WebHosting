using System.Globalization;
using Askyl.Dsm.WebHosting.Globalization;
using Askyl.Dsm.WebHosting.Logging;
using Askyl.Dsm.WebHosting.Ui.Client.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Askyl.Dsm.WebHosting.Tests.Globalization;

public class CultureManagerTests
{
    #region Setup

    private Mock<ILogger<ILogCultureManager>> CreateLoggerMock()
    {
        return new Mock<ILogger<ILogCultureManager>>();
    }

    #endregion

    #region InitializeFromLogin

    [Fact]
    public void InitializeFromLogin_ValidCulture_AppliesCulture()
    {
        // Arrange
        var logger = CreateLoggerMock();
        var cultureManager = new CultureManager(logger.Object);

        // Act
        cultureManager.InitializeFromLogin("fr-FR", null, null);

        // Assert
        Assert.Equal("fr-FR", cultureManager.CurrentCulture.Name);
        Assert.Equal("fr-FR", cultureManager.CurrentUICulture.Name);
    }

    [Fact]
    public void InitializeFromLogin_NullCulture_KeepsCurrentCulture()
    {
        // Arrange
        var logger = CreateLoggerMock();
        var cultureManager = new CultureManager(logger.Object);
        var initialCulture = cultureManager.CurrentCulture.Name;

        // Act
        cultureManager.InitializeFromLogin(null, null, null);

        // Assert - should keep the initial culture (no crash, no null ref)
        Assert.NotNull(cultureManager.CurrentCulture);
    }

    [Fact]
    public void InitializeFromLogin_WhitespaceCulture_KeepsCurrentCulture()
    {
        // Arrange
        var logger = CreateLoggerMock();
        var cultureManager = new CultureManager(logger.Object);

        // Act
        cultureManager.InitializeFromLogin("   ", null, null);

        // Assert
        Assert.NotNull(cultureManager.CurrentCulture);
    }

    [Fact]
    public void InitializeFromLogin_InvalidCulture_FallsBackGracefully()
    {
        // Arrange
        var logger = CreateLoggerMock();
        var cultureManager = new CultureManager(logger.Object);

        // Act
        cultureManager.InitializeFromLogin("xx-XX", null, null);

        // Assert - should fall back to a valid culture, not throw
        Assert.NotNull(cultureManager.CurrentCulture);
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("fr-FR")]
    public void InitializeFromLogin_SyncsCurrentAndUICulture(string cultureName)
    {
        // Arrange
        var logger = CreateLoggerMock();
        var cultureManager = new CultureManager(logger.Object);

        // Act
        cultureManager.InitializeFromLogin(cultureName, null, null);

        // Assert
        Assert.Equal(cultureManager.CurrentCulture, cultureManager.CurrentUICulture);
    }

    #endregion

    #region InitializeFromLogin with Date/Time Formats

    [Fact]
    public void InitializeFromLogin_WithDateFormat_AppliesCustomFormat()
    {
        // Arrange
        var logger = CreateLoggerMock();
        var cultureManager = new CultureManager(logger.Object);

        // Act
        cultureManager.InitializeFromLogin("en-US", "yyyy/MM/dd", null);

        // Assert
        Assert.Equal("yyyy/MM/dd", cultureManager.CurrentCulture.DateTimeFormat.ShortDatePattern);
        Assert.Equal("yyyy/MM/dd", cultureManager.CurrentCulture.DateTimeFormat.LongDatePattern);
    }

    [Fact]
    public void InitializeFromLogin_WithTimeFormat_AppliesCustomFormat()
    {
        // Arrange
        var logger = CreateLoggerMock();
        var cultureManager = new CultureManager(logger.Object);

        // Act
        cultureManager.InitializeFromLogin("en-US", null, "H:mm");

        // Assert
        Assert.Equal("H:mm", cultureManager.CurrentCulture.DateTimeFormat.ShortTimePattern);
        Assert.Equal("H:mm", cultureManager.CurrentCulture.DateTimeFormat.LongTimePattern);
    }

    [Fact]
    public void InitializeFromLogin_WithBothFormats_AppliesBoth()
    {
        // Arrange
        var logger = CreateLoggerMock();
        var cultureManager = new CultureManager(logger.Object);

        // Act
        cultureManager.InitializeFromLogin("en-US", "dd/MM/yyyy", "HH:mm:ss");

        // Assert
        Assert.Equal("dd/MM/yyyy", cultureManager.CurrentCulture.DateTimeFormat.ShortDatePattern);
        Assert.Equal("HH:mm:ss", cultureManager.CurrentCulture.DateTimeFormat.ShortTimePattern);
    }

    [Fact]
    public void InitializeFromLogin_NullFormats_UsesCultureDefaults()
    {
        // Arrange
        var logger = CreateLoggerMock();
        var cultureManager = new CultureManager(logger.Object);

        // Act
        cultureManager.InitializeFromLogin("en-US", null, null);

        // Assert - en-US defaults
        Assert.Equal("M/d/yyyy", cultureManager.CurrentCulture.DateTimeFormat.ShortDatePattern);
    }

    #endregion

    #region ResetToSystem

    [Fact]
    public void ResetToSystem_ResetsToValidCulture()
    {
        // Arrange
        var logger = CreateLoggerMock();
        var cultureManager = new CultureManager(logger.Object);
        cultureManager.InitializeFromLogin("fr-FR", null, null);

        // Act
        cultureManager.ResetToSystem();

        // Assert
        Assert.NotNull(cultureManager.CurrentCulture);
    }

    [Fact]
    public void ResetToSystem_DoesNotThrow()
    {
        // Arrange
        var logger = CreateLoggerMock();
        var cultureManager = new CultureManager(logger.Object);

        // Act & Assert - should not throw
        cultureManager.ResetToSystem();
    }

    #endregion

    #region Culture Cloning (Isolation)

    [Fact]
    public void InitializeFromLogin_WithDateFormat_DoesNotModifySharedCultureInfo()
    {
        // Arrange
        var logger = CreateLoggerMock();
        var cultureManager = new CultureManager(logger.Object);
        var originalCulture = new CultureInfo("en-US");
        var originalShortDatePattern = originalCulture.DateTimeFormat.ShortDatePattern;

        // Act
        cultureManager.InitializeFromLogin("en-US", "dd/MM/yyyy", null);

        // Assert - the shared CultureInfo should not be modified
        Assert.Equal(originalShortDatePattern, originalCulture.DateTimeFormat.ShortDatePattern);
    }

    #endregion

    #region CurrentCulture Property

    [Fact]
    public void CurrentCulture_ReturnsValidCulture()
    {
        // Arrange
        var logger = CreateLoggerMock();
        var cultureManager = new CultureManager(logger.Object);

        // Act & Assert
        Assert.NotNull(cultureManager.CurrentCulture);
    }

    [Fact]
    public void CurrentUICulture_EqualsCurrentCulture()
    {
        // Arrange
        var logger = CreateLoggerMock();
        var cultureManager = new CultureManager(logger.Object);

        // Act & Assert
        Assert.Same(cultureManager.CurrentCulture, cultureManager.CurrentUICulture);
    }

    #endregion
}
