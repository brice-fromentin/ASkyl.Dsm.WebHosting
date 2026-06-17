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

    [Fact]
    public void InitializeFromLogin_ValidCulture_AppliesCulture()
    {
        // Arrange
        var jsRuntime = CreateJsRuntimeMock();
        var logger = CreateLoggerMock();
        var cultureManager = new CultureManager(jsRuntime.Object, logger.Object);

        // Act
        cultureManager.InitializeFromLogin("fr-FR", null, null);

        // Assert
        Assert.Equal("fr-FR", cultureManager.CurrentCulture.Name);
        Assert.Equal("fr-FR", cultureManager.CurrentUICulture.Name);
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

    [Theory]
    [InlineData("en-US")]
    [InlineData("fr-FR")]
    public void InitializeFromLogin_SyncsCurrentAndUICulture(string cultureName)
    {
        // Arrange
        var jsRuntime = CreateJsRuntimeMock();
        var logger = CreateLoggerMock();
        var cultureManager = new CultureManager(jsRuntime.Object, logger.Object);

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
        var jsRuntime = CreateJsRuntimeMock();
        var logger = CreateLoggerMock();
        var cultureManager = new CultureManager(jsRuntime.Object, logger.Object);

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
        var jsRuntime = CreateJsRuntimeMock();
        var logger = CreateLoggerMock();
        var cultureManager = new CultureManager(jsRuntime.Object, logger.Object);

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
        var jsRuntime = CreateJsRuntimeMock();
        var logger = CreateLoggerMock();
        var cultureManager = new CultureManager(jsRuntime.Object, logger.Object);

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
        var jsRuntime = CreateJsRuntimeMock();
        var logger = CreateLoggerMock();
        var cultureManager = new CultureManager(jsRuntime.Object, logger.Object);

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
        var jsRuntime = CreateJsRuntimeMock();
        var logger = CreateLoggerMock();
        var cultureManager = new CultureManager(jsRuntime.Object, logger.Object);
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
        var jsRuntime = CreateJsRuntimeMock();
        var logger = CreateLoggerMock();
        var cultureManager = new CultureManager(jsRuntime.Object, logger.Object);

        // Act & Assert - should not throw
        cultureManager.ResetToSystem();
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

    /// <summary>
    /// Verifies that SafeParseSupportedCultures falls back to default culture
    /// when no environment variable is set (the common case in tests).
    /// </summary>
    [Fact]
    public void StaticInitialization_SupportedCultures_FallsBackToDefaultCulture()
    {
        // Act — force static initialization
        _ = new CultureManager(CreateJsRuntimeMock().Object, CreateLoggerMock().Object);

        // Assert — direct access to internal static field
        Assert.Single(CultureManager.SupportedCultures);
        Assert.Equal("en-US", CultureManager.SupportedCultures[0].Name);
    }

    /// <summary>
    /// Verifies that SafeGetBrowserCulture falls back to en-US (not InvariantCulture)
    /// when the browser culture is invalid.
    /// </summary>
    [Fact]
    public void StaticInitialization_BrowserCulture_FallsBackToDefaultCultureNotInvariant()
    {
        // Act — force static initialization
        _ = new CultureManager(CreateJsRuntimeMock().Object, CreateLoggerMock().Object);

        // Assert — direct access to internal static field
        Assert.NotEqual(CultureInfo.InvariantCulture.Name, CultureManager.BrowserCulture.Name);
    }

    /// <summary>
    /// Verifies that SafeResolveSystemCultureFromEnv returns null when no env var is set,
    /// and the resolution chain falls through to browser culture.
    /// </summary>
    [Fact]
    public void StaticInitialization_SystemCulture_NullWhenNoEnvVarSet()
    {
        // Act — force static initialization
        _ = new CultureManager(CreateJsRuntimeMock().Object, CreateLoggerMock().Object);

        // Assert — direct access to internal static field
        Assert.Null(CultureManager.SystemCulture);
    }

    /// <summary>
    /// Verifies that all fallback paths converge on a user-friendly culture (not InvariantCulture).
    /// The Safe methods use consistent fallbacks:
    /// SafeParseSupportedCultures → [en-US], SafeGetBrowserCulture → en-US,
    /// SafeResolveSystemCultureFromEnv → null (falls through to browser).
    /// The net effect: even if all env vars are missing, the UI defaults to en-US.
    /// </summary>
    [Fact]
    public void StaticInitialization_AllFallbacks_ConvergeOnUserFriendlyCultureNotInvariant()
    {
        // Act — force static initialization
        _ = new CultureManager(CreateJsRuntimeMock().Object, CreateLoggerMock().Object);

        // Assert — direct access to internal static fields
        Assert.NotEqual(CultureInfo.InvariantCulture.Name, CultureManager.BrowserCulture.Name);
        Assert.NotEqual("Invariant-Language", CultureManager.BrowserCulture.Name);
        Assert.NotEmpty(CultureManager.SupportedCultures);
    }

    #endregion
}
