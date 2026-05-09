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
        Assert.False(response.IsValid<EmptyResponse>());
    }

    [Fact]
    public void IsValid_SuccessTrue_NoData_ReturnsTrue()
    {
        // Arrange
        var response = new ApiResponseBase<EmptyResponse> { Success = true };

        // Act & Assert
        Assert.True(response.IsValid<EmptyResponse>());
    }

    [Fact]
    public void IsValid_SuccessFalse_ReturnsFalse()
    {
        // Arrange
        var response = new ApiResponseBase<EmptyResponse> { Success = false };

        // Act & Assert
        Assert.False(response.IsValid<EmptyResponse>());
    }

    [Fact]
    public void IsValid_WithHasDataTrue_NullData_ReturnsFalse()
    {
        // Arrange
        var response = new ApiResponseBase<EmptyResponse> { Success = true, Data = null };

        // Act & Assert
        Assert.False(response.IsValid<EmptyResponse>(hasData: true));
    }

    [Fact]
    public void IsValid_WithHasDataTrue_WithData_ReturnsTrue()
    {
        // Arrange
        var response = new ApiResponseBase<EmptyResponse> { Success = true, Data = new EmptyResponse() };

        // Act & Assert
        Assert.True(response.IsValid<EmptyResponse>(hasData: true));
    }

    #endregion

    #region UriExtensions - ToLower

    [Fact]
    public void BooleanToLower_True_ReturnsTrueString()
    {
        // Act & Assert
        Assert.Equal("true", true.ToLower());
    }

    [Fact]
    public void BooleanToLower_False_ReturnsFalseString()
    {
        // Act & Assert
        Assert.Equal("false", false.ToLower());
    }

    #endregion

    #region UriExtensions - WithQuery

    [Fact]
    public void WithQuery_NoParameters_ReturnsOriginalUri()
    {
        // Arrange
        string uri = "https://example.com/api";

        // Act
        var result = uri.WithQuery();

        // Assert
        Assert.Equal("https://example.com/api", result);
    }

    [Fact]
    public void WithQuery_SingleParameter_AppendsQueryString()
    {
        // Arrange
        string uri = "https://example.com/api";

        // Act
        var result = uri.WithQuery(("key", "value"));

        // Assert
        Assert.Equal("https://example.com/api?key=value", result);
    }

    [Fact]
    public void WithQuery_MultipleParameters_AppendsAll()
    {
        // Arrange
        string uri = "https://example.com/api";

        // Act
        var result = uri.WithQuery(("key1", "val1"), ("key2", "val2"));

        // Assert
        Assert.Equal("https://example.com/api?key1=val1&key2=val2", result);
    }

    [Fact]
    public void WithQuery_NullValue_SkipsParameter()
    {
        // Arrange
        string uri = "https://example.com/api";

        // Act
        var result = uri.WithQuery(("key1", "val1"), ("key2", null!));

        // Assert
        Assert.Equal("https://example.com/api?key1=val1", result);
    }

    [Fact]
    public void WithQuery_AllNullValues_ReturnsOriginalUri()
    {
        // Arrange
        string uri = "https://example.com/api";

        // Act
        var result = uri.WithQuery(("key", null!));

        // Assert
        Assert.Equal("https://example.com/api", result);
    }

    [Fact]
    public void WithQuery_EscapesSpecialCharacters()
    {
        // Arrange
        string uri = "https://example.com/api";

        // Act
        var result = uri.WithQuery(("key", "value with spaces"));

        // Assert
        Assert.Equal("https://example.com/api?key=value%20with%20spaces", result);
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
        SetResponse(statusCode: (int)System.Net.HttpStatusCode.NotFound);
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
        var result = await client.GetJsonOrDefaultAsync<TestModel>("/default", () => new TestModel());

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test", result.Name);
    }

    [Fact]
    public async Task GetJsonOrDefaultAsync_Failure_ReturnsDefaultValue()
    {
        // Arrange
        SetResponse(statusCode: (int)System.Net.HttpStatusCode.NotFound);
        var client = CreateClient();

        // Act
        var result = await client.GetJsonOrDefaultAsync<TestModel>("/", () => new TestModel { Name = "fallback" });

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
        SetResponse(statusCode: (int)System.Net.HttpStatusCode.BadRequest);
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
        SetResponse(statusCode: (int)System.Net.HttpStatusCode.NotFound);
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
        SetResponse(statusCode: (int)System.Net.HttpStatusCode.NotFound);
        var client = CreateClient();

        // Act
        var result = await client.DeleteJsonOrDefaultAsync<TestModel>("/", () => new TestModel { Name = "fallback" });

        // Assert
        Assert.NotNull(result);
        Assert.Equal("fallback", result.Name);
    }

    [Fact]
    public async Task PostJsonOrDefaultAsync_Failure_ReturnsDefaultValue()
    {
        // Arrange
        SetResponse(statusCode: (int)System.Net.HttpStatusCode.BadRequest);
        var client = CreateClient();

        // Act
        var result = await client.PostJsonOrDefaultAsync<TestModel, TestModel>("/", new TestModel(), () => new TestModel { Name = "fallback" });

        // Assert
        Assert.NotNull(result);
        Assert.Equal("fallback", result.Name);
    }

    [Fact]
    public async Task ReadFromJsonAsync_DeserializesContent()
    {
        // Arrange
        var json = "{\"name\":\"test\",\"value\":42}";
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

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
        private int _statusCode = (int)System.Net.HttpStatusCode.OK;

        public void AddResponse(string content, int statusCode = 200)
        {
            _content = content;
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage((System.Net.HttpStatusCode)_statusCode)
            {
                Content = new StringContent(_content ?? "")
            });
        }
    }

    #endregion
}
