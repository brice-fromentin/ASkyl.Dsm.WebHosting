using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Logging;
using Askyl.Dsm.WebHosting.Ui.Client.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Askyl.Dsm.WebHosting.Tests.Ui.Services;

public class LicenseServiceTests
{
    private readonly Mock<ILogger<ILogLicenseService>> _logger;

    public LicenseServiceTests()
    {
        _logger = new Mock<ILogger<ILogLicenseService>>();
    }

    private LicenseService CreateService(Func<string, string?>? responseFactory = null)
    {
        var handler = new MockHttpMessageHandler(responseFactory ?? (_ => "license content"));
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost/")
        };

        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(f => f.CreateClient(ApplicationConstants.HttpClientName)).Returns(httpClient);

        return new LicenseService(httpClientFactory.Object, _logger.Object);
    }

    #region Successful License Loading

    [Fact]
    public async Task GetLicensesAsync_ValidLicenses_ReturnsAllLicenses()
    {
        // Arrange
        var service = CreateService();

        // Act
        var licenses = await service.GetLicensesAsync();

        // Assert
        Assert.Equal(4, licenses.Count);
        Assert.Contains(licenses, l => l.Name == "Application");
        Assert.Contains(licenses, l => l.Name == "NET");
    }

    #endregion

    #region Caching Behavior

    [Fact]
    public async Task GetLicensesAsync_CalledTwice_OnlyMakesOneRequest()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(_ => "license content");
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost/")
        };

        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(f => f.CreateClient(ApplicationConstants.HttpClientName)).Returns(httpClient);
        var service = new LicenseService(httpClientFactory.Object, _logger.Object);

        // Act
        await service.GetLicensesAsync();
        await service.GetLicensesAsync();

        // Assert
        Assert.Equal(4, handler.RequestCount);
    }

    #endregion

    #region Failed License Loading

    [Fact]
    public async Task GetLicensesAsync_SomeLicensesFail_ExcludesFailedLicenses()
    {
        // Arrange
        var service = CreateService(fileName => fileName == "NET.txt" ? null : "license content");

        // Act
        var licenses = await service.GetLicensesAsync();

        // Assert
        Assert.DoesNotContain(licenses, l => l.Name == "NET");
        Assert.Contains(licenses, l => l.Name == "Application");
    }

    #endregion

    #region Empty Content Handling

    [Fact]
    public async Task GetLicensesAsync_EmptyContent_ExcludesEmptyLicenses()
    {
        // Arrange
        var service = CreateService(_ => String.Empty);

        // Act
        var licenses = await service.GetLicensesAsync();

        // Assert
        Assert.Empty(licenses);
    }

    #endregion

    #region HTTP Error Handling

    [Fact]
    public async Task GetLicensesAsync_HttpError_ExcludesFailedLicenses()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(_ => throw new HttpRequestException("Network error"));
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost/")
        };

        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(f => f.CreateClient(ApplicationConstants.HttpClientName)).Returns(httpClient);
        var service = new LicenseService(httpClientFactory.Object, _logger.Object);

        // Act
        var licenses = await service.GetLicensesAsync();

        // Assert
        Assert.Empty(licenses);
    }

    #endregion

    private sealed class MockHttpMessageHandler(Func<string, string?> responseFactory) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            var fileName = Path.GetFileName(request.RequestUri?.AbsolutePath ?? String.Empty);
            var content = responseFactory(fileName);

            if (content is null)
            {
                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent(String.Empty, System.Text.Encoding.UTF8)
                });
            }

            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(content, System.Text.Encoding.UTF8)
            });
        }
    }
}
