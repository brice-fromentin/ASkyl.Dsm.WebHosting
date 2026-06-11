using Askyl.Dsm.WebHosting.Globalization;

namespace Askyl.Dsm.WebHosting.Tests.Globalization;

public class GlobalizationSettingsTests
{
    #region Supported Cultures

    [Fact]
    public void SupportedCultures_ContainsDefaultCulture()
    {
        // Act
        var cultures = GlobalizationSettings.SupportedCultures;

        // Assert
        Assert.Contains(cultures, c => c.Name == "en-US");
    }

    [Fact]
    public void SupportedCultures_HasAtLeastOneCulture()
    {
        // Act
        var cultures = GlobalizationSettings.SupportedCultures;

        // Assert
        Assert.NotEmpty(cultures);
    }

    [Fact]
    public void SupportedCultures_NoDuplicates()
    {
        // Act
        var cultures = GlobalizationSettings.SupportedCultures;
        var names = cultures.Select(c => c.Name).ToList();

        // Assert
        Assert.Equal(names.Count, names.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void SupportedCultures_ContainsFrench()
    {
        // Act
        var cultures = GlobalizationSettings.SupportedCultures;

        // Assert
        Assert.Contains(cultures, c => c.Name == "fr-FR");
    }

    #endregion

    #region JSON Serialization

    [Fact]
    public void SupportedCultureNamesJson_IsValidJson()
    {
        // Act & Assert
        var exception = Record.Exception(() => System.Text.Json.JsonSerializer.Deserialize<string[]>(GlobalizationSettings.SupportedCultureNamesJson));

        Assert.Null(exception);
    }

    [Fact]
    public void SupportedCultureNamesJson_MatchesSupportedCultures()
    {
        // Act
        var names = System.Text.Json.JsonSerializer.Deserialize<string[]>(GlobalizationSettings.SupportedCultureNamesJson)!;

        // Assert
        Assert.Equal(GlobalizationSettings.SupportedCultures.Select(c => c.Name).OrderBy(n => n), names.OrderBy(n => n));
    }

    [Fact]
    public void SupportedCultureNamesJson_ContainsDefaultCulture()
    {
        // Act
        var names = System.Text.Json.JsonSerializer.Deserialize<string[]>(GlobalizationSettings.SupportedCultureNamesJson)!;

        // Assert
        Assert.Contains("en-US", names);
    }

    #endregion

    #region SystemCulture

    [Fact]
    public void SystemCulture_IsNullable()
    {
        // Act & Assert - SystemCulture starts null (set at server startup)
        // This test validates the property exists and is nullable
        var value = GlobalizationSettings.SystemCulture;
        Assert.Null(value); // Null by default in tests (no server startup)
    }

    #endregion
}
