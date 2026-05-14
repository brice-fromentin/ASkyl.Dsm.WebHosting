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
| **Logging project** | Placeholder — single unused "HelloWorld" demo extension |
| **Serilog (server)** | File sink (rolling daily), Console in dev, min level `Information` |
| **Serilog (client)** | BrowserConsole sink, min level `Debug` |
| **Logger calls** | 126 total calls, 123 unique templates |
| **CA2254 warnings** | ~76 calls trigger "expensive argument evaluation" warning |
| **LogDownloadService** | Archives package/debug/app logs to ZIP |

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

/// <summary>
/// Structured logging extension methods for authentication-related events.
/// </summary>
public static partial class AuthenticationLoggingExtensions
{
    /// <summary>
    /// Logs a successful login event for the specified user.
    /// </summary>
    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "Login successful for user: {Login} - SID stored")]
    public static partial void LoginSuccessful(this ILogger logger, string login);
}
```

**Rules:**

- `public static partial class` — class MUST be partial for source generator
- `this ILogger logger` — defines the extension target type on each method
- `public static partial void` — the source-generated method signature (no body)
- Event IDs are `int` literals (not constants) — required by source generator
- XML doc comments on each method

### Semantic File Split

The `[LoggerMessage]` extensions are organized into separate files **per service** for maintainability:

| File | Domain | Methods | Event IDs | Service Covered |
|------|--------|---------|-----------|-----------------|
| `AuthenticationLoggingExtensions.cs` | Authentication | 4 | 1001-1004 | AuthenticationService |
| `FileSystemServiceLoggingExtensions.cs` | File system | 12 | 1100-1111 | FileSystemService |
| `FileManagerServiceLoggingExtensions.cs` | File system | 6 | 1112-1117 | FileManagerService |
| `LogDownloadServiceLoggingExtensions.cs` | File system | 7 | 1118-1124 | LogDownloadService |
| `FrameworkManagementLoggingExtensions.cs` | .NET framework | 7 | 1200-1206 | FrameworkManagementService |
| `DotnetVersionServiceLoggingExtensions.cs` | .NET framework | 7 | 1207-1213 | DotnetVersionService |
| `ProcessLoggingExtensions.cs` | Process lifecycle | 17 | 1300-1316 | SiteLifecycleManager |
| `ReverseProxyLoggingExtensions.cs` | Reverse proxy | 11 | 1400-1410 | ReverseProxyManagerService |
| `WebsiteLoggingExtensions.cs` | Website hosting | 34 | 1500-1533 | WebSiteHostingService |
| `ConfigurationLoggingExtensions.cs` | Configuration | 12 | 1600-1611 | WebSitesConfigurationService |
| `DsmApiLoggingExtensions.cs` | DSM API | 5 | 1700-1704 | DsmApiClient |
| `ArchiveExtractorLoggingExtensions.cs` | Infrastructure | 6 | 1800-1805 | ArchiveExtractorService |
| `VersionsDetectorLoggingExtensions.cs` | Infrastructure | 4 | 1806-1809 | VersionsDetectorService |
| `PlatformInfoLoggingExtensions.cs` | Infrastructure | 2 | 1810-1811 | PlatformInfoService |
| `DownloaderLoggingExtensions.cs` | Infrastructure | 4 | 1812-1815 | DownloaderService |
| `ProcessRunnerLoggingExtensions.cs` | Infrastructure | 1 | 1816 | SystemProcessRunner |
| `ProcessHandleLoggingExtensions.cs` | Infrastructure | 2 | 1817-1818 | SystemProcessHandle |
| `ProcessTerminatorLoggingExtensions.cs` | Infrastructure | 3 | 1819-1821 | ProcessTerminator |
| `ClientLoggingExtensions.cs` | Client-side (WASM) | 1 | 1900 | LicenseService |
| **Total** | | **144** | | |

### Event ID Ranges

Each file gets a dedicated event ID range to avoid collisions and make log correlation easier:

| Range | Domain |
|-------|--------|
| `1000-1099` | Authentication |
| `1100-1199` | File management |
| `1200-1299` | Framework management |
| `1300-1399` | Process lifecycle |
| `1400-1499` | Reverse proxy |
| `1500-1599` | Website hosting |
| `1600-1699` | Configuration |
| `1700-1799` | DSM API |
| `1800-1899` | Infrastructure |
| `1900-1999` | Client-side |

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
| **T1.19** | Create `ProcessTerminatorLoggingExtensions.cs` — 3 methods (1819-1821) | ✅ Done |
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
- `ProcessTerminator` — Static class; logging handled by `SystemProcessHandle` wrapper (logs before/after calling `ProcessTerminator.SendGracefulShutdownSignal`)
- `DotnetVersionService` (server-side) — Logs detection failures, channel queries, and release lookups (event IDs 1207-1213 in `FrameworkManagementLoggingExtensions.cs`)
- Event IDs updated: `DownloaderService` 1812-1815, `SystemProcessRunner` 1816, `SystemProcessHandle` 1817-1818, `ProcessTerminator` 1819-1821

### Phase 4: Specialized `ILogger<TService>` Refactoring

**Objective:** Replace generic `this ILogger logger` parameters with specialized `this ILogger<TService> logger` for better log categorization and DI integration.

**Why specialized `ILogger<T>`:**

- Logs are automatically categorized by service name in log output
- Enables service-specific log level filtering
- Aligns with .NET DI conventions (`ILogger<TCategoryName>`)
- Better log aggregation and correlation in production

**Approach:**

1. Ensure all service interfaces exist in `Data/Contracts/` (already done for 13/19)
2. Add missing interfaces to `Data/Contracts/`
3. Add project reference: `Logging` → `Data` (safe — no circular dependency)
4. Update all extension method signatures: `ILogger` → `ILogger<TService>`
5. Update all service implementations to inject `ILogger<TService>` (most already do)

**Interfaces already in `Data/Contracts/`** (13):

| Interface | Logging Extension |
|-----------|------------------|
| `IAuthenticationService` | `AuthenticationLoggingExtensions.cs` |
| `IFileSystemService` | `FileSystemServiceLoggingExtensions.cs` |
| `IFileManagerService` | `FileManagerServiceLoggingExtensions.cs` |
| `ILogDownloadService` | `LogDownloadServiceLoggingExtensions.cs` |
| `IFrameworkManagementService` | `FrameworkManagementLoggingExtensions.cs` |
| `IDotnetVersionService` | `DotnetVersionServiceLoggingExtensions.cs` |
| `IPlatformInfoService` | `PlatformInfoLoggingExtensions.cs` |
| `IReverseProxyManagerService` | `ReverseProxyLoggingExtensions.cs` |
| `IWebSiteHostingService` | `WebsiteLoggingExtensions.cs` |
| `IWebSitesConfigurationService` | `ConfigurationLoggingExtensions.cs` |
| `IDownloaderService` | `DownloaderLoggingExtensions.cs` |
| `IVersionsDetectorService` | `VersionsDetectorLoggingExtensions.cs` |
| `IArchiveExtractorService` | `ArchiveExtractorLoggingExtensions.cs` |

**Interfaces to move/create in `Data/Contracts/`** (6):

| Interface | Current Location | Move to | Logging Extension |
|-----------|-----------------|---------|-------------------|
| `IProcessRunner` | `Tools/Infrastructure/ProcessRunner.cs` | `Data/Contracts/` | `ProcessRunnerLoggingExtensions.cs` |
| `IProcessHandle` | `Tools/Infrastructure/ProcessHandle.cs` | `Data/Contracts/` | `ProcessHandleLoggingExtensions.cs` |
| `ILicenseService` | `Ui.Client/Interfaces/` | `Data/Contracts/` | `ClientLoggingExtensions.cs` |
| `IDsmApiClient` | (none — concrete class only) | `Data/Contracts/` | `DsmApiLoggingExtensions.cs` |
| `ISiteLifecycleManager` | (none — concrete class only) | `Data/Contracts/` | `ProcessLoggingExtensions.cs` |
| `IProcessTerminator` | (none — static class) | N/A (use `IProcessRunner`) | `ProcessTerminatorLoggingExtensions.cs` |

**Note:** `ProcessTerminator` is a static class — logging is handled by `SystemProcessHandle` (uses `ILogger<IProcessRunner>`).

| Task | Description | Status |
|------|-------------|--------|
| **T4.1** | Move `IProcessRunner` interface to `Data/Contracts/` | ⬜ Not started |
| **T4.2** | Move `IProcessHandle` interface to `Data/Contracts/` | ⬜ Not started |
| **T4.3** | Move `ILicenseService` interface to `Data/Contracts/` | ⬜ Not started |
| **T4.4** | Create `IDsmApiClient` interface in `Data/Contracts/` | ⬜ Not started |
| **T4.5** | Create `ISiteLifecycleManager` interface in `Data/Contracts/` | ⬜ Not started |
| **T4.6** | Add project reference `Logging` → `Data` | ⬜ Not started |
| **T4.7** | Update all 19 extension files — change `this ILogger` to `this ILogger<TService>` | ⬜ Not started |
| **T4.8** | Update all service implementations — verify `ILogger<TService>` injection | ⬜ Not started |
| **T4.9** | Update `SystemProcessHandle` — use `ILogger<IProcessRunner>` instead of `ILogger<SystemProcessRunner>` | ⬜ Not started |

**Dependency Graph After Change:**

```text
Constants (leaf node)
  ↑
Data (references: Constants)
  ↑
Logging (references: Data)
  ↑
Tools (references: Constants, Data, Logging)
  ↑
Ui (references: Constants, Data, Logging, Tools, Ui.Client)
```

**No circular dependency** — Data is a leaf node (only references Constants), Logging references Data, and all consumers reference Logging.

### Phase 5: DSM API Request/Response Logging

| Task | Description | Status |
|------|-------------|--------|
| **T5.1** | Add logging to `DsmApiClient.ExecuteAsync()` — URL, method, status code, duration | ⬜ Not started |
| **T5.2** | Add logging for authentication failures and session expiration | ⬜ Not started |
| **T5.3** | Add logging for API errors (non-success responses with error codes) | ⬜ Not started |

### Phase 6: Serilog Configuration Enhancements

| Task | Description | Status |
|------|-------------|--------|
| **T6.1** | Add `{EventId}` and `{EventType}` to Serilog output template | ⬜ Not started |
| **T6.2** | Add `Log.CloseAndFlush()` on graceful shutdown in `Program.cs` | ⬜ Not started |
| **T6.3** | Add Serilog `WithActivityId()` enricher for correlation | ⬜ Not started |

---

## Execution Order

The recommended execution order is:

1. **Phase 1** — Build the `[LoggerMessage]` extension files (foundation)
2. **Phase 2** — Migrate existing calls one service at a time (verify build/tests after each)
3. **Phase 3** — Add logging to services that currently have none
4. **Phase 4** — Refactor to specialized `ILogger<TService>` (improves log categorization)
5. **Phase 5** — Add DSM API request/response logging (benefits from Phase 4)
6. **Phase 6** — Polish Serilog configuration and shutdown handling

Each phase can be committed independently. Phase 2 tasks can be batched (e.g., T2.1-T2.2 in one commit).

---

## Acceptance Criteria

- [x] All 137 `[LoggerMessage]` methods created and assigned event IDs
- [x] All 126 logger calls migrated to extension methods
- [x] Zero CA2254 warnings remaining
- [ ] All services log key operations (start, success, failure, duration)
- [ ] DSM API requests are logged with URL, status, duration
- [ ] Application shutdown flushes all pending logs (`Log.CloseAndFlush()`)
- [ ] Serilog output includes event ID and event type
- [x] Log levels are consistent (Debug/Information/Warning/Error/Critical)
- [x] Build passes with 0 errors, 0 warnings
- [ ] All tests pass
- [x] Format clean (`dotnet format`)
- [x] Markdown valid (`markdownlint`)

---

## Notes

- **DsmApi classes are external contracts** — do not modify DsmApi model/response classes during this work
- **Client-side (WASM) logging** — minimal scope (1 existing call); focus server-side first
- **Event IDs are semantically significant** — they allow filtering and correlation in log aggregation tools
- **Log levels guidance:**
  - `Debug` — method entry/exit, variable state, intermediate results
  - `Information` — business events (login, site started, framework installed)
  - `Warning` — recoverable issues (retry, fallback, degraded state)
  - `Error` — failures that require attention (operation failed, resource unavailable)
  - `Critical` — unrecoverable, application may stop functioning
- **Extension pattern** — all `[LoggerMessage]` methods use `public static partial class` with `this ILogger logger`
  (not `extension(...)` blocks — the source generator requires a partial type)
