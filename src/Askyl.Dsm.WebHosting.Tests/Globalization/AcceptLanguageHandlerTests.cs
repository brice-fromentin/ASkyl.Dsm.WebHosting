using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using Askyl.Dsm.WebHosting.Globalization;
using Askyl.Dsm.WebHosting.Ui.Client.Services;
using Moq;

namespace Askyl.Dsm.WebHosting.Tests.Globalization;

public class AcceptLanguageHandlerTests
{
    #region Accept-Language Header

    [Fact]
    public async Task SendAsync_SetsAcceptLanguageHeader()
    {
        // Arrange
        var mockCultureManager = new Mock<ICultureManager>();
        mockCultureManager.Setup(cm => cm.CurrentUICulture).Returns(new CultureInfo("fr-FR"));

        var handler = new TestableAcceptLanguageHandler(mockCultureManager.Object);
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/test");

        // Act
        using var response = await handler.SendAsync(request, CancellationToken.None);

        // Assert
        Assert.Single(request.Headers.AcceptLanguage);
        Assert.Equal("fr-FR", request.Headers.AcceptLanguage.First().Value);
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("fr-FR")]
    [InlineData("de-DE")]
    [InlineData("ja-JP")]
    public async Task SendAsync_RespectsCurrentCulture(string cultureName)
    {
        // Arrange
        var mockCultureManager = new Mock<ICultureManager>();
        mockCultureManager.Setup(cm => cm.CurrentUICulture).Returns(new CultureInfo(cultureName));

        var handler = new TestableAcceptLanguageHandler(mockCultureManager.Object);
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/test");

        // Act
        using var response = await handler.SendAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(cultureName, request.Headers.AcceptLanguage.First().Value);
    }

    [Fact]
    public async Task SendAsync_ClearsExistingAcceptLanguageHeaders()
    {
        // Arrange
        var mockCultureManager = new Mock<ICultureManager>();
        mockCultureManager.Setup(cm => cm.CurrentUICulture).Returns(new CultureInfo("en-US"));

        var handler = new TestableAcceptLanguageHandler(mockCultureManager.Object);
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/test");
        request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("zh-CN"));

        // Act
        using var response = await handler.SendAsync(request, CancellationToken.None);

        // Assert
        Assert.Single(request.Headers.AcceptLanguage);
        Assert.Equal("en-US", request.Headers.AcceptLanguage.First().Value);
    }

    [Fact]
    public async Task SendAsync_UsesCurrentUICultureFromManager()
    {
        // Arrange
        var mockCultureManager = new Mock<ICultureManager>();
        var expectedCulture = new CultureInfo("de-DE");
        mockCultureManager.Setup(cm => cm.CurrentUICulture).Returns(expectedCulture);

        var handler = new TestableAcceptLanguageHandler(mockCultureManager.Object);
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/test");

        // Act
        using var response = await handler.SendAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(expectedCulture.Name, request.Headers.AcceptLanguage.First().Value);
    }

    #endregion

    #region Test Helpers

    /// <summary>
    /// Testable subclass that exposes the protected SendAsync as public.
    /// </summary>
    private sealed class TestableAcceptLanguageHandler(ICultureManager cultureManager)
        : AcceptLanguageHandler(cultureManager)
    {
        public TestableAcceptLanguageHandler()
            : this(new Mock<ICultureManager>().Object)
        {
        }

        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Set a dummy InnerHandler to prevent InvalidOperationException
            InnerHandler ??= new DummyHttpMessageHandler();
            return base.SendAsync(request, cancellationToken);
        }

        private sealed class DummyHttpMessageHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
            }
        }
    }

    #endregion
}
