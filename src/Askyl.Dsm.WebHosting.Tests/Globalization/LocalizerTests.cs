using Askyl.Dsm.WebHosting.Globalization;
using Microsoft.Extensions.Localization;
using Moq;

namespace Askyl.Dsm.WebHosting.Tests.Globalization;

public class LocalizerTests
{
    private readonly Mock<IStringLocalizer<Askyl.Dsm.WebHosting.Globalization.Resources.SharedResource>> _mockLocalizer;
    private readonly Localizer _localizer;

    public LocalizerTests()
    {
        _mockLocalizer = new Mock<IStringLocalizer<Askyl.Dsm.WebHosting.Globalization.Resources.SharedResource>>();
        _localizer = new Localizer(_mockLocalizer.Object);
    }

    #region Simple Key Lookup

    [Fact]
    public void Indexer_SimpleKey_DelegatesToLocalizer()
    {
        // Arrange
        var key = "Common.OK";
        var expected = new LocalizedString(key, "OK");
        _mockLocalizer.Setup(l => l[key]).Returns(expected);

        // Act
        var result = _localizer[key];

        // Assert
        Assert.Equal(expected, result);
        _mockLocalizer.Verify(l => l[key], Times.Once);
    }

    #endregion

    #region Key With Arguments

    [Fact]
    public void Indexer_WithArgs_DelegatesToLocalizerWithArgs()
    {
        // Arrange
        var key = "Home.DeleteConfirmation";
        var arg = "TestSite";
        var expected = new LocalizedString(key, "Are you sure you want to delete 'TestSite'?");
        _mockLocalizer.Setup(l => l[key, It.IsAny<object[]>()]).Returns(expected);

        // Act
        var result = _localizer[key, arg];

        // Assert
        Assert.Equal(expected, result);
        _mockLocalizer.Verify(l => l[key, It.Is<object[]>(a => a.Length == 1 && a[0].Equals(arg))], Times.Once);
    }

    [Fact]
    public void Indexer_WithMultipleArgs_DelegatesToLocalizerWithArgs()
    {
        // Arrange
        var key = "WebsiteConfig.ErrorModifying";
        var arg1 = "Updating";
        var arg2 = "Connection timeout";
        var expected = new LocalizedString(key, "Error Updating website: Connection timeout");
        _mockLocalizer.Setup(l => l[key, It.IsAny<object[]>()]).Returns(expected);

        // Act
        var result = _localizer[key, arg1, arg2];

        // Assert
        Assert.Equal(expected, result);
        _mockLocalizer.Verify(l => l[key, It.Is<object[]>(a => a.Length == 2)], Times.Once);
    }

    #endregion

    #region Returns LocalizedString

    [Fact]
    public void Indexer_ReturnsLocalizedString_WithCorrectValue()
    {
        // Arrange
        var key = "Login.PageTitle";
        var value = "ADWH - Login";
        var expected = new LocalizedString(key, value);
        _mockLocalizer.Setup(l => l[key]).Returns(expected);

        // Act
        var result = _localizer[key];

        // Assert
        Assert.Equal(value, result.Value);
    }

    #endregion
}
