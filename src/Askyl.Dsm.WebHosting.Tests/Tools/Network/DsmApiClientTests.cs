using System.Net;
using Askyl.Dsm.WebHosting.Constants.DSM.API;
using Askyl.Dsm.WebHosting.Constants.Network;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core;
using Askyl.Dsm.WebHosting.Data.DsmApi.Parameters;
using Askyl.Dsm.WebHosting.Data.DsmApi.Responses;
using Askyl.Dsm.WebHosting.Logging;
using Askyl.Dsm.WebHosting.Tools.Infrastructure;
using Askyl.Dsm.WebHosting.Tools.Network;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace Askyl.Dsm.WebHosting.Tests.Tools.Network;

[Trait("Category", "FileSystem")]
public class DsmApiClientTests : IDisposable
{
    readonly Mock<HttpMessageHandler> _httpHandler;
    readonly HttpClient _httpClient;
    readonly DsmSettingsService _settingsService;
    readonly Mock<ILogger<ILogDsmApiClient>> _logger;

    public DsmApiClientTests()
    {
        _httpHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpHandler.Object) { BaseAddress = new Uri("https://localhost:5001/") };
        _settingsService = new DsmSettingsService(new Mock<ILogger<ILogDsmSettingsService>>().Object, new SystemFileReader());
        _logger = new Mock<ILogger<ILogDsmApiClient>>();
    }

    DsmApiClient CreateClient()
    {
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(_httpClient);

        var client = new DsmApiClient(factory.Object, _settingsService, _logger.Object);

        client.ApiInformations.Replace(new Dictionary<string, ApiInformation>
        {
            { ApiConstants.Auth, new ApiInformation { Path = "entry.cgi", MinVersion = 1, MaxVersion = 7 } },
            { "test", new ApiInformation { Path = "entry.cgi", MinVersion = 1, MaxVersion = 7 } }
        });

        return client;
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _httpHandler.Object.Dispose();
    }

    #region Cookie Header

    [Fact]
    public async Task ExecuteAsync_WithSid_AttachesCookieHeader()
    {
        // Arrange
        var receivedRequest = (HttpRequestMessage?)null;
        _httpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken _) =>
            {
                receivedRequest = req;
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
            })
            .Verifiable();

        var client = CreateClient();
        var parameters = new TestFormParameters();

        // Act
        await client.ExecuteAsync<TestResponse>("test-session-id", parameters);

        // Assert
        Assert.NotNull(receivedRequest);
        Assert.True(receivedRequest.Headers.TryGetValues(NetworkConstants.CookieHeader, out var cookies));
        Assert.Contains(cookies, c => c == NetworkConstants.SsidCookiePrefix + "test-session-id");
    }

    [Fact]
    public async Task ExecuteAsync_WithoutSid_SkipsCookieHeader()
    {
        // Arrange
        var receivedRequest = (HttpRequestMessage?)null;
        _httpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken _) =>
            {
                receivedRequest = req;
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
            })
            .Verifiable();

        var client = CreateClient();
        var parameters = new TestFormParameters();

        // Act
        await client.ExecuteAsync<TestResponse>(null, parameters);

        // Assert
        Assert.NotNull(receivedRequest);
        Assert.False(receivedRequest.Headers.TryGetValues(NetworkConstants.CookieHeader, out _));
    }

    #endregion

    #region Serialization Strategy

    [Fact]
    public async Task ExecuteAsync_FormSerialization_UsesToForm()
    {
        // Arrange
        var receivedContent = String.Empty;
        _httpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken _) =>
            {
                receivedContent = req.Content?.ReadAsStringAsync().Result ?? String.Empty;
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
            })
            .Verifiable();

        var client = CreateClient();
        var parameters = new TestFormParameters();

        // Act
        await client.ExecuteAsync<TestResponse>(null, parameters);

        // Assert
        Assert.NotEmpty(receivedContent);
    }

    [Fact]
    public async Task ExecuteAsync_JsonSerialization_UsesToJson()
    {
        // Arrange
        var receivedContentType = String.Empty;
        _httpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken _) =>
            {
                receivedContentType = req.Content?.Headers.ContentType?.MediaType ?? String.Empty;
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
            })
            .Verifiable();

        var client = CreateClient();
        var parameters = new TestJsonParameters();

        // Act
        await client.ExecuteAsync<TestResponse>(null, parameters);

        // Assert
        Assert.Equal(NetworkConstants.ApplicationJson, receivedContentType);
    }

    #endregion

    #region HTTP Error Handling

    [Fact]
    public async Task ExecuteAsync_HttpError_ReturnsDefault()
    {
        // Arrange
        _httpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError))
            .Verifiable();

        var client = CreateClient();
        var parameters = new TestFormParameters();

        // Act
        var result = await client.ExecuteAsync<TestResponse>(null, parameters);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region URL Construction

    [Fact]
    public async Task ExecuteAsync_UsesDsmSettingsServiceForUrl()
    {
        // Arrange
        string? receivedUrl = null;
        _httpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken _) =>
            {
                receivedUrl = req.RequestUri?.ToString();
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
            })
            .Verifiable();

        var client = CreateClient();
        var parameters = new TestFormParameters();

        // Act
        await client.ExecuteAsync<TestResponse>(null, parameters);

        // Assert
        Assert.NotNull(receivedUrl);
        Assert.Contains(_settingsService.Server, receivedUrl);
        Assert.Contains(_settingsService.Port.ToString(), receivedUrl);
    }

    #endregion

    #region ExecuteSimpleAsync

    [Fact]
    public async Task ExecuteSimpleAsync_DelegatesToExecuteAsync()
    {
        // Arrange
        _httpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") })
            .Verifiable();

        var client = CreateClient();
        var parameters = new TestFormParameters();

        // Act
        var result = await client.ExecuteSimpleAsync(null, parameters);

        // Assert
        Assert.NotNull(result);
    }

    #endregion

    #region Concurrency

    [Fact]
    public async Task ExecuteAsync_ConcurrentRequests_NoSharedStateMutation()
    {
        // Arrange
        var requestCount = 0;
        _httpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken _) =>
            {
                Interlocked.Increment(ref requestCount);
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
            })
            .Verifiable();

        var client = CreateClient();
        var parameters = new TestFormParameters();

        // Act — 10 concurrent requests with different SIDs
        var tasks = Enumerable.Range(0, 10)
            .Select(async i => await client.ExecuteAsync<TestResponse>($"sid-{i}", parameters))
            .ToList();

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(10, requestCount);
    }

    #endregion

    #region Test Helpers

    sealed class TestFormParameters : IApiParameters
    {
        public string Name => "test";
        public int Version => 1;
        public string Method => "test";
        public SerializationFormats SerializationFormat => SerializationFormats.Form;

        public string BuildUrl(string server, int port, string path) => $"https://{server}:{port}/webapi/{path}/test";

        public StringContent ToForm() => new("api=test&version=1&method=test");
        public StringContent ToJson() => new("{}");
    }

    sealed class TestJsonParameters : IApiParameters
    {
        public string Name => "test";
        public int Version => 1;
        public string Method => "test";
        public SerializationFormats SerializationFormat => SerializationFormats.Json;

        public string BuildUrl(string server, int port, string path) => $"https://{server}:{port}/webapi/{path}/test";

        public StringContent ToForm() => new("api=test&version=1&method=test");
        public StringContent ToJson() => new("{}", System.Text.Encoding.UTF8, NetworkConstants.ApplicationJson);
    }

    sealed class TestResponse : IApiResponse
    {
        public bool Success => true;
        public ApiError? Error => null;
    }

    #endregion
}
