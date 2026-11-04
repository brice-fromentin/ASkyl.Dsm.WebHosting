using System.Net;
using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Globalization;
using Askyl.Dsm.WebHosting.Ui.Client.Services;
using Moq;
using Moq.Protected;

namespace Askyl.Dsm.WebHosting.Tests.Ui.Services;

public class DsmConnectionDiagnosticClientTests
{
    const string RateLimitMessage = "Too many attempts";

    [Fact]
    public async Task RecheckAsync_WhenThrottled_ReturnsTheRateLimitMessage()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => new HttpResponseMessage(HttpStatusCode.TooManyRequests));

        using var httpClient = new HttpClient(handler.Object) { BaseAddress = new Uri("https://localhost/") };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(ApplicationConstants.HttpClientName)).Returns(httpClient);

        var localizer = new Mock<ILocalizer>();
        localizer.Setup(l => l[LK.Error.RateLimitExceeded]).Returns(RateLimitMessage);

        var result = await new DsmConnectionDiagnosticService(factory.Object, localizer.Object).RecheckAsync();

        Assert.False(result.Success);
        Assert.Equal(RateLimitMessage, result.Message);
    }
}
