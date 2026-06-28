using System.Globalization;
using Askyl.Dsm.WebHosting.Logging;
using Askyl.Dsm.WebHosting.Ui.Client.Services;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Moq;

namespace Askyl.Dsm.WebHosting.Tests.Globalization;

public class CultureManagerTests
{
    #region Setup

    private Mock<IJSRuntime> CreateJsRuntimeMock()
    {
        return new Mock<IJSRuntime>();
    }

    private Mock<ILogger<ILogCultureManager>> CreateLoggerMock()
    {
        return new Mock<ILogger<ILogCultureManager>>();
    }

    #endregion

    #region InitializeFromLogin

    [Theory]
    [InlineData("en-US")]
    [InlineData("fr-FR")]
    public void InitializeFromLogin_ValidCulture_AppliesAndSyncsCulture(string cultureName)
    {
        // Arrange
        var jsRuntime = CreateJsRuntimeMock();
        var logger = CreateLoggerMock();
        var cultureManager = new CultureManager(jsRuntime.Object, logger.Object);

        // Act
        cultureManager.InitializeFromLogin(cultureName, null, null);

        // Assert
        Assert.Equal(cultureName, cultureManager.CurrentCulture.Name);
        Assert.Equal(cultureName, cultureManager.CurrentUICulture.Name);
        Assert.Same(cultureManager.CurrentCulture, cultureManager.CurrentUICulture);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("xx-XX")]
    public void InitializeFromLogin_InvalidInput_FallsBackGracefully(string? cultureName)
    {
        // Arrange
        var jsRuntime = CreateJsRuntimeMock();
        var logger = CreateLoggerMock();
        var cultureManager = new CultureManager(jsRuntime.Object, logger.Object);

        // Act
        cultureManager.InitializeFromLogin(cultureName, null, null);

        // Assert - should fall back to a valid culture, not throw
        Assert.NotNull(cultureManager.CurrentCulture);
    }

    #endregion

    #region InitializeFromLogin with Date/Time Formats

    [Theory]
    [InlineData("yyyy/MM/dd", null, "yyyy/MM/dd", null)]
    [InlineData(null, "H:mm", null, "H:mm")]
    [InlineData("dd/MM/yyyy", "HH:mm:ss", "dd/MM/yyyy", "HH:mm:ss")]
    public void InitializeFromLogin_ApplyFormats(string? dateFormat, string? timeFormat, string? expectedDatePattern, string? expectedTimePattern)
    {
        // Arrange
        var jsRuntime = CreateJsRuntimeMock();
        var logger = CreateLoggerMock();
        var cultureManager = new CultureManager(jsRuntime.Object, logger.Object);

        // Act
        cultureManager.InitializeFromLogin("en-US", dateFormat, timeFormat);

        // Assert
        if (expectedDatePattern != null)
        {
            Assert.Equal(expectedDatePattern, cultureManager.CurrentCulture.DateTimeFormat.ShortDatePattern);
        }

        if (expectedTimePattern != null)
        {
            Assert.Equal(expectedTimePattern, cultureManager.CurrentCulture.DateTimeFormat.ShortTimePattern);
        }
    }

    [Fact]
    public void InitializeFromLogin_NullFormats_UsesCultureDefaults()
    {
        // Arrange
        var jsRuntime = CreateJsRuntimeMock();
        var logger = CreateLoggerMock();
        var cultureManager = new CultureManager(jsRuntime.Object, logger.Object);

        // Act
        cultureManager.InitializeFromLogin("en-US", null, null);

        // Assert — patterns are non-empty culture defaults, not null
        Assert.NotEmpty(cultureManager.CurrentCulture.DateTimeFormat.ShortDatePattern);
        Assert.NotEmpty(cultureManager.CurrentCulture.DateTimeFormat.ShortTimePattern);
    }

    #endregion

    #region ResetToSystem

    [Fact]
    public void ResetToSystem_ResetsToValidCulture()
    {
        // Arrange
        var jsRuntime = CreateJsRuntimeMock();
        var logger = CreateLoggerMock();
        var cultureManager = new CultureManager(jsRuntime.Object, logger.Object);
        cultureManager.InitializeFromLogin("fr-FR", null, null);

        // Act
        cultureManager.ResetToSystem();

        // Assert — resets without throwing and returns to a valid culture
        Assert.NotNull(cultureManager.CurrentCulture);
    }

    #endregion

    #region Culture Cloning (Isolation)

    [Fact]
    public void InitializeFromLogin_WithDateFormat_DoesNotModifySharedCultureInfo()
    {
        // Arrange
        var jsRuntime = CreateJsRuntimeMock();
        var logger = CreateLoggerMock();
        var cultureManager = new CultureManager(jsRuntime.Object, logger.Object);
        var originalCulture = new CultureInfo("en-US");
        var originalShortDatePattern = originalCulture.DateTimeFormat.ShortDatePattern;

        // Act
        cultureManager.InitializeFromLogin("en-US", "dd/MM/yyyy", null);

        // Assert - the shared CultureInfo should not be modified
        Assert.Equal(originalShortDatePattern, originalCulture.DateTimeFormat.ShortDatePattern);
    }

    #endregion

    #region Null/Empty/Invalid Format Handling

    [Theory]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData(null, "")]
    [InlineData(null, "   ")]
    public void InitializeFromLogin_NullOrWhitespaceFormat_KeepsCultureDefault(string? dateFormat, string? timeFormat)
    {
        // Arrange
        var jsRuntime = CreateJsRuntimeMock();
        var logger = CreateLoggerMock();
        var cultureManager = new CultureManager(jsRuntime.Object, logger.Object);
        var originalCulture = new CultureInfo("en-US");

        // Act — null/whitespace is treated as absent by IsNullOrWhiteSpace guard
        cultureManager.InitializeFromLogin("en-US", dateFormat, timeFormat);

        // Assert — format is ignored, culture keeps its default pattern
        Assert.Equal(originalCulture.DateTimeFormat.ShortDatePattern, cultureManager.CurrentCulture.DateTimeFormat.ShortDatePattern);
        Assert.Equal(originalCulture.DateTimeFormat.ShortTimePattern, cultureManager.CurrentCulture.DateTimeFormat.ShortTimePattern);
    }

    [Theory]
    [InlineData("@@@")]
    [InlineData("xx-YY")]
    [InlineData("H:mm:zz")]
    [InlineData("invalid-format-string")]
    public void InitializeFromLogin_ArbitraryDateFormat_HandlesGracefully(string dateFormat)
    {
        // Arrange
        var jsRuntime = CreateJsRuntimeMock();
        var logger = CreateLoggerMock();
        var cultureManager = new CultureManager(jsRuntime.Object, logger.Object);

        // Act — arbitrary format strings should not throw
        // Note: DateTimeFormatInfo.ShortDatePattern is very permissive in .NET and accepts
        // most strings as custom format patterns. The try/catch in CloneCultureWithFormats
        // is defensive against theoretical edge cases (FormatException/NotSupportedException)
        // that are not triggerable with standard format strings in unit tests.
        cultureManager.InitializeFromLogin("en-US", dateFormat, null);

        // Assert — method completes without throwing, format is applied (or kept as default if rejected)
        Assert.NotNull(cultureManager.CurrentCulture);
    }

    [Theory]
    [InlineData("@@@")]
    [InlineData("xx-YY")]
    [InlineData("H:mm:zz")]
    [InlineData("invalid-format-string")]
    public void InitializeFromLogin_ArbitraryTimeFormat_HandlesGracefully(string timeFormat)
    {
        // Arrange
        var jsRuntime = CreateJsRuntimeMock();
        var logger = CreateLoggerMock();
        var cultureManager = new CultureManager(jsRuntime.Object, logger.Object);

        // Act — arbitrary format strings should not throw
        cultureManager.InitializeFromLogin("en-US", null, timeFormat);

        // Assert — method completes without throwing
        Assert.NotNull(cultureManager.CurrentCulture);
    }

    #endregion

    #region CurrentCulture Property

    [Fact]
    public void CurrentCulture_ReturnsValidCulture()
    {
        // Arrange
        var jsRuntime = CreateJsRuntimeMock();
        var logger = CreateLoggerMock();
        var cultureManager = new CultureManager(jsRuntime.Object, logger.Object);

        // Act & Assert
        Assert.NotNull(cultureManager.CurrentCulture);
    }

    [Fact]
    public void CurrentUICulture_EqualsCurrentCulture()
    {
        // Arrange
        var jsRuntime = CreateJsRuntimeMock();
        var logger = CreateLoggerMock();
        var cultureManager = new CultureManager(jsRuntime.Object, logger.Object);

        // Act & Assert
        Assert.Same(cultureManager.CurrentCulture, cultureManager.CurrentUICulture);
    }

    #endregion

    #region Safe Static Initialization (Fallback Paths)

    [Fact]
    public void StaticInitialization_AllFallbacks_ConvergeOnExpectedValues()
    {
        // Act — force static initialization
        _ = new CultureManager(CreateJsRuntimeMock().Object, CreateLoggerMock().Object);

        // Assert — supported cultures defaults to en-US when environment variable is not set
        Assert.Single(CultureManager.SupportedCultures);
        Assert.Equal("en-US", CultureManager.SupportedCultures[0].Name);

        // Assert — browser is not InvariantCulture, system is null (no environment variable)
        Assert.NotEqual(CultureInfo.InvariantCulture.Name, CultureManager.BrowserCulture.Name);
        Assert.NotEqual("Invariant-Language", CultureManager.BrowserCulture.Name);
        Assert.Null(CultureManager.SystemCulture);
        Assert.NotEmpty(CultureManager.SupportedCultures);
    }

    #endregion
}
