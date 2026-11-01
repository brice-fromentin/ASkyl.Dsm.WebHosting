using System.Net;
using System.Net.Sockets;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Domain.Diagnostics;
using Askyl.Dsm.WebHosting.Logging;
using Askyl.Dsm.WebHosting.Tests.Tools.Network;
using Askyl.Dsm.WebHosting.Tools.Network;
using Askyl.Dsm.WebHosting.Ui.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace Askyl.Dsm.WebHosting.Tests.Ui.Services;

public class DsmConnectionDiagnosticServiceTests
{
    const int DsmPort = 5001;

    readonly Mock<IDsmSettingsService> _settings = new();

    public DsmConnectionDiagnosticServiceTests()
    {
        // A port nothing listens on: every run stops at the first check, quickly
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var closedPort = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        _settings.SetupGet(s => s.Server).Returns(IPAddress.Loopback.ToString());
        _settings.SetupGet(s => s.Port).Returns(closedPort);
    }

    DsmConnectionDiagnosticService CreateService(bool rootListens = false, HttpMessageHandler? dsmApi = null)
    {
        // Without a DSM API to answer, the probe has no client: only a run that stops before it can use this
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(() => dsmApi is null ? null! : new HttpClient(dsmApi));

        var apiClient = new DsmApiClient(factory.Object, _settings.Object, Mock.Of<ILogger<ILogDsmApiClient>>());
        var probe = Mock.Of<IRootListenerProbe>(listener => listener.IsRootListening(It.IsAny<IPAddress>(), It.IsAny<int>()) == rootListens);

        return new DsmConnectionDiagnosticService(_settings.Object, apiClient, new DsmCertificatePolicy(probe, TestCertificates.Pin(null)), Mock.Of<ILogger<ILogDsmConnectionDiagnosticService>>());
    }

    /// <summary>
    /// Points the checks at a local TLS server with a self-signed certificate, as DSM's default one is.
    /// </summary>
    LocalTlsServer StartDsm()
    {
        var server = LocalTlsServer.Start(TestCertificates.CreateCertificate(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1)));
        _settings.SetupGet(s => s.Port).Returns(server.Port);

        return server;
    }

    static HttpMessageHandler DsmApi(Func<Task<HttpResponseMessage>> answer)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns(answer);

        return handler.Object;
    }

    static HttpMessageHandler HealthyDsmApi() => DsmApi(() => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(DsmApiClientTests.ApiInformationJson) }));

    #region Ordering

    [Fact]
    public async Task GetReportAsync_AClosedPort_FailsTheFirstCheckAndSkipsTheRest()
    {
        var report = (await CreateService().GetReportAsync()).Value;

        Assert.NotNull(report);
        Assert.False(report.IsHealthy);
        Assert.Equal(IPAddress.Loopback.ToString(), report.Host);
        Assert.Equal(_settings.Object.Port, report.Port);
        Assert.Equal(
            [
                new(DsmConnectionCheck.PortAnswers, DsmCheckStatus.Failed, DsmCheckFailure.PortRefused),
                new(DsmConnectionCheck.ConnectionTrusted, DsmCheckStatus.Skipped),
                new DsmConnectionCheckResult(DsmConnectionCheck.ApiAnswers, DsmCheckStatus.Skipped)
            ],
            report.Checks);
    }

    [Fact]
    public async Task GetReportAsync_WhenEveryCheckPasses_IsHealthy()
    {
        // Otherwise every visitor gets the diagnostic in place of the login form
        await using var server = StartDsm();

        var report = (await CreateService(rootListens: true, HealthyDsmApi()).GetReportAsync()).Value;

        // And, healthy, it tells an anonymous visitor nothing more
        Assert.NotNull(report);
        Assert.True(report.IsHealthy);
        Assert.Empty(report.Checks);
        Assert.Null(report.Host);
    }

    [Fact]
    public async Task GetReportAsync_AnUntrustedConnection_NeverAsksTheApi()
    {
        // DSM's API answers, but the policy refused the connection the login would use
        await using var server = StartDsm();

        var report = (await CreateService(rootListens: false, HealthyDsmApi()).GetReportAsync()).Value;

        Assert.NotNull(report);
        Assert.Equal(
            [
                new(DsmConnectionCheck.PortAnswers, DsmCheckStatus.Passed),
                new(DsmConnectionCheck.ConnectionTrusted, DsmCheckStatus.Failed, DsmCheckFailure.NotTrusted),
                new DsmConnectionCheckResult(DsmConnectionCheck.ApiAnswers, DsmCheckStatus.Skipped)
            ],
            report.Checks);
    }

    [Fact]
    public async Task GetReportAsync_WhenTheApiNeverAnswers_GivesUpWithinItsStep()
    {
        // DSM's API frozen behind a live nginx; a stall that ignores its token too, as a blocking DNS lookup does.
        // Unbounded, the login's diagnostic outlasted the browser's wait
        await using var server = StartDsm();
        var api = DsmApi(async () =>
        {
            await Task.Delay(Timeout.Infinite);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var report = (await CreateService(rootListens: true, api).GetReportAsync().WaitAsync(TimeSpan.FromSeconds(10))).Value;

        Assert.NotNull(report);
        Assert.Equal(new DsmConnectionCheckResult(DsmConnectionCheck.ApiAnswers, DsmCheckStatus.Failed, DsmCheckFailure.UnexpectedResponse), report.Checks[^1]);
    }

    [Fact]
    public async Task GetReportAsync_WhenTheSettingsCannotBeReloaded_ReportsItAsUnexpected()
    {
        // A bad ADWH_DSM_URL is caught at startup; one that appears later must still end in a report
        _settings.Setup(s => s.Reload()).Throws(new InvalidOperationException("bad URL"));

        var report = (await CreateService().GetReportAsync()).Value;

        Assert.NotNull(report);
        Assert.Equal(new DsmConnectionCheckResult(DsmConnectionCheck.PortAnswers, DsmCheckStatus.Failed, DsmCheckFailure.Unexpected), report.Checks[0]);
        Assert.All(report.Checks.Skip(1), check => Assert.Equal(DsmCheckStatus.Skipped, check.Status));
    }

    [Fact]
    public async Task GetReportAsync_WhenACheckThrows_ReportsItAsUnexpectedAndCachesTheReport()
    {
        // A port out of range: TcpClient.ConnectAsync throws instead of failing
        _settings.SetupGet(s => s.Port).Returns(70000);

        var service = CreateService();
        var report = (await service.GetReportAsync()).Value;
        await service.GetReportAsync();

        Assert.NotNull(report);
        Assert.Equal(new DsmConnectionCheckResult(DsmConnectionCheck.PortAnswers, DsmCheckStatus.Failed, DsmCheckFailure.Unexpected), report.Checks[0]);
        Assert.All(report.Checks.Skip(1), check => Assert.Equal(DsmCheckStatus.Skipped, check.Status));
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
        var healthy = new DsmConnectionReport(true, checkedAt, IPAddress.Loopback.ToString(), DsmPort, [new(DsmConnectionCheck.PortAnswers, DsmCheckStatus.Passed)]);

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
        var unhealthy = new DsmConnectionReport(false, DateTimeOffset.UtcNow, IPAddress.Loopback.ToString(), DsmPort, []);

        Assert.Same(unhealthy, DsmConnectionDiagnosticService.ForAnonymousVisitor(unhealthy));
    }

    #endregion
}
