using System.Globalization;
using Askyl.Dsm.WebHosting.Data.Domain.WebSites;
using Askyl.Dsm.WebHosting.Globalization;
using Askyl.Dsm.WebHosting.Globalization.Validators;
using FluentValidation;
using FluentValidation.TestHelper;

namespace Askyl.Dsm.WebHosting.Tests.Globalization;

public class DeferredMessageExtensionsTests
{
    [Fact]
    public void WithLocalizedMessage_ResolvesMessageInCurrentCulture()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            // Arrange
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            var validator = new WebSiteConfigurationValidator();
            var config = new WebSiteConfiguration { Name = String.Empty };

            // Act
            var result = validator.Validate(config);

            // Assert
            var error = result.Errors.FirstOrDefault(e => e.PropertyName == nameof(WebSiteConfiguration.Name));
            Assert.NotNull(error);
            Assert.NotEqual(nameof(WebSiteConfiguration.Name), error.ErrorMessage); // Not the key name
            Assert.Contains("name", error.ErrorMessage.ToLowerInvariant());
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Fact]
    public void WithLocalizedMessage_RespectsCultureSwitchAtValidationTime()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            // Arrange - validator constructed under en-US
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            var validator = new WebSiteConfigurationValidator();
            var config = new WebSiteConfiguration { Name = String.Empty };

            // Act - switch to fr-FR before validation
            CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");
            var result = validator.Validate(config);

            // Assert - message should be in French
            var error = result.Errors.FirstOrDefault(e => e.PropertyName == nameof(WebSiteConfiguration.Name));
            Assert.NotNull(error);

            // Compare with direct resource lookup to ensure culture was respected
            var expectedFrench = ResourceManagerCache.SharedResource.GetString("WebSiteConfiguration_NameRequired", new CultureInfo("fr-FR")) ?? "WebSiteConfiguration_NameRequired";
            Assert.Equal(expectedFrench, error.ErrorMessage);
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Fact]
    public void WithLocalizedMessage_FallsBackToKeyForMissingResource()
    {
        // Arrange
        var validator = new TestValidator();

        // Act
        var result = validator.TestValidate(new TestObject());

        // Assert - missing key falls back to bracketed key name
        result.ShouldHaveValidationErrorFor(x => x.Value)
              .WithErrorMessage("[NonExistent_Resource_Key_That_Does_Not_Exist]");
    }

    [Fact]
    public void WithLocalizedMessage_ResolvesExistingResourceCorrectly()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            // Arrange
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            var validator = new WebSiteConfigurationValidator();
            var config = new WebSiteConfiguration
            {
                Name = "TestSite",
                ApplicationPath = "/path/to/app.dll",
                InternalPort = 5000,
                HostName = "example.com",
                Environment = "Production",
                ProcessTimeoutSeconds = 30
            };
            config.InternalPort = 1; // Below minimum to trigger range error

            // Act
            var result = validator.Validate(config);

            // Assert
            var error = result.Errors.FirstOrDefault(e => e.PropertyName == nameof(WebSiteConfiguration.InternalPort));
            Assert.NotNull(error);

            // Verify the message was resolved from resources (not the raw key name)
            var expected = ResourceManagerCache.SharedResource.GetString("WebSiteConfiguration_InternalPortRange", new CultureInfo("en-US")) ?? "WebSiteConfiguration_InternalPortRange";
            Assert.Equal(expected, error.ErrorMessage);
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    #region Test Helpers

    private sealed class TestValidator : AbstractValidator<TestObject>
    {
        public TestValidator()
        {
            RuleFor(x => x.Value)
                .NotEmpty()
                .WithLocalizedMessage("NonExistent_Resource_Key_That_Does_Not_Exist");
        }
    }

    private sealed class TestObject
    {
        public string Value { get; set; } = String.Empty;
    }

    #endregion
}
