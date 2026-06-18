using System.Globalization;
using Askyl.Dsm.WebHosting.Globalization;
using Askyl.Dsm.WebHosting.Globalization.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Askyl.Dsm.WebHosting.Tests.Globalization;

public class GlobalizationServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGlobalization_RegistersILocalizerAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddGlobalization();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var localizer1 = serviceProvider.GetService<ILocalizer>();
        var localizer2 = serviceProvider.GetService<ILocalizer>();

        Assert.NotNull(localizer1);
        Assert.Same(localizer1, localizer2);
    }

    [Fact]
    public void AddGlobalization_ILocalizerReturnsTranslatedValues()
    {
        // Arrange
        var original = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            var services = new ServiceCollection();
            services.AddGlobalization();
            var serviceProvider = services.BuildServiceProvider();

            // Act
            var localizer = serviceProvider.GetRequiredService<ILocalizer>();
            var result = localizer["Login_PageTitle"];

            // Assert
            Assert.Equal("ADWH - Login", result.Value);
        }
        finally
        {
            CultureInfo.CurrentUICulture = original;
        }
    }
}
