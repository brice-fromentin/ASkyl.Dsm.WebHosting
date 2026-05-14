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

### .NET 10 Extension Method Pattern

All `[LoggerMessage]` methods use the .NET 10 `extension(...)` block pattern
(same as `HttpClientExtensions.cs`):

```csharp
namespace Askyl.Dsm.WebHosting.Logging;

/// <summary>
/// Structured logging extension methods for authentication-related events.
/// </summary>
public static class AuthenticationLoggingExtensions
{
    extension(ILogger logger)
    {
        /// <summary>
        /// Logs a successful login event for the specified user.
        /// </summary>
        [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "Login successful for user: {Login}")]
        public partial void LoginSuccessful(string login);
    }
}
```

**Rules:**

- `public static class` — never `partial` at class level
- `extension(ILogger logger)` — defines the extension target type
- `public partial void` — the source-generated method signature (no body)
- One `extension(ILogger logger)` block per class
- Event IDs are `int` literals (not constants) — required by source generator
- XML doc comments on each method (not on the extension block)

### Semantic File Split

The `[LoggerMessage]` extensions are organized into separate files by domain for maintainability:

| File | Domain | ~Methods | Services Covered |
|------|--------|----------|-----------------|
| `AuthenticationLoggingExtensions.cs` | Authentication | 4 | AuthenticationService |
| `FileManagementLoggingExtensions.cs` | File system | 13 | FileSystemService, FileManagerService, LogDownloadService |
| `FrameworkManagementLoggingExtensions.cs` | .NET framework | 8 | FrameworkManagementService |
| `ProcessLoggingExtensions.cs` | Process lifecycle | 15 | SiteLifecycleManager, SystemProcessRunner, SystemProcessHandle, ProcessTerminator |
| `ReverseProxyLoggingExtensions.cs` | Reverse proxy | 10 | ReverseProxyManagerService |
| `WebsiteLoggingExtensions.cs` | Website hosting | 33 | WebSiteHostingService |
| `ConfigurationLoggingExtensions.cs` | Configuration | 12 | WebSitesConfigurationService |
| `DsmApiLoggingExtensions.cs` | DSM API | 5 | DsmApiClient |
| `InfrastructureLoggingExtensions.cs` | Infrastructure | 6 | DownloaderService, VersionsDetectorService, PlatformInfoService |
| `ClientLoggingExtensions.cs` | Client-side (WASM) | 1 | LicenseService |
| **Total** | | **123** | |

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
| **T1.1** | Replace `HelloWorldExtensions.cs` with project structure — add `Microsoft.Extensions.Logging.Abstractions` reference | ⬜ Not started |
| **T1.2** | Create `AuthenticationLoggingExtensions.cs` — 4 methods (event IDs 1000-1004) | ⬜ Not started |
| **T1.3** | Create `FileManagementLoggingExtensions.cs` — 13 methods (1100-1113) | ⬜ Not started |
| **T1.4** | Create `FrameworkManagementLoggingExtensions.cs` — 8 methods (1200-1208) | ⬜ Not started |
| **T1.5** | Create `ProcessLoggingExtensions.cs` — 15 methods (1300-1315) | ⬜ Not started |
| **T1.6** | Create `ReverseProxyLoggingExtensions.cs` — 10 methods (1400-1410) | ⬜ Not started |
| **T1.7** | Create `WebsiteLoggingExtensions.cs` — 33 methods (1500-1533) | ⬜ Not started |
| **T1.8** | Create `ConfigurationLoggingExtensions.cs` — 12 methods (1600-1612) | ⬜ Not started |
| **T1.9** | Create `DsmApiLoggingExtensions.cs` — 5 methods (1700-1705) | ⬜ Not started |
| **T1.10** | Create `InfrastructureLoggingExtensions.cs` — 6 methods (1800-1806) | ⬜ Not started |
| **T1.11** | Create `ClientLoggingExtensions.cs` — 1 method (1900) | ⬜ Not started |

### Phase 2: Migrate Existing Calls

| Task | Description | Status |
|------|-------------|--------|
| **T2.1** | Migrate `AuthenticationService.cs` — 4 calls → extension methods | ⬜ Not started |
| **T2.2** | Migrate `FileSystemService.cs` — 13 calls → extension methods | ⬜ Not started |
| **T2.3** | Migrate `FileManagerService.cs` — 6 calls → extension methods | ⬜ Not started |
| **T2.4** | Migrate `LogDownloadService.cs` — 7 calls → extension methods | ⬜ Not started |
| **T2.5** | Migrate `FrameworkManagementService.cs` — 8 calls → extension methods | ⬜ Not started |
| **T2.6** | Migrate `SiteLifecycleManager.cs` — 17 calls → extension methods | ⬜ Not started |
| **T2.7** | Migrate `ReverseProxyManagerService.cs` — 10 calls → extension methods | ⬜ Not started |
| **T2.8** | Migrate `WebSiteHostingService.cs` — 33 calls → extension methods | ⬜ Not started |
| **T2.9** | Migrate `WebSitesConfigurationService.cs` — 12 calls → extension methods | ⬜ Not started |
| **T2.10** | Migrate `DsmApiClient.cs` — 2 calls → extension methods | ⬜ Not started |
| **T2.11** | Migrate `ArchiveExtractorService.cs` — 6 calls → extension methods | ⬜ Not started |
| **T2.12** | Migrate `VersionsDetectorService.cs` — 4 calls → extension methods | ⬜ Not started |
| **T2.13** | Migrate `PlatformInfoService.cs` — 2 calls → extension methods | ⬜ Not started |
| **T2.14** | Migrate `LicenseService.cs` (client) — 1 call → extension method | ⬜ Not started |

### Phase 3: Add Missing Logging

| Task | Description | Status |
|------|-------------|--------|
| **T3.1** | Add `ILogger` to `DownloaderService` — log download start/end, progress, failures | ⬜ Not started |
| **T3.2** | Add `ILogger` to `SystemProcessRunner` — log process spawn, working directory, arguments | ⬜ Not started |
| **T3.3** | Add `ILogger` to `SystemProcessHandle` — log process exit, wait timeout | ⬜ Not started |
| **T3.4** | Add `ILogger` to `ProcessTerminator` — log SIGTERM/SIGKILL attempts | ⬜ Not started |
| **T3.5** | Add `ILogger` to `DotnetVersionService` — log detection failures, channel queries | ⬜ Not started |
| **T3.6** | Add new `[LoggerMessage]` methods for services above — extend existing extension files | ⬜ Not started |

### Phase 4: DSM API Request/Response Logging

| Task | Description | Status |
|------|-------------|--------|
| **T4.1** | Add logging to `DsmApiClient.ExecuteAsync()` — URL, method, status code, duration | ⬜ Not started |
| **T4.2** | Add logging for authentication failures and session expiration | ⬜ Not started |
| **T4.3** | Add logging for API errors (non-success responses with error codes) | ⬜ Not started |

### Phase 5: Serilog Configuration Enhancements

| Task | Description | Status |
|------|-------------|--------|
| **T5.1** | Add `{EventId}` and `{EventType}` to Serilog output template | ⬜ Not started |
| **T5.2** | Add `Log.CloseAndFlush()` on graceful shutdown in `Program.cs` | ⬜ Not started |
| **T5.3** | Add Serilog `WithActivityId()` enricher for correlation | ⬜ Not started |

---

## Execution Order

The recommended execution order is:

1. **Phase 1** — Build the `[LoggerMessage]` extension files (foundation)
2. **Phase 2** — Migrate existing calls one service at a time (verify build/tests after each)
3. **Phase 3** — Add logging to services that currently have none
4. **Phase 4** — Add DSM API request/response logging
5. **Phase 5** — Polish Serilog configuration and shutdown handling

Each phase can be committed independently. Phase 2 tasks can be batched (e.g., T2.1-T2.2 in one commit).

---

## Acceptance Criteria

- [ ] All 123 `[LoggerMessage]` methods created and assigned event IDs
- [ ] All 126 logger calls migrated to extension methods
- [ ] Zero CA2254 warnings remaining
- [ ] All services log key operations (start, success, failure, duration)
- [ ] DSM API requests are logged with URL, status, duration
- [ ] Application shutdown flushes all pending logs (`Log.CloseAndFlush()`)
- [ ] Serilog output includes event ID and event type
- [ ] Log levels are consistent (Debug/Information/Warning/Error/Critical)
- [ ] Build passes with 0 errors, 0 warnings
- [ ] All 181 tests pass
- [ ] Format clean (`dotnet format`)
- [ ] Markdown valid (`markdownlint`)

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
- **.NET 10 extension method flavor** — all `[LoggerMessage]` methods use `extension(ILogger logger)` blocks inside `public static class`, matching `HttpClientExtensions.cs` pattern
