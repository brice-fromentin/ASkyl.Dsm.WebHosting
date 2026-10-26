using System.Net;
using Askyl.Dsm.WebHosting.Constants.DSM.System;
using Askyl.Dsm.WebHosting.Tools.Infrastructure;
using Askyl.Dsm.WebHosting.Tools.Network;
using Moq;

namespace Askyl.Dsm.WebHosting.Tests.Tools.Network;

public class ProcNetTcpRootListenerProbeTests
{
    const string Header = "  sl  local_address rem_address   st tx_queue rx_queue tr tm->when retrnsmt   uid  timeout inode";

    static readonly IPAddress OtherAddress = IPAddress.Parse("192.0.2.10");

    static string Row(string local, string state, int uid)
        => $"   0: {local} 00000000:0000 {state} 00000000:00000000 00:00000000 00000000     {uid}        0 12345 1 0000000000000000 100 0 0 10 0";

    static ProcNetTcpRootListenerProbe Probe(string? tcp, string? tcp6 = null)
    {
        var reader = new Mock<IFileReader>();

        if (tcp is not null)
        {
            reader.Setup(f => f.FileExists(SystemDefaults.ProcNetTcpPath)).Returns(true);
            reader.Setup(f => f.ReadAllLines(SystemDefaults.ProcNetTcpPath)).Returns([Header, .. tcp.Split('\n')]);
        }

        if (tcp6 is not null)
        {
            reader.Setup(f => f.FileExists(SystemDefaults.ProcNetTcp6Path)).Returns(true);
            reader.Setup(f => f.ReadAllLines(SystemDefaults.ProcNetTcp6Path)).Returns([Header, .. tcp6.Split('\n')]);
        }

        return new(reader.Object);
    }

    [Fact]
    public void IsRootListening_TheMeasuredWildcardRows_IsTrueForBothLoopbacks()
    {
        var probe = Probe(Row("00000000:1389", "0A", 0), Row("00000000000000000000000000000000:1389", "0A", 0));

        Assert.True(probe.IsRootListening(IPAddress.Loopback, 5001));
        Assert.True(probe.IsRootListening(IPAddress.IPv6Loopback, 5001));
    }

    [Fact]
    public void IsRootListening_ARootRowOnTheLoopbackAddress_IsTrue()
    {
        Assert.True(Probe(Row("0100007F:1389", "0A", 0), Row("00000000000000000000000001000000:1389", "0A", 0)).IsRootListening(IPAddress.IPv6Loopback, 5001));
        Assert.True(Probe(Row("0100007F:1389", "0A", 0)).IsRootListening(IPAddress.Loopback, 5001));
    }

    [Fact]
    public void IsRootListening_ANonRootOwner_IsFalse()
        => Assert.False(Probe(Row("0100007F:1389", "0A", 1000)).IsRootListening(IPAddress.Loopback, 5001));

    [Fact]
    public void IsRootListening_ARootRowOnAnotherAddress_IsFalse()
        => Assert.False(Probe(Row("0A0200C0:1389", "0A", 0) + Environment.NewLine + Row("0100007F:1389", "0A", 1000)).IsRootListening(IPAddress.Loopback, 5001));

    [Fact]
    public void IsRootListening_NotListening_IsFalse()
        => Assert.False(Probe(Row("00000000:1389", "01", 0)).IsRootListening(IPAddress.Loopback, 5001));

    [Fact]
    public void IsRootListening_AnotherPort_IsFalse()
        => Assert.False(Probe(Row("00000000:138A", "0A", 0)).IsRootListening(IPAddress.Loopback, 5001));

    [Fact]
    public void IsRootListening_AMissingFileOrGarbage_IsFalse()
    {
        Assert.False(Probe(null).IsRootListening(IPAddress.Loopback, 5001));
        Assert.False(Probe("not a row").IsRootListening(IPAddress.Loopback, 5001));
        Assert.False(Probe(Row("00000000:1389", "0A", 0)).IsRootListening(OtherAddress.MapToIPv6(), 5001));
    }
}
