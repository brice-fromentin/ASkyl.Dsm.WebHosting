using Askyl.Dsm.WebHosting.Logging;
using Askyl.Dsm.WebHosting.Ui.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;

namespace Askyl.Dsm.WebHosting.Tests.Globalization;

public class GlobalizationSettingsTests
{
    private static GlobalizationSettings CreateSettings()
    {
        var loggerMock = new Mock<ILogger<ILogGlobalizationSettings>>();
        return new(loggerMock.Object);
    }

    #region Supported Cultures

    [Theory]
    [InlineData("en-US")]
    [InlineData("fr-FR")]
    public void SupportedCultures_ContainsExpectedCulture(string cultureName)
    {
        // Arrange
        var settings = CreateSettings();

        // Assert
        Assert.Contains(settings.SupportedCultures, c => c.Name == cultureName);
    }

    [Fact]
    public void SupportedCultures_HasAtLeastOneCulture()
    {
        // Arrange
        var settings = CreateSettings();

        // Assert
        Assert.NotEmpty(settings.SupportedCultures);
    }

    [Fact]
    public void SupportedCultures_NoDuplicates()
    {
        // Arrange
        var settings = CreateSettings();
        var names = settings.SupportedCultures.Select(c => c.Name).ToList();

        // Assert
        Assert.Equal(names.Count, names.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    #endregion

    #region JSON Serialization

    [Fact]
    public void SupportedCultureNamesJson_IsValidJson()
    {
        // Arrange
        var settings = CreateSettings();

        // Act & Assert
        var exception = Record.Exception(() => System.Text.Json.JsonSerializer.Deserialize<string[]>(settings.SupportedCultureNamesJson));

        Assert.Null(exception);
    }

    [Fact]
    public void SupportedCultureNamesJson_MatchesSupportedCultures()
    {
        // Arrange
        var settings = CreateSettings();

        // Act
        var names = System.Text.Json.JsonSerializer.Deserialize<string[]>(settings.SupportedCultureNamesJson)!;

        // Assert
        Assert.Equal(settings.SupportedCultures.Select(c => c.Name).OrderBy(n => n), names.OrderBy(n => n));
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("fr-FR")]
    public void SupportedCultureNamesJson_ContainsExpectedCulture(string cultureName)
    {
        // Arrange
        var settings = CreateSettings();

        // Act
        var names = System.Text.Json.JsonSerializer.Deserialize<string[]>(settings.SupportedCultureNamesJson)!;

        // Assert
        Assert.Contains(cultureName, names);
    }

    #endregion

    #region SystemCulture

    [Fact]
    public void SystemCulture_IsNullByDefault()
    {
        // Arrange
        var settings = CreateSettings();

        // Assert
        Assert.Null(settings.SystemCulture);
    }

    [Fact]
    public void SystemCulture_IsSettable()
    {
        // Arrange
        var settings = CreateSettings();

        // Act
        settings.SystemCulture = "fr-FR";

        // Assert
        Assert.Equal("fr-FR", settings.SystemCulture);
    }

    #endregion
}
