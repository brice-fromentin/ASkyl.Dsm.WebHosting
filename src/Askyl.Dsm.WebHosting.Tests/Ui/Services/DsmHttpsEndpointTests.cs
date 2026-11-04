using Askyl.Dsm.WebHosting.Ui.Client.Services;

namespace Askyl.Dsm.WebHosting.Tests.Ui.Services;

public class DsmHttpsEndpointTests
{
    [Theory]
    [InlineData(5001)]
    [InlineData(5443)]
    public void PlainHttpOnARemoteHost_IsInsecure_AndMapsToTheHttpsPort(int port)
    {
        var endpoint = new DsmHttpsEndpoint(port);
        var page = new Uri("http://nas.test:5000/adwh/login?x=1");

        Assert.True(endpoint.IsInsecure(page));
        Assert.Equal(new Uri($"https://nas.test:{port}/adwh/login?x=1"), endpoint.ToHttps(page));
    }

    [Theory]
    [InlineData("https://nas.test:5001/adwh/login")]
    [InlineData("http://localhost/adwh/login")]
    [InlineData("http://127.0.0.1:5000/adwh/login")]
    public void HttpsAndLoopbackPages_AreNotInsecure(string uri) => Assert.False(new DsmHttpsEndpoint(5001).IsInsecure(new Uri(uri)));
}
