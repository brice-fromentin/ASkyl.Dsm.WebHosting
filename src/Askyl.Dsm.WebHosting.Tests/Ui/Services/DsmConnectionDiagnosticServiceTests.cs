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

        return new DsmConnectionDiagnosticService(_settings.Object, apiClient, Mock.Of<ILogger<ILogDsmConnectionDiagnosticService>>());
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

    #endregion
}
