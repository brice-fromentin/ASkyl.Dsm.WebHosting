using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Runtime.Versioning;

namespace Askyl.Dsm.WebHosting.Tests.Globalization;

[UnsupportedOSPlatform("browser")]
public class ResourceCompletenessTests
{
    private static ResourceManager GetResourceManager()
    {
        return new ResourceManager(
            "Askyl.Dsm.WebHosting.Globalization.Resources.SharedResource",
            typeof(WebHosting.Globalization.Resources.SharedResource).Assembly);
    }

    #region Key Parity

    [Fact]
    public void Resources_AllEnglishKeys_HaveFrenchTranslation()
    {
        // Arrange
        var resourceManager = GetResourceManager();
        var englishCulture = new CultureInfo("en-US");
        var frenchCulture = new CultureInfo("fr-FR");

        // Act
        var englishKeys = GetResourceKeys(resourceManager, englishCulture);
        var frenchKeys = GetResourceKeys(resourceManager, frenchCulture);

        // Assert
        var missing = englishKeys.Except(frenchKeys).OrderBy(k => k).ToList();
        Assert.Empty(missing);
    }

    #endregion

    #region No Empty Values

    [Fact]
    public void Resources_FrenchTranslations_HaveNoEmptyValues()
    {
        // Arrange
        var resourceManager = GetResourceManager();
        var frenchCulture = new CultureInfo("fr-FR");

        // Act
        var emptyKeys = new List<string>();
        using (var reader = resourceManager.GetResourceSet(frenchCulture, true, false))
        {
            foreach (DictionaryEntry entry in reader!)
            {
                var value = entry.Value?.ToString();
                if (string.IsNullOrWhiteSpace(value))
                {
                    emptyKeys.Add(entry.Key.ToString()!);
                }
            }
        }

        // Assert
        Assert.Empty(emptyKeys);
    }

    #endregion

    #region Key Names Match LocalizationKeys

    [Fact]
    public void Resources_LocalizationKeys_MatchResxKeys()
    {
        // Arrange - read .resx file directly to get all keys
        var assembly = typeof(WebHosting.Globalization.Resources.SharedResource).Assembly;
        var resxKeys = GetEmbeddedResxKeys(assembly);

        // Act — collect all key values from L.cs via reflection
        var keysType = typeof(WebHosting.Globalization.L);
        var localizationKeys = CollectLocalizationKeys(keysType);

        // Assert — every key in L should exist in the .resx
        var missing = localizationKeys.Except(resxKeys).OrderBy(k => k).ToList();
        Assert.Empty(missing);
    }

    #endregion

    #region Helpers

    private static HashSet<string> GetResourceKeys(ResourceManager resourceManager, CultureInfo culture)
    {
        var keys = new HashSet<string>();
        using (var resourceSet = resourceManager.GetResourceSet(culture, true, false))
        {
            if (resourceSet is not null)
            {
                foreach (DictionaryEntry entry in resourceSet)
                {
                    keys.Add(entry.Key.ToString()!);
                }
            }
        }

        return keys;
    }

    private static HashSet<string> GetEmbeddedResxKeys(Assembly assembly)
    {
        var keys = new HashSet<string>();
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(".SharedResource.resources"));

        if (resourceName is not null)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName)!;
            using var reader = new ResourceReader(stream);
            foreach (DictionaryEntry entry in reader)
            {
                keys.Add(entry.Key.ToString()!);
            }
        }

        return keys;
    }

    private static HashSet<string> CollectLocalizationKeys(Type type)
    {
        var keys = new HashSet<string>();

        // Collect field values from the type and nested types
        foreach (var field in type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy))
        {
            var value = field.GetValue(null)?.ToString();
            if (!string.IsNullOrEmpty(value))
            {
                keys.Add(value);
            }
        }

        // Recurse into nested types
        foreach (var nested in type.GetNestedTypes(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
        {
            keys.UnionWith(CollectLocalizationKeys(nested));
        }

        return keys;
    }

    #endregion
}
