using System.Globalization;
using Askyl.Dsm.WebHosting.Globalization;

namespace Askyl.Dsm.WebHosting.Tests.Globalization;

[CollectionDefinition(nameof(LocalizerTests))]
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
            var localizer = new Localizer(ResourceManagerCache.SharedResource);

            // Act
            var result = localizer["Login_PageTitle"];

            // Assert
            Assert.Equal("ADWH - Login", result.Value);
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
            var localizer = new Localizer(ResourceManagerCache.SharedResource);

            // Act
            var result = localizer["Home_DeleteConfirmation", "TestSite"];

            // Assert
            Assert.NotNull(result.Value);
            Assert.Contains("TestSite", result.Value);
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

        try
        {
            // Act — English
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            var enValue = localizer["Login_PageTitle"].Value;

            // Act — French
            CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");
            var frValue = localizer["Login_PageTitle"].Value;

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

    #region Implicit Conversion

    [Fact]
    public void ImplicitOperator_ConvertsToString()
    {
        // Arrange
        var original = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            var localizer = new Localizer(ResourceManagerCache.SharedResource);

            // Act
            string text = localizer["Login_PageTitle"];

            // Assert
            Assert.Equal("ADWH - Login", text);
        }
        finally
        {
            CultureInfo.CurrentUICulture = original;
        }
    }

    #endregion

    #region Null Handling

    [Fact]
    public void ImplicitOperator_NullReturnsEmptyString()
    {
        // Arrange
        LocalizedText? localizableText = null;

        // Act
        string text = localizableText;

        // Assert
        Assert.Equal(String.Empty, text);
    }

    #endregion
}
