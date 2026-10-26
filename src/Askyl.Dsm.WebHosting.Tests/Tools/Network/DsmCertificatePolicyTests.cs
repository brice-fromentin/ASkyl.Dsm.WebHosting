using System.Net.Security;
using Askyl.Dsm.WebHosting.Tools.Network;

namespace Askyl.Dsm.WebHosting.Tests.Tools.Network;

public class DsmCertificatePolicyTests
{
    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("127.0.0.2")]
    [InlineData("::1")]
    [InlineData("[::1]")]
    public void Accepts_ALoopbackIpLiteral_AcceptsAnyError(string host)
        => Assert.True(DsmCertificatePolicy.Accepts(host, SslPolicyErrors.RemoteCertificateChainErrors | SslPolicyErrors.RemoteCertificateNameMismatch));

    [Theory]
    [InlineData("localhost")]
    [InlineData("192.0.2.10")]
    [InlineData("dsm.test")]
    [InlineData(null)]
    public void Accepts_AnythingElse_RejectsErrors(string? host)
        => Assert.False(DsmCertificatePolicy.Accepts(host, SslPolicyErrors.RemoteCertificateChainErrors));

    [Theory]
    [InlineData("localhost")]
    [InlineData("192.0.2.10")]
    [InlineData(null)]
    public void Accepts_NoErrors_AcceptsAnyHost(string? host)
        => Assert.True(DsmCertificatePolicy.Accepts(host, SslPolicyErrors.None));
}
