using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using Askyl.Dsm.WebHosting.Tests.Ui.Services;
using Askyl.Dsm.WebHosting.Tools.Network;
using Moq;

namespace Askyl.Dsm.WebHosting.Tests.Tools.Network;

public class DsmCertificatePolicyTests
{
    const int Port = 5001;

    const SslPolicyErrors Errors = SslPolicyErrors.RemoteCertificateChainErrors | SslPolicyErrors.RemoteCertificateNameMismatch;

    static DsmCertificatePolicy Policy(bool rootListens) => new(Mock.Of<IRootListenerProbe>(probe => probe.IsRootListening(It.IsAny<IPAddress>(), It.IsAny<int>()) == rootListens), TestCertificates.Pin(null));

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("127.0.0.2")]
    [InlineData("::1")]
    [InlineData("[::1]")]
    public void Accepts_ARootOwnedLoopbackIpLiteral_AcceptsAnyError(string host)
        => Assert.True(Policy(rootListens: true).Accepts(host, Port, null, Errors));

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("::1")]
    public void Accepts_ALoopbackIpLiteralNobodyRootOwns_RejectsErrors(string host)
        => Assert.False(Policy(rootListens: false).Accepts(host, Port, null, Errors));

    [Theory]
    [InlineData("localhost")]
    [InlineData("192.0.2.10")]
    [InlineData("dsm.test")]
    [InlineData(null)]
    public void Accepts_AnythingElse_RejectsErrorsEvenIfRootListens(string? host)
        => Assert.False(Policy(rootListens: true).Accepts(host, Port, null, Errors));

    [Theory]
    [InlineData("localhost")]
    [InlineData("192.0.2.10")]
    [InlineData(null)]
    public void Accepts_NoErrors_AcceptsAnyHost(string? host)
        => Assert.True(Policy(rootListens: false).Accepts(host, Port, null, SslPolicyErrors.None));

    [Fact]
    public void Accepts_AUri_UsesItsHostAndPort()
    {
        var probe = new Mock<IRootListenerProbe>();

        probe.Setup(p => p.IsRootListening(IPAddress.Loopback, Port)).Returns(true);

        var policy = new DsmCertificatePolicy(probe.Object, TestCertificates.Pin(null));

        Assert.True(policy.Accepts(new Uri($"https://127.0.0.1:{Port}/"), null, Errors));
        Assert.False(policy.Accepts(new Uri($"https://127.0.0.1:{Port + 1}/"), null, Errors));
        Assert.False(policy.Accepts(new Uri($"https://localhost:{Port}/"), null, Errors));
        Assert.False(policy.Accepts((Uri?)null, null, Errors));
    }

    [Fact]
    public void Accepts_ThePinnedCertificate_AcceptsAnyErrorForAName()
    {
        using var pinned = TestCertificates.CreateCertificate(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        using var other = TestCertificates.CreateCertificate(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));

        // Lower case, as openssl prints it
        var policy = new DsmCertificatePolicy(Mock.Of<IRootListenerProbe>(), TestCertificates.Pin(pinned.GetCertHashString(HashAlgorithmName.SHA256).ToLowerInvariant()));

        Assert.True(policy.Accepts("dsm.test", Port, pinned, Errors));
        Assert.False(policy.Accepts("dsm.test", Port, other, Errors));
        Assert.False(policy.Accepts("dsm.test", Port, null, Errors));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Accepts_NoPin_RejectsACertificateWithErrors(string? sha256)
    {
        using var certificate = TestCertificates.CreateCertificate(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));

        Assert.False(new DsmCertificatePolicy(Mock.Of<IRootListenerProbe>(), TestCertificates.Pin(sha256)).Accepts("dsm.test", Port, certificate, Errors));
    }

    [Fact]
    public void CreateHandler_NeverUsesAProxyNorFollowsRedirects()
    {
        using var handler = Policy(rootListens: true).CreateHandler();

        Assert.False(handler.UseProxy);
        Assert.False(handler.AllowAutoRedirect);
    }

    [Fact]
    public void CreateHandler_Callback_RelaxesOnlyARootOwnedLoopbackLiteral()
    {
        using var handler = Policy(rootListens: true).CreateHandler();
        var callback = handler.ServerCertificateCustomValidationCallback!;

        Assert.False(callback(new HttpRequestMessage(HttpMethod.Get, "https://localhost:1/"), null, null, Errors));
        Assert.True(callback(new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1:{Port}/"), null, null, Errors));
    }
}
