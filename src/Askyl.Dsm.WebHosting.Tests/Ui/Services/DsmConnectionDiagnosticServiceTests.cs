using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Domain.Diagnostics;
using Askyl.Dsm.WebHosting.Logging;
using Askyl.Dsm.WebHosting.Tools.Network;
using Askyl.Dsm.WebHosting.Ui.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Askyl.Dsm.WebHosting.Tests.Ui.Services;

public class DsmConnectionDiagnosticServiceTests
{
    /// <summary>
    /// Reserved by RFC 6761: never resolves.
    /// </summary>
    const string UnresolvableHost = "dsm.invalid";

    const int DsmPort = 5001;

    readonly Mock<IDsmSettingsService> _settings = new();

    public DsmConnectionDiagnosticServiceTests()
    {
        _settings.SetupGet(s => s.Port).Returns(DsmPort);
    }

    DsmConnectionDiagnosticService CreateService()
    {
        // The API probe is never reached by these tests: every one stops at an earlier check
        var apiClient = new DsmApiClient(Mock.Of<IHttpClientFactory>(), _settings.Object, Mock.Of<ILogger<ILogDsmApiClient>>());

        return new DsmConnectionDiagnosticService(_settings.Object, apiClient, new DsmCertificatePolicy(Mock.Of<IRootListenerProbe>()), Mock.Of<ILogger<ILogDsmConnectionDiagnosticService>>());
    }

    #region Ordering

    [Fact]
    public async Task GetReportAsync_WithoutAHostname_FailsTheFirstCheckAndSkipsTheRest()
    {
        var report = (await CreateService().GetReportAsync()).Value;

        Assert.NotNull(report);
        Assert.False(report.IsHealthy);
        Assert.Null(report.Host);
        Assert.Equal(new DsmConnectionCheckResult(DsmConnectionCheck.HostnameConfigured, DsmCheckStatus.Failed, DsmCheckFailure.HostnameMissing), report.Checks[0]);
        Assert.Equal(Enum.GetValues<DsmConnectionCheck>().Skip(1), report.Checks.Skip(1).Select(check => check.Check));
        Assert.All(report.Checks.Skip(1), check => Assert.Equal(DsmCheckStatus.Skipped, check.Status));
    }

    [Fact]
    public async Task GetReportAsync_WhenTheSettingsCannotBeRead_SaysSo()
    {
        _settings.Setup(s => s.Reload()).Throws(new InvalidOperationException("unreadable"));

        var report = (await CreateService().GetReportAsync()).Value;

        Assert.NotNull(report);
        Assert.Equal(DsmCheckFailure.SettingsUnreadable, report.Checks[0].Failure);
    }

    [Fact]
    public async Task GetReportAsync_AnUnresolvableHost_StopsAtResolution()
    {
        _settings.SetupGet(s => s.Server).Returns(UnresolvableHost);

        var report = (await CreateService().GetReportAsync()).Value;

        Assert.NotNull(report);
        Assert.Equal(UnresolvableHost, report.Host);
        Assert.Equal(DsmPort, report.Port);
        Assert.Equal(
            [DsmCheckStatus.Passed, DsmCheckStatus.Failed, DsmCheckStatus.Skipped, DsmCheckStatus.Skipped, DsmCheckStatus.Skipped],
            report.Checks.Select(check => check.Status));
        Assert.Equal(DsmCheckFailure.NameNotFound, report.Checks[1].Failure);
    }

    [Fact]
    public async Task GetReportAsync_WhenACheckThrows_ReportsItAsUnexpectedAndCachesTheReport()
    {
        // A port a typo can leave in synoinfo.conf: TcpClient.ConnectAsync throws instead of failing
        _settings.SetupGet(s => s.Server).Returns("127.0.0.1");
        _settings.SetupGet(s => s.Port).Returns(70000);

        var service = CreateService();
        var report = (await service.GetReportAsync()).Value;
        await service.GetReportAsync();

        Assert.NotNull(report);
        Assert.Equal(new DsmConnectionCheckResult(DsmConnectionCheck.PortAnswers, DsmCheckStatus.Failed, DsmCheckFailure.Unexpected), report.Checks[2]);
        Assert.All(report.Checks.Skip(3), check => Assert.Equal(DsmCheckStatus.Skipped, check.Status));
        _settings.Verify(s => s.Reload(), Times.Once);
    }

    #endregion

    #region Caching

    [Fact]
    public async Task GetReportAsync_RunsOnce_UntilARecheck()
    {
        var service = CreateService();

        await service.GetReportAsync();
        await service.GetReportAsync();
        _settings.Verify(s => s.Reload(), Times.Once);

        await service.RecheckAsync();
        _settings.Verify(s => s.Reload(), Times.Exactly(2));
    }

    #endregion

    #region Coalescing

    [Fact]
    public async Task RecheckAsync_CallsArrivingDuringARun_ShareOneFollowUpRun()
    {
        using var entered = new ManualResetEventSlim();
        using var gate = new ManualResetEventSlim();
        var reloads = 0;

        _settings.Setup(s => s.Reload()).Callback(() =>
        {
            if (Interlocked.Increment(ref reloads) == 1)
            {
                entered.Set();
                Assert.True(gate.Wait(TimeSpan.FromSeconds(5)));
            }
        });

        var service = CreateService();
        var first = Task.Run(() => service.GetReportAsync());

        Assert.True(entered.Wait(TimeSpan.FromSeconds(5)));

        // Each call captures the run counter synchronously, before it waits for the lock
        var second = service.RecheckAsync();
        var third = service.RecheckAsync();

        gate.Set();
        await Task.WhenAll(first, second, third).WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(2, reloads);
    }

    #endregion

    #region Exposure

    [Fact]
    public void ForAnonymousVisitor_AHealthyReport_TellsNothingButThat()
    {
        var checkedAt = DateTimeOffset.UtcNow;
        var healthy = new DsmConnectionReport(true, checkedAt, UnresolvableHost, DsmPort, [new(DsmConnectionCheck.HostnameConfigured, DsmCheckStatus.Passed)]);

        var visible = DsmConnectionDiagnosticService.ForAnonymousVisitor(healthy);

        Assert.True(visible.IsHealthy);
        Assert.Equal(checkedAt, visible.CheckedAt);
        Assert.Null(visible.Host);
        Assert.Equal(0, visible.Port);
        Assert.Empty(visible.Checks);
    }

    [Fact]
    public void ForAnonymousVisitor_AnUnhealthyReport_KeepsItsDetails()
    {
        var unhealthy = new DsmConnectionReport(false, DateTimeOffset.UtcNow, UnresolvableHost, DsmPort, []);

        Assert.Same(unhealthy, DsmConnectionDiagnosticService.ForAnonymousVisitor(unhealthy));
    }

    [Theory]
    [InlineData("192.0.2.10")]
    [InlineData("192.0.2.10.")]
    [InlineData(" 192.0.2.10 ")]
    public void ForAnonymousVisitor_AnIpAddressHost_IsNeverShown(string address)
    {
        DsmConnectionCheckResult[] checks = [new(DsmConnectionCheck.HostnameConfigured, DsmCheckStatus.Passed), new(DsmConnectionCheck.NameResolves, DsmCheckStatus.Failed, DsmCheckFailure.NameNotFound)];
        var unhealthy = new DsmConnectionReport(false, DateTimeOffset.UtcNow, address, DsmPort, checks);

        var visible = DsmConnectionDiagnosticService.ForAnonymousVisitor(unhealthy);

        Assert.Null(visible.Host);
        Assert.True(visible.HostIsIpAddress);
        Assert.Equal(DsmPort, visible.Port);
        Assert.Equal(checks, visible.Checks);
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("::1")]
    public void ForAnonymousVisitor_ALoopbackHost_IsKept(string address)
    {
        var unhealthy = new DsmConnectionReport(false, DateTimeOffset.UtcNow, address, DsmPort, []);

        var visible = DsmConnectionDiagnosticService.ForAnonymousVisitor(unhealthy);

        Assert.Equal(address, visible.Host);
        Assert.False(visible.HostIsIpAddress);
    }

    #endregion
}
