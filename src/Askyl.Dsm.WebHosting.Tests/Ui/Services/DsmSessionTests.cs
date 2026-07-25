using System.Diagnostics.CodeAnalysis;
using System.Net;
using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Constants.DSM.API;
using Askyl.Dsm.WebHosting.Data.Domain.Authentication;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core;
using Askyl.Dsm.WebHosting.Data.DsmApi.Parameters;
using Askyl.Dsm.WebHosting.Data.DsmApi.Responses;
using Askyl.Dsm.WebHosting.Data.Results;
using Askyl.Dsm.WebHosting.Logging;
using Askyl.Dsm.WebHosting.Tests.Tools.Infrastructure;
using Askyl.Dsm.WebHosting.Tools.Infrastructure;
using Askyl.Dsm.WebHosting.Tools.Network;
using Askyl.Dsm.WebHosting.Ui.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace Askyl.Dsm.WebHosting.Tests.Ui.Services;

public class DsmSessionTests : IDisposable
{
    readonly Mock<HttpMessageHandler> _httpHandler;
    readonly HttpClient _httpClient;
    readonly DsmSettingsService _settingsService;
    readonly Mock<ILogger<ILogDsmApiClient>> _clientLogger;
    readonly FakeSession _session;
    readonly Mock<IHttpContextAccessor> _httpContextAccessor;
    readonly Mock<ILogger<ILogDsmSession>> _logger;

    public DsmSessionTests()
    {
        _httpHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpHandler.Object) { BaseAddress = new Uri("https://localhost:5001/") };
        _settingsService = FakeDsmSettings.Create();
        _clientLogger = new Mock<ILogger<ILogDsmApiClient>>();
        _session = new FakeSession();
        _httpContextAccessor = new Mock<IHttpContextAccessor>();
        _httpContextAccessor.Setup(h => h.HttpContext!.Session).Returns(_session);
        _logger = new Mock<ILogger<ILogDsmSession>>();
    }

    DsmApiClient CreateClient()
    {
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(_httpClient);

        var client = new DsmApiClient(factory.Object, _settingsService, _clientLogger.Object);

        client.ApiInformations.Replace(new Dictionary<string, ApiInformation>
        {
            { ApiConstants.Auth, new ApiInformation { Path = "entry.cgi", MinVersion = 1, MaxVersion = 7 } },
            { ApiConstants.CoreUser, new ApiInformation { Path = "entry.cgi", MinVersion = 1, MaxVersion = 7 } },
            { ApiConstants.CoreUserSettings, new ApiInformation { Path = "entry.cgi", MinVersion = 1, MaxVersion = 7 } },
            { "test", new ApiInformation { Path = "entry.cgi", MinVersion = 1, MaxVersion = 7 } }
        });

        return client;
    }

    DsmSession CreateSession()
    {
        return new DsmSession(CreateClient(), _httpContextAccessor.Object, _logger.Object);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _httpHandler.Object.Dispose();
    }

    #region ValidateSessionAsync

    [Fact]
    public async Task ValidateSessionAsync_NoSid_ReturnsFalse()
    {
        // Arrange
        var session = CreateSession();

        // Act
        var result = await session.ValidateSessionAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateSessionAsync_NoUsername_ReturnsFalse()
    {
        // Arrange
        _session.Set(ApplicationConstants.DsmSessionKey, "test-sid");
        var session = CreateSession();

        // Act
        var result = await session.ValidateSessionAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateSessionAsync_SuccessfulResponse_ReturnsTrue()
    {
        // Arrange
        _session.Set(ApplicationConstants.DsmSessionKey, "test-sid");
        _session.Set(ApplicationConstants.DsmUsernameKey, "admin");

        SetupHttpResponse("{\"success\":true,\"data\":{\"users\":[{\"name\":\"admin\",\"uid\":1024}]}}");

        var session = CreateSession();

        // Act
        var result = await session.ValidateSessionAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateSessionAsync_PermissionError_ReturnsFalse()
    {
        // Arrange — 105 is "insufficient user privilege", the code a non-administrator gets
        // from SYNO.Core.User.get. Every code other than -4 used to be read as a valid session.
        _session.Set(ApplicationConstants.DsmSessionKey, "test-sid");
        _session.Set(ApplicationConstants.DsmUsernameKey, "guest");

        SetupHttpResponse($"{{\"success\":false,\"error\":{{\"code\":{DsmConstants.ErrorCodePermissionDenied}}}}}");

        var session = CreateSession();

        // Act
        var result = await session.ValidateSessionAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateSessionAsync_NonOkStatusCode_ReturnsFalse()
    {
        // Arrange
        _session.Set(ApplicationConstants.DsmSessionKey, "test-sid");
        _session.Set(ApplicationConstants.DsmUsernameKey, "admin");

        SetupHttpResponse(String.Empty, HttpStatusCode.InternalServerError);

        var session = CreateSession();

        // Act
        var result = await session.ValidateSessionAsync();

        // Assert
        Assert.False(result);
    }

    #endregion

    #region ConnectAsync

    [Fact]
    public async Task ConnectAsync_NonAdministrator_ReturnsForbiddenAndClearsSession()
    {
        // Arrange — login succeeds for any DSM user, then the admin-only SYNO.Core.User.get
        // returns a permission error, which must reject the login outright.
        SetupHttpResponses(
            "{\"success\":true,\"data\":{\"sid\":\"test-sid\"}}",
            $"{{\"success\":false,\"error\":{{\"code\":{DsmConstants.ErrorCodePermissionDenied}}}}}");

        var session = CreateSession();

        // Act
        var result = await session.ConnectAsync(new LoginCredentials("guest", "password", null));

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ApiErrorCode.Forbidden, result.ErrorCode);
        Assert.Null(_session.Get(ApplicationConstants.DsmSessionKey));
        Assert.Null(_session.Get(ApplicationConstants.DsmUsernameKey));
    }

    [Fact]
    public async Task ConnectAsync_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange — DSM rejects the login itself, which must stay distinct from the
        // non-administrator case so the UI can show a different message.
        SetupHttpResponse($"{{\"success\":false,\"error\":{{\"code\":{DsmConstants.ErrorCodeAuthenticationFailed}}}}}");

        var session = CreateSession();

        // Act
        var result = await session.ConnectAsync(new LoginCredentials("admin", "wrong", null));

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ApiErrorCode.Unauthorized, result.ErrorCode);
    }

    [Fact]
    public async Task ConnectAsync_Administrator_ReturnsSuccessAndPersistsSession()
    {
        // Arrange
        SetupHttpResponses(
            "{\"success\":true,\"data\":{\"sid\":\"test-sid\"}}",
            "{\"success\":true,\"data\":{\"users\":[{\"name\":\"admin\",\"uid\":1024}]}}",
            "{\"success\":true,\"data\":{\"personal\":{}}}");

        var session = CreateSession();

        // Act
        var result = await session.ConnectAsync(new LoginCredentials("admin", "password", null));

        // Assert
        Assert.True(result.Success);
        Assert.Equal("test-sid", _session.Get(ApplicationConstants.DsmSessionKey));
        Assert.Equal("admin", _session.Get(ApplicationConstants.DsmUsernameKey));
    }

    #endregion

    #region Disconnect

    [Fact]
    public void Disconnect_ClearsSessionState()
    {
        // Arrange
        _session.Set(ApplicationConstants.DsmSessionKey, "test-sid");
        _session.Set(ApplicationConstants.DsmUsernameKey, "admin");

        var session = CreateSession();

        // Act
        session.Disconnect();

        // Assert
        Assert.Null(_session.Get(ApplicationConstants.DsmSessionKey));
        Assert.Null(_session.Get(ApplicationConstants.DsmUsernameKey));
    }

    #endregion

    #region ExecuteAsync

    [Fact]
    public async Task ExecuteAsync_DelegatesToClientWithSid()
    {
        // Arrange
        var sid = "test-session-id";
        _session.Set(ApplicationConstants.DsmSessionKey, sid);

        _httpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"success\":true}")
            });

        var session = CreateSession();

        // Act
        var result = await session.ExecuteAsync<TestResponse>(CreateTestParameters());

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    #endregion

    #region Test Helpers

    TestParameters CreateTestParameters()
    {
        return new TestParameters();
    }

    void SetupHttpResponse(string json, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _httpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json)
            });
    }

    /// <summary>
    /// Queues one JSON body per successive HTTP call, in order.
    /// </summary>
    void SetupHttpResponses(params string[] jsonBodies)
    {
        var sequence = _httpHandler.Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());

        foreach (var json in jsonBodies)
        {
            sequence = sequence.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            });
        }
    }

    sealed class TestParameters : IApiParameters
    {
        public string Name => "test";
        public int Version => 1;
        public string Method => "test";
        public SerializationFormats SerializationFormat => SerializationFormats.Form;

        public string BuildUrl(string server, int port, string path) => $"https://{server}:{port}/webapi/{path}/test";

        public StringContent ToForm() => new("test");
        public StringContent ToJson() => new("{}");
    }

    sealed class TestResponse : IApiResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("success")]
        public bool Success { get; set; }
        public ApiError? Error => null;
    }

    /// <summary>
    /// Fake ISession implementation that doesn't require mocking extension methods.
    /// </summary>
    sealed class FakeSession : ISession
    {
#pragma warning disable IDE0028 // Collection initialization can be simplified
        readonly Dictionary<string, byte[]> _data = new();
#pragma warning restore IDE0028
        readonly byte[] _emptyArray = [];

        public string Id { get; } = "test-session-id";
        public bool IsAvailable => true;

        public Task CommitAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public void Clear() => _data.Clear();

        public IEnumerable<string> Keys => _data.Keys;

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out byte[] value) => _data.TryGetValue(key, out value);
        public void Set(string key, byte[] value) => _data[key] = value;
        public void Remove(string key) => _data.Remove(key);

        // Public helpers for test setup
        public void Set(string key, string value) => _data[key] = System.Text.Encoding.UTF8.GetBytes(value);
        public string? Get(string key) => _data.TryGetValue(key, out var bytes) ? System.Text.Encoding.UTF8.GetString(bytes) : null;
    }
    #endregion
}
