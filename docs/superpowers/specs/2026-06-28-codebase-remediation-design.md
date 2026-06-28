# Codebase Review Remediation Plan

**Date:** June 28, 2026
**Source:** `docs/ai/codebase-review-2026-06-28.md`
**Delivery:** Single PR, commits ordered by priority tier

---

## Overview

This plan refines the action items from the full codebase review (94 findings: 8 critical, 19 high, 43 medium, 24 low) into a prioritized execution order grouped by area within each tier.

**Structure:** Priority-first, thematic within. Most impactful changes ship first; related fixes stay together for coherent `git diff` review.

---

## P0 — Critical (Runtime Failures & Resource Exhaustion)

### Services (3 items)

| # | File | Issue |
|---|------|-------|
| S1 | `Ui.Client/Services/AuthenticationService.cs` | HttpClient per-call creation — socket exhaustion. Cache as field with `IHttpClientFactory`. |
| S2 | `Ui/Controllers/*.cs` (6 files) | Missing `CancellationToken` — client disconnects leave operations running. Add parameter, propagate through call chain. |
| S7 | `Ui.Client/Services/DotnetVersionService.cs:53` | `CancellationToken.None` hardcoded — cancellation never propagates. Accept and pass through. |

### UI/Blazor (2 items)

| # | File | Issue |
|---|------|-------|
| UI-C1 | `Login.razor`, `WebSiteConfigurationDialog.razor`, `RealTimeTextField.razor`, `RealTimeNumberField.razor` | Inline styles violate FluentUI policy. Replace with CSS classes or FluentUI theming. |
| UI-C2 | `FileSelectionDialog.razor:213`, `tree-navigation.js:29-30` | JS interop without error handling — unhandled exceptions fault the Blazor circuit. Add try/catch and null checks. |

### Data Layer (1 item)

| # | File | Issue |
|---|------|-------|
| D1 | `Data/Results/*.cs` (7 types) | Mutable Result properties (`get; set;`). Change to `get; init;` — foundational fix; enables correct test assertions in P1. |

---

## P1 — High Priority (Correctness & Reliability)

### Services (2 items)

| # | File | Issue |
|---|------|-------|
| S5 | `DsmSession.cs:195`, `DsmSettingsService.cs:52`, `FrameworkManagementService.cs:81,86` | Logging `ex.Message` loses stack trace. Change `[LoggerMessage]` to accept `Exception ex`. |
| S6 | `Ui.Client/Services/CultureManager.cs:294` | Static method takes `ILogger` as parameter. Convert to instance method with injected logger. |

### Data Layer (2 items)

| # | File | Issue |
|---|------|-------|
| D2 | `WebSiteInstance.cs` | Mutable DTO properties (`Configuration`, `IsRunning`, `Process`, `RequiredFramework`). Change to `get; init;`. |
| D3 | `LoginCredentials.cs` | `Password` mutable — security risk. Change to `get; init;`. |

### Tests (3 items)

| # | File | Issue |
|---|------|-------|
| T-C1 | Multiple service files | Core business logic untested: `WebSiteHostingService`, `AuthenticationService`, `FrameworkManagementService`, `DotnetVersionService`, `WebSitesConfigurationService`. |
| T-C2 | `AuthenticationNavigationGuardTests.cs` | Reflection-based test against sealed `NavigationContext`. Add Bunit; use `JSInvokeInterceptors`. |
| T-C3 | `LicenseServiceTests.cs:72` | Hardcoded `Assert.Equal(4, ...)` — brittle. Assert against expected data count. |

---

## P2 — Medium Priority (Maintenance & Compliance)

### Services (7 items)

| # | File | Issue |
|---|------|-------|
| S3 | `SiteLifecycleManager.cs`, `PlatformInfoService.cs` | Traditional constructors — convert to primary constructors. |
| S8 | `AuthenticationService.cs`, `DotnetVersionService.cs`, `FileSystemService.cs` | `NotImplementedException` in production — implement or guard with clear error path. |
| S9 | `WebSiteHostingService.cs:110,167` | Broad `catch (Exception)` — narrow to expected exception types. |
| S10 | `LogDownloadService.cs` | Missing `CancellationToken` on file I/O operations. |
| S11 | `VersionsDetectorService.cs:73` | `_cachedFrameworks` not thread-safe for concurrent reads before initialization. |
| S14 | `DsmSession.cs:28-29` | `_sessionValid` and `_lastSessionValidation` not thread-safe. |
| S15 | `WebSitesConfigurationService.cs:24` | `_initialized` not volatile. |

### UI/Blazor (6 items)

| # | File | Issue |
|---|------|-------|
| UI-H1 | 6 component files | Duplicated `IWorkingState` boilerplate (~42 lines). Extract `WorkingStateBase : ComponentBase, IWorkingState`. |
| UI-H3 | `LoadingOverlay.razor` | Implicit render coupling — relies on parent `StateHasChanged()` cascade. |
| UI-H4 | `AutoDataGrid.razor:133-153` | Double-click emulation fires single-click before detecting double-click. |
| UI-H5 | `Login.razor:22` | `FluentDialog` without `Open` parameter — focus-trap accessibility issue. |
| UI-M1 | `RealTimeTextField.razor`, `RealTimeNumberField.razor` | Missing `[EditorRequired]` on `ValueChanged`. |
| UI-M7 | `AspNetReleasesDialog.razor:203` | `StateHasChanged()` in `finally` — use `InvokeAsync<StateHasChanged>()`. |

### Data Layer (5 items)

| # | File | Issue |
|---|------|-------|
| D4 | `LastReleaseUninstallException.cs` | Generic constructors set `Version`/`Channel` to `String.Empty` — defeats purpose. |
| D5 | `WebSiteConfiguration.cs` | All properties mutable — change to `get; init;`. |
| D6 | `AspNetCoreReleaseInfo.cs` | Uses `[SetsRequiredMembers]` — convert to primary constructor. |
| D7 | `ReverseProxy.cs:27-38` | Magic numbers `60` and `1` — reference `ReverseProxyConstants`. |
| D10 | `ApiResponseBase.cs` | Mutable response properties — change to `get; init;`. |

### Tests (5 items)

| # | File | Issue |
|---|------|-------|
| T-H1 | `DsmSessionTests.cs`, `DsmApiClientTests.cs` | Shared `HttpClient` not disposed between tests — socket leak risk. |
| T-H2 | `FileSystemServiceTests.cs:264-313` | `FakeDsmSession` doesn't validate parameters — tests pass with wrong API calls. |
| T-H3 | `SiteLifecycleManagerTests.cs:39-43` | Temp directory can leak on construction failure. |
| T-M2 | `CultureManagerTests.cs:252-275` | Static state tests order-dependent — flaky under parallel execution. |
| T-M4 | `SemaphoreLockTests.cs:119-160` | Concurrency test uses `Task.Delay(10)` — can fail on slow CI. |

### Compliance (4 items)

| # | File | Issue |
|---|------|-------|
| CO-1 | 6 parameter files | Hardcoded API method strings — add to `ApiConstants`. |
| CO-2 | `GlobalizationSettings.cs` | Not using primary constructor. |
| CO-3 | `Ui.Client/Program.cs:16` | Hardcoded `"appsettings.json"` — use `ApplicationConstants.SettingsFileName`. |
| CO-L1 | `Application` and `DSM.System` namespaces | Duplicate `ConfigurationFileName` constant. |

---

## P3 — Low Priority (Polish & Cleanup)

### Services (3 items)

| # | File | Issue |
|---|------|-------|
| S12 | `ReverseProxyManagerService.cs:194-218` | `IsNotFoundError` overloaded with different semantics — string version is fragile. |
| S13 | `FileSystemService.cs:103-143` | Large ACL object initialization (~40 lines) — extract to factory method. |
| — | N/A | No additional P3 service items. (S15 thread-safety is covered in P2.) |

### UI/Blazor (8 items)

| # | File | Issue |
|---|------|-------|
| UI-H2 | `WebSiteConfigurationDialog.razor:141,206` | Magic string `"80%"` — use `DialogConstants`. |
| UI-M2 | `Home.razor:25-57` | 10+ event handlers create new delegates every render — cache as fields. |
| UI-M3 | `FileSelectionDialog.razor`, `DotnetVersionsDialog.razor` | `GetFileIcon`/`GetFrameworkIcon` create new `Icon` instances — use `static readonly`. |
| UI-M4 | `WebSiteConfigurationDialog.razor:114` | `Enum.Parse<ProtocolType>` in setter can throw `ArgumentException`. |
| UI-M5 | `DotnetVersionsDialog.razor:38` | `<strong>` inside `FluentLabel` — FluentUI violation. |
| UI-M6 | `aspnet-releases.css:2-3` | Hardcoded pixel dimensions — not responsive. |
| UI-L1/L2/L4/L5/L6/L7/L8 | Various | Unused fields, dead code, aria-labels, JS namespace pollution, lazy init thread-safety. |

### Data Layer (4 items)

| # | File | Issue |
|---|------|-------|
| D8 | `FileStationFile.cs:14` | `Type` is `string` for closed set — convert to enum. |
| D9 | `Data/Results/` | Inconsistent factory methods across 7 Result subclasses — add to abstract base. |
| D11 | `ReverseProxyFrontend.cs`, `ReverseProxyBackend.cs`, `ReverseProxyHttps.cs` | Redundant primary constructor parameters repeated as `get; init;` properties. |
| D-L1/L2/L3 | Various | Magic strings, missing `sealed`, null handling in `FormattedMessage`. |

### Tests (6 items)

| # | File | Issue |
|---|------|-------|
| T-M1 | `LogDownloadServiceTests.cs` | Integration-style tests — environment-dependent. |
| T-M3 | `OperationTimerTests.cs:53-60` | Time assertion only checks `>= 0` — always true. |
| T-M5 | Multiple test files | Missing `[Trait]` categorization for file-system-dependent tests. |
| T-M6 | `DsmSettingsServiceTests.cs:37-68` | Tests parsing logic inline, not actual service methods. |
| T-L1/L2/L3/L4/L5 | Various | Magic numbers, `dynamic` assertions, redundant tests, naming inconsistencies. |

### Compliance (4 items)

| # | Issue |
|---|-------|
| CO-L2 | Duplicate error code value `-4` in `DsmConstants` and `ReverseProxyConstants`. |
| CO-L3 | `channels.ToList()` — use `[.. channels]` collection expression. |
| CO-L4 | `new TaskCompletionSource<ApiResult>()` — use `new()`. |
| CO-L5 | Hardcoded `"Installation completed successfully."` — extract to constant. |

---

## Execution Order (Single PR)

Commits within the single PR should follow this order:

1. **P0 commits** (6 items) — foundational fixes first
2. **P1 commits** (7 items) — correctness and test coverage
3. **P2 commits** (22 items) — maintenance and compliance
4. **P3 commits** (21 items) — polish and cleanup

Each commit should group related file changes (e.g., all Result immutability changes in one commit, all primary constructor conversions in one commit).

---

## Dependencies

- **D1 (Result immutability)** must precede **T-C1** (new tests) and **T-C3** (test assertion fix)
- **S1 (HttpClient fix)** should precede **T-H1** (test HttpClient disposal)
- **CO-1 (API constants)** has no blockers but should precede any new API parameter usage
- **D2/D3/D5/D10 (immutability)** can be batched with D1 for efficiency
