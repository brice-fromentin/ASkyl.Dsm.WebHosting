# DSM Connection Diagnostic Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or
> superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** The host starts whatever `synoinfo.conf` says, and the login page shows a live five-check diagnostic
of the DSM connection, with remedies and a re-check, whenever DSM cannot be reached.

**Architecture:** `DsmSettingsService` stops throwing on a missing hostname and gains `Reload()`. Stateless
checks live in `Ui/Services/DsmConnectionChecks.cs`; `DsmConnectionDiagnosticService` (Singleton) runs them
in order, caches the report and coalesces re-runs. The usual dual implementation exposes it: contract in
`Data/Contracts/`, anonymous controller, HTTP proxy in `Ui.Client/Services/`. `Login.razor` renders
`DsmDiagnosticPanel` in place of the form when the report is unhealthy.

**Tech Stack:** .NET 10, ASP.NET Core controllers, Blazor WebAssembly, FluentUI Blazor 5.0.1, xUnit, Moq, bUnit.

**Spec:** `docs/ai/specs/2026-10-25-dsm-connection-diagnostic-design.md`. Branch `feat/dsm-connection-diagnostic`,
draft PR #82, one commit per task.

## Global Constraints

- Everything persisted is English. No AI attribution in any commit or PR text (AGENTS.md §6.1).
- After every task: `dotnet format ./src/Askyl.Dsm.WebHosting.slnx --verbosity quiet`, then
  `dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx`, then the test command redirected to a file with
  its exit code checked — never piped:
  `dotnet test ./src/Askyl.Dsm.WebHosting.Tests --no-build > "$SCRATCH/test.log" 2>&1; echo "exit=$?"`.
  Zero warnings. A single test: append `--filter "FullyQualifiedName~<Name>"`.
- Rebuild before every test run: `--no-build` runs the last binary.
- Never commit a hostname, IP, port or credential read from local state; run
  `grep -Ecf .git/forbidden-patterns <file>` on every commit message and PR text before publishing.
- **The certificate check observes, it never bypasses:** its callback returns exactly
  `errors == SslPolicyErrors.None`.
- The report carries no resolved IP address, no exception message, no stack trace. The configured hostname
  is shown as configured. A healthy report carries only `IsHealthy` and `CheckedAt`.
- The check target comes from `synoinfo.conf` only, never from the request.
- A missing `synoinfo.conf` still throws at startup.
- Each network step is bounded by `ApplicationConstants.DsmDiagnosticStepTimeoutSeconds` = 3. The spec said
  about 5 s; 3 keeps the four network steps inside the client's 15 s `HttpClientTimeoutSeconds`.
- Magic strings and numbers go to `Askyl.Dsm.WebHosting.Constants`; logging only through `[LoggerMessage]`;
  primary constructors; declarations with ≤ 4 parameters on one line.
- FluentUI components only, no inline style; `UiSourceAuditTests` must stay green.
- Version 0.8.1.

## File Map

| File | Task | Responsibility |
|------|------|----------------|
| `Data/Domain/DsmSystem/DsmSystemPreferences.cs` | 1 | `Server` becomes nullable |
| `Data/Contracts/IDsmSettingsService.cs` | 1 | `string? Server`, `Reload()` |
| `Tools/Infrastructure/DsmSettingsService.cs` | 1 | No throw on missing hostname; atomic reload |
| `Tools/Network/DsmApiClient.cs` | 1, 2 | Fail fast without hostname; `ProbeAsync` |
| `Data/Domain/Diagnostics/*.cs` (6 files) | 2 | Report types and enums |
| `Ui/Services/DsmConnectionChecks.cs` | 2 | Stateless checks 2–4 and TLS classification |
| `Data/Results/DsmConnectionReportResult.cs` | 3 | Result wrapper |
| `Data/Contracts/IDsmConnectionDiagnosticService.cs` | 3 | Contract |
| `Ui/Services/DsmConnectionDiagnosticService.cs` | 3 | Orchestration, cache, coalescing, logging |
| `Logging/Server/DsmApi/DsmConnectionDiagnosticLoggingExtensions.cs` | 3 | Two log methods |
| `Constants/WebApi/DsmDiagnosticRoutes.cs` | 3 | Routes |
| `Ui/Controllers/DsmDiagnosticController.cs` | 3 | Anonymous GET and rate-limited POST |
| `Ui.Client/Services/DsmConnectionDiagnosticService.cs` | 3 | HTTP proxy |
| `Ui/Services/AuthenticationService.cs` | 4 | Diagnostic on login failure |
| `Ui.Client/Components/Controls/DsmDiagnosticPanel.razor` | 5 | Checklist component |
| `Ui.Client/Components/Pages/Login.razor` | 5 | Panel in place of the form |

All paths are under `src/Askyl.Dsm.WebHosting.` + project, abbreviated as above.

---

### Task 1: Settings tolerate a missing hostname and reload

**Files:**

- Modify: `src/Askyl.Dsm.WebHosting.Data/Domain/DsmSystem/DsmSystemPreferences.cs`
- Modify: `src/Askyl.Dsm.WebHosting.Data/Contracts/IDsmSettingsService.cs`
- Modify: `src/Askyl.Dsm.WebHosting.Tools/Infrastructure/DsmSettingsService.cs`
- Modify: `src/Askyl.Dsm.WebHosting.Tools/Network/DsmApiClient.cs`
- Modify: `src/Askyl.Dsm.WebHosting.Tests/Tools/Infrastructure/DsmSettingsServiceTests.cs`
- Modify: `src/Askyl.Dsm.WebHosting.Tests/Tools/Network/DsmApiClientTests.cs`
- Modify: `src/Askyl.Dsm.WebHosting.Tests/Ui/ApplicationHostFactory.cs`
- Modify: `src/Askyl.Dsm.WebHosting.Tests/Ui/ApplicationStartupTests.cs`

**Interfaces:**

- Produces: `IDsmSettingsService.Server` is `string?`; `void IDsmSettingsService.Reload()` throws
  `InvalidOperationException` when the file is missing or unreadable and then keeps the previous values.
- Produces: `DsmApiClient` throws `InvalidOperationException("DSM hostname is not configured")` before sending
  anything when `Server` is null.
- Produces (tests): `ApplicationHostFactory.ConfigPathSettingKey` (internal const), `ApplicationHostFactory.StateDirectory`,
  and `ApplicationStartupTests.WithoutDsmHostname()`.

- [ ] **Step 1: Write the failing settings tests**

In `DsmSettingsServiceTests.cs`, replace the two tests of the `Missing Mandatory Settings` region and add a
`Reload` region before `Malformed Configuration`:

```csharp
    #region Missing Hostname

    [Fact]
    public void Constructor_MissingServerIp_LeavesServerNull()
    {
        // Arrange — a fresh DSM has no external_host_ip until External Access is configured
        _fileReader.Setup(f => f.FileExists(SystemDefaults.SynoInfoConfPath)).Returns(true);
        _fileReader.Setup(f => f.ReadAllLines(SystemDefaults.SynoInfoConfPath))
                   .Returns([
                       "external_port_dsm_https=5001",
                   ]);

        // Act
        var service = new DsmSettingsService(_logger.Object, _fileReader.Object, _configuration);

        // Assert — the host must start, so the diagnostic can say what is missing
        Assert.Null(service.Server);
        Assert.Equal(5001, service.Port);
    }

    [Fact]
    public void Constructor_EmptyServerIp_LeavesServerNull()
    {
        // Arrange
        _fileReader.Setup(f => f.FileExists(SystemDefaults.SynoInfoConfPath)).Returns(true);
        _fileReader.Setup(f => f.ReadAllLines(SystemDefaults.SynoInfoConfPath))
                   .Returns([
                       "external_host_ip=",
                       "external_port_dsm_https=5001",
                   ]);

        // Act
        var service = new DsmSettingsService(_logger.Object, _fileReader.Object, _configuration);

        // Assert
        Assert.Null(service.Server);
    }

    #endregion

    #region Reload

    [Fact]
    public void Reload_PicksUpAChangedFile()
    {
        // Arrange — the hostname is set in DSM after the package started
        _fileReader.Setup(f => f.FileExists(SystemDefaults.SynoInfoConfPath)).Returns(true);
        _fileReader.SetupSequence(f => f.ReadAllLines(SystemDefaults.SynoInfoConfPath))
                   .Returns(["external_port_dsm_https=5001"])
                   .Returns(["external_host_ip=10.0.0.9", "external_port_dsm_https=8443"]);

        var service = new DsmSettingsService(_logger.Object, _fileReader.Object, _configuration);

        // Act
        service.Reload();

        // Assert
        Assert.Equal("10.0.0.9", service.Server);
        Assert.Equal(8443, service.Port);
    }

    [Fact]
    public void Reload_WhenTheFileIsGone_ThrowsAndKeepsThePreviousValues()
    {
        // Arrange
        _fileReader.SetupSequence(f => f.FileExists(SystemDefaults.SynoInfoConfPath))
                   .Returns(true)
                   .Returns(false);
        _fileReader.Setup(f => f.ReadAllLines(SystemDefaults.SynoInfoConfPath))
                   .Returns(["external_host_ip=10.0.0.1"]);

        var service = new DsmSettingsService(_logger.Object, _fileReader.Object, _configuration);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(service.Reload);
        Assert.Equal("10.0.0.1", service.Server);
    }

    #endregion
```

- [ ] **Step 2: Write the failing client and startup tests**

In `DsmApiClientTests.cs`, let `CreateClient` take the settings, fix the assertion that reads `Server`, and add
a test. Add `using Askyl.Dsm.WebHosting.Data.Contracts;`.

```csharp
    DsmApiClient CreateClient(IDsmSettingsService? settings = null)
    {
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(_httpClient);

        var client = new DsmApiClient(factory.Object, settings ?? _settingsService, _logger.Object);
```

The rest of `CreateClient` is unchanged. At line 227 replace `Assert.Contains(_settingsService.Server, receivedUrl);`
with `Assert.Contains(FakeDsmSettings.Server, receivedUrl);`. Then add this region after the
`Cookie Header` region:

```csharp
    #region Missing Hostname

    [Fact]
    public async Task ExecuteAsync_WithoutAHostname_FailsBeforeSendingAnything()
    {
        // Arrange — without the guard the URL would read "https://:5001/…" and fail somewhere less telling
        var settings = new Mock<IDsmSettingsService>();
        settings.SetupGet(s => s.Port).Returns(FakeDsmSettings.Port);

        var client = CreateClient(settings.Object);

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => client.ExecuteAsync<TestResponse>(null, new TestFormParameters()));

        // Assert
        Assert.Contains("hostname", ex.Message, StringComparison.Ordinal);
        _httpHandler.Protected().Verify("SendAsync", Times.Never(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    #endregion
```

In `ApplicationHostFactory.cs`, change `const string ConfigPathSettingKey` to `internal const string ConfigPathSettingKey`,
and add under `#region Properties`:

```csharp
    /// <summary>
    /// The throwaway directory holding every file this factory writes; deleted with the factory.
    /// </summary>
    public string StateDirectory => _stateDirectory;
```

In `ApplicationStartupTests.cs`, add `using Askyl.Dsm.WebHosting.Constants.DSM.System;`,
`using Askyl.Dsm.WebHosting.Tests.Tools.Infrastructure;`, `using Askyl.Dsm.WebHosting.Ui.Components;`,
`using Microsoft.AspNetCore.Hosting;` and `using Microsoft.Extensions.Configuration;`, then:

```csharp
    const string HostnamelessSettingsFileName = "synoinfo-without-hostname.conf";

    [Fact]
    public async Task Application_StartsWithoutADsmHostname()
    {
        // A fresh DSM has no external_host_ip. The host used to die resolving DsmSettingsService at startup,
        // with the reason only in a log the next start truncates.
        using var hostnameless = WithoutDsmHostname();

        var response = await hostnameless.CreateClient().GetAsync(ApplicationConstants.ApplicationUrlSubPath);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// A host reading a synoinfo.conf without external_host_ip. Its configuration is added after the
    /// factory's own, so it wins.
    /// </summary>
    WebApplicationFactory<App> WithoutDsmHostname()
    {
        var settingsPath = Path.Combine(factory.StateDirectory, HostnamelessSettingsFileName);

        Directory.CreateDirectory(factory.StateDirectory);
        File.WriteAllLines(settingsPath, [$"{SystemDefaults.KeyExternalHttpsPort}=\"{FakeDsmSettings.Port}\""]);

        return factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(
            new Dictionary<string, string?> { [ApplicationHostFactory.ConfigPathSettingKey] = settingsPath })));
    }
```

- [ ] **Step 3: Build and watch the tests fail**

Run format, build, then the test command with
`--filter "FullyQualifiedName~DsmSettingsServiceTests|FullyQualifiedName~DsmApiClientTests|FullyQualifiedName~ApplicationStartupTests"`.
Expected: the build fails on `service.Reload` (no such method). That is the first failure; keep going.

- [ ] **Step 4: Implement**

`DsmSystemPreferences.cs`: change the parameter to `string? server` and the property to
`public string? Server { get; init; } = server;`; doc of `server`: "External hostname from configuration; null
when DSM has none."

`IDsmSettingsService.cs`:

```csharp
namespace Askyl.Dsm.WebHosting.Data.Contracts;

/// <summary>
/// DSM system preferences from /etc/synoinfo.conf, read at startup and again on <see cref="Reload"/>.
/// </summary>
public interface IDsmSettingsService
{
    /// <summary>
    /// The DSM hostname (external_host_ip), or null when DSM has none configured.
    /// </summary>
    string? Server { get; }

    /// <summary>
    /// The DSM HTTPS port (e.g. 5001).
    /// </summary>
    int Port { get; }

    /// <summary>
    /// The system language in DSM format (e.g. "enu", "fra").
    /// </summary>
    string Language { get; }

    /// <summary>
    /// Re-reads the settings file, so a change made in DSM applies without a restart.
    /// Throws <see cref="InvalidOperationException"/> when the file is missing or unreadable, keeping the previous values.
    /// </summary>
    void Reload();
}
```

`DsmSettingsService.cs` — class doc, field, members and `ReadSettings`; delete `GetMandatorySetting`:

```csharp
/// <summary>
/// Reads DSM system preferences from /etc/synoinfo.conf at startup and again on <see cref="Reload"/>.
/// Throws if the file is missing or unreadable; a missing hostname leaves <see cref="Server"/> null.
/// </summary>
/// <param name="logger">Logger instance.</param>
/// <param name="fileReader">File system abstraction for reading configuration files.</param>
/// <param name="configuration">Application configuration for resolving the settings file path.</param>
public sealed class DsmSettingsService(ILogger<ILogDsmSettingsService> logger, IFileReader fileReader, IConfiguration configuration) : IDsmSettingsService
{
    // Swapped whole by Reload, so a reader sees one file's values, never a mix of two
    private volatile DsmSystemPreferences _preferences = ReadSettings(logger, fileReader, configuration);

    public string? Server => _preferences.Server;

    public int Port => _preferences.Port;

    public string Language => _preferences.Language;

    public void Reload() => _preferences = ReadSettings(logger, fileReader, configuration);
```

and in `ReadSettings`, replace the `server` line and the two `catch` blocks:

```csharp
            // Left null rather than thrown: the connection diagnostic names what is missing
            var server = settings.TryGetValue(SystemDefaults.KeyExternalHostIp, out var host) && host.Length > 0 ? host : null;

            if (server is null)
            {
                logger.MandatorySettingMissing(SystemDefaults.KeyExternalHostIp);
            }

            var language = settings.TryGetValue(SystemDefaults.KeyLanguage, out var lang) && lang.Length > 0 ? lang : SystemDefaults.DefaultLanguage;
            var port = Int32.TryParse(settings.TryGetValue(SystemDefaults.KeyExternalHttpsPort, out var p) ? p : null, out var parsedPort) ? parsedPort : SystemDefaults.DefaultHttpsPort;

            return new DsmSystemPreferences(server, port, language);
        }
        catch (Exception ex)
        {
            logger.SettingsReadFailed(ex);
            throw new InvalidOperationException($"Failed to read DSM settings from '{configPath}'.", ex);
        }
```

`DsmApiClient.cs`: in `ExecuteAsync` and `HandShakeAsync` replace `settingsService.Server` with `RequireServer()`,
and add under `#region HTTP Helpers`:

```csharp
    // Fails before building "https://:5001/…", so a missing hostname surfaces as itself
    private string RequireServer()
        => settingsService.Server ?? throw new InvalidOperationException("DSM hostname is not configured");
```

- [ ] **Step 5: Run the tests and watch them pass**

Same filter as step 3. Expected: PASS, exit 0.

- [ ] **Step 6: Prove the startup test by reverting**

Temporarily make `ReadSettings` throw `new InvalidOperationException("probe")` when `server is null`. Rebuild, run
`--filter "FullyQualifiedName~Application_StartsWithoutADsmHostname"`. Expected: FAIL, the exception naming
`probe` thrown while building the host. Restore, rebuild, run again: PASS.

- [ ] **Step 7: Full sequence and commit**

Format, build, full test run, exit 0. Then:

```bash
git add -A src
git commit -m "fix: start the host without a DSM hostname" -m "A fresh DSM has no external_host_ip, and the host died resolving the
settings at startup. Server is now null there, and Reload() lets a
later change in DSM apply without a restart."
```

---

### Task 2: The connection checks

**Files:**

- Create: `src/Askyl.Dsm.WebHosting.Data/Domain/Diagnostics/DsmConnectionCheck.cs`
- Create: `src/Askyl.Dsm.WebHosting.Data/Domain/Diagnostics/DsmCheckStatus.cs`
- Create: `src/Askyl.Dsm.WebHosting.Data/Domain/Diagnostics/DsmCheckFailure.cs`
- Create: `src/Askyl.Dsm.WebHosting.Data/Domain/Diagnostics/DsmCertificateSummary.cs`
- Create: `src/Askyl.Dsm.WebHosting.Data/Domain/Diagnostics/DsmConnectionCheckResult.cs`
- Create: `src/Askyl.Dsm.WebHosting.Data/Domain/Diagnostics/DsmConnectionReport.cs`
- Create: `src/Askyl.Dsm.WebHosting.Ui/Services/DsmConnectionChecks.cs`
- Modify: `src/Askyl.Dsm.WebHosting.Tools/Network/DsmApiClient.cs`
- Modify: `src/Askyl.Dsm.WebHosting.Constants/Application/ApplicationConstants.cs`
- Create: `src/Askyl.Dsm.WebHosting.Tests/Ui/Services/DsmConnectionChecksTests.cs`
- Modify: `src/Askyl.Dsm.WebHosting.Tests/Tools/Network/DsmApiClientTests.cs`

**Interfaces:**

- Consumes: `DsmApiClient.RequireServer()` from Task 1.
- Produces: the domain types below, exactly; `internal static class DsmConnectionChecks` with
  `Task<DsmCheckFailure> ResolveAsync(string host, TimeSpan timeout)`,
  `Task<DsmCheckFailure> ConnectAsync(string host, int port, TimeSpan timeout)`,
  `Task<(DsmCheckFailure Failure, DsmCertificateSummary? Certificate)> InspectCertificateAsync(string host, int port, TimeSpan timeout)`,
  `DsmCheckFailure ClassifyCertificateErrors(SslPolicyErrors errors, X509ChainStatusFlags chainStatus)`;
  `Task<(bool Answered, int StatusCode)> DsmApiClient.ProbeAsync(CancellationToken cancellationToken)`;
  `ApplicationConstants.DsmDiagnosticStepTimeoutSeconds`.

- [ ] **Step 1: Add the domain types**

`DsmConnectionCheck.cs`:

```csharp
namespace Askyl.Dsm.WebHosting.Data.Domain.Diagnostics;

/// <summary>
/// The checks of the DSM connection diagnostic, in the order they run.
/// </summary>
public enum DsmConnectionCheck
{
    /// <summary>synoinfo.conf names a DSM hostname.</summary>
    HostnameConfigured,

    /// <summary>The hostname resolves from the NAS.</summary>
    NameResolves,

    /// <summary>DSM accepts a TCP connection on its HTTPS port.</summary>
    PortAnswers,

    /// <summary>DSM's certificate passes full validation for the hostname.</summary>
    CertificateCoversName,

    /// <summary>SYNO.API.Info answers with the API list.</summary>
    ApiAnswers
}
```

`DsmCheckStatus.cs`:

```csharp
namespace Askyl.Dsm.WebHosting.Data.Domain.Diagnostics;

/// <summary>
/// Outcome of one diagnostic check.
/// </summary>
public enum DsmCheckStatus
{
    /// <summary>The check passed.</summary>
    Passed,

    /// <summary>The check failed; the run stopped here.</summary>
    Failed,

    /// <summary>The check did not run, because an earlier one failed.</summary>
    Skipped
}
```

`DsmCheckFailure.cs`:

```csharp
namespace Askyl.Dsm.WebHosting.Data.Domain.Diagnostics;

/// <summary>
/// Why a diagnostic check failed. Carried as data so the client can localize it.
/// </summary>
public enum DsmCheckFailure
{
    /// <summary>No failure.</summary>
    None,

    /// <summary>synoinfo.conf has no external_host_ip.</summary>
    HostnameMissing,

    /// <summary>synoinfo.conf could not be read.</summary>
    SettingsUnreadable,

    /// <summary>The hostname does not resolve.</summary>
    NameNotFound,

    /// <summary>The port refused the connection.</summary>
    PortRefused,

    /// <summary>The port did not answer in time.</summary>
    PortTimedOut,

    /// <summary>The certificate does not cover the hostname.</summary>
    CertificateNameMismatch,

    /// <summary>The certificate does not chain to a trusted authority.</summary>
    CertificateUntrusted,

    /// <summary>The certificate is outside its validity period.</summary>
    CertificateExpired,

    /// <summary>The TLS handshake failed without a certificate to blame.</summary>
    HandshakeFailed,

    /// <summary>SYNO.API.Info did not answer with the API list.</summary>
    UnexpectedResponse
}
```

`DsmCertificateSummary.cs`:

```csharp
using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.Domain.Diagnostics;

/// <summary>
/// What the diagnostic shows of DSM's certificate.
/// </summary>
/// <param name="Names">The DNS names the certificate covers; never an IP address.</param>
/// <param name="Issuer">The issuer's simple name.</param>
/// <param name="NotAfter">The end of the validity period.</param>
public record DsmCertificateSummary(IReadOnlyList<string> Names, string Issuer, DateTimeOffset NotAfter)
{
    /// <summary>
    /// Initializes an empty summary for deserialization.
    /// </summary>
    [JsonConstructor]
    public DsmCertificateSummary() : this([], String.Empty, default) { }
}
```

`DsmConnectionCheckResult.cs`:

```csharp
using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.Domain.Diagnostics;

/// <summary>
/// The outcome of one diagnostic check.
/// </summary>
/// <param name="Check">Which check.</param>
/// <param name="Status">Passed, failed or skipped.</param>
/// <param name="Failure">Why it failed; <see cref="DsmCheckFailure.None"/> otherwise.</param>
/// <param name="Certificate">DSM's certificate, for the certificate check when one was presented.</param>
/// <param name="HttpStatus">The HTTP status SYNO.API.Info answered with, when it answered.</param>
public record DsmConnectionCheckResult(
    DsmConnectionCheck Check,
    DsmCheckStatus Status,
    DsmCheckFailure Failure = DsmCheckFailure.None,
    DsmCertificateSummary? Certificate = null,
    int? HttpStatus = null)
{
    /// <summary>
    /// Initializes an empty result for deserialization.
    /// </summary>
    [JsonConstructor]
    public DsmConnectionCheckResult() : this(default, default) { }
}
```

`DsmConnectionReport.cs`:

```csharp
using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.Domain.Diagnostics;

/// <summary>
/// The result of one run of the DSM connection diagnostic.
/// </summary>
/// <param name="IsHealthy">Whether every check passed.</param>
/// <param name="CheckedAt">When the run started.</param>
/// <param name="Host">The configured DSM hostname; null when missing, and in a healthy report.</param>
/// <param name="Port">The configured DSM HTTPS port; 0 in a healthy report.</param>
/// <param name="Checks">Every check, in order; empty in a healthy report.</param>
public record DsmConnectionReport(
    bool IsHealthy,
    DateTimeOffset CheckedAt,
    string? Host,
    int Port,
    IReadOnlyList<DsmConnectionCheckResult> Checks)
{
    /// <summary>
    /// Initializes an empty report for deserialization.
    /// </summary>
    [JsonConstructor]
    public DsmConnectionReport() : this(false, default, null, 0, []) { }
}
```

In `ApplicationConstants.cs`, after `HttpClientTimeoutSeconds`:

```csharp
    /// <summary>
    /// Time allowed to each network step of the DSM connection diagnostic. Four such steps run in sequence,
    /// and together they must fit inside <see cref="HttpClientTimeoutSeconds"/>, the client's wait for the result.
    /// </summary>
    public const int DsmDiagnosticStepTimeoutSeconds = 3;
```

- [ ] **Step 2: Write the failing checks tests**

`DsmConnectionChecksTests.cs`:

```csharp
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Askyl.Dsm.WebHosting.Constants.Network;
using Askyl.Dsm.WebHosting.Data.Domain.Diagnostics;
using Askyl.Dsm.WebHosting.Ui.Services;

namespace Askyl.Dsm.WebHosting.Tests.Ui.Services;

public class DsmConnectionChecksTests
{
    /// <summary>
    /// Reserved by RFC 6761: never resolves, so the test needs no outside network.
    /// </summary>
    const string UnresolvableHost = "dsm.invalid";

    const string CertificateSubject = "CN=localhost";

    static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);

    static readonly string LoopbackAddress = IPAddress.Loopback.ToString();

    #region Name Resolution

    [Fact]
    public async Task ResolveAsync_AnUnresolvableName_ReportsNameNotFound()
        => Assert.Equal(DsmCheckFailure.NameNotFound, await DsmConnectionChecks.ResolveAsync(UnresolvableHost, Timeout));

    [Fact]
    public async Task ResolveAsync_AnIpLiteral_Passes()
        => Assert.Equal(DsmCheckFailure.None, await DsmConnectionChecks.ResolveAsync(LoopbackAddress, Timeout));

    #endregion

    #region Port

    [Fact]
    public async Task ConnectAsync_AClosedPort_ReportsPortRefused()
    {
        // Reserve a port, then release it: nothing listens there any more
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        Assert.Equal(DsmCheckFailure.PortRefused, await DsmConnectionChecks.ConnectAsync(LoopbackAddress, port, Timeout));
    }

    #endregion

    #region Certificate

    [Fact]
    public async Task InspectCertificateAsync_ASelfSignedCertificate_IsNeverAccepted()
    {
        // The guarantee that the check observes validation instead of bypassing it
        await using var server = LocalTlsServer.Start(CreateCertificate(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1)));

        var (failure, certificate) = await DsmConnectionChecks.InspectCertificateAsync(NetworkConstants.Localhost, server.Port, Timeout);

        Assert.Equal(DsmCheckFailure.CertificateUntrusted, failure);
        Assert.NotNull(certificate);
        Assert.Contains(NetworkConstants.Localhost, certificate.Names);
    }

    [Fact]
    public async Task InspectCertificateAsync_ANameTheCertificateDoesNotCover_ReportsNameMismatch()
    {
        await using var server = LocalTlsServer.Start(CreateCertificate(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1)));

        var (failure, _) = await DsmConnectionChecks.InspectCertificateAsync(LoopbackAddress, server.Port, Timeout);

        Assert.Equal(DsmCheckFailure.CertificateNameMismatch, failure);
    }

    [Fact]
    public async Task InspectCertificateAsync_AnExpiredCertificate_ReportsExpired()
    {
        await using var server = LocalTlsServer.Start(CreateCertificate(DateTimeOffset.UtcNow.AddDays(-2), DateTimeOffset.UtcNow.AddDays(-1)));

        var (failure, _) = await DsmConnectionChecks.InspectCertificateAsync(NetworkConstants.Localhost, server.Port, Timeout);

        Assert.Equal(DsmCheckFailure.CertificateExpired, failure);
    }

    [Theory]
    [InlineData(SslPolicyErrors.None)]
    [InlineData(SslPolicyErrors.RemoteCertificateNotAvailable)]
    public void ClassifyCertificateErrors_WithoutACertificateToBlame_ReportsHandshakeFailed(SslPolicyErrors errors)
        => Assert.Equal(DsmCheckFailure.HandshakeFailed, DsmConnectionChecks.ClassifyCertificateErrors(errors, X509ChainStatusFlags.NoError));

    #endregion

    #region Helpers

    static X509Certificate2 CreateCertificate(DateTimeOffset notBefore, DateTimeOffset notAfter)
    {
        using var key = RSA.Create();
        var request = new CertificateRequest(CertificateSubject, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var names = new SubjectAlternativeNameBuilder();
        names.AddDnsName(NetworkConstants.Localhost);
        request.CertificateExtensions.Add(names.Build());

        using var ephemeral = request.CreateSelfSigned(notBefore, notAfter);

        // A PKCS#12 round trip: macOS cannot serve TLS from the ephemeral key CreateSelfSigned returns
        return X509CertificateLoader.LoadPkcs12(ephemeral.Export(X509ContentType.Pkcs12), null);
    }

    /// <summary>
    /// Serves one TLS handshake on loopback with the given certificate.
    /// </summary>
    sealed class LocalTlsServer(X509Certificate2 certificate) : IAsyncDisposable
    {
        readonly TcpListener _listener = Listen();

        Task _handshake = Task.CompletedTask;

        public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

        public static LocalTlsServer Start(X509Certificate2 certificate)
        {
            var server = new LocalTlsServer(certificate);
            server._handshake = server.ServeOneHandshakeAsync();

            return server;
        }

        public async ValueTask DisposeAsync()
        {
            _listener.Stop();
            await _handshake;
            certificate.Dispose();
        }

        static TcpListener Listen()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();

            return listener;
        }

        async Task ServeOneHandshakeAsync()
        {
            try
            {
                using var client = await _listener.AcceptTcpClientAsync();
                await using var stream = new SslStream(client.GetStream());
                await stream.AuthenticateAsServerAsync(certificate);
            }
            catch (Exception ex) when (ex is AuthenticationException or IOException or SocketException or ObjectDisposedException)
            {
                // Expected: the client refuses the certificate and aborts the handshake
            }
        }
    }

    #endregion
}
```

- [ ] **Step 3: Write the failing probe tests**

In `DsmApiClientTests.cs`, add (the client is created without `CreateClient`, whose pre-filled cache would hide
a write):

```csharp
    #region Probe

    const string ApiInformationJson = """{"success":true,"data":{"SYNO.API.Auth":{"path":"entry.cgi","minVersion":1,"maxVersion":7}}}""";

    DsmApiClient CreateClientWithEmptyCache(HttpStatusCode status, string body)
    {
        _httpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => new HttpResponseMessage(status) { Content = new StringContent(body) });

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(_httpClient);

        return new DsmApiClient(factory.Object, _settingsService, _logger.Object);
    }

    [Fact]
    public async Task ProbeAsync_WhenDsmAnswers_ReportsItWithoutTouchingTheCache()
    {
        var client = CreateClientWithEmptyCache(HttpStatusCode.OK, ApiInformationJson);

        var (answered, statusCode) = await client.ProbeAsync(CancellationToken.None);

        Assert.True(answered);
        Assert.Equal((int)HttpStatusCode.OK, statusCode);
        Assert.Null(client.ApiInformations.Get(ApiConstants.Auth));
    }

    [Fact]
    public async Task ProbeAsync_WhenSomethingElseAnswers_ReportsItsStatus()
    {
        var client = CreateClientWithEmptyCache(HttpStatusCode.BadGateway, String.Empty);

        var (answered, statusCode) = await client.ProbeAsync(CancellationToken.None);

        Assert.False(answered);
        Assert.Equal((int)HttpStatusCode.BadGateway, statusCode);
    }

    [Fact]
    public async Task ProbeAsync_WhenAPageAnswers_ReportsNotDsm()
    {
        var client = CreateClientWithEmptyCache(HttpStatusCode.OK, "<html></html>");

        var (answered, _) = await client.ProbeAsync(CancellationToken.None);

        Assert.False(answered);
    }

    #endregion
```

- [ ] **Step 4: Build and watch the tests fail**

Expected: build errors on `DsmConnectionChecks` and `ProbeAsync`.

- [ ] **Step 5: Implement the checks**

`DsmConnectionChecks.cs`:

```csharp
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Askyl.Dsm.WebHosting.Data.Domain.Diagnostics;

namespace Askyl.Dsm.WebHosting.Ui.Services;

/// <summary>
/// The network checks of the DSM connection diagnostic. Stateless; each is bounded by its timeout and
/// reports a failure kind instead of throwing.
/// </summary>
internal static class DsmConnectionChecks
{
    /// <summary>
    /// Resolves the hostname. An IP literal passes without a lookup.
    /// </summary>
    internal static async Task<DsmCheckFailure> ResolveAsync(string host, TimeSpan timeout)
    {
        using var cancellation = new CancellationTokenSource(timeout);

        try
        {
            var addresses = await Dns.GetHostAddressesAsync(host, cancellation.Token);

            return addresses.Length > 0 ? DsmCheckFailure.None : DsmCheckFailure.NameNotFound;
        }
        catch (Exception ex) when (ex is SocketException or OperationCanceledException)
        {
            return DsmCheckFailure.NameNotFound;
        }
    }

    /// <summary>
    /// Opens a TCP connection to the port.
    /// </summary>
    internal static async Task<DsmCheckFailure> ConnectAsync(string host, int port, TimeSpan timeout)
    {
        using var cancellation = new CancellationTokenSource(timeout);
        using var client = new TcpClient();

        try
        {
            await client.ConnectAsync(host, port, cancellation.Token);

            return DsmCheckFailure.None;
        }
        catch (OperationCanceledException)
        {
            return DsmCheckFailure.PortTimedOut;
        }
        catch (SocketException)
        {
            return DsmCheckFailure.PortRefused;
        }
    }

    /// <summary>
    /// Runs a TLS handshake against the hostname with default validation, recording what DSM presented.
    /// </summary>
    internal static async Task<(DsmCheckFailure Failure, DsmCertificateSummary? Certificate)> InspectCertificateAsync(string host, int port, TimeSpan timeout)
    {
        using var cancellation = new CancellationTokenSource(timeout);

        DsmCertificateSummary? summary = null;
        var errors = SslPolicyErrors.None;
        var chainStatus = X509ChainStatusFlags.NoError;

        var options = new SslClientAuthenticationOptions
        {
            TargetHost = host,
            RemoteCertificateValidationCallback = (_, certificate, chain, policyErrors) =>
            {
                summary = certificate is X509Certificate2 presented ? Summarize(presented) : null;
                errors = policyErrors;
                chainStatus = chain?.ChainStatus.Aggregate(X509ChainStatusFlags.NoError, (all, status) => all | status.Status) ?? X509ChainStatusFlags.NoError;

                // Observes default validation and never relaxes it
                return policyErrors == SslPolicyErrors.None;
            }
        };

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(host, port, cancellation.Token);

            await using var stream = new SslStream(client.GetStream());
            await stream.AuthenticateAsClientAsync(options, cancellation.Token);

            return (DsmCheckFailure.None, summary);
        }
        catch (Exception ex) when (ex is AuthenticationException or IOException or SocketException or OperationCanceledException)
        {
            return (ClassifyCertificateErrors(errors, chainStatus), summary);
        }
    }

    /// <summary>
    /// Names what a failed handshake reported. Called only after a failure, so no error at all means the
    /// handshake broke for a reason other than the certificate.
    /// </summary>
    internal static DsmCheckFailure ClassifyCertificateErrors(SslPolicyErrors errors, X509ChainStatusFlags chainStatus)
    {
        if (errors == SslPolicyErrors.None || errors.HasFlag(SslPolicyErrors.RemoteCertificateNotAvailable))
        {
            return DsmCheckFailure.HandshakeFailed;
        }

        if (errors.HasFlag(SslPolicyErrors.RemoteCertificateNameMismatch))
        {
            return DsmCheckFailure.CertificateNameMismatch;
        }

        return chainStatus.HasFlag(X509ChainStatusFlags.NotTimeValid) ? DsmCheckFailure.CertificateExpired : DsmCheckFailure.CertificateUntrusted;
    }

    static DsmCertificateSummary Summarize(X509Certificate2 certificate)
    {
        // DNS names only: the report never carries an IP address
        List<string> names = [.. certificate.Extensions.OfType<X509SubjectAlternativeNameExtension>().SelectMany(extension => extension.EnumerateDnsNames())];

        if (names.Count == 0)
        {
            names.Add(certificate.GetNameInfo(X509NameType.SimpleName, forIssuer: false));
        }

        return new(names, certificate.GetNameInfo(X509NameType.SimpleName, forIssuer: true), new DateTimeOffset(certificate.NotAfter.ToUniversalTime()));
    }
}
```

If `InspectCertificateAsync_ASelfSignedCertificate_IsNeverAccepted` fails on `certificate.Names` only, the
extension collection did not return the typed SAN extension: select `extension.Oid?.Value` equal to a new
`NetworkConstants.SubjectAlternativeNameOid = "2.5.29.17"` and wrap `new X509SubjectAlternativeNameExtension(extension.RawData)`.

- [ ] **Step 6: Implement the probe**

In `DsmApiClient.cs`, under `#region HTTP Request calls`, after `ExecuteSimpleAsync`:

```csharp
    /// <summary>
    /// Sends the SYNO.API.Info request for the connection diagnostic, leaving the handshake cache alone.
    /// </summary>
    /// <returns>Whether DSM answered with its API list, and the HTTP status it answered with.</returns>
    public async Task<(bool Answered, int StatusCode)> ProbeAsync(CancellationToken cancellationToken)
    {
        var parameters = new InformationsQueryParameters();
        var url = parameters.BuildUrl(RequireServer(), settingsService.Port, ApiConstants.Handshake);

        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = parameters.ToForm() };
        using var response = await _httpClient.SendAsync(request, cancellationToken);

        var statusCode = (int)response.StatusCode;

        if (response.StatusCode != HttpStatusCode.OK)
        {
            return (false, statusCode);
        }

        try
        {
            var result = JsonSerializer.Deserialize<ApiInformationResponse>(await response.Content.ReadAsStringAsync(cancellationToken));

            return (result is { Success: true, Data.Count: > 0 }, statusCode);
        }
        catch (JsonException)
        {
            // Something other than DSM answered on that port
            return (false, statusCode);
        }
    }
```

- [ ] **Step 7: Run the tests and watch them pass**

Filter `FullyQualifiedName~DsmConnectionChecksTests|FullyQualifiedName~DsmApiClientTests`. Expected: PASS.

- [ ] **Step 8: Prove the no-bypass test by reverting**

Temporarily make the validation callback `return true;`. Rebuild, run
`--filter "FullyQualifiedName~IsNeverAccepted"`. Expected: FAIL, `None` instead of `CertificateUntrusted`.
Restore, rebuild, PASS.

- [ ] **Step 9: Full sequence and commit**

```bash
git add -A src
git commit -m "feat: add the DSM connection checks" -m "Resolution, port, certificate and SYNO.API.Info, each bounded and
reporting a failure kind. The certificate check records what DSM
presents and keeps default validation; a self-signed server proves it."
```

---

### Task 3: The diagnostic service, its endpoints and its client

**Files:**

- Create: `src/Askyl.Dsm.WebHosting.Data/Results/DsmConnectionReportResult.cs`
- Create: `src/Askyl.Dsm.WebHosting.Data/Contracts/IDsmConnectionDiagnosticService.cs`
- Create: `src/Askyl.Dsm.WebHosting.Ui/Services/DsmConnectionDiagnosticService.cs`
- Create: `src/Askyl.Dsm.WebHosting.Logging/Server/DsmApi/DsmConnectionDiagnosticLoggingExtensions.cs`
- Modify: `src/Askyl.Dsm.WebHosting.Constants/Logging/LogEventIds.cs`
- Create: `src/Askyl.Dsm.WebHosting.Constants/WebApi/DsmDiagnosticRoutes.cs`
- Modify: `src/Askyl.Dsm.WebHosting.Constants/Application/ApplicationConstants.cs`
- Create: `src/Askyl.Dsm.WebHosting.Ui/Controllers/DsmDiagnosticController.cs`
- Modify: `src/Askyl.Dsm.WebHosting.Ui/Program.cs`
- Create: `src/Askyl.Dsm.WebHosting.Ui.Client/Services/DsmConnectionDiagnosticService.cs`
- Modify: `src/Askyl.Dsm.WebHosting.Ui.Client/Program.cs`
- Create: `src/Askyl.Dsm.WebHosting.Tests/Ui/Services/DsmConnectionDiagnosticServiceTests.cs`
- Create: `src/Askyl.Dsm.WebHosting.Tests/Ui/Services/DsmConnectionDiagnosticClientTests.cs`
- Modify: `src/Askyl.Dsm.WebHosting.Tests/Ui/ApplicationStartupTests.cs`

**Interfaces:**

- Consumes: Task 1's `Reload()` and `WithoutDsmHostname()`; Task 2's checks, `ProbeAsync`, domain types and timeout.
- Produces: `IDsmConnectionDiagnosticService` with `Task<DsmConnectionReportResult> GetReportAsync(CancellationToken cancellationToken = default)`
  and `Task<DsmConnectionReportResult> RecheckAsync(CancellationToken cancellationToken = default)`;
  `DsmConnectionReportResult.CreateSuccess(DsmConnectionReport)` / `CreateFailure(string)`;
  `DsmDiagnosticRoutes.ReportFullRoute` / `RecheckFullRoute`; `ApplicationConstants.RateLimitPolicyDiagnostic`;
  `LK.Diagnostic.Unavailable` (key added here, text in Task 5).

- [ ] **Step 1: Add the contract, result, routes and constants**

`DsmConnectionReportResult.cs`:

```csharp
using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.Data.Domain.Diagnostics;

namespace Askyl.Dsm.WebHosting.Data.Results;

/// <summary>
/// Result carrying a DSM connection diagnostic report.
/// </summary>
public sealed class DsmConnectionReportResult(bool success, string? message, DsmConnectionReport? value, ApiErrorCode errorCode = default)
    : ApiResultData<DsmConnectionReport>(success, message, value, errorCode)
{
    [JsonConstructor]
    private DsmConnectionReportResult() : this(false, null, default!, ApiErrorCode.Failure) { }

    /// <summary>
    /// Creates a successful result with the report.
    /// </summary>
    /// <param name="value">The report.</param>
    public static DsmConnectionReportResult CreateSuccess(DsmConnectionReport value)
        => new(true, null, value, ApiErrorCode.None);

    /// <summary>
    /// Creates a failure result. The Value property will be null.
    /// </summary>
    /// <param name="message">Error message describing the failure.</param>
    public static DsmConnectionReportResult CreateFailure(string message)
        => new(false, message, null, ApiErrorCode.Failure);
}
```

`IDsmConnectionDiagnosticService.cs`:

```csharp
using Askyl.Dsm.WebHosting.Data.Results;

namespace Askyl.Dsm.WebHosting.Data.Contracts;

/// <summary>
/// Checks whether this application can reach its DSM, and says what to fix when it cannot.
/// Anonymous by design: it is what a visitor sees when signing in is impossible.
/// </summary>
public interface IDsmConnectionDiagnosticService
{
    /// <summary>
    /// Returns the latest report, running the checks only when none exists yet.
    /// </summary>
    Task<DsmConnectionReportResult> GetReportAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Re-reads the DSM settings and runs the checks again.
    /// </summary>
    Task<DsmConnectionReportResult> RecheckAsync(CancellationToken cancellationToken = default);
}
```

`DsmDiagnosticRoutes.cs`:

```csharp
namespace Askyl.Dsm.WebHosting.Constants.WebApi;

/// <summary>
/// Defines the DSM connection diagnostic API route constants.
/// </summary>
public static class DsmDiagnosticRoutes
{
    /// <summary>
    /// Base route of the DsmDiagnostic controller (versioned); a GET on it returns the report.
    /// </summary>
    public const string ControllerBaseRoute = "api/v1/dsm-diagnostic";

    /// <summary>
    /// Re-check endpoint route segment.
    /// </summary>
    public const string RecheckRoute = "recheck";

    /// <summary>
    /// Full route for the report endpoint.
    /// </summary>
    public const string ReportFullRoute = ControllerBaseRoute;

    /// <summary>
    /// Full route for the re-check endpoint.
    /// </summary>
    public static readonly string RecheckFullRoute = String.Join("/", ControllerBaseRoute, RecheckRoute);
}
```

In `ApplicationConstants.cs`, after `RateLimitPolicyLogin`:

```csharp
    /// <summary>
    /// Rate limiting policy name for re-running the DSM connection diagnostic. Its own bucket, with the login
    /// partitioning, so a re-check never spends a login attempt.
    /// </summary>
    public const string RateLimitPolicyDiagnostic = "diagnostic-throttle";
```

In `LocalizationKeys.cs`, add after `Login`:

```csharp
    /// <summary>DSM connection diagnostic strings.</summary>
    public static class Diagnostic
    {
        public const string Unavailable = "Diagnostic_Unavailable";
    }
```

and to both `.resx` files, after `Login_Authenticating`:

```xml
  <data name="Diagnostic_Unavailable" xml:space="preserve">
    <value>The DSM connection check is unavailable.</value>
  </data>
```

```xml
  <data name="Diagnostic_Unavailable" xml:space="preserve">
    <value>Le diagnostic de connexion à DSM est indisponible.</value>
  </data>
```

- [ ] **Step 2: Add the logging**

In `LogEventIds.cs`, after the `Application host` region:

```csharp
    #region DSM connection diagnostic — 3100000–3100999

    /// <summary>DsmConnectionDiagnosticService.</summary>
    public const int DsmConnectionDiagnosticBase = 3100000;
    public const int DsmConnectionDiagnosticLast = 3100002;

    #endregion
```

`DsmConnectionDiagnosticLoggingExtensions.cs`:

```csharp
using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Logging;

/// <summary>Category marker for ILogger&lt;T&gt; — no implementation required.</summary>
public interface ILogDsmConnectionDiagnosticService { }

/// <summary>
/// Structured logging extension methods for the DSM connection diagnostic.
/// </summary>
public static partial class DsmConnectionDiagnosticLoggingExtensions
{
    /// <summary>
    /// Logs that every check of the DSM connection passed.
    /// </summary>
    [LoggerMessage(EventId = 3100001, Level = LogLevel.Information, Message = "DSM connection healthy: {Host}:{Port}")]
    public static partial void DsmConnectionHealthy(this ILogger<ILogDsmConnectionDiagnosticService> logger, string host, int port);

    /// <summary>
    /// Logs the first failing check of the DSM connection and why it failed.
    /// </summary>
    [LoggerMessage(EventId = 3100002, Level = LogLevel.Warning, Message = "DSM connection check {Check} failed ({Failure}) for {Host}:{Port}")]
    public static partial void DsmConnectionFailed(this ILogger<ILogDsmConnectionDiagnosticService> logger, string check, string failure, string host, int port);
}
```

- [ ] **Step 3: Write the failing service tests**

`DsmConnectionDiagnosticServiceTests.cs`:

```csharp
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
```

- [ ] **Step 4: Write the failing endpoint and client tests**

In `ApplicationStartupTests.cs`, add `using System.Net.Http.Json;`, `using Askyl.Dsm.WebHosting.Constants.JSON;`,
`using Askyl.Dsm.WebHosting.Data.Domain.Authentication;`, `using Askyl.Dsm.WebHosting.Data.Domain.Diagnostics;` and
`using Askyl.Dsm.WebHosting.Data.Results;`, then:

```csharp
    static readonly string DiagnosticReportRoute = String.Join("/", ApplicationConstants.ApplicationUrlSubPath, DsmDiagnosticRoutes.ReportFullRoute);

    static readonly string DiagnosticRecheckRoute = String.Join("/", ApplicationConstants.ApplicationUrlSubPath, DsmDiagnosticRoutes.RecheckFullRoute);

    static readonly string LoginRoute = String.Join("/", ApplicationConstants.ApplicationUrlSubPath, AuthenticationRoutes.LoginFullRoute);

    [Fact]
    public async Task DsmDiagnostic_AnswersAnonymously_NamingTheMissingHostname()
    {
        using var hostnameless = WithoutDsmHostname();

        var result = await hostnameless.CreateClient().GetFromJsonAsync<DsmConnectionReportResult>(DiagnosticReportRoute, JsonOptionsCache.Options);

        Assert.NotNull(result?.Value);
        Assert.False(result.Value.IsHealthy);
        Assert.Equal(new DsmConnectionCheckResult(DsmConnectionCheck.HostnameConfigured, DsmCheckStatus.Failed, DsmCheckFailure.HostnameMissing), result.Value.Checks[0]);
    }

    [Fact]
    public async Task DsmDiagnosticRecheck_IsThrottled_WithoutSpendingLoginAttempts()
    {
        using var hostnameless = WithoutDsmHostname();
        var client = hostnameless.CreateClient();

        for (var attempt = 0; attempt < ApplicationConstants.LoginRateLimitPermitLimit; attempt++)
        {
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsync(DiagnosticRecheckRoute, null)).StatusCode);
        }

        var throttled = await client.PostAsync(DiagnosticRecheckRoute, null);

        // Empty credentials fail validation before any DSM call, so only the throttle can refuse them
        var login = await client.PostAsJsonAsync(LoginRoute, new LoginCredentials(String.Empty, String.Empty, null));

        Assert.Equal(HttpStatusCode.TooManyRequests, throttled.StatusCode);
        Assert.NotEqual(HttpStatusCode.TooManyRequests, login.StatusCode);
    }
```

`DsmConnectionDiagnosticClientTests.cs`:

```csharp
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
```

- [ ] **Step 5: Build and watch the tests fail**

Expected: build errors on `DsmConnectionDiagnosticService` (server and client) and `DsmDiagnosticRoutes` users.

- [ ] **Step 6: Implement the server service**

`Ui/Services/DsmConnectionDiagnosticService.cs`:

```csharp
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
/// <param name="logger">Logger for the outcome of each run.</param>
public sealed class DsmConnectionDiagnosticService(IDsmSettingsService settings, DsmApiClient apiClient, ILogger<ILogDsmConnectionDiagnosticService> logger)
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
    /// A healthy DSM needs no explanation, so an anonymous visitor learns nothing about it.
    /// </summary>
    internal static DsmConnectionReport ForAnonymousVisitor(DsmConnectionReport report)
        => report.IsHealthy ? new(true, report.CheckedAt, null, 0, []) : report;

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
        var results = new List<DsmConnectionCheckResult> { Result(DsmConnectionCheck.HostnameConfigured, ReloadSettings()) };

        // Empty only when the first check failed, and then nothing below runs
        var host = settings.Server ?? String.Empty;
        var port = settings.Port;

        if (LastPassed(results))
        {
            results.Add(Result(DsmConnectionCheck.NameResolves, await DsmConnectionChecks.ResolveAsync(host, StepTimeout)));
        }

        if (LastPassed(results))
        {
            results.Add(Result(DsmConnectionCheck.PortAnswers, await DsmConnectionChecks.ConnectAsync(host, port, StepTimeout)));
        }

        if (LastPassed(results))
        {
            var (failure, certificate) = await DsmConnectionChecks.InspectCertificateAsync(host, port, StepTimeout);
            results.Add(Result(DsmConnectionCheck.CertificateCoversName, failure) with { Certificate = certificate });
        }

        if (LastPassed(results))
        {
            results.Add(await ProbeApiAsync());
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
            var (answered, statusCode) = await apiClient.ProbeAsync(cancellation.Token);

            return answered
                ? new(DsmConnectionCheck.ApiAnswers, DsmCheckStatus.Passed)
                : new(DsmConnectionCheck.ApiAnswers, DsmCheckStatus.Failed, DsmCheckFailure.UnexpectedResponse, HttpStatus: statusCode);
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
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
```

- [ ] **Step 7: Implement the controller, registrations and client**

`DsmDiagnosticController.cs`:

```csharp
using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Constants.WebApi;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Askyl.Dsm.WebHosting.Ui.Controllers;

/// <summary>
/// The DSM connection diagnostic. No [AuthorizeSession]: it is shown precisely when nobody can sign in.
/// It takes no input, so a request cannot point the checks anywhere but the configured DSM.
/// </summary>
[ApiController]
[Route(DsmDiagnosticRoutes.ControllerBaseRoute)]
public class DsmDiagnosticController(IDsmConnectionDiagnosticService diagnosticService) : ControllerBase
{
    /// <summary>
    /// Returns the latest report; only the first call after startup runs the checks.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<DsmConnectionReportResult>> GetReportAsync(CancellationToken cancellationToken)
        => Ok(await diagnosticService.GetReportAsync(cancellationToken));

    /// <summary>
    /// Re-reads the DSM settings and runs the checks again.
    /// </summary>
    [HttpPost(DsmDiagnosticRoutes.RecheckRoute)]
    [EnableRateLimiting(ApplicationConstants.RateLimitPolicyDiagnostic)]
    public async Task<ActionResult<DsmConnectionReportResult>> RecheckAsync(CancellationToken cancellationToken)
        => Ok(await diagnosticService.RecheckAsync(cancellationToken));
}
```

In `Ui/Program.cs`, after `builder.Services.AddSingleton<DsmApiClient>();`:

```csharp
// Register the DSM connection diagnostic (singleton - caches the latest report for every visitor)
builder.Services.AddSingleton<IDsmConnectionDiagnosticService, DsmConnectionDiagnosticService>();
```

and in `AddRateLimiter`, after the login policy:

```csharp
    // Its own bucket, so re-checking the DSM connection never spends a login attempt
    options.AddPolicy(ApplicationConstants.RateLimitPolicyDiagnostic, LoginRateLimitPolicy.Partition);
```

Also update the `DsmSettingsService` registration comment to "(singleton - reads /etc/synoinfo.conf at startup,
again on each diagnostic run)".

`Ui.Client/Services/DsmConnectionDiagnosticService.cs`:

```csharp
using System.Net;
using System.Text.Json;
using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Constants.JSON;
using Askyl.Dsm.WebHosting.Constants.WebApi;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Results;
using Askyl.Dsm.WebHosting.Globalization;
using Askyl.Dsm.WebHosting.Tools.Extensions;

namespace Askyl.Dsm.WebHosting.Ui.Client.Services;

/// <summary>
/// Proxy implementation of IDsmConnectionDiagnosticService that calls the REST API.
/// </summary>
/// <param name="httpClientFactory">HttpClientFactory to create the named client.</param>
/// <param name="localizer">Localizer for user-facing strings.</param>
public class DsmConnectionDiagnosticService(IHttpClientFactory httpClientFactory, ILocalizer localizer) : IDsmConnectionDiagnosticService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(ApplicationConstants.HttpClientName);

    /// <inheritdoc/>
    public async Task<DsmConnectionReportResult> GetReportAsync(CancellationToken cancellationToken = default)
        => await _httpClient.GetJsonOrDefaultAsync(DsmDiagnosticRoutes.ReportFullRoute, () => DsmConnectionReportResult.CreateFailure(localizer[LK.Diagnostic.Unavailable]), cancellationToken);

    /// <inheritdoc/>
    public async Task<DsmConnectionReportResult> RecheckAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsync(DsmDiagnosticRoutes.RecheckFullRoute, null, cancellationToken);

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            return DsmConnectionReportResult.CreateFailure(localizer[LK.Error.RateLimitExceeded]);
        }

        if (!response.IsSuccessStatusCode)
        {
            return DsmConnectionReportResult.CreateFailure(localizer[LK.Diagnostic.Unavailable]);
        }

        // Deserialized as the client AuthenticationService does: the SDK's implicit System.Net.Http.Json
        // makes ReadFromJsonAsync ambiguous with the Tools extension
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        return JsonSerializer.Deserialize<DsmConnectionReportResult>(json, JsonOptionsCache.Options)
            ?? DsmConnectionReportResult.CreateFailure(localizer[LK.Diagnostic.Unavailable]);
    }
}
```

In `Ui.Client/Program.cs`, after the `IFileSystemService` registration:

```csharp
builder.Services.AddScoped<IDsmConnectionDiagnosticService, DsmConnectionDiagnosticService>();
```

- [ ] **Step 8: Run the tests and watch them pass**

Filter `FullyQualifiedName~DsmConnectionDiagnostic|FullyQualifiedName~ApplicationStartupTests|FullyQualifiedName~LogEventIdRegistryTests`.
Expected: PASS.

- [ ] **Step 9: Prove the throttle test by reverting**

Temporarily put `ApplicationConstants.RateLimitPolicyLogin` on `RecheckAsync`. Rebuild, run
`--filter "FullyQualifiedName~WithoutSpendingLoginAttempts"`. Expected: FAIL, the login answered 429. Restore,
rebuild, PASS.

- [ ] **Step 10: Full sequence and commit**

```bash
git add -A src
git commit -m "feat: serve the DSM connection diagnostic" -m "A Singleton runs the checks, caches the report and coalesces re-runs.
Anonymous endpoints serve it, the re-check under its own throttle, and
a healthy report tells an anonymous visitor nothing but that."
```

---

### Task 4: A failed login consults the diagnostic

**Files:**

- Modify: `src/Askyl.Dsm.WebHosting.Data/Results/ApiErrorCode.cs`
- Modify: `src/Askyl.Dsm.WebHosting.Data/Results/AuthenticationResult.cs`
- Modify: `src/Askyl.Dsm.WebHosting.Ui/Services/AuthenticationService.cs`
- Modify: `src/Askyl.Dsm.WebHosting.Logging/Server/Authentication/AuthenticationLoggingExtensions.cs`
- Modify: `src/Askyl.Dsm.WebHosting.Constants/Logging/LogEventIds.cs`
- Modify: `src/Askyl.Dsm.WebHosting.Globalization/LocalizationKeys.cs` and both `.resx`
- Modify: `src/Askyl.Dsm.WebHosting.Tests/Ui/Services/AuthenticationServiceTests.cs`

**Interfaces:**

- Consumes: `IDsmConnectionDiagnosticService.RecheckAsync` from Task 3.
- Produces: `ApiErrorCode.DsmUnreachable = 1002`; `AuthenticationResult.CreateDsmUnreachable(string message)`;
  `LK.Diagnostic.DsmUnreachable`.

- [ ] **Step 1: Write the failing tests**

In `AuthenticationServiceTests.cs` add `using Askyl.Dsm.WebHosting.Data.Domain.Diagnostics;`, a field and a
localizer setup, and pass the mock to the constructor:

```csharp
    readonly Mock<IDsmConnectionDiagnosticService> _diagnostic = new();
```

```csharp
        _localizer.Setup(l => l[LK.Diagnostic.DsmUnreachable]).Returns("DSM cannot be reached");
```

```csharp
    AuthenticationService CreateService()
    {
        return new AuthenticationService(_dsmSession.Object, _diagnostic.Object, new LoginCredentialsValidator(), _logger.Object, _localizer.Object);
    }
```

Then add a region:

```csharp
    #region LoginAsync — DSM Unreachable

    void DiagnosticReports(bool isHealthy)
        => _diagnostic.Setup(d => d.RecheckAsync(It.IsAny<CancellationToken>()))
                      .ReturnsAsync(DsmConnectionReportResult.CreateSuccess(new DsmConnectionReport(isHealthy, DateTimeOffset.UtcNow, null, 0, [])));

    [Fact]
    public async Task LoginAsync_WhenDsmCannotBeReached_SaysSoInsteadOfThrowing()
    {
        // Arrange — a TLS failure surfaced as an unhandled 500 and a generic toast
        _dsmSession.Setup(s => s.ConnectAsync(It.IsAny<LoginCredentials>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("TLS failure"));
        DiagnosticReports(isHealthy: false);

        // Act
        var result = await CreateService().LoginAsync("admin", "password123", null);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ApiErrorCode.DsmUnreachable, result.ErrorCode);
    }

    [Fact]
    public async Task LoginAsync_WhenTheDiagnosticFindsDsmHealthy_FailsAsBefore()
    {
        // Arrange — the diagnostic decides; a healthy DSM means the failure lies elsewhere
        _dsmSession.Setup(s => s.ConnectAsync(It.IsAny<LoginCredentials>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("transient"));
        DiagnosticReports(isHealthy: true);

        // Act
        var result = await CreateService().LoginAsync("admin", "password123", null);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ApiErrorCode.Failure, result.ErrorCode);
        Assert.Equal("Authentication failed", result.Message);
    }

    [Fact]
    public async Task LoginAsync_CancelledByTheCaller_PropagatesWithoutADiagnostic()
    {
        // Arrange — cancelled during the DSM call, not before, so validation cannot be what throws
        using var cancellation = new CancellationTokenSource();
        _dsmSession.Setup(s => s.ConnectAsync(It.IsAny<LoginCredentials>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                cancellation.Cancel();
                return Task.FromException<ApiResult>(new OperationCanceledException(cancellation.Token));
            });

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => CreateService().LoginAsync("admin", "password123", null, cancellation.Token));
        _diagnostic.Verify(d => d.RecheckAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
```

- [ ] **Step 2: Build and watch it fail**

Expected: build errors on the constructor, `ApiErrorCode.DsmUnreachable` and `LK.Diagnostic.DsmUnreachable`.

- [ ] **Step 3: Implement**

`ApiErrorCode.cs`, after `InvalidState`:

```csharp
    /// <summary>
    /// The application cannot reach its DSM; the connection diagnostic says why.
    /// </summary>
    DsmUnreachable = 1002,
```

`AuthenticationResult.cs`, after `CreateNotAuthenticated`:

```csharp
    /// <summary>
    /// Creates a failure result for a login that never reached DSM.
    /// </summary>
    /// <param name="message">Error message saying DSM cannot be reached.</param>
    public static AuthenticationResult CreateDsmUnreachable(string message)
        => new(false, message, null, null, null, ApiErrorCode.DsmUnreachable);
```

`AuthenticationLoggingExtensions.cs`, after `SessionInvalidated`:

```csharp
    /// <summary>
    /// Logs a login that failed before DSM answered.
    /// </summary>
    [LoggerMessage(EventId = 1000008, Level = LogLevel.Warning, Message = "Login for user {Login} failed before DSM answered")]
    public static partial void LoginTransportFailed(this ILogger<ILogAuthenticationService> logger, string login, Exception exception);
```

`LogEventIds.cs`: `AuthenticationLast = 1000008`.

`LocalizationKeys.cs`, in `Diagnostic`: `public const string DsmUnreachable = "Diagnostic_DsmUnreachable";`.
Resx, after `Diagnostic_Unavailable` — EN `DSM cannot be reached.`, FR `DSM est injoignable.`

`AuthenticationService.cs`: add the parameter after `dsmSession`, with
`/// <param name="diagnosticService">Decides whether a failed login means DSM is unreachable.</param>`:

```csharp
public class AuthenticationService(
    IDsmSession dsmSession,
    IDsmConnectionDiagnosticService diagnosticService,
    IValidator<LoginCredentials> credentialsValidator,
    ILogger<ILogAuthenticationService> logger,
    ILocalizer localizer) : IAuthenticationService
```

and replace `var connection = await dsmSession.ConnectAsync(model, cancellationToken);` with:

```csharp
        ApiResult connection;

        try
        {
            connection = await dsmSession.ConnectAsync(model, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            // Whatever broke on the way to DSM, the diagnostic decides whether DSM itself is the problem
            logger.LoginTransportFailed(login, ex);

            var report = await diagnosticService.RecheckAsync(CancellationToken.None);

            return report.Value is { IsHealthy: false }
                ? AuthenticationResult.CreateDsmUnreachable(localizer[LK.Diagnostic.DsmUnreachable])
                : AuthenticationResult.CreateNotAuthenticated(localizer[LK.Error.AuthenticationFailed]);
        }
```

- [ ] **Step 4: Run the tests and watch them pass**

Filter `FullyQualifiedName~AuthenticationServiceTests|FullyQualifiedName~LogEventIdRegistryTests`. Expected: PASS.

- [ ] **Step 5: Prove the cancellation test by reverting**

Temporarily drop the `when` filter. Rebuild, run `--filter "FullyQualifiedName~PropagatesWithoutADiagnostic"`.
Expected: FAIL, no exception thrown. Restore, rebuild, PASS.

- [ ] **Step 6: Full sequence and commit**

```bash
git add -A src
git commit -m "feat: let the diagnostic judge a failed login" -m "Any exception on the way to DSM used to escape as a 500 behind a
generic toast. The login now re-runs the diagnostic: unreachable DSM
gets its own error code, anything else the usual failure."
```

---

### Task 5: The diagnostic in place of the login form

**Files:**

- Create: `src/Askyl.Dsm.WebHosting.Ui.Client/Components/Controls/DsmDiagnosticPanel.razor`
- Modify: `src/Askyl.Dsm.WebHosting.Ui.Client/Components/Pages/Login.razor`
- Modify: `src/Askyl.Dsm.WebHosting.Globalization/LocalizationKeys.cs` and both `.resx`
- Modify: `src/Askyl.Dsm.WebHosting.Tests/Ui/ComponentRenderingTests.cs`

**Interfaces:**

- Consumes: everything above.
- Produces: `DsmDiagnosticPanel` with `[Parameter, EditorRequired] DsmConnectionReport Report` and
  `[Parameter] EventCallback OnRecheck`.

- [ ] **Step 1: Read the DSM 7.4 menu labels off the vDSM**

The remedies below name DSM menus. Their English wording follows Synology's documentation; the French is
unverified. Run the AGENTS.md §13 guard first, then read both languages from DSM's own string files:

SSH takes the password from an askpass script in the session scratchpad (`$SCRATCH`), never from the command
line:

```bash
. src/Askyl.Dsm.WebHosting.Ui/dev-mock/vdsm.env && grep -qx "external_host_ip=\"$VDSM_HOST\"" src/Askyl.Dsm.WebHosting.Ui/dev-mock/synoinfo.conf; echo "guard=$?"
printf '#!/bin/sh\n. %s/src/Askyl.Dsm.WebHosting.Ui/dev-mock/vdsm.env\nprintf %%s "$VDSM_PASSWORD"\n' "$PWD" > "$SCRATCH/askpass.sh" && chmod 700 "$SCRATCH/askpass.sh"
SSH_ASKPASS="$SCRATCH/askpass.sh" SSH_ASKPASS_REQUIRE=force ssh -o PubkeyAuthentication=no -p "$VDSM_SSH_PORT" "$VDSM_USER@$VDSM_HOST" \
  'cd /usr/syno/synoman && for s in "Control Panel" "External Access" "Advanced" "Hostname or external IP address" "Security" "Certificate"; do
     f=$(grep -rlF "\"$s\"" --include=strings . | grep /enu/ | head -1); k=$(grep -F "=\"$s\"" "$f" | head -1 | cut -d= -f1)
     echo "$s => $(grep "^$k=" "${f/\/enu\//\/fre\/}" | head -1)"; done'
```

If a label is missing from the files, read it in DSM's interface with the browser language set to each
language. Correct the menu paths in both `.resx` files below before writing them. If the vDSM is unreachable,
stop and ask the maintainer.

- [ ] **Step 2: Add the strings**

`LocalizationKeys.cs`, complete `Diagnostic`:

```csharp
    /// <summary>DSM connection diagnostic strings.</summary>
    public static class Diagnostic
    {
        public const string Unavailable = "Diagnostic_Unavailable";
        public const string DsmUnreachable = "Diagnostic_DsmUnreachable";
        public const string Title = "Diagnostic_Title";
        public const string Intro = "Diagnostic_Intro";
        public const string Target = "Diagnostic_Target";
        public const string CheckedAt = "Diagnostic_CheckedAt";
        public const string Recheck = "Diagnostic_Recheck";
        public const string Checking = "Diagnostic_Checking";
        public const string StatusPassed = "Diagnostic_StatusPassed";
        public const string StatusFailed = "Diagnostic_StatusFailed";
        public const string StatusSkipped = "Diagnostic_StatusSkipped";
        public const string CheckHostnameConfigured = "Diagnostic_CheckHostnameConfigured";
        public const string CheckNameResolves = "Diagnostic_CheckNameResolves";
        public const string CheckPortAnswers = "Diagnostic_CheckPortAnswers";
        public const string CheckCertificateCoversName = "Diagnostic_CheckCertificateCoversName";
        public const string CheckApiAnswers = "Diagnostic_CheckApiAnswers";
        public const string FailureHostnameMissing = "Diagnostic_FailureHostnameMissing";
        public const string FailureSettingsUnreadable = "Diagnostic_FailureSettingsUnreadable";
        public const string FailureNameNotFound = "Diagnostic_FailureNameNotFound";
        public const string FailurePortRefused = "Diagnostic_FailurePortRefused";
        public const string FailurePortTimedOut = "Diagnostic_FailurePortTimedOut";
        public const string FailureCertificateNameMismatch = "Diagnostic_FailureCertificateNameMismatch";
        public const string FailureCertificateUntrusted = "Diagnostic_FailureCertificateUntrusted";
        public const string FailureCertificateExpired = "Diagnostic_FailureCertificateExpired";
        public const string FailureHandshakeFailed = "Diagnostic_FailureHandshakeFailed";
        public const string FailureUnexpectedResponse = "Diagnostic_FailureUnexpectedResponse";
        public const string FailureNoResponse = "Diagnostic_FailureNoResponse";
        public const string CertificateNames = "Diagnostic_CertificateNames";
        public const string CertificateIssuer = "Diagnostic_CertificateIssuer";
        public const string CertificateExpiry = "Diagnostic_CertificateExpiry";
    }
```

Resx values, each as `<data name="KEY" xml:space="preserve"><value>…</value></data>` after `Diagnostic_DsmUnreachable`:

| Key | English | French |
|-----|---------|--------|
| `Diagnostic_Title` | DSM cannot be reached | DSM est injoignable |
| `Diagnostic_Intro` | This application signs you in through DSM, and cannot reach it. The first failed check below says what to fix in DSM. | Cette application vous connecte via DSM, qu'elle ne parvient pas à joindre. Le premier contrôle en échec ci-dessous indique quoi corriger dans DSM. |
| `Diagnostic_Target` | Configured DSM address: {0}:{1} | Adresse DSM configurée : {0}:{1} |
| `Diagnostic_CheckedAt` | Checked at {0:T} | Vérifié à {0:T} |
| `Diagnostic_Recheck` | Re-check | Revérifier |
| `Diagnostic_Checking` | Checking the connection to DSM... | Vérification de la connexion à DSM... |
| `Diagnostic_StatusPassed` | Passed | Réussi |
| `Diagnostic_StatusFailed` | Failed | Échoué |
| `Diagnostic_StatusSkipped` | Not run | Non exécuté |
| `Diagnostic_CheckHostnameConfigured` | DSM has a hostname | DSM a un nom d'hôte |
| `Diagnostic_CheckNameResolves` | The hostname resolves | Le nom d'hôte se résout |
| `Diagnostic_CheckPortAnswers` | DSM answers on its HTTPS port | DSM répond sur son port HTTPS |
| `Diagnostic_CheckCertificateCoversName` | DSM's certificate is valid for that name | Le certificat de DSM est valide pour ce nom |
| `Diagnostic_CheckApiAnswers` | The DSM API answers | L'API de DSM répond |
| `Diagnostic_FailureHostnameMissing` | DSM has no hostname. In DSM, open Control Panel > External Access > Advanced, enter the NAS's name in Hostname or external IP address, then re-check. | DSM n'a pas de nom d'hôte. Dans DSM, ouvrez Panneau de configuration > Accès externe > Avancé, saisissez le nom du NAS dans Nom d'hôte ou adresse IP externe, puis revérifiez. |
| `Diagnostic_FailureSettingsUnreadable` | The DSM settings file cannot be read. The application log says why. | Le fichier de réglages de DSM est illisible. Le journal de l'application en donne la raison. |
| `Diagnostic_FailureNameNotFound` | {0} does not resolve from the NAS. Correct the name in Control Panel > External Access > Advanced, or add it to your DNS. | {0} ne se résout pas depuis le NAS. Corrigez le nom dans Panneau de configuration > Accès externe > Avancé, ou ajoutez-le à votre DNS. |
| `Diagnostic_FailurePortRefused` | {0} refuses connections on port {1}. Check the DSM HTTPS port in Control Panel > External Access > Advanced. | {0} refuse les connexions sur le port {1}. Vérifiez le port HTTPS de DSM dans Panneau de configuration > Accès externe > Avancé. |
| `Diagnostic_FailurePortTimedOut` | {0} did not answer on port {1}. Check that no firewall blocks it, and the DSM HTTPS port in Control Panel > External Access > Advanced. | {0} n'a pas répondu sur le port {1}. Vérifiez qu'aucun pare-feu ne le bloque, ainsi que le port HTTPS de DSM dans Panneau de configuration > Accès externe > Avancé. |
| `Diagnostic_FailureCertificateNameMismatch` | DSM's certificate does not cover {0}. In Control Panel > Security > Certificate, add a certificate for that name and make it the default. | Le certificat de DSM ne couvre pas {0}. Dans Panneau de configuration > Sécurité > Certificat, ajoutez un certificat pour ce nom et définissez-le par défaut. |
| `Diagnostic_FailureCertificateUntrusted` | DSM's certificate is not trusted: it is self-signed or issued by an unknown authority. In Control Panel > Security > Certificate, add one from a trusted authority, such as Let's Encrypt, and make it the default. | Le certificat de DSM n'est pas approuvé : il est autosigné ou émis par une autorité inconnue. Dans Panneau de configuration > Sécurité > Certificat, ajoutez-en un d'une autorité reconnue, comme Let's Encrypt, et définissez-le par défaut. |
| `Diagnostic_FailureCertificateExpired` | DSM's certificate has expired. Renew it in Control Panel > Security > Certificate. | Le certificat de DSM a expiré. Renouvelez-le dans Panneau de configuration > Sécurité > Certificat. |
| `Diagnostic_FailureHandshakeFailed` | {0}:{1} accepted the connection but did not complete a TLS handshake. Check that this port serves DSM over HTTPS. | {0}:{1} a accepté la connexion sans terminer la négociation TLS. Vérifiez que ce port sert DSM en HTTPS. |
| `Diagnostic_FailureUnexpectedResponse` | The DSM API answered HTTP {0} instead of its API list. Check that this port serves DSM itself. | L'API de DSM a répondu HTTP {0} au lieu de sa liste d'API. Vérifiez que ce port sert bien DSM. |
| `Diagnostic_FailureNoResponse` | The DSM API did not answer in time. | L'API de DSM n'a pas répondu à temps. |
| `Diagnostic_CertificateNames` | Certificate names: {0} | Noms du certificat : {0} |
| `Diagnostic_CertificateIssuer` | Issued by: {0} | Émis par : {0} |
| `Diagnostic_CertificateExpiry` | Expires: {0:d} | Expire le : {0:d} |

- [ ] **Step 3: Write the failing rendering tests**

In `ComponentRenderingTests.cs`, add `using Askyl.Dsm.WebHosting.Data.Domain.Diagnostics;` and
`using Askyl.Dsm.WebHosting.Ui.Client.Components.Controls;`, a field, its registration and a healthy default in
the constructor:

```csharp
    readonly Mock<IDsmConnectionDiagnosticService> _diagnosticService = new();
```

```csharp
        _ctx.Services.AddSingleton(_diagnosticService.Object);
```

```csharp
        _diagnosticService.Setup(service => service.GetReportAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Report(isHealthy: true));
```

Then the tests and a helper:

```csharp
    [Fact]
    public void Login_ShowsTheDiagnostic_InsteadOfTheForm_WhenDsmIsUnreachable()
    {
        _diagnosticService.Setup(service => service.GetReportAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Report(isHealthy: false));

        var cut = _ctx.Render<Login>();

        cut.WaitForAssertion(() => Assert.Contains(_localizer[LK.Diagnostic.FailureHostnameMissing], cut.Markup, StringComparison.Ordinal));
        Assert.Empty(cut.FindAll("[name=username]"));
    }

    [Fact]
    public void DsmDiagnosticPanel_ReCheck_RaisesTheCallback()
    {
        var rechecked = false;

        var cut = _ctx.Render<DsmDiagnosticPanel>(parameters => parameters
            .Add(panel => panel.Report, Report(isHealthy: false).Value!)
            .Add(panel => panel.OnRecheck, () => rechecked = true));

        cut.Find("fluent-button").Click();

        Assert.True(rechecked);
    }

    static DsmConnectionReportResult Report(bool isHealthy)
        => DsmConnectionReportResult.CreateSuccess(isHealthy
            ? new DsmConnectionReport(true, DateTimeOffset.UtcNow, null, 0, [])
            : new DsmConnectionReport(false, DateTimeOffset.UtcNow, null, 5001,
            [
                new(DsmConnectionCheck.HostnameConfigured, DsmCheckStatus.Failed, DsmCheckFailure.HostnameMissing),
                .. Enum.GetValues<DsmConnectionCheck>().Skip(1).Select(check => new DsmConnectionCheckResult(check, DsmCheckStatus.Skipped))
            ]));
```

If bUnit reports no `onclick` handler on `fluent-button`, find the element FluentButton 5.0.1 binds it to in
`cut.Markup` and select that instead; do not add a handler to the component for the test's sake.

- [ ] **Step 4: Build and watch the tests fail**

Expected: build error on `DsmDiagnosticPanel`.

- [ ] **Step 5: Implement the panel**

`DsmDiagnosticPanel.razor`:

```html
@using Askyl.Dsm.WebHosting.Data.Domain.Diagnostics
@using Askyl.Dsm.WebHosting.Globalization

@inject ILocalizer localizer

<FluentStack Orientation="Orientation.Vertical">
    <FluentText As="TextTag.H2" Size="TextSize.Size600" Weight="TextWeight.Semibold">@localizer[LK.Diagnostic.Title]</FluentText>
    <FluentText>@localizer[LK.Diagnostic.Intro]</FluentText>

    @if (Report.Host is { } host)
    {
        <FluentLabel>@localizer[LK.Diagnostic.Target, host, Report.Port]</FluentLabel>
    }

    @foreach (var check in Report.Checks)
    {
        <FluentStack Orientation="Orientation.Horizontal" VerticalAlignment="VerticalAlignment.Center">
            <FluentIcon Value="@StatusIcon(check.Status)" Title="@localizer[StatusKey(check.Status)]" />
            <FluentLabel>@localizer[CheckKey(check.Check)]</FluentLabel>
        </FluentStack>

        @if (check.Status == DsmCheckStatus.Failed)
        {
            <FluentMessageBar Intent="MessageBarIntent.Error">
                <FluentStack Orientation="Orientation.Vertical">
                    <FluentText>@FailureText(check)</FluentText>

                    @if (check.Certificate is { } certificate)
                    {
                        <FluentText>@localizer[LK.Diagnostic.CertificateNames, String.Join(", ", certificate.Names)]</FluentText>
                        <FluentText>@localizer[LK.Diagnostic.CertificateIssuer, certificate.Issuer]</FluentText>
                        <FluentText>@localizer[LK.Diagnostic.CertificateExpiry, certificate.NotAfter.ToLocalTime()]</FluentText>
                    }
                </FluentStack>
            </FluentMessageBar>
        }
    }

    <FluentLabel>@localizer[LK.Diagnostic.CheckedAt, Report.CheckedAt.ToLocalTime()]</FluentLabel>
    <FluentButton Appearance="ButtonAppearance.Primary" OnClick="OnRecheck">@localizer[LK.Diagnostic.Recheck]</FluentButton>
</FluentStack>

@code
{
    /// <summary>The report to show; the panel is only rendered for an unhealthy one.</summary>
    [Parameter, EditorRequired]
    public DsmConnectionReport Report { get; set; } = new();

    /// <summary>Raised by the re-check button.</summary>
    [Parameter]
    public EventCallback OnRecheck { get; set; }

    private static Icon StatusIcon(DsmCheckStatus status) => status switch
    {
        DsmCheckStatus.Passed => new IconsRegular20.CheckmarkCircle().WithColor(Color.Success),
        DsmCheckStatus.Failed => new IconsRegular20.DismissCircle().WithColor(Color.Error),
        _ => new IconsRegular20.Circle().WithColor(Color.Disabled)
    };

    private static string StatusKey(DsmCheckStatus status) => status switch
    {
        DsmCheckStatus.Passed => LK.Diagnostic.StatusPassed,
        DsmCheckStatus.Failed => LK.Diagnostic.StatusFailed,
        _ => LK.Diagnostic.StatusSkipped
    };

    private static string CheckKey(DsmConnectionCheck check) => check switch
    {
        DsmConnectionCheck.HostnameConfigured => LK.Diagnostic.CheckHostnameConfigured,
        DsmConnectionCheck.NameResolves => LK.Diagnostic.CheckNameResolves,
        DsmConnectionCheck.PortAnswers => LK.Diagnostic.CheckPortAnswers,
        DsmConnectionCheck.CertificateCoversName => LK.Diagnostic.CheckCertificateCoversName,
        _ => LK.Diagnostic.CheckApiAnswers
    };

    private string FailureText(DsmConnectionCheckResult check)
    {
        var host = Report.Host ?? String.Empty;

        return check.Failure switch
        {
            DsmCheckFailure.HostnameMissing => localizer[LK.Diagnostic.FailureHostnameMissing],
            DsmCheckFailure.SettingsUnreadable => localizer[LK.Diagnostic.FailureSettingsUnreadable],
            DsmCheckFailure.NameNotFound => localizer[LK.Diagnostic.FailureNameNotFound, host],
            DsmCheckFailure.PortRefused => localizer[LK.Diagnostic.FailurePortRefused, host, Report.Port],
            DsmCheckFailure.PortTimedOut => localizer[LK.Diagnostic.FailurePortTimedOut, host, Report.Port],
            DsmCheckFailure.CertificateNameMismatch => localizer[LK.Diagnostic.FailureCertificateNameMismatch, host],
            DsmCheckFailure.CertificateUntrusted => localizer[LK.Diagnostic.FailureCertificateUntrusted],
            DsmCheckFailure.CertificateExpired => localizer[LK.Diagnostic.FailureCertificateExpired],
            DsmCheckFailure.HandshakeFailed => localizer[LK.Diagnostic.FailureHandshakeFailed, host, Report.Port],
            DsmCheckFailure.UnexpectedResponse when check.HttpStatus is { } status => localizer[LK.Diagnostic.FailureUnexpectedResponse, status],
            _ => localizer[LK.Diagnostic.FailureNoResponse]
        };
    }
}
```

- [ ] **Step 6: Wire it into the login page**

`Login.razor`: add `@using Askyl.Dsm.WebHosting.Data.Domain.Diagnostics`, `@using Askyl.Dsm.WebHosting.Data.Results`
and `@inject IDsmConnectionDiagnosticService diagnosticService`. Wrap the `EditForm` inside the `FluentCard`:

```html
    <FluentCard Class="login-card">
        @if (Report is { IsHealthy: false })
        {
            <DsmDiagnosticPanel Report="@Report" OnRecheck="Recheck" />
        }
        else
        {
            <EditForm Model="@Model" OnValidSubmit="Authenticate">
                @* unchanged content *@
            </EditForm>
        }
    </FluentCard>
```

In `@code`:

```csharp
    private LoginCredentials Model = new();

    // Null until read, and when reading failed: the form then shows as it always did
    private DsmConnectionReport? Report;

    protected override async Task OnInitializedAsync()
    {
        var isAuthenticatedResult = await authService.IsAuthenticatedAsync();
        if (isAuthenticatedResult.Success && isAuthenticatedResult.Value == true)
        {
            navigation.NavigateTo(ApplicationConstants.HomePagePath);
            return;
        }

        Report = (await diagnosticService.GetReportAsync()).Value;
    }
```

In `Authenticate`, the failure block becomes:

```csharp
        if (!result.Success || !result.IsAuthenticated)
        {
            if (result.ErrorCode == ApiErrorCode.DsmUnreachable)
            {
                // The server re-ran the checks before answering; the panel says more than a toast
                Report = (await diagnosticService.GetReportAsync()).Value;

                if (Report is { IsHealthy: false })
                {
                    return;
                }
            }

            // failed - either HTTP call failed or credentials invalid
            var errorMessage = result.Message?.Trim() ?? localizer[LK.Error.AuthenticationFailed];
            await ToastService.ShowErrorToastAsync(errorMessage);
            return;
        }
```

And add:

```csharp
    private async Task Recheck()
    {
        if (IsWorking)
        {
            return;
        }

        using var worker = this.CreateWorkingState(localizer[LK.Diagnostic.Checking]);

        var result = await diagnosticService.RecheckAsync();

        if (!result.Success)
        {
            await ToastService.ShowErrorToastAsync(result.Message ?? localizer[LK.Diagnostic.Unavailable]);
            return;
        }

        Report = result.Value;
    }
```

- [ ] **Step 7: Run the tests and watch them pass**

Filter `FullyQualifiedName~ComponentRenderingTests|FullyQualifiedName~UiSourceAuditTests` plus the FR/EN parity
test (find it with `grep -rln "fr-FR" src/Askyl.Dsm.WebHosting.Tests`). Expected: PASS, including the existing
`Login_RendersItsThreeCredentialFields` (healthy default).

- [ ] **Step 8: Full sequence and commit**

```bash
git add -A src
git commit -m "feat: show the DSM diagnostic in place of the login" -m "When DSM cannot be reached the login page lists the five checks, the
data behind the failing one and the DSM setting that fixes it, with a
re-check. Remedies name DSM 7.4's own menus in English and French."
```

---

### Task 6: Documentation and version

**Files:**

- Modify: `docs/ai/technical-architecture.md`
- Modify: `docs/ai/open-technical-items.md`
- Modify (script): `src/Directory.Build.props`, `src/spk-project/INFO`

- [ ] **Step 1: Update `technical-architecture.md`**

- Interfaces table (around line 261): `IDsmSettingsService` row becomes `Server (nullable), Port, Language,
  Reload()`; add a row `| **IDsmConnectionDiagnosticService** | GetReportAsync(), RecheckAsync() | Ui.Services (Singleton) |`.
- Services table (around line 351): the `DsmSettingsService` purpose becomes "Reads /etc/synoinfo.conf via
  IFileReader at startup and on Reload(); a missing hostname leaves Server null".
- `DsmApiClient Key Features`: add "- `ProbeAsync` sends `SYNO.API.Info` for the connection diagnostic without
  touching the handshake cache; every call fails fast when no hostname is configured".
- `### 6. Askyl.Dsm.WebHosting.Ui`, Controllers bullet: add `DsmDiagnostic` to the list, and "Protected by
  `[AuthorizeSession]` except AuthenticationController and DsmDiagnosticController".
- Authentication Flow, step 2: "settings read at startup and again on each diagnostic run; a missing file throws,
  a missing hostname leaves `Server` null; only the language and HTTPS port fall back to defaults". Add after the
  numbered list: "A login that throws before DSM answers re-runs the connection diagnostic; when DSM is
  unreachable the result carries `ApiErrorCode.DsmUnreachable` and the login page shows the diagnostic."
- API Security: item 1 adds "`DsmDiagnosticController` is public by design and takes no input"; item 3 adds
  "Diagnostic re-check: 5/minute/IP, a separate bucket".
- Add a subsection `#### Connection Diagnostic` under `### DSM API Integration`, after `#### Session Revocation`:

```markdown
#### Connection Diagnostic

`DsmConnectionDiagnosticService` (Singleton, `Ui/Services/`) runs five checks in order and stops at the first
failure: hostname configured, name resolves, port answers, certificate covers the name, `SYNO.API.Info` answers.
Each network step is bounded by `DsmDiagnosticStepTimeoutSeconds`. The certificate check runs a real handshake and
records what DSM presents; its callback returns default validation's own verdict, so it never accepts a
certificate the application would refuse. The report is cached; the first login-page visit computes it, the
re-check endpoint and a failed login recompute it, and concurrent runs are coalesced. A healthy report reaches
anonymous visitors as nothing but "healthy". `Login.razor` shows `DsmDiagnosticPanel` in place of the form while
the report is unhealthy.
```

- [ ] **Step 2: Update `open-technical-items.md`**

In *The package needs a DSM hostname and a matching certificate, and says so nowhere*:

- Replace the **No hostname: the package does not start.** bullet with: "**No hostname** used to stop the package
  at startup. Since 0.8.1 the host starts and the login page shows the diagnostic."
- Replace the **Any later change in DSM needs a package restart** bullet with: "**A later change in DSM** applies
  on the diagnostic's re-check, which reloads `synoinfo.conf`."
- After the decided list, add: "**Step 1 landed in 0.8.1** (PR #82). Step 2 remains open."

- [ ] **Step 3: Bump the version**

Run `./src/scripts/update-version.sh 0.8.1`.

- [ ] **Step 4: Lint, full sequence, leak scan, commit**

`markdownlint docs/ai/technical-architecture.md`, `markdownlint docs/ai/open-technical-items.md`, then format,
build, test. `grep -Ecf .git/forbidden-patterns` on both docs: expected 0.

```bash
git add -A docs src
git commit -m "docs: record the DSM connection diagnostic (0.8.1)" -m "Architecture, security and the tracker entry now describe the
diagnostic; step 1 of the hostname fix is marked done."
```

Push with `git push -u origin feat/dsm-connection-diagnostic` and update the PR #82 description (leak-scanned
first) to describe the implementation, not only the design.

---

### Task 7: Acceptance on the vDSM

No code. Results reported to the maintainer; screenshots stay local.

- [ ] **Step 1: Local run against the vDSM**

Run the §13 guard (exit 0 required). Build, then from the repository root:
`dotnet run --project src/Askyl.Dsm.WebHosting.Ui --no-build --launch-profile https`. Open
`https://localhost:5000/adwh/login`. Expected: checks 1–3 passed, check 4 failed as untrusted or name mismatch,
showing DSM's default self-signed certificate (`synology`), check 5 skipped. Screenshot. Stop it:
`pgrep -fl "Askyl.Dsm.WebHosting.Ui"`, then `pkill -f "Askyl.Dsm.WebHosting.Ui"` for the process started here only.

- [ ] **Step 2: SPK on the vDSM**

`dotnet clean /nr:false ./src/Askyl.Dsm.WebHosting.slnx`, then `./src/scripts/build-spk.sh`. Install the package
from `dist/` on the vDSM (Package Center, manual install, or `sudo synopkg install` over SSH). Open the package.
Expected: check 1 failed, hostname missing. Screenshot.

- [ ] **Step 3: Set the hostname without restarting**

In the vDSM's DSM: Control Panel > External Access > Advanced, set the hostname. Press *Re-check*. Expected:
check 1 passes without a package restart; the run then stops at check 2 or 4 depending on how the name resolves
from inside the vDSM. Screenshot, and report which.

- [ ] **Step 4: Leave the vDSM as found, or as agreed**

Ask the maintainer whether the hostname stays set — the next step, a trusted certificate for step 2, needs it.
