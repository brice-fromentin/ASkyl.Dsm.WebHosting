using System.Net;
using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Domain.Diagnostics;
using Askyl.Dsm.WebHosting.Data.Results;
using Askyl.Dsm.WebHosting.Logging;
using Askyl.Dsm.WebHosting.Tools.Network;
using Askyl.Dsm.WebHosting.Tools.Threading;

namespace Askyl.Dsm.WebHosting.Ui.Services;

/// <summary>
/// Runs the DSM connection checks in order, stopping at the first failure, and keeps the latest report.
/// </summary>
/// <param name="settings">DSM settings, reloaded at the start of every run.</param>
/// <param name="apiClient">DSM API client, for the SYNO.API.Info probe.</param>
/// <param name="certificatePolicy">The certificate policy of the DSM HTTP client, so the checklist decides as it does.</param>
/// <param name="logger">Logger for the outcome of each run.</param>
public sealed class DsmConnectionDiagnosticService(
    IDsmSettingsService settings, DsmApiClient apiClient, DsmCertificatePolicy certificatePolicy, ILogger<ILogDsmConnectionDiagnosticService> logger)
    : IDsmConnectionDiagnosticService, ISemaphoreOwner, IDisposable
{
    static readonly TimeSpan StepTimeout = TimeSpan.FromSeconds(ApplicationConstants.DsmDiagnosticStepTimeoutSeconds);

    // Replaced whole after each run, and read without the lock
    private volatile DsmConnectionReport? _report;

    private int _runsStarted;

    public SemaphoreSlim Semaphore { get; } = new(1, 1);

    public void Dispose() => Semaphore.Dispose();

    /// <inheritdoc/>
    public async Task<DsmConnectionReportResult> GetReportAsync(CancellationToken cancellationToken = default)
        => DsmConnectionReportResult.CreateSuccess(ForAnonymousVisitor(_report ?? await RunAsync(reuseAnyReport: true)));

    /// <inheritdoc/>
    public async Task<DsmConnectionReportResult> RecheckAsync(CancellationToken cancellationToken = default)
        => DsmConnectionReportResult.CreateSuccess(ForAnonymousVisitor(await RunAsync(reuseAnyReport: false)));

    /// <summary>
    /// A healthy DSM needs no explanation, so an anonymous visitor learns nothing about it. An unhealthy one
    /// shows its configured hostname, but never an IP address, which would disclose the NAS's network position.
    /// </summary>
    internal static DsmConnectionReport ForAnonymousVisitor(DsmConnectionReport report)
        => report.IsHealthy ? new(true, report.CheckedAt, null, 0, [])
         : report.Host is { } host && IPAddress.TryParse(host.Trim().TrimEnd('.'), out var address) && !IPAddress.IsLoopback(address) ? report with { Host = null, HostIsIpAddress = true }
         : report;

    async Task<DsmConnectionReport> RunAsync(bool reuseAnyReport)
    {
        var runsBeforeWaiting = Volatile.Read(ref _runsStarted);

        // The caller's token is deliberately not used: a visitor closing the tab must not cancel a run that
        // another visitor is waiting for. Every step is bounded by StepTimeout instead.
        using var @lock = await SemaphoreLock.AcquireAsync(this);

        // A run that started after this caller asked read the same file this caller would read
        if (_report is { } current && (reuseAnyReport || _runsStarted > runsBeforeWaiting))
        {
            return current;
        }

        _runsStarted++;

        var report = await RunChecksAsync();

        LogOutcome(report);
        _report = report;

        return report;
    }

    async Task<DsmConnectionReport> RunChecksAsync()
    {
        var checkedAt = DateTimeOffset.UtcNow;
        var results = new List<DsmConnectionCheckResult>();

        var port = 0;

        try
        {
            results.Add(Result(DsmConnectionCheck.HostnameConfigured, ReloadSettings()));

            // Empty only when the first check failed, and then nothing below runs
            var host = settings.Server ?? String.Empty;

            port = settings.Port;
            IPAddress[] addresses = [];

            if (LastPassed(results))
            {
                var (resolution, resolved) = await DsmConnectionChecks.ResolveAsync(host, StepTimeout);

                addresses = resolved;
                results.Add(Result(DsmConnectionCheck.NameResolves, resolution));
            }

            if (LastPassed(results))
            {
                results.Add(Result(DsmConnectionCheck.PortAnswers, await DsmConnectionChecks.ConnectAsync(addresses, port, StepTimeout)));
            }

            if (LastPassed(results))
            {
                var (failure, certificate) = await DsmConnectionChecks.InspectCertificateAsync(host, addresses, port, certificatePolicy, StepTimeout);
                results.Add(Result(DsmConnectionCheck.CertificateCoversName, failure) with { Certificate = certificate });
            }

            if (LastPassed(results))
            {
                results.Add(await ProbeApiAsync());
            }
        }
        catch (Exception ex)
        {
            // Whatever a check fails to classify still ends in a report, never an error page
            var running = Enum.GetValues<DsmConnectionCheck>()[results.Count];

            logger.DsmConnectionCheckThrew(running.ToString(), ex);
            results.Add(new(running, DsmCheckStatus.Failed, DsmCheckFailure.Unexpected));
        }

        results.AddRange(Enum.GetValues<DsmConnectionCheck>().Skip(results.Count).Select(check => new DsmConnectionCheckResult(check, DsmCheckStatus.Skipped)));

        return new(results.TrueForAll(result => result.Status == DsmCheckStatus.Passed), checkedAt, settings.Server, port, results);
    }

    DsmCheckFailure ReloadSettings()
    {
        try
        {
            settings.Reload();
        }
        catch (InvalidOperationException)
        {
            return DsmCheckFailure.SettingsUnreadable;
        }

        return settings.Server is null ? DsmCheckFailure.HostnameMissing : DsmCheckFailure.None;
    }

    async Task<DsmConnectionCheckResult> ProbeApiAsync()
    {
        using var cancellation = new CancellationTokenSource(StepTimeout);

        try
        {
            // WaitAsync bounds the await itself: a blocking DNS lookup inside HttpClient ignores its token on Linux and macOS
            var (answered, statusCode) = await apiClient.ProbeAsync(cancellation.Token).WaitAsync(cancellation.Token);

            return answered
                ? new(DsmConnectionCheck.ApiAnswers, DsmCheckStatus.Passed)
                : new(DsmConnectionCheck.ApiAnswers, DsmCheckStatus.Failed, DsmCheckFailure.UnexpectedResponse, HttpStatus: statusCode);
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or TimeoutException)
        {
            return new(DsmConnectionCheck.ApiAnswers, DsmCheckStatus.Failed, DsmCheckFailure.UnexpectedResponse);
        }
    }

    void LogOutcome(DsmConnectionReport report)
    {
        var failed = report.Checks.FirstOrDefault(check => check.Status == DsmCheckStatus.Failed);

        if (failed is null)
        {
            logger.DsmConnectionHealthy(report.Host ?? String.Empty, report.Port);
            return;
        }

        logger.DsmConnectionFailed(failed.Check.ToString(), failed.Failure.ToString(), report.Host ?? String.Empty, report.Port);
    }

    static DsmConnectionCheckResult Result(DsmConnectionCheck check, DsmCheckFailure failure)
        => new(check, failure == DsmCheckFailure.None ? DsmCheckStatus.Passed : DsmCheckStatus.Failed, failure);

    static bool LastPassed(List<DsmConnectionCheckResult> results) => results[^1].Status == DsmCheckStatus.Passed;
}
