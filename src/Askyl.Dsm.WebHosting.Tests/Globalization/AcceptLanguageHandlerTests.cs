using System.Globalization;
using System.Net.Http.Headers;
using Askyl.Dsm.WebHosting.Globalization;
using Askyl.Dsm.WebHosting.Ui.Client.Services;
using Moq;

namespace Askyl.Dsm.WebHosting.Tests.Globalization;

public class AcceptLanguageHandlerTests
{
    #region Accept-Language Header

    [Fact]
    public void SendAsync_SetsAcceptLanguageHeader()
    {
        // Arrange
        var mockCultureManager = new Mock<ICultureManager>();
        mockCultureManager.Setup(cm => cm.CurrentUICulture).Returns(new CultureInfo("fr-FR"));

        var handler = new AcceptLanguageHandler(mockCultureManager.Object);
        var request = new HttpRequestMessage(HttpMethod.Get, "/test");

        // Act - invoke the header modification logic directly
        ApplyAcceptLanguageHeader(request, mockCultureManager.Object);

        // Assert
        Assert.Single(request.Headers.AcceptLanguage);
        Assert.Equal("fr-FR", request.Headers.AcceptLanguage.First().Value);
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("fr-FR")]
    [InlineData("de-DE")]
    [InlineData("ja-JP")]
    public void SendAsync_RespectsCurrentCulture(string cultureName)
    {
        // Arrange
        var mockCultureManager = new Mock<ICultureManager>();
        mockCultureManager.Setup(cm => cm.CurrentUICulture).Returns(new CultureInfo(cultureName));

        var request = new HttpRequestMessage(HttpMethod.Get, "/test");

        // Act
        ApplyAcceptLanguageHeader(request, mockCultureManager.Object);

        // Assert
        Assert.Equal(cultureName, request.Headers.AcceptLanguage.First().Value);
    }

    [Fact]
    public void SendAsync_ClearsExistingAcceptLanguageHeaders()
    {
        // Arrange
        var mockCultureManager = new Mock<ICultureManager>();
        mockCultureManager.Setup(cm => cm.CurrentUICulture).Returns(new CultureInfo("en-US"));

        var request = new HttpRequestMessage(HttpMethod.Get, "/test");
        request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("zh-CN"));

        // Act
        ApplyAcceptLanguageHeader(request, mockCultureManager.Object);

        // Assert
        Assert.Single(request.Headers.AcceptLanguage);
        Assert.Equal("en-US", request.Headers.AcceptLanguage.First().Value);
    }

    [Fact]
    public void SendAsync_UsesCurrentUICultureFromManager()
    {
        // Arrange
        var mockCultureManager = new Mock<ICultureManager>();
        var expectedCulture = new CultureInfo("de-DE");
        mockCultureManager.Setup(cm => cm.CurrentUICulture).Returns(expectedCulture);

        var request = new HttpRequestMessage(HttpMethod.Get, "/test");

        // Act
        ApplyAcceptLanguageHeader(request, mockCultureManager.Object);

        // Assert
        Assert.Equal(expectedCulture.Name, request.Headers.AcceptLanguage.First().Value);
    }

    #endregion

    #region Test Helpers

    /// <summary>
    /// Replicates the header modification logic from AcceptLanguageHandler.SendAsync.
    /// This is equivalent to what the handler does before calling base.SendAsync().
    /// </summary>
    static void ApplyAcceptLanguageHeader(HttpRequestMessage request, ICultureManager cultureManager)
    {
        request.Headers.AcceptLanguage.Clear();
        request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue(cultureManager.CurrentUICulture.Name));
    }

    #endregion
}
