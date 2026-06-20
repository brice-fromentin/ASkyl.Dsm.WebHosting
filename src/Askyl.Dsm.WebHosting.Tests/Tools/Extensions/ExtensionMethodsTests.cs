using System.Net;
using System.Text;
using Askyl.Dsm.WebHosting.Data.DsmApi.Responses;
using Askyl.Dsm.WebHosting.Tools.Extensions;

namespace Askyl.Dsm.WebHosting.Tests.Tools.Extensions;

public class ExtensionMethodsTests : IDisposable
{
    private readonly MockHttpMessageHandler _handler = new();

    private void SetResponse(string content = "", int statusCode = 200)
    {
        _handler.AddResponse(content, statusCode);
    }

    private HttpClient CreateClient() => new(_handler) { BaseAddress = new Uri("https://example.com") };

    public void Dispose() => _handler.Dispose();

    #region ApiResponseExtensions

    [Fact]
    public void IsValid_ResponseNull_ReturnsFalse()
    {
        // Arrange
        ApiResponseBase<EmptyResponse>? response = null;

        // Act & Assert
        Assert.False(response.IsValid());
    }

    [Fact]
    public void IsValid_SuccessTrue_NoData_ReturnsTrue()
    {
        // Arrange
        var response = new ApiResponseBase<EmptyResponse> { Success = true };

        // Act & Assert
        Assert.True(response.IsValid());
    }

    [Fact]
    public void IsValid_SuccessFalse_ReturnsFalse()
    {
        // Arrange
        var response = new ApiResponseBase<EmptyResponse> { Success = false };

        // Act & Assert
        Assert.False(response.IsValid());
    }

    [Fact]
    public void IsValid_WithHasDataTrue_NullData_ReturnsFalse()
    {
        // Arrange
        var response = new ApiResponseBase<EmptyResponse> { Success = true, Data = null };

        // Act & Assert
        Assert.False(response.IsValid(hasData: true));
    }

    [Fact]
    public void IsValid_WithHasDataTrue_WithData_ReturnsTrue()
    {
        // Arrange
        var response = new ApiResponseBase<EmptyResponse> { Success = true, Data = new EmptyResponse() };

        // Act & Assert
        Assert.True(response.IsValid(hasData: true));
    }

    #endregion

    #region HttpClientExtensions

    [Fact]
    public async Task GetJsonAsync_Success_DeserializesResponse()
    {
        // Arrange
        SetResponse("{\"name\":\"test\",\"value\":42}");
        var client = CreateClient();

        // Act
        var result = await client.GetJsonAsync<TestModel>("/");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test", result.Name);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task GetJsonAsync_NonSuccess_ReturnsDefault()
    {
        // Arrange
        SetResponse(statusCode: (int)HttpStatusCode.NotFound);
        var client = CreateClient();

        // Act
        var result = await client.GetJsonAsync<TestModel>("/");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetJsonOrDefaultAsync_Success_ReturnsDeserialized()
    {
        // Arrange
        SetResponse("{\"name\":\"test\",\"value\":42}");
        var client = CreateClient();

        // Act
        var result = await client.GetJsonOrDefaultAsync("/default", () => new TestModel());

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test", result.Name);
    }

    [Fact]
    public async Task GetJsonOrDefaultAsync_Failure_ReturnsDefaultValue()
    {
        // Arrange
        SetResponse(statusCode: (int)HttpStatusCode.NotFound);
        var client = CreateClient();

        // Act
        var result = await client.GetJsonOrDefaultAsync("/", () => new TestModel { Name = "fallback" });

        // Assert
        Assert.NotNull(result);
        Assert.Equal("fallback", result.Name);
    }

    [Fact]
    public async Task PostJsonAsync_Success_DeserializesResponse()
    {
        // Arrange
        SetResponse("{\"name\":\"test\",\"value\":42}");
        var client = CreateClient();

        // Act
        var result = await client.PostJsonAsync<TestModel, TestModel>("/", new TestModel { Name = "input" });

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test", result.Name);
    }

    [Fact]
    public async Task PostJsonAsync_NonSuccess_ReturnsDefault()
    {
        // Arrange
        SetResponse(statusCode: (int)HttpStatusCode.BadRequest);
        var client = CreateClient();

        // Act
        var result = await client.PostJsonAsync<TestModel, TestModel>("/", new TestModel { Name = "input" });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task PostJsonAsync_NullContent_SendsNullContent()
    {
        // Arrange
        SetResponse("{\"name\":\"test\",\"value\":42}");
        var client = CreateClient();

        // Act
        var result = await client.PostJsonAsync<TestModel, TestModel>("/", null);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task DeleteJsonAsync_Success_DeserializesResponse()
    {
        // Arrange
        SetResponse("{\"name\":\"test\",\"value\":42}");
        var client = CreateClient();

        // Act
        var result = await client.DeleteJsonAsync<TestModel>("/");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test", result.Name);
    }

    [Fact]
    public async Task DeleteJsonAsync_NonSuccess_ReturnsDefault()
    {
        // Arrange
        SetResponse(statusCode: (int)HttpStatusCode.NotFound);
        var client = CreateClient();

        // Act
        var result = await client.DeleteJsonAsync<TestModel>("/");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteJsonOrDefaultAsync_Failure_ReturnsDefaultValue()
    {
        // Arrange
        SetResponse(statusCode: (int)HttpStatusCode.NotFound);
        var client = CreateClient();

        // Act
        var result = await client.DeleteJsonOrDefaultAsync("/", () => new TestModel { Name = "fallback" });

        // Assert
        Assert.NotNull(result);
        Assert.Equal("fallback", result.Name);
    }

    [Fact]
    public async Task PostJsonOrDefaultAsync_Failure_ReturnsDefaultValue()
    {
        // Arrange
        SetResponse(statusCode: (int)HttpStatusCode.BadRequest);
        var client = CreateClient();

        // Act
        var result = await client.PostJsonOrDefaultAsync("/", new TestModel(), () => new TestModel { Name = "fallback" });

        // Assert
        Assert.NotNull(result);
        Assert.Equal("fallback", result.Name);
    }

    [Fact]
    public async Task ReadFromJsonAsync_DeserializesContent()
    {
        // Arrange
        const string json = "{\"name\":\"test\",\"value\":42}";
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var result = await content.ReadFromJsonAsync<TestModel>();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test", result.Name);
        Assert.Equal(42, result.Value);
    }

    #endregion

    #region Test Model

    private sealed class TestModel
    {
        public string Name { get; set; } = "";
        public int Value { get; set; }
    }

    #endregion

    #region Mock Handler

    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private string? _content;
        private int _statusCode = (int)HttpStatusCode.OK;

        public void AddResponse(string content, int statusCode = 200)
        {
            _content = content;
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage((HttpStatusCode)_statusCode)
            {
                Content = new StringContent(_content ?? "")
            });
        }
    }

    #endregion
}
