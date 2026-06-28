# ASkyl.Dsm.WebHosting — Full Codebase Review

**Date:** June 28, 2026
**Target Framework:** .NET 10 (net10.0)

---

## Summary

| Area | Critical | High | Medium | Low |
|------|----------|------|--------|-----|
| **Services Layer** | 2 | 6 | 8 | 3 |
| **UI/Blazor** | 2 | 5 | 8 | 8 |
| **Data Layer** | 1 | 3 | 9 | 3 |
| **Tests** | 3 | 5 | 8 | 5 |
| **Compliance** | 0 | 0 | 10 | 5 |
| **TOTAL** | **8** | **19** | **43** | **24** |

---

## CRITICAL FINDINGS (Fix Immediately)

### S1. HttpClient Lifetime Violation in `AuthenticationService` (Client)

**File:** `Ui.Client/Services/AuthenticationService.cs:28,54,61`

Creates a new `HttpClient` on **every method call**. All other client services correctly cache `HttpClient` as a field. This causes socket exhaustion under load.

**Fix:** Cache `HttpClient` as a field matching the pattern in sibling services:

```csharp
private readonly HttpClient _httpClient = httpClientFactory.CreateClient(ApplicationConstants.HttpClientName);
```

### S2. CancellationToken Missing in All Controllers

**File:** `Ui/Controllers/*.cs` (all 6 controllers)

None of the 6 controllers accept/propagate `CancellationToken`. Client disconnections leave in-flight operations running, wasting server resources.

**Fix:** Add `CancellationToken` parameter to controller actions and propagate through the call chain.

### UI-C1. Inline Styles Violate FluentUI Policy

**Files:**

- `Login.razor:39` — `Style="flex-grow: 1;"`
- `WebSiteConfigurationDialog.razor:90` — `Style="display: none;"`
- `RealTimeTextField.razor:1` — exposes `Style` parameter
- `RealTimeNumberField.razor:1` — exposes `Style` parameter

AGENTS.md §10 explicitly forbids inline styles. Use CSS classes or FluentUI theming.

### UI-C2. JS Interop Without Error Handling

**Files:**

- `FileSelectionDialog.razor:213` — `JSRuntime.InvokeVoidAsync` has no try/catch
- `tree-navigation.js:29-30` — no null check on `parentItem`

Unhandled JS exceptions fault the Blazor circuit. `Path.GetDirectoryName()` can return `null` for root paths.

### D1. Mutable Result Properties

**Files:** All Result types in `Data/Results/`

- `ApiResult.cs:16-29`
- `ApiResultValue.cs:19-37`
- `ApiResultItems.cs:19-38`
- `ApiResultData.cs:19-37`
- `AuthenticationResult.cs:27-39`
- `InstallationResult.cs:17-22`
- `WebSiteInstanceResult.cs:18`

All Result types use `get; set;` (fully mutable) instead of `get; init;`. The Result pattern implies immutability — once created, a result should not be modified.

**Fix:** Change all `get; set;` to `get; init;` on Result properties.

### T-C1. Core Business Logic Untested

**Untested files:**

- `Ui.Client/Services/AuthenticationService.cs`
- `Ui/Services/AuthenticationService.cs`
- `Ui/Services/WebSiteHostingService.cs`
- `Ui.Client/Services/WebSiteHostingService.cs`
- `Ui/Services/WebSitesConfigurationService.cs`
- `Ui/Services/FrameworkManagementService.cs`
- `Ui/Services/DotnetVersionService.cs`
- `Tools/Runtime/DownloaderService.cs`

The core business logic has **zero** test coverage.

### T-C2. Fragile Reflection-Based Test

**File:** `AuthenticationNavigationGuardTests.cs:33-58`

Uses reflection to construct a sealed `NavigationContext` — will break on any Blazor update. The test may silently pass without actually testing anything when `ctor` is null.

**Fix:** Add Bunit dependency; use `ctx.JSInvokeInterceptors` pattern instead of reflection.

### T-C3. Brittle LicenseService Assertion

**File:** `LicenseServiceTests.cs:72`

`Assert.Equal(4, handler.RequestCount)` hardcodes the license count. Adding/removing a license breaks the test silently.

**Fix:** Assert `handler.RequestCount == expectedLicenses.Count` or verify the second call doesn't increment the counter.

---

## HIGH PRIORITY FINDINGS

### Services

| # | File | Line | Issue |
|---|------|------|-------|
| S3 | `Ui/Services/SiteLifecycleManager.cs` | 36-49 | 5-parameter traditional constructor — should use primary constructor |
| S4 | `Tools/Infrastructure/PlatformInfoService.cs` | 28-32 | Single `ILogger` parameter — should use primary constructor |
| S5 | `Ui/Services/DsmSession.cs` | 195 | `FetchUserPreferencesFailed(ex.Message)` — loses stack trace. Change to `Exception ex` |
| S5 | `Tools/Infrastructure/DsmSettingsService.cs` | 52 | Same: `SettingsReadFailed(ex.Message)` |
| S5 | `Ui/Services/FrameworkManagementService.cs` | 81,86 | Same: `UninstallFailed(ex.Message)` |
| S6 | `Ui.Client/Services/CultureManager.cs` | 294 | `static` method takes `ILogger` as parameter — should be instance method |
| S7 | `Ui.Client/Services/DotnetVersionService.cs` | 53 | `CancellationToken.None` hardcoded — cancellation can never propagate |

### UI/Blazor

| # | File | Line | Issue |
|---|------|------|-------|
| UI-H1 | 6 component files | — | Duplicated `IWorkingState` boilerplate (~42 lines). Extract `WorkingStateBase : ComponentBase, IWorkingState` |
| UI-H2 | `WebSiteConfigurationDialog.razor` | 141,206 | Magic string `"80%"` — should use `DialogConstants` |
| UI-H3 | `LoadingOverlay.razor` | 1 | Implicit render coupling — relies on parent `StateHasChanged()` cascade |
| UI-H4 | `AutoDataGrid.razor` | 133-153 | Double-click emulation fires single-click before detecting double-click |
| UI-H5 | `Login.razor` | 22 | `FluentDialog` without `Open` parameter — focus-trap accessibility issue |

### Data Layer

| # | File | Issue |
|---|------|-------|
| D2 | `WebSiteInstance.cs` | Mutable DTO — `Configuration`, `IsRunning`, `Process`, `RequiredFramework` use `get; set;` |
| D3 | `LoginCredentials.cs` | `Password` mutable — security risk. Should be `get; init;` |
| D4 | `LastReleaseUninstallException.cs` | Generic constructors set `Version`/`Channel` to `String.Empty` — defeats purpose |

### Tests

| # | File | Issue |
|---|------|-------|
| T-H1 | `DsmSessionTests.cs:32`, `DsmApiClientTests.cs:26` | Shared `HttpClient` not disposed between tests — socket leak risk |
| T-H2 | `FileSystemServiceTests.cs:264-313` | `FakeDsmSession` doesn't validate parameters — tests pass with wrong API calls |
| T-H3 | `SiteLifecycleManagerTests.cs:39-43` | Temp directory in constructor can leak on construction failure |
| T-H4 | `ReverseProxyManagerServiceTests.cs` | No mock verification for List calls — doesn't verify correct parameters |

---

## MEDIUM PRIORITY FINDINGS

### Compliance (Systematic Issues)

| # | File | Issue |
|---|------|-------|
| CO-1 | `Data/.../FileStationListParameters.cs:13` | Hardcoded `"list"` — use `ApiConstants.MethodList` |
| CO-1 | `Data/.../CoreUserGetParameters.cs:17` | Hardcoded `"get"` — use `ApiConstants.MethodGet` |
| CO-1 | `Data/.../AuthLoginParameters.cs:13` | Hardcoded `"login"` — add `MethodLogin` to `ApiConstants` |
| CO-1 | `Data/.../FileStationListShareParameters.cs:13` | Hardcoded `"list_share"` — add `MethodListShare` to `ApiConstants` |
| CO-1 | `Data/.../CoreAclSetParameters.cs:13` | Hardcoded `"set"` — add `MethodSet` to `ApiConstants` |
| CO-1 | `Data/.../InformationsQueryParameters.cs:13` | Hardcoded `"query"` — add `MethodQuery` to `ApiConstants` |
| CO-2 | `Ui/Infrastructure/GlobalizationSettings.cs` | Not using primary constructor |
| CO-3 | `Ui.Client/Program.cs:16` | `"appsettings.json"` hardcoded — use `ApplicationConstants.SettingsFileName` |

### Services

| # | File | Issue |
|---|------|-------|
| S8 | `AuthenticationService.cs:67`, `DotnetVersionService.cs:58`, `FileSystemService.cs:36` | `NotImplementedException` in production — will crash at runtime if called |
| S9 | `WebSiteHostingService.cs:110,167` | Broad `catch (Exception ex)` — catches `OutOfMemoryException`, `StackOverflowException`, etc. |
| S10 | `LogDownloadService.cs:13,81` | Missing `CancellationToken` on file I/O operations |
| S11 | `VersionsDetectorService.cs:73` | `_cachedFrameworks` not thread-safe for concurrent reads before initialization |
| S12 | `ReverseProxyManagerService.cs:194-218` | `IsNotFoundError` overloaded with different semantics — string version is fragile |
| S13 | `FileSystemService.cs:103-143` | Large ACL object initialization (~40 lines) — extract to factory method |
| S14 | `DsmSession.cs:28-29` | `_sessionValid` and `_lastSessionValidation` not thread-safe |
| S15 | `WebSitesConfigurationService.cs:24` | `_initialized` not volatile |

### UI/Blazor

| # | File | Issue |
|---|------|-------|
| UI-M1 | `RealTimeTextField.razor`, `RealTimeNumberField.razor` | Missing `[EditorRequired]` on `ValueChanged` |
| UI-M2 | `Home.razor:25-57` | 10+ event handlers create new delegates on every render — cache as fields |
| UI-M3 | `FileSelectionDialog.razor:288-313`, `DotnetVersionsDialog.razor:108-118` | `GetFileIcon`/`GetFrameworkIcon` create new `Icon` instances every render — should be `static readonly` |
| UI-M4 | `WebSiteConfigurationDialog.razor:114` | `Enum.Parse<ProtocolType>` in setter can throw `ArgumentException` |
| UI-M5 | `DotnetVersionsDialog.razor:38` | `<strong>` inside `FluentLabel` — FluentUI compliance violation |
| UI-M6 | `aspnet-releases.css:2-3` | Hardcoded pixel dimensions — not responsive |
| UI-M7 | `AspNetReleasesDialog.razor:203` | `StateHasChanged()` in `finally` — should use `InvokeAsync<StateHasChanged>()` for correctness |

### Data Layer

| # | File | Issue |
|---|------|-------|
| D5 | `WebSiteConfiguration.cs:14-53` | All properties mutable — should use `get; init;` |
| D6 | `AspNetCoreReleaseInfo.cs:30-72` | Uses `[SetsRequiredMembers]` instead of primary constructor |
| D7 | `ReverseProxy.cs:27-38` | Magic numbers `60` and `1` — should reference `ReverseProxyConstants` |
| D8 | `FileStationFile.cs:14` | `Type` is `string` for a closed set (`"file"`/`"dir"`) — should be an enum |
| D9 | `Data/Results/` | Inconsistent factory methods across 7 Result subclasses — add to abstract base |
| D10 | `ApiResponseBase.cs:17-24` | Mutable response properties — should use `get; init;` |
| D11 | `ReverseProxyFrontend.cs`, `ReverseProxyBackend.cs`, `ReverseProxyHttps.cs` | Redundant primary constructor parameters repeated as `get; init;` properties |

### Tests

| # | File | Issue |
|---|------|-------|
| T-M1 | `LogDownloadServiceTests.cs` | Integration-style tests — read actual log files, environment-dependent |
| T-M2 | `CultureManagerTests.cs:252-275` | Static state tests are order-dependent — flaky under parallel execution |
| T-M3 | `OperationTimerTests.cs:53-60` | Time-based assertion only checks `>= 0` — always true, doesn't verify elapsed time |
| T-M4 | `SemaphoreLockTests.cs:119-160` | Concurrency test uses `Task.Delay(10)` — can fail on slow CI |
| T-M5 | Multiple test files | Missing `[Trait]` categorization for file-system-dependent tests |
| T-M6 | `DsmSettingsServiceTests.cs:37-68` | Tests parsing logic inline, not actual service methods |
| T-M7 | `BlankLineAnalyzerTests.cs:142-143` | Extra blank lines — violates project's own rules |
| T-M8 | `AuthenticationNavigationGuardTests.cs` | Only 2 tests — critical async navigation logic untested |

---

## LOW PRIORITY FINDINGS

### UI/Blazor

| # | File | Issue |
|---|------|-------|
| UI-L1 | `AspNetReleasesDialog.razor:97` | Unused `CanInstallSelected` field |
| UI-L2 | `Home.razor:131` | `EditWebSite` dead code — ignores `instance` parameter |
| UI-L3 | `WebSiteConfigurationDialog.razor:157` | `OnEnterKeyAsync` bypasses form validation |
| UI-L4 | `Home.razor:35-40`, `AutoDataGrid.razor:51` | Missing `aria-label` on icon-only buttons |
| UI-L5 | `Routes.razor:6` | `FocusOnNavigate` selector `"h1"` — `Home.razor` has no `<h1>` |
| UI-L6 | `tree-navigation.js:1-48` | Global scope pollution — use `window.ADWH` namespace |
| UI-L7 | `LicensesDialog.razor:23` | `FluentTabs` without explicit `SelectedIndex` binding |
| UI-L8 | `App.razor:69` | `_resolvedCulture ??=` lazy initialization — not strictly thread-safe |

### Data Layer

| # | File | Issue |
|---|------|-------|
| D-L1 | `WebSiteInstance.cs:46` | `"Running"`/`"Stopped"` magic strings — move to constants or enum |
| D-L2 | `ReverseProxyNotFoundException.cs:7` | Not sealed |
| D-L3 | `FileStationApiException.cs:28-29` | `FormattedMessage` doesn't handle null `Message` |

### Tests

| # | File | Issue |
|---|------|-------|
| T-L1 | `WebSiteConfigurationTests.cs:8-14` | Magic numbers — should reference production constants |
| T-L2 | `ResultTypesTests.cs:173-206` | Uses `dynamic` for assertions — bypasses compile-time checking |
| T-L3 | `DsmLanguageToCultureConverterTests.cs:156-161` | Tests a constant value — adds no value |
| T-L4 | Multiple test files | Inconsistent naming conventions |
| T-L5 | `ExtensionMethodsTests.cs:269-288` | `MockHttpMessageHandler` only supports single response |

### Compliance

| # | Issue |
|---|-------|
| CO-L1 | Duplicate constant name `ConfigurationFileName` in `Application` and `DSM.System` namespaces |
| CO-L2 | Duplicate error code value `-4` in `DsmConstants` and `ReverseProxyConstants` |
| CO-L3 | `DotnetVersionService.cs:85` — `channels.ToList()` could use `[.. channels]` |
| CO-L4 | `SiteLifecycleManager.cs:63,85,105` — `new TaskCompletionSource<ApiResult>()` should use `new()` |
| CO-L5 | `InstallationResult.cs:29` — `"Installation completed successfully."` hardcoded — should be a constant |

---

## POSITIVE PATTERNS (Keep Doing)

1. **LoggerMessage migration complete** — zero direct `ILogger` calls in production code
2. **Custom Roslyn analyzers** enforce project standards at compile time (ADWH01001-03001)
3. **Result pattern** consistently applied across the codebase
4. **Primary constructors** used correctly in most services
5. **Channel-based command queue** in `SiteLifecycleManager` eliminates TOCTOU races
6. **Smart caching** in `VersionsDetectorService` with lazy initialization and `SemaphoreLock`
7. **Cross-platform process termination** with SIGTERM/CloseMainWindow via P/Invoke
8. **Zip slip protection** tests with actual malicious archive creation
9. **Bi-directional resource verification** catches missing translations and orphaned entries
10. **Clean layered architecture** with clear separation of concerns across 8 projects
11. **Centralized constants** — magic strings/numbers extracted to dedicated project
12. **Source-generated logging** — zero-allocation, compile-time validated messages
13. **Immutable C# records** for DSM API models with `init` setters
14. **Session validation** with 1-minute TTL cache against DSM server

---

## RECOMMENDED ACTION PLAN

| Priority | Action | Estimated Effort |
|----------|--------|-----------------|
| **P0** | Fix `AuthenticationService` HttpClient per-call creation | 15 min |
| **P0** | Add `CancellationToken` to all 6 controller actions | 1 hour |
| **P0** | Write tests for `WebSiteHostingService`, `AuthenticationService` | 2-3 days |
| **P1** | Fix JS interop error handling in `FileSelectionDialog` | 30 min |
| **P1** | Remove inline styles, use CSS classes | 1 hour |
| **P1** | Make Result types immutable (`get; init;`) | 1 hour |
| **P1** | Fix logging extensions to accept `Exception` instead of `string` | 1 hour |
| **P2** | Extract `WorkingStateBase` to eliminate duplication across 6 components | 1 hour |
| **P2** | Add missing API method constants to `ApiConstants` | 30 min |
| **P2** | Convert `SiteLifecycleManager`, `PlatformInfoService`, `GlobalizationSettings` to primary constructors | 1 hour |
| **P2** | Fix `AutoDataGrid` double-click emulation | 1 hour |
| **P2** | Write tests for `FrameworkManagementService`, `DotnetVersionService` | 1-2 days |
| **P3** | Add `[EditorRequired]` to component parameters | 30 min |
| **P3** | Cache event handlers as delegates | 1 hour |
| **P3** | Add `[Trait]` categorization to file-system-dependent tests | 30 min |
| **P3** | Fix `LicenseServiceTests` brittle assertion | 15 min |
| **P3** | Add Bunit for `AuthenticationNavigationGuard` async tests | 2 hours |
