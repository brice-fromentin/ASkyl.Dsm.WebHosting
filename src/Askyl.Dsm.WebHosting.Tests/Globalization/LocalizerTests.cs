using System.Globalization;
using Askyl.Dsm.WebHosting.Globalization;

namespace Askyl.Dsm.WebHosting.Tests.Globalization;

public class LocalizerTests
{
    #region Simple Key Lookup

    [Fact]
    public void Indexer_ExistingKey_ReturnsTranslatedValue()
    {
        // Arrange
        var original = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = new CultureInfo("en-US");
        var localizer = new Localizer(ResourceManagerCache.SharedResource);

        // Act
        var result = localizer["Login_PageTitle"];

        // Cleanup
        CultureInfo.CurrentUICulture = original;

        // Assert
        Assert.Equal("ADWH - Login", result.Value);
    }

    #endregion

    #region Key With Arguments

    [Fact]
    public void Indexer_WithArgs_FormatsString()
    {
        // Arrange
        var original = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = new CultureInfo("en-US");
        var localizer = new Localizer(ResourceManagerCache.SharedResource);

        // Act
        var result = localizer["Home_DeleteConfirmation", "TestSite"];

        // Cleanup
        CultureInfo.CurrentUICulture = original;

        // Assert
        Assert.NotNull(result.Value);
        Assert.Contains("TestSite", result.Value);
    }

    #endregion

    #region Missing Key Fallback

    [Fact]
    public void Indexer_MissingKey_ReturnsKeyAsFallback()
    {
        // Arrange
        var localizer = new Localizer(ResourceManagerCache.SharedResource);

        // Act
        var result = localizer["NonExistent.Key.That.Does.Not.Exist"];

        // Assert
        Assert.Equal("NonExistent.Key.That.Does.Not.Exist", result.Value);
    }

    #endregion

    #region Culture Awareness

    [Fact]
    public void Indexer_RespectsCurrentUICulture()
    {
        // Arrange
        var localizer = new Localizer(ResourceManagerCache.SharedResource);
        var originalCulture = CultureInfo.CurrentUICulture;

        // Act — English
        CultureInfo.CurrentUICulture = new CultureInfo("en-US");
        var enValue = localizer["Login_PageTitle"].Value;

        // Act — French
        CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");
        var frValue = localizer["Login_PageTitle"].Value;

        // Cleanup
        CultureInfo.CurrentUICulture = originalCulture;

        // Assert
        Assert.Equal("ADWH - Login", enValue);
        Assert.Equal("ADWH - Connexion", frValue);
    }

    #endregion

    #region Implicit Conversion

    [Fact]
    public void ImplicitOperator_ConvertsToString()
    {
        // Arrange
        var original = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = new CultureInfo("en-US");
        var localizer = new Localizer(ResourceManagerCache.SharedResource);

        // Act
        string text = localizer["Login_PageTitle"];

        // Cleanup
        CultureInfo.CurrentUICulture = original;

        // Assert
        Assert.Equal("ADWH - Login", text);
    }

    #endregion
}
