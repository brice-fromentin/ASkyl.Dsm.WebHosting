# Logging Enhancement Plan

**Branch:** `feat/enhanced-logging`  
**Target Framework:** .NET 10 (net10.0)

---

## Objective

Migrate all structured logging to `[LoggerMessage]` source-generated extensions for
zero-allocation performance, eliminate all CA2254 warnings, and enhance the Logging
project as a proper shared infrastructure library.

---

## Current State

| Component | Status |
|-----------|--------|
| **Logging project** | ✅ Complete — 18 extension files, 216 `[LoggerMessage]` methods |
| **Serilog (server)** | ✅ Enhanced — `{EventId}` in template, `CloseAndFlush()` on shutdown, `WithActivity` enricher |
| **Serilog (client)** | BrowserConsole sink, min level `Debug` |
| **Server-side logger calls** | ✅ 126 calls migrated to extension methods |
| **Client-side logger calls** | ✅ 26 calls migrated to extension methods |
| **UI service logger calls** | ✅ 1 call migrated to extension method |
| **CA2254 warnings** | ✅ Zero — eliminated by `[LoggerMessage]` migration |
| **LogDownloadService** | ✅ Migrated — 7 calls → extension methods |

### CA2254 Breakdown

| Severity | Count | Pattern |
|----------|-------|---------|
| **High** | 2 | `$"..."` interpolation, `String.Join` + LINQ |
| **Medium** | 3 | Nullable chains (`response?.Error?.Code`) |
| **Low** | ~70 | Property access (`.Name`, `.Count`, `.Message`, `.UUID`) |
| **Clean** | ~50 | Simple local variables/parameters |

---

## Approach: `[LoggerMessage]` Source-Generated Extensions

### Why `[LoggerMessage]`

| Benefit | Impact |
|---------|--------|
| **Compiled message templates** | No runtime string parsing |
| **No `object[]` allocation** | Arguments passed as strongly-typed parameters |
| **Zero cost when disabled** | Log level checked before method entry (compiler-generated guard) |
| **Eliminates CA2254** | Arguments are lazily evaluated only when level is enabled |
| **Type-safe** | Compile-time validation of format strings and argument count |

### Extension Method Pattern

All `[LoggerMessage]` methods use the `public static partial class` pattern
(with `this ILogger logger` on each method).

**Note:** The .NET 10 `extension(...)` block pattern (from `HttpClientExtensions.cs`)
cannot be used with `[LoggerMessage]` — the source generator requires a `partial` type
to inject method implementations, but `extension(...)` blocks create a non-partial synthetic type.

```csharp
namespace Askyl.Dsm.WebHosting.Logging;

/// <summary>Category marker for ILogger&lt;T&gt; — no implementation required.</summary>
public interface ILogAuthenticationService { }

/// <summary>
/// Structured logging extension methods for authentication-related events.
/// </summary>
public static partial class AuthenticationLoggingExtensions
{
    /// <summary>
    /// Logs a successful login event for the specified user.
    /// </summary>
    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "Login successful for user: {Login} - SID stored")]
    public static partial void LoginSuccessful(this ILogger<ILogAuthenticationService> logger, string login);
}
```

**Rules:**

- `public static partial class` — class MUST be partial for source generator
- `public interface ILogXxx { }` — namespace-level marker interface for `ILogger<T>` categorization
- `this ILogger<ILogXxx> logger` — defines the specialized extension target type on each method
- `public static partial void` — the source-generated method signature (no body)
- Event IDs are `int` literals (not constants) — required by source generator
- XML doc comments on each method

### Semantic File Split

The `[LoggerMessage]` extensions are organized into separate files **per service** for maintainability:

| File | Domain | Methods | Event IDs | Service Covered |
|------|--------|---------|-----------|-----------------|
| `AuthenticationLoggingExtensions.cs` | Authentication | 8 | 1000001-1000008 | AuthenticationService |
| `FileSystemServiceLoggingExtensions.cs` | File system | 12 | 1100001-1100012 | FileSystemService |
| `FileManagerServiceLoggingExtensions.cs` | File system | 6 | 1200001-1200006 | FileManagerService |
| `LogDownloadServiceLoggingExtensions.cs` | File system | 7 | 1300001-1300007 | LogDownloadService |
| `FrameworkManagementLoggingExtensions.cs` | .NET framework | 11 | 1400001-1400011 | FrameworkManagementService |
| `DotnetVersionServiceLoggingExtensions.cs` | .NET framework | 9 | 1500001-1500009 | DotnetVersionService |
| `ProcessLoggingExtensions.cs` | Process lifecycle | 22 | 1600001-1600022 | SiteLifecycleManager |
| `ReverseProxyLoggingExtensions.cs` | Reverse proxy | 16 | 1700001-1700016 | ReverseProxyManagerService |
| `WebsiteLoggingExtensions.cs` | Website hosting | 44 | 1800001-1800044 | WebSiteHostingService |
| `ConfigurationLoggingExtensions.cs` | Configuration | 18 | 1900001-1900018 | WebSitesConfigurationService |
| `DsmApiLoggingExtensions.cs` | DSM API | 12 | 2000001-2000012 | DsmApiClient |
| `ArchiveExtractorLoggingExtensions.cs` | Infrastructure | 8 | 2100001-2100008 | ArchiveExtractorService |
| `VersionsDetectorLoggingExtensions.cs` | Infrastructure | 4 | 2200001-2200004 | VersionsDetectorService |
| `PlatformInfoLoggingExtensions.cs` | Infrastructure | 2 | 2300001-2300002 | PlatformInfoService |
| `DownloaderLoggingExtensions.cs` | Infrastructure | 5 | 2400001-2400005 | DownloaderService |
| `ProcessRunnerLoggingExtensions.cs` | Infrastructure | 1 | 2500001 | SystemProcessRunner |
| `ProcessHandleLoggingExtensions.cs` | Infrastructure | 5 | 2600001-2600005 | SystemProcessHandle |
| `ClientLoggingExtensions.cs` | Client-side (WASM) | 26 | 7000001, 7100001-7100015, 7200001-7200004, 7300001-7300004, 7400001, 7500001 | LicenseService, Home, dialogs |
| **Total** | | **216** | | |

### Event ID Ranges

Each **service** gets a dedicated 100K range at 1M spacing to prevent cross-service collisions
(e.g., FrameworkManagement and DotnetVersion no longer share the same numbering space):

| Base | Service | Range |
|------|---------|-------|
| `1000000` | AuthenticationService | `1000001–1000008` |
| `1100000` | FileSystemService | `1100001–1100012` |
| `1200000` | FileManagerService | `1200001–1200006` |
| `1300000` | LogDownloadService | `1300001–1300007` |
| `1400000` | FrameworkManagementService | `1400001–1400011` |
| `1500000` | DotnetVersionService | `1500001–1500009` |
| `1600000` | SiteLifecycleManager | `1600001–1600022` |
| `1700000` | ReverseProxyManagerService | `1700001–1700016` |
| `1800000` | WebSiteHostingService | `1800001–1800044` |
| `1900000` | WebSitesConfigurationService | `1900001–1900018` |
| `2000000` | DsmApiClient | `2000001–2000012` |
| `2100000` | ArchiveExtractorService | `2100001–2100008` |
| `2200000` | VersionsDetectorService | `2200001–2200004` |
| `2300000` | PlatformInfoService | `2300001–2300002` |
| `2400000` | DownloaderService | `2400001–2400005` |
| `2500000` | SystemProcessRunner | `2500001` |
| `2600000` | SystemProcessHandle | `2600001–2600005` |
| `7000000` | LicenseService (client) | `7000001` |
| `7100000` | Home page (client) | `7100001–7100015` |
| `7200000` | AspNetReleasesDialog (client) | `7200001–7200004` |
| `7300000` | FileSelectionDialog (client) | `7300001–7300004` |
| `7400000` | DotnetVersionsDialog (client) | `7400001` |
| `7500000` | WebSiteConfigurationDialog (client) | `7500001` |

---

## Enhancement Tasks

### Phase 1: Logging Project Foundation

| Task | Description | Status |
|------|-------------|--------|
| **T1.1** | Replace `HelloWorldExtensions.cs` with project structure — add `Microsoft.Extensions.Logging.Abstractions` reference | ✅ Done |
| **T1.2** | Create `AuthenticationLoggingExtensions.cs` — 4 methods (event IDs 1001-1004) | ✅ Done |
| **T1.3** | Create `FileSystemServiceLoggingExtensions.cs` — 12 methods (1100-1111) | ✅ Done |
| **T1.4** | Create `FileManagerServiceLoggingExtensions.cs` — 6 methods (1112-1117) | ✅ Done |
| **T1.5** | Create `LogDownloadServiceLoggingExtensions.cs` — 7 methods (1118-1124) | ✅ Done |
| **T1.6** | Create `FrameworkManagementLoggingExtensions.cs` — 7 methods (1200-1206) | ✅ Done |
| **T1.7** | Create `DotnetVersionServiceLoggingExtensions.cs` — 7 methods (1207-1213) | ✅ Done |
| **T1.8** | Create `ProcessLoggingExtensions.cs` — 17 methods (1300-1316) | ✅ Done |
| **T1.9** | Create `ReverseProxyLoggingExtensions.cs` — 11 methods (1400-1410) | ✅ Done |
| **T1.10** | Create `WebsiteLoggingExtensions.cs` — 34 methods (1500-1533) | ✅ Done |
| **T1.11** | Create `ConfigurationLoggingExtensions.cs` — 12 methods (1600-1611) | ✅ Done |
| **T1.12** | Create `DsmApiLoggingExtensions.cs` — 5 methods (1700-1704) | ✅ Done |
| **T1.13** | Create `ArchiveExtractorLoggingExtensions.cs` — 6 methods (1800-1805) | ✅ Done |
| **T1.14** | Create `VersionsDetectorLoggingExtensions.cs` — 4 methods (1806-1809) | ✅ Done |
| **T1.15** | Create `PlatformInfoLoggingExtensions.cs` — 2 methods (1810-1811) | ✅ Done |
| **T1.16** | Create `DownloaderLoggingExtensions.cs` — 4 methods (1812-1815) | ✅ Done |
| **T1.17** | Create `ProcessRunnerLoggingExtensions.cs` — 1 method (1816) | ✅ Done |
| **T1.18** | Create `ProcessHandleLoggingExtensions.cs` — 2 methods (1817-1818) | ✅ Done |
| **T1.19** | Create `ProcessTerminatorLoggingExtensions.cs` — 3 methods (1819-1821) — later consolidated into `ProcessHandleLoggingExtensions.cs` (T4.17) | ✅ Done |
| **T1.20** | Create `ClientLoggingExtensions.cs` — 1 method (1900) | ✅ Done |

**Note:** Pattern uses `public static partial class` with `this ILogger logger` extension methods
(not .NET 10 `extension(...)` blocks) — the `[LoggerMessage]` source generator requires a `partial` type to inject method implementations.

### Phase 2: Migrate Existing Calls

| Task | Description | Status |
|------|-------------|--------|
| **T2.1** | Migrate `AuthenticationService.cs` — 4 calls → extension methods | ✅ Done |
| **T2.2** | Migrate `FileSystemService.cs` — 13 calls → extension methods | ✅ Done |
| **T2.3** | Migrate `FileManagerService.cs` — 6 calls → extension methods | ✅ Done |
| **T2.4** | Migrate `LogDownloadService.cs` — 7 calls → extension methods | ✅ Done |
| **T2.5** | Migrate `FrameworkManagementService.cs` — 8 calls → extension methods | ✅ Done |
| **T2.6** | Migrate `SiteLifecycleManager.cs` — 17 calls → extension methods | ✅ Done |
| **T2.7** | Migrate `ReverseProxyManagerService.cs` — 11 calls → extension methods | ✅ Done |
| **T2.8** | Migrate `WebSiteHostingService.cs` — 34 calls → extension methods | ✅ Done |
| **T2.9** | Migrate `WebSitesConfigurationService.cs` — 12 calls → extension methods | ✅ Done |
| **T2.10** | Migrate `DsmApiClient.cs` — 2 calls → extension methods | ✅ Done |
| **T2.11** | Migrate `ArchiveExtractorService.cs` — 6 calls → extension methods | ✅ Done |
| **T2.12** | Migrate `VersionsDetectorService.cs` — 4 calls → extension methods | ✅ Done |
| **T2.13** | Migrate `PlatformInfoService.cs` — 2 calls → extension methods | ✅ Done |
| **T2.14** | Migrate `LicenseService.cs` (client) — 1 call → extension method | ✅ Done |

### Phase 3: Add Missing Logging

| Task | Description | Status |
|------|-------------|--------|
| **T3.1** | Add `ILogger` to `DownloaderService` — log download start/end, progress, failures | ✅ Done |
| **T3.2** | Add `ILogger` to `SystemProcessRunner` — log process spawn, working directory, arguments | ✅ Done |
| **T3.3** | Add `ILogger` to `SystemProcessHandle` — log process exit, wait timeout | ✅ Done |
| **T3.4** | Add `ILogger` to `ProcessTerminator` — log SIGTERM/SIGKILL attempts | ✅ Done |
| **T3.5** | Add `ILogger` to `DotnetVersionService` — log detection failures, channel queries | ✅ Done |
| **T3.6** | Add new `[LoggerMessage]` methods for services above — extend existing extension files | ✅ Done |

**Implementation Notes:**

- `DownloaderService` — Logs download start (file name + destination), completion (with file size), skip (file exists), and failure
- `SystemProcessRunner` — Logs process spawn (file name, arguments, working directory)
- `SystemProcessHandle` — Logs SIGTERM/SIGKILL sent, process exit (with exit code), and termination failures
- `ProcessTerminator` — Static class; logging handled by `SystemProcessHandle` wrapper. Methods later consolidated
  into `ProcessHandleLoggingExtensions.cs` (T4.17).
- `DotnetVersionService` (server-side) — Logs detection failures, channel queries, and release lookups (event IDs 1207-1213 in `FrameworkManagementLoggingExtensions.cs`)
- Event IDs updated: `DownloaderService` 1812-1815, `SystemProcessRunner` 1816, `SystemProcessHandle` 1817-1821 (includes consolidated ProcessTerminator methods)

### Phase 4: Specialized `ILogger<T>` with Namespace-Level Category Interfaces

**Objective:** Replace generic `this ILogger logger` parameters with specialized `this ILogger<ICategory> logger` for better log categorization and DI integration.

**Why specialized `ILogger<T>`:**

- Logs are automatically categorized by service name in log output
- Enables service-specific log level filtering
- Aligns with .NET DI conventions (`ILogger<TCategoryName>`)
- Better log aggregation and correlation in production

**Approach — Namespace-Level Category Interfaces:**

Each `[LoggerMessage]` extension file declares a namespace-level empty marker interface
used **only** as an `ILogger<T>` category name. This keeps the Logging project
as a leaf node with **zero project references**. The interface at namespace level
allows services to reference it directly (e.g., `ILogAuthenticationService`) without
qualifying through the extension class name.

```csharp
namespace Askyl.Dsm.WebHosting.Logging;

/// <summary>Category marker for ILogger&lt;T&gt; — no implementation required.</summary>
public interface ILogAuthenticationService { }

public static partial class AuthenticationLoggingExtensions
{
    [LoggerMessage(EventId = 1001, Level = LogLevel.Warning, Message = "Login failed for user: {Login}")]
    public static partial void LoginFailed(
        this ILogger<ILogAuthenticationService> logger, string login);
}
```

Services inject the interface type directly:

```csharp
public class AuthenticationService(
    ILogger<ILogAuthenticationService> logger, ...)
```

**No project reference changes needed** — Logging stays leaf-node (zero references).

| Task | Description | Status |
|------|-------------|--------|
| **T4.1** | Add `ILogXxx` interface + update all methods in `AuthenticationLoggingExtensions.cs` | ✅ Done |
| **T4.2** | Add `ILogXxx` interface + update all methods in `FileSystemServiceLoggingExtensions.cs` | ✅ Done |
| **T4.3** | Add `ILogXxx` interface + update all methods in `FileManagerServiceLoggingExtensions.cs` | ✅ Done |
| **T4.4** | Add `ILogXxx` interface + update all methods in `LogDownloadServiceLoggingExtensions.cs` | ✅ Done |
| **T4.5** | Add `ILogXxx` interface + update all methods in `FrameworkManagementLoggingExtensions.cs` | ✅ Done |
| **T4.6** | Add `ILogXxx` interface + update all methods in `DotnetVersionServiceLoggingExtensions.cs` | ✅ Done |
| **T4.7** | Add `ILogXxx` interface + update all methods in `ProcessLoggingExtensions.cs` | ✅ Done |
| **T4.8** | Add `ILogXxx` interface + update all methods in `ReverseProxyLoggingExtensions.cs` | ✅ Done |
| **T4.9** | Add `ILogXxx` interface + update all methods in `WebsiteLoggingExtensions.cs` | ✅ Done |
| **T4.10** | Add `ILogXxx` interface + update all methods in `ConfigurationLoggingExtensions.cs` | ✅ Done |
| **T4.11** | Add `ILogXxx` interface + update all methods in `DsmApiLoggingExtensions.cs` | ✅ Done |
| **T4.12** | Add `ILogXxx` interface + update all methods in `ArchiveExtractorLoggingExtensions.cs` | ✅ Done |
| **T4.13** | Add `ILogXxx` interface + update all methods in `VersionsDetectorLoggingExtensions.cs` | ✅ Done |
| **T4.14** | Add `ILogXxx` interface + update all methods in `PlatformInfoLoggingExtensions.cs` | ✅ Done |
| **T4.15** | Add `ILogXxx` interface + update all methods in `DownloaderLoggingExtensions.cs` | ✅ Done |
| **T4.16** | Add `ILogXxx` interface + update all methods in `ProcessRunnerLoggingExtensions.cs` | ✅ Done |
| **T4.17** | Consolidate process termination methods into `ProcessHandleLoggingExtensions.cs` (removed `ProcessTerminatorLoggingExtensions.cs`) | ✅ Done |
| **T4.18** | Add `ILogXxx` interface + update all methods in `ClientLoggingExtensions.cs` | ✅ Done |
| **T4.19** | Update all service implementations — change `ILogger<T>` type argument to namespace-level interface | ✅ Done |

**Note:** `ProcessTerminatorLoggingExtensions.cs` was removed — its 3 methods
(`SigTermSent`, `SigKillSent`, `FailedToTerminateProcess`) were consolidated
into `ProcessHandleLoggingExtensions.cs` since `SystemProcessHandle` is the
sole caller for all process termination logging.

### Phase 5: DSM API Request/Response Logging

| Task | Description | Status |
|------|-------------|--------|
| **T5.1** | Add logging to `DsmApiClient.ExecuteAsync()` — URL, method, status code, duration | ✅ Done |
| **T5.2** | Add logging for authentication failures and session expiration | ✅ Done |
| **T5.3** | Add logging for API errors (non-success responses with error codes) | ✅ Done |

**Implementation Notes:**

- `ExecutePostAsync` — Logs every HTTP request via `logger.ApiRequest("POST", url, statusCode, duration)` using `Stopwatch` for timing
- `AuthenticateAsync` — Logs auth failure via `logger.AuthenticationFailed(errorMessage)` with error reason from response
- `ExecuteAsync<R>` — Logs API-level errors (HTTP 200 but `Success: false`) via `logger.ApiError(reason, errorCode)` using reflection to access `ApiResponseBase<T>` properties on the generic type `R`

### Phase 6: Serilog Configuration Enhancements

| Task | Description | Status |
|------|-------------|--------|
| **T6.1** | Add `{EventId}` and `{EventType}` to Serilog output template | ✅ Done |
| **T6.2** | Add `Log.CloseAndFlush()` on graceful shutdown in `Program.cs` | ✅ Done |
| **T6.3** | Add Serilog `WithActivity` enricher for correlation | ✅ Done |

**Implementation Notes:**

- Output template: `{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [EventId:{EventId}] {Message:lj}{NewLine}{Exception}`
- `Log.CloseAndFlush()` registered via `IHostApplicationLifetime.ApplicationStopping` hook — single line in `Program.cs`
- `WithActivity` enricher (built into Serilog.AspNetCore 8.0+) — adds `ActivityId`, `ActivityTraceId`, `ActivitySpanId` to log context. No additional NuGet package required.

---

### Phase 7: Blazor UI Component Logging (Remaining Direct Calls)

**Objective:** Migrate remaining 26 direct `ILogger` calls in client-side Blazor `.razor` components and the Ui service to `[LoggerMessage]` extension methods.

**Remaining calls by file:**

| File | Project | Calls | Log Levels |
|------|---------|-------|------------|
| `Home.razor` | Ui.Client | 15 | 5× Information, 5× Warning, 5× Error |
| `AspNetReleasesDialog.razor` | Ui.Client | 4 | 4× Error |
| `FileSelectionDialog.razor` | Ui.Client | 4 | 3× Error, 1× Debug |
| `DotnetVersionsDialog.razor` | Ui.Client | 1 | 1× Error |
| `WebSiteConfigurationDialog.razor` | Ui.Client | 1 | 1× Error |
| `WebSiteHostingService.cs` | Ui | 1 | 1× Information |

| Task | Description | Status |
|------|-------------|--------|
| **T7.1** | Extend `ClientLoggingExtensions.cs` — add `ILogHome` category + 15 methods for `Home.razor` | ✅ Done |
| **T7.2** | Extend `ClientLoggingExtensions.cs` — add `ILogAspNetReleasesDialog` category + 4 methods | ✅ Done |
| **T7.3** | Extend `ClientLoggingExtensions.cs` — add `ILogFileSelectionDialog` category + 4 methods | ✅ Done |
| **T7.4** | Extend `ClientLoggingExtensions.cs` — add `ILogDotnetVersionsDialog` category + 1 method | ✅ Done |
| **T7.5** | Extend `ClientLoggingExtensions.cs` — add `ILogWebSiteConfigurationDialog` category + 1 method | ✅ Done |
| **T7.6** | Reuse existing `ReverseProxyRuleDeleted` in `WebsiteLoggingExtensions.cs` (ID 1800033) | ✅ Done |
| **T7.7** | Migrate `Home.razor` — replace 15 direct calls with extension method calls | ✅ Done |
| **T7.8** | Migrate `AspNetReleasesDialog.razor` — replace 4 direct calls | ✅ Done |
| **T7.9** | Migrate `FileSelectionDialog.razor` — replace 4 direct calls | ✅ Done |
| **T7.10** | Migrate `DotnetVersionsDialog.razor` — replace 1 direct call | ✅ Done |
| **T7.11** | Migrate `WebSiteConfigurationDialog.razor` — replace 1 direct call | ✅ Done |
| **T7.12** | Migrate `WebSiteHostingService.cs` (Ui) — replace 1 direct call | ✅ Done |

**Implementation Notes:**

- Client-side components switched from `ILogger` to `ILogger<ILogXxx>` specialized types injected via `[Inject]`
- `.razor` files use `Logger` field directly in `@code` blocks — extension methods called as `Logger.MethodName(args)`
- All client-side extensions consolidated into a single `ClientLoggingExtensions.cs` file with multiple category interfaces
- Event ID base for client UI: `7000000` range with 10K slots per component:
  LicenseService `7000000`, Home `7100000`, AspNetReleasesDialog `7200000`,
  FileSelectionDialog `7300000`, DotnetVersionsDialog `7400000`,
  WebSiteConfigurationDialog `7500000`

## Execution Order

The recommended execution order is:

1. **Phase 1** — Build the `[LoggerMessage]` extension files (foundation) ✅ Done
2. **Phase 2** — Migrate existing server-side calls one service at a time ✅ Done
3. **Phase 3** — Add logging to services that currently have none ✅ Done
4. **Phase 4** — Refactor to specialized `ILogger<TService>` (improves log categorization) ✅ Done
5. **Phase 5** — Add DSM API request/response logging (benefits from Phase 4) ✅ Done
6. **Phase 6** — Polish Serilog configuration and shutdown handling ✅ Done
7. **Phase 7** — Migrate client-side Blazor component logging ✅ Done

Each phase can be committed independently. Phase 2 tasks can be batched (e.g., T2.1-T2.2 in one commit).

---

## Acceptance Criteria

- [x] All 216 `[LoggerMessage]` methods created and assigned event IDs (server + client)
- [x] All 126 server-side logger calls migrated to extension methods
- [x] All 26 client-side logger calls migrated to extension methods
- [x] Zero CA2254 warnings remaining
- [x] All services log key operations (start, success, failure, duration)
- [x] DSM API requests are logged with URL, status, duration
- [x] Application shutdown flushes all pending logs (`Log.CloseAndFlush()`)
- [x] Serilog output includes event ID and event type
- [x] Log levels are consistent (Debug/Information/Warning/Error/Critical)
- [x] Build passes with 0 errors, 0 warnings
- [x] All tests pass (194 tests)
- [x] Format clean (`dotnet format`)
- [x] Markdown valid (`markdownlint`)
- [x] All server-side services use specialized `ILogger<ILogXxx>` for log categorization

---

## Notes

- **DsmApi classes are external contracts** — do not modify DsmApi model/response classes during this work
- **Client-side (WASM) logging** — 26 calls migrated in Phase 7 (5 Blazor dialogs + Home page + 1 Ui service); initial plan only covered 1 call in `LicenseService`
- **Event IDs are semantically significant** — they allow filtering and correlation in log aggregation tools
- **Log levels guidance:**
  - `Debug` — method entry/exit, variable state, intermediate results
  - `Information` — business events (login, site started, framework installed)
  - `Warning` — recoverable issues (retry, fallback, degraded state)
  - `Error` — failures that require attention (operation failed, resource unavailable)
  - `Critical` — unrecoverable, application may stop functioning
- **Extension pattern** — all `[LoggerMessage]` methods use `public static partial class` with `this ILogger<ILogXxx> logger`
  (not `extension(...)` blocks — the source generator requires a partial type)
- **Namespace-level category interfaces** — each extension file declares `public interface ILogXxx { }` at namespace level for `ILogger<T>` categorization
  (no project reference changes needed — Logging remains a leaf node with zero references)
- **ProcessHandle consolidation** — `ProcessTerminatorLoggingExtensions.cs` was removed;
  its methods were consolidated into `ProcessHandleLoggingExtensions.cs`
  (event IDs 1817-1821) since `SystemProcessHandle` is the sole caller
- **OperationTimer** — `Askyl.Dsm.WebHosting.Tools.Diagnostics.OperationTimer` is a value-type
  (`struct`) that starts a `Stopwatch` on construction and invokes a callback with elapsed
  milliseconds on disposal. Use with `using var` to guarantee duration capture on all exit paths
  (success, exception, early return). Replaced manual `Stopwatch.StartNew()` boilerplate in
  ReverseProxyManagerService, FrameworkManagementService, WebSiteHostingService,
  SiteLifecycleManager, DownloaderService, DotnetVersionService, and WebSitesConfigurationService.
  DsmApiClient retains inline `Stopwatch` (single HTTP call, duration passed to `ApiRequest` method)
