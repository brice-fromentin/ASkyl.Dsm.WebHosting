using System.Globalization;
using Askyl.Dsm.WebHosting.Globalization;

namespace Askyl.Dsm.WebHosting.Tests.Globalization;

[CollectionDefinition(nameof(LocalizerTests), DisableParallelization = true)]
public class LocalizerTestsCollection
{
}

[Collection(nameof(LocalizerTests))]
public class LocalizerTests
{
    #region Simple Key Lookup

    [Fact]
    public void Indexer_ExistingKey_ReturnsTranslatedValue()
    {
        // Arrange
        var original = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            var localizer = new Localizer();

            // Act
            var result = localizer["Login_PageTitle"];

            // Assert
            Assert.Equal("ADWH - Login", result);
        }
        finally
        {
            CultureInfo.CurrentUICulture = original;
        }
    }

    #endregion

    #region Key With Arguments

    [Fact]
    public void Indexer_WithArgs_FormatsString()
    {
        // Arrange
        var original = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            var localizer = new Localizer();

            // Act
            var result = localizer["Home_DeleteConfirmation", "TestSite"];

            // Assert
            Assert.NotNull(result);
            Assert.Contains("TestSite", result);
        }
        finally
        {
            CultureInfo.CurrentUICulture = original;
        }
    }

    #endregion

    #region Missing Key Fallback

    [Fact]
    public void Indexer_MissingKey_ReturnsKeyAsFallback()
    {
        // Arrange
        var localizer = new Localizer();

        // Act
        var result = localizer["NonExistent.Key.That.Does.Not.Exist"];

        // Assert
        Assert.Equal("[NonExistent.Key.That.Does.Not.Exist]", result);
    }

    #endregion

    #region Culture Awareness

    [Fact]
    public void Indexer_RespectsCurrentUICulture()
    {
        // Arrange
        var localizer = new Localizer();
        var originalCulture = CultureInfo.CurrentUICulture;

        try
        {
            // Act — English
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            var enValue = localizer["Login_PageTitle"];

            // Act — French
            CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");
            var frValue = localizer["Login_PageTitle"];

            // Assert
            Assert.Equal("ADWH - Login", enValue);
            Assert.Equal("ADWH - Connexion", frValue);
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    #endregion
}
