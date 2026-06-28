# Codebase Review Remediation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix 94 codebase review findings across 4 priority tiers in a single PR with ordered commits.

**Architecture:** Priority-first execution (P0 → P1 → P2 → P3), thematic grouping within each tier. Each task produces independently verifiable changes. Follow existing patterns: primary constructors, `[LoggerMessage]` extensions, `get; init;` for immutability, collection expressions, target-typed `new()`.

**Tech Stack:** .NET 10 (net10.0), C# 14, Blazor Interactive Server, FluentUI, xUnit, Bunit

## Global Constraints

- **Framework:** .NET 10 (net10.0) — all code must target this
- **C# version:** C# 14 — use primary constructors, collection expressions, target-typed `new()`
- **String pattern:** `String.` for static members, `string` for types/variables/parameters
- **Logging:** `[LoggerMessage]` source-generated extensions only — no direct `ILogger` calls
- **Constants:** No magic strings/numbers — use `Askyl.Dsm.WebHosting.Constants` project
- **Nullability:** Enabled and enforced — no `!` or `?? null` without justification
- **Format:** Run `dotnet format ./src/Askyl.Dsm.WebHosting.slnx --verbosity quiet` after every change
- **Build:** Run `dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx` — zero errors, zero warnings
- **Solution path:** `./src/Askyl.Dsm.WebHosting.slnx`
- **Test framework:** xUnit with `VerifyXunit` for snapshot testing

---

## P0 — Critical (Runtime Failures & Resource Exhaustion)

### Task 1: Fix HttpClient Lifetime in AuthenticationService (S1)

**Files:**
- Modify: `src/Askyl.Dsm.WebHosting.Ui.Client/Services/AuthenticationService.cs:28,54,61`

**Interfaces:**
- Consumes: None
- Produces: Cached `_httpClient` field pattern matching sibling services

- [ ] **Step 1: Add cached HttpClient field**

Replace the three per-call `httpClientFactory.CreateClient()` calls with a single field-based HttpClient. Add this field after the primary constructor:

```csharp
private readonly HttpClient _httpClient = httpClientFactory.CreateClient(ApplicationConstants.HttpClientName);
```

- [ ] **Step 2: Replace per-call HttpClient creation**

In `LoginAsync()` (line 28), replace:
```csharp
var httpClient = httpClientFactory.CreateClient(ApplicationConstants.HttpClientName);
```
With:
```csharp
var httpClient = _httpClient;
```

In `LogoutAsync()` (line 54), replace:
```csharp
var httpClient = httpClientFactory.CreateClient(ApplicationConstants.HttpClientName);
```
With:
```csharp
var httpClient = _httpClient;
```

In `IsAuthenticatedAsync()` (line 61), replace:
```csharp
var httpClient = httpClientFactory.CreateClient(ApplicationConstants.HttpClientName);
```
With:
```csharp
var httpClient = _httpClient;
```

- [ ] **Step 3: Format and build**

Run: `dotnet format ./src/Askyl.Dsm.WebHosting.slnx --verbosity quiet && dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx`
Expected: Build succeeds with zero errors and zero warnings.

- [ ] **Step 4: Commit**

```bash
git add src/Askyl.Dsm.WebHosting.Ui.Client/Services/AuthenticationService.cs
git commit -m "fix: cache HttpClient in AuthenticationService to prevent socket exhaustion"
```

---

### Task 2: Add CancellationToken to Controllers (S2)

**Files:**
- Modify: `src/Askyl.Dsm.WebHosting.Ui/Controllers/AuthenticationController.cs`
- Modify: `src/Askyl.Dsm.WebHosting.Ui/Controllers/FileManagementController.cs`
- Modify: `src/Askyl.Dsm.WebHosting.Ui/Controllers/FrameworkManagementController.cs`
- Modify: `src/Askyl.Dsm.WebHosting.Ui/Controllers/LogDownloadController.cs`
- Modify: `src/Askyl.Dsm.WebHosting.Ui/Controllers/RuntimeManagementController.cs`
- Modify: `src/Askyl.Dsm.WebHosting.Ui/Controllers/WebsiteHostingController.cs`
- Modify: All service interfaces in `src/Askyl.Dsm.WebHosting.Data/Contracts/` (methods called from controllers)
- Modify: All service implementations in `src/Askyl.Dsm.WebHosting.Ui/Services/`

**Interfaces:**
- Consumes: Existing service interfaces (need CancellationToken added)
- Produces: Controllers that propagate CancellationToken through service layer

- [ ] **Step 1: Add CancellationToken to service interfaces**

For each service interface method called from controllers, add `CancellationToken cancellationToken = default` as the last parameter.

Files to modify in `src/Askyl.Dsm.WebHosting.Data/Contracts/`:
- `IAuthenticationService.cs` — `IsAuthenticatedAsync()`, `LoginAsync()`, `LogoutAsync()`
- `IFileManagementService.cs` — all async methods
- `IDotnetVersionService.cs` — already has CancellationToken (skip)
- `IFrameworkManagementService.cs` — all async methods
- `ILogDownloadService.cs` — all async methods
- `IWebSiteHostingService.cs` — all async methods

Example change for `IWebSiteHostingService.GetAllWebsitesAsync()`:
```csharp
Task<WebSiteInstancesResult> GetAllWebsitesAsync(CancellationToken cancellationToken = default);
```

- [ ] **Step 2: Update service implementations**

For each service implementation, add `CancellationToken` parameter and propagate to downstream calls.

Files to modify in `src/Askyl.Dsm.WebHosting.Ui/Services/`:
- `AuthenticationService.cs`
- `FileSystemService.cs`
- `FrameworkManagementService.cs`
- `LogDownloadService.cs`
- `DotnetVersionService.cs` (server-side)
- `WebSiteHostingService.cs`
- `WebSitesConfigurationService.cs`

Example change for `AuthenticationService.IsAuthenticatedAsync()`:
```csharp
public async Task<ApiResultBool> IsAuthenticatedAsync(CancellationToken cancellationToken = default)
    => await _dsmSession.IsAuthenticatedAsync(cancellationToken);
```

- [ ] **Step 3: Update controller actions**

For each controller action, add `CancellationToken cancellationToken = default` parameter and propagate:

Example change for `WebsiteHostingController.GetAllWebsitesAsync()`:
```csharp
[HttpGet(WebsiteHostingRoutes.AllRoute)]
public async Task<ActionResult<List<WebSiteInstance>>> GetAllWebsitesAsync(CancellationToken cancellationToken)
    => Ok(await hostingService.GetAllWebsitesAsync(cancellationToken));
```

Apply to all 6 controllers. Note: `HttpContext.RequestAborted` is automatically wired by ASP.NET Core when `CancellationToken` is present.

- [ ] **Step 4: Format and build**

Run: `dotnet format ./src/Askyl.Dsm.WebHosting.slnx --verbosity quiet && dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx`
Expected: Build succeeds with zero errors and zero warnings.

- [ ] **Step 5: Commit**

```bash
git add src/Askyl.Dsm.WebHosting.Ui/Controllers/ src/Askyl.Dsm.WebHosting.Data/Contracts/ src/Askyl.Dsm.WebHosting.Ui/Services/
git commit -m "fix: add CancellationToken propagation to all controllers and services

Prevents wasted server resources when client disconnects. ASP.NET Core
automatically wires HttpContext.RequestAborted for controller parameters."
```

---

### Task 3: Fix CancellationToken.None in DotnetVersionService (S7)

**Files:**
- Modify: `src/Askyl.Dsm.WebHosting.Ui.Client/Services/DotnetVersionService.cs:53`

**Interfaces:**
- Consumes: `IDotnetVersionService.GetInstalledVersionsAsync` (already accepts CancellationToken)
- Produces: `RefreshCacheAsync` that accepts and propagates CancellationToken

- [ ] **Step 1: Update RefreshCacheAsync signature and call**

Change the method signature and propagate the token:

```csharp
public async Task RefreshCacheAsync(CancellationToken cancellationToken = default)
{
    await GetInstalledVersionsAsync(cancellationToken);
}
```

Also update the interface `IDotnetVersionService.RefreshCacheAsync()` to accept `CancellationToken cancellationToken = default`.

- [ ] **Step 2: Format and build**

Run: `dotnet format ./src/Askyl.Dsm.WebHosting.slnx --verbosity quiet && dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx`
Expected: Build succeeds with zero errors and zero warnings.

- [ ] **Step 3: Commit**

```bash
git add src/Askyl.Dsm.WebHosting.Ui.Client/Services/DotnetVersionService.cs src/Askyl.Dsm.WebHosting.Data/Contracts/IDotnetVersionService.cs
git commit -m "fix: propagate CancellationToken in DotnetVersionService.RefreshCacheAsync"
```

---

### Task 4: Remove Inline Styles (UI-C1)

**Files:**
- Modify: `src/Askyl.Dsm.WebHosting.Ui.Client/Components/Pages/Login.razor:39`
- Modify: `src/Askyl.Dsm.WebHosting.Ui.Client/Components/Dialogs/WebSiteConfigurationDialog.razor:90`
- Modify: `src/Askyl.Dsm.WebHosting.Ui.Client/Components/Controls/RealTimeTextField.razor`
- Modify: `src/Askyl.Dsm.WebHosting.Ui.Client/Components/Controls/RealTimeNumberField.razor`

**Interfaces:**
- Consumes: FluentUI CSS classes
- Produces: Components without inline styles

- [ ] **Step 1: Fix Login.razor inline style**

Replace line 39:
```html
<FluentButton Type="ButtonType.Submit" Appearance="Appearance.Accent" Style="flex-grow: 1;">@localizer[LK.Common.OK]</FluentButton>
```
With:
```html
<FluentButton Type="ButtonType.Submit" Appearance="Appearance.Accent" Fill="Fill.Horizontal">@localizer[LK.Common.OK]</FluentButton>
```

- [ ] **Step 2: Fix WebSiteConfigurationDialog.razor inline style**

Find the element at line 90 with `Style="display: none;"` and replace with FluentUI visibility control or CSS class. If it's hiding a component conditionally, use `@if` instead.

- [ ] **Step 3: Remove Style parameter from RealTimeTextField and RealTimeNumberField**

Remove the `Style` parameter from both components. If consumers were using it, update those consumers to use CSS classes instead.

- [ ] **Step 4: Format and build**

Run: `dotnet format ./src/Askyl.Dsm.WebHosting.slnx --verbosity quiet && dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx`
Expected: Build succeeds with zero errors and zero warnings.

- [ ] **Step 5: Commit**

```bash
git add src/Askyl.Dsm.WebHosting.Ui.Client/Components/Pages/Login.razor src/Askyl.Dsm.WebHosting.Ui.Client/Components/Dialogs/WebSiteConfigurationDialog.razor src/Askyl.Dsm.WebHosting.Ui.Client/Components/Controls/RealTimeTextField.razor src/Askyl.Dsm.WebHosting.Ui.Client/Components/Controls/RealTimeNumberField.razor
git commit -m "fix: remove inline styles, use FluentUI theming per AGENTS.md policy"
```

---

### Task 5: Add JS Interop Error Handling (UI-C2)

**Files:**
- Modify: `src/Askyl.Dsm.WebHosting.Ui.Client/Components/Dialogs/FileSelectionDialog.razor:213`
- Modify: `src/Askyl.Dsm.WebHosting.Ui.Client/wwwroot/js/tree-navigation.js:29-30`

**Interfaces:**
- Consumes: `IJSRuntime`
- Produces: JS interop calls wrapped in try/catch

- [ ] **Step 1: Wrap JS interop in FileSelectionDialog**

At line 213, wrap `JSRuntime.InvokeVoidAsync` in a try/catch:

```csharp
try
{
    await JSRuntime.InvokeVoidAsync("adwhTreeNavigation.scrollIntoView", elementId, parentItem);
}
catch (JSException ex)
{
    logger.JsInteropFailed(ex);
}
```

Add the `JsInteropFailed` logging method to `ClientLoggingExtensions.cs` if it doesn't exist.

- [ ] **Step 2: Add null check in tree-navigation.js**

At lines 29-30, add null check for `parentItem`:

```javascript
if (parentItem && parentItem.scrollIntoView) {
    parentItem.scrollIntoView({ block: 'nearest' });
}
```

- [ ] **Step 3: Format and build**

Run: `dotnet format ./src/Askyl.Dsm.WebHosting.slnx --verbosity quiet && dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx`
Expected: Build succeeds with zero errors and zero warnings.

- [ ] **Step 4: Commit**

```bash
git add src/Askyl.Dsm.WebHosting.Ui.Client/Components/Dialogs/FileSelectionDialog.razor src/Askyl.Dsm.WebHosting.Ui.Client/wwwroot/js/tree-navigation.js
git commit -m "fix: add error handling to JS interop calls to prevent Blazor circuit faults"
```

---

### Task 6: Make Result Types Immutable (D1)

**Files:**
- Modify: `src/Askyl.Dsm.WebHosting.Data/Results/ApiResult.cs:16-29`
- Modify: `src/Askyl.Dsm.WebHosting.Data/Results/ApiResultValue.cs:19-37`
- Modify: `src/Askyl.Dsm.WebHosting.Data/Results/ApiResultItems.cs:19-38`
- Modify: `src/Askyl.Dsm.WebHosting.Data/Results/ApiResultData.cs:19-37`
- Modify: `src/Askyl.Dsm.WebHosting.Data/Results/AuthenticationResult.cs:27-39`
- Modify: `src/Askyl.Dsm.WebHosting.Data/Results/InstallationResult.cs:17-22`
- Modify: `src/Askyl.Dsm.WebHosting.Data/Results/WebSiteInstanceResult.cs:18`

**Interfaces:**
- Consumes: Existing Result types
- Produces: Immutable Result types with `get; init;` properties

- [ ] **Step 1: Change all Result properties from `get; set;` to `get; init;`**

For each file, change all property accessors:

`ApiResult.cs` — change lines 16, 23, 29:
```csharp
public bool Success { get; init; } = success;
public string? Message { get; init; } = message;
public ApiErrorCode ErrorCode { get; init; } = errorCode != default ? errorCode : (success ? ApiErrorCode.None : ApiErrorCode.Failure);
```

Apply the same `get; set;` → `get; init;` change to all properties in:
- `ApiResultValue.cs` — `Success`, `Message`, `ErrorCode`, `Value`
- `ApiResultItems.cs` — `Success`, `Message`, `ErrorCode`, `Items`
- `ApiResultData.cs` — `Success`, `Message`, `ErrorCode`, `Data`
- `AuthenticationResult.cs` — `Success`, `Message`, `ErrorCode`, `IsAuthenticated`, `Culture`, `DateFormat`, `TimeFormat`
- `InstallationResult.cs` — `Success`, `Message`, `ErrorCode`, `Version`
- `WebSiteInstanceResult.cs` — `Success`, `Message`, `ErrorCode`, `Instance`

- [ ] **Step 2: Verify test compatibility**

Check that existing tests don't mutate Result properties after creation. If any test uses `result.Success = true`, change to use the factory method instead.

- [ ] **Step 3: Format and build**

Run: `dotnet format ./src/Askyl.Dsm.WebHosting.slnx --verbosity quiet && dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx`
Expected: Build succeeds with zero errors and zero warnings.

- [ ] **Step 4: Commit**

```bash
git add src/Askyl.Dsm.WebHosting.Data/Results/
git commit -m "fix: make Result properties immutable (get; init;) per Result pattern

Result types represent operation outcomes and should not be modified
after creation. Enables correct test assertions in subsequent tasks."
```

---

## P1 — High Priority (Correctness & Reliability)

### Task 7: Fix Logging Extensions to Accept Exception (S5)

**Files:**
- Modify: `src/Askyl.Dsm.WebHosting.Logging/Server/DsmApi/DsmSessionLoggingExtensions.cs:40-41`
- Modify: `src/Askyl.Dsm.WebHosting.Logging/Server/Infrastructure/DsmSettingsServiceLoggingExtensions.cs`
- Modify: `src/Askyl.Dsm.WebHosting.Logging/Server/Framework/FrameworkManagementLoggingExtensions.cs`
- Modify: `src/Askyl.Dsm.WebHosting.Ui/Services/DsmSession.cs:195`
- Modify: `src/Askyl.Dsm.WebHosting.Tools/Infrastructure/DsmSettingsService.cs:52`
- Modify: `src/Askyl.Dsm.WebHosting.Ui.Client/Services/FrameworkManagementService.cs:81,86`

**Interfaces:**
- Consumes: Existing `[LoggerMessage]` methods
- Produces: New overloads that accept `Exception ex` with `{Exception}` template

- [ ] **Step 1: Update DsmSessionLoggingExtensions.FetchUserPreferencesFailed**

Add an overload that accepts `Exception`:

```csharp
/// <summary>
/// Logs that fetching user preferences failed with an exception.
/// </summary>
[LoggerMessage(EventId = 2900005, Level = LogLevel.Debug, Message = "Failed to fetch user preferences")]
public static partial void FetchUserPreferencesFailed(this ILogger<ILogDsmSession> logger, Exception exception);
```

Remove the `string error` overload (or keep it for non-exception cases).

- [ ] **Step 2: Update DsmSettingsServiceLoggingExtensions.SettingsReadFailed**

Add `Exception` overload:

```csharp
[LoggerMessage(EventId = 2800002, Level = LogLevel.Warning, Message = "Failed to read settings")]
public static partial void SettingsReadFailed(this ILogger<ILogDsmSettingsService> logger, Exception exception);
```

- [ ] **Step 3: Update FrameworkManagementLoggingExtensions.UninstallFailed**

Add `Exception` overload:

```csharp
[LoggerMessage(EventId = 2600004, Level = LogLevel.Error, Message = "Failed to uninstall framework")]
public static partial void UninstallFailed(this ILogger<ILogFrameworkManagement> logger, Exception exception);
```

- [ ] **Step 4: Update call sites**

Change each call site from passing `ex.Message` to passing `ex`:

`DsmSession.cs:195`: `logger.FetchUserPreferencesFailed(ex);`
`DsmSettingsService.cs:52`: `logger.SettingsReadFailed(ex);`
`FrameworkManagementService.cs:81,86`: `logger.UninstallFailed(ex);`

- [ ] **Step 5: Format and build**

Run: `dotnet format ./src/Askyl.Dsm.WebHosting.slnx --verbosity quiet && dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx`
Expected: Build succeeds with zero errors and zero warnings.

- [ ] **Step 6: Commit**

```bash
git add src/Askyl.Dsm.WebHosting.Logging/ src/Askyl.Dsm.WebHosting.Ui/Services/DsmSession.cs src/Askyl.Dsm.WebHosting.Tools/Infrastructure/DsmSettingsService.cs src/Askyl.Dsm.WebHosting.Ui.Client/Services/FrameworkManagementService.cs
git commit -m "fix: logging extensions accept Exception instead of ex.Message

Preserves stack trace in structured logs. Uses {Exception} message
template parameter for automatic exception serialization."
```

---

### Task 8: Fix CultureManager Static Method with ILogger (S6)

**Files:**
- Modify: `src/Askyl.Dsm.WebHosting.Ui.Client/Services/CultureManager.cs:294`

**Interfaces:**
- Consumes: `ILogger<ICultureManager>`
- Produces: Instance method with injected logger

- [ ] **Step 1: Convert static method to instance method**

Identify the static method at line 294 that takes `ILogger` as a parameter. Convert it to an instance method and inject `ILogger<ICultureManager>` through the constructor.

- [ ] **Step 2: Update callers**

Replace static calls (`CultureManager.MethodName(logger)`) with instance calls (`cultureManager.MethodName()`).

- [ ] **Step 3: Format and build**

Run: `dotnet format ./src/Askyl.Dsm.WebHosting.slnx --verbosity quiet && dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx`
Expected: Build succeeds with zero errors and zero warnings.

- [ ] **Step 4: Commit**

```bash
git add src/Askyl.Dsm.WebHosting.Ui.Client/Services/CultureManager.cs
git commit -m "fix: convert CultureManager static method with ILogger to instance method"
```

---

### Task 9: Make WebSiteInstance and LoginCredentials Immutable (D2, D3)

**Files:**
- Modify: `src/Askyl.Dsm.WebHosting.Data/Domain/WebSites/WebSiteInstance.cs`
- Modify: `src/Askyl.Dsm.WebHosting.Data/Domain/Authentication/LoginCredentials.cs`

**Interfaces:**
- Consumes: Existing domain models
- Produces: Immutable domain models

- [ ] **Step 1: Make WebSiteInstance properties immutable**

Change `Configuration`, `IsRunning`, `Process`, `RequiredFramework` from `get; set;` to `get; init;`.

If `IsRunning` or `Process` need to be mutable at runtime (e.g., updated by lifecycle manager), keep those as `get; set;` but document why. Only make immutable what the Result pattern justifies.

- [ ] **Step 2: Make LoginCredentials.Password immutable**

Change `Password` from `get; set;` to `get; init;`.

- [ ] **Step 3: Format and build**

Run: `dotnet format ./src/Askyl.Dsm.WebHosting.slnx --verbosity quiet && dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx`
Expected: Build succeeds with zero errors and zero warnings.

- [ ] **Step 4: Commit**

```bash
git add src/Askyl.Dsm.WebHosting.Data/Domain/WebSites/WebSiteInstance.cs src/Askyl.Dsm.WebHosting.Data/Domain/Authentication/LoginCredentials.cs
git commit -m "fix: make domain model properties immutable (get; init;)

Prevents accidental mutation of sensitive data (Password) and ensures
domain objects maintain consistent state after construction."
```

---

### Task 10: Fix LicenseServiceTests Brittle Assertion (T-C3)

**Files:**
- Modify: `src/Askyl.Dsm.WebHosting.Tests/Ui/Services/LicenseServiceTests.cs:72`

**Interfaces:**
- Consumes: Existing test data
- Produces: Assertion that references expected data count

- [ ] **Step 1: Replace hardcoded assertion**

Change line 72 from:
```csharp
Assert.Equal(4, handler.RequestCount);
```
To:
```csharp
Assert.Equal(expectedLicenses.Count, handler.RequestCount);
```

Where `expectedLicenses` is the collection of licenses used in the test setup. If the variable name differs, reference the actual collection.

- [ ] **Step 2: Run tests**

Run: `dotnet test ./src/Askyl.Dsm.WebHosting.Tests/Askyl.Dsm.WebHosting.Tests.csproj --filter "FullyQualifiedName~LicenseServiceTests" --verbosity normal`
Expected: All tests pass.

- [ ] **Step 3: Commit**

```bash
git add src/Askyl.Dsm.WebHosting.Tests/Ui/Services/LicenseServiceTests.cs
git commit -m "fix: replace hardcoded license count assertion with data-driven check"
```

---

### Task 11: Write Core Service Tests (T-C1)

**Files:**
- Create: `src/Askyl.Dsm.WebHosting.Tests/Ui/Services/WebSiteHostingServiceTests.cs`
- Create: `src/Askyl.Dsm.WebHosting.Tests/Ui/Services/AuthenticationServiceTests.cs`
- Create: `src/Askyl.Dsm.WebHosting.Tests/Ui/Services/FrameworkManagementServiceTests.cs`
- Create: `src/Askyl.Dsm.WebHosting.Tests/Ui/Services/DotnetVersionServiceTests.cs`
- Create: `src/Askyl.Dsm.WebHosting.Tests/Ui/Services/WebSitesConfigurationServiceTests.cs`

**Interfaces:**
- Consumes: Service interfaces, existing Fake/Stub patterns from codebase
- Produces: Unit tests with mocked dependencies

- [ ] **Step 1: Study existing test patterns**

Read `src/Askyl.Dsm.WebHosting.Tests/Ui/Services/DsmSessionTests.cs` and `src/Askyl.Dsm.WebHosting.Tests/Ui/Services/FileSystemServiceTests.cs` to understand mocking patterns, `FakeDsmSession` usage, and test structure.

- [ ] **Step 2: Write AuthenticationServiceTests**

Create tests for:
- `IsAuthenticatedAsync()` — returns authenticated/not authenticated
- `LoginAsync()` — success, failure, rate limited scenarios
- `LogoutAsync()` — success/failure

Use `FakeDsmSession` pattern from existing tests. Mock the DSM API responses.

- [ ] **Step 3: Write WebSiteHostingServiceTests**

Create tests for:
- `GetAllWebsitesAsync()` — empty, single, multiple websites
- `AddWebsiteAsync()` — success, duplicate ID
- `RemoveWebsiteAsync()` — success, not found
- `StartWebsiteAsync()` / `StopWebsiteAsync()` — success, already running/stopped

- [ ] **Step 4: Write FrameworkManagementServiceTests**

Create tests for:
- Install/uninstall operations with mocked file system
- Version validation

- [ ] **Step 5: Write DotnetVersionServiceTests**

Create tests for:
- `GetInstalledVersionsAsync()` — success, failure
- `RefreshCacheAsync()` — token propagation

- [ ] **Step 6: Write WebSitesConfigurationServiceTests**

Create tests for:
- Configuration load/save operations
- Initialization flow

- [ ] **Step 7: Run all new tests**

Run: `dotnet test ./src/Askyl.Dsm.WebHosting.Tests/Askyl.Dsm.WebHosting.Tests.csproj --filter "FullyQualifiedName~WebSiteHostingServiceTests|FullyQualifiedName~AuthenticationServiceTests|FullyQualifiedName~FrameworkManagementServiceTests|FullyQualifiedName~DotnetVersionServiceTests|FullyQualifiedName~WebSitesConfigurationServiceTests" --verbosity normal`
Expected: All tests pass.

- [ ] **Step 8: Commit**

```bash
git add src/Askyl.Dsm.WebHosting.Tests/Ui/Services/
git commit -m "test: add unit tests for core business logic services

Covers AuthenticationService, WebSiteHostingService, FrameworkManagementService,
DotnetVersionService, and WebSitesConfigurationService with mocked dependencies."
```

---

### Task 12: Replace Reflection-Based Test with Bunit (T-C2)

**Files:**
- Modify: `src/Askyl.Dsm.WebHosting.Tests/Ui/Services/AuthenticationNavigationGuardTests.cs`
- Modify: `src/Askyl.Dsm.WebHosting.Tests/Askyl.Dsm.WebHosting.Tests.csproj` (add Bunit package)

**Interfaces:**
- Consumes: Bunit test infrastructure
- Produces: Tests that use `JSInvokeInterceptors` instead of reflection

- [ ] **Step 1: Add Bunit NuGet package**

Add to the test project's `<ItemGroup>`:
```xml
<PackageReference Include="bunit" Version="1.*" />
```

Use the latest stable 1.x version.

- [ ] **Step 2: Rewrite AuthenticationNavigationGuardTests**

Replace reflection-based `NavigationContext` construction with Bunit's `BunitContext`:

```csharp
using var ctx = new Bunit.TestContext();
ctx.JSInterop.Mode = JSRuntimeMode.Strict;

// Set up JS interop interceptors
ctx.JSInterop.SetupVoid("authenticationGuard.someMethod", _ => true);

// Render the component or test the guard directly
var guard = ctx.Services.GetRequiredService<AuthenticationNavigationGuard>();
```

- [ ] **Step 3: Run tests**

Run: `dotnet test ./src/Askyl.Dsm.WebHosting.Tests/Askyl.Dsm.WebHosting.Tests.csproj --filter "FullyQualifiedName~AuthenticationNavigationGuardTests" --verbosity normal`
Expected: All tests pass.

- [ ] **Step 4: Commit**

```bash
git add src/Askyl.Dsm.WebHosting.Tests/Ui/Services/AuthenticationNavigationGuardTests.cs src/Askyl.Dsm.WebHosting.Tests/Askyl.Dsm.WebHosting.Tests.csproj
git commit -m "test: replace reflection-based navigation guard test with Bunit

Uses JSInvokeInterceptors for reliable JS interop testing. Eliminates
fragile reflection that breaks on Blazor updates."
```

---

## P2 — Medium Priority (Maintenance & Compliance)

### Task 13: Convert to Primary Constructors (S3, CO-2)

**Files:**
- Modify: `src/Askyl.Dsm.WebHosting.Ui/Services/SiteLifecycleManager.cs:36-49`
- Modify: `src/Askyl.Dsm.WebHosting.Tools/Infrastructure/PlatformInfoService.cs:28-32`
- Modify: `src/Askyl.Dsm.WebHosting.Ui/Infrastructure/GlobalizationSettings.cs`

**Interfaces:**
- Consumes: Existing constructor patterns
- Produces: Primary constructors per AGENTS.md §6.2

- [ ] **Step 1: Convert SiteLifecycleManager**

Replace traditional constructor with primary constructor. Move constructor parameters to the class declaration. Remove explicit constructor body and use field initialization where needed.

- [ ] **Step 2: Convert PlatformInfoService**

Replace single-parameter traditional constructor with primary constructor.

- [ ] **Step 3: Convert GlobalizationSettings**

Replace traditional constructor with primary constructor.

- [ ] **Step 4: Format and build**

Run: `dotnet format ./src/Askyl.Dsm.WebHosting.slnx --verbosity quiet && dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx`
Expected: Build succeeds with zero errors and zero warnings.

- [ ] **Step 5: Commit**

```bash
git add src/Askyl.Dsm.WebHosting.Ui/Services/SiteLifecycleManager.cs src/Askyl.Dsm.WebHosting.Tools/Infrastructure/PlatformInfoService.cs src/Askyl.Dsm.WebHosting.Ui/Infrastructure/GlobalizationSettings.cs
git commit -m "refactor: convert traditional constructors to primary constructors

SiteLifecycleManager, PlatformInfoService, GlobalizationSettings now use
C# 14 primary constructors per project standards."
```

---

### Task 14: Replace NotImplementedException (S8)

**Files:**
- Modify: `src/Askyl.Dsm.WebHosting.Ui.Client/Services/AuthenticationService.cs:67`
- Modify: `src/Askyl.Dsm.WebHosting.Ui.Client/Services/DotnetVersionService.cs:58`
- Modify: `src/Askyl.Dsm.WebHosting.Ui.Client/Services/FileSystemService.cs:36`

**Interfaces:**
- Consumes: Service interfaces with unimplemented methods
- Produces: Implemented methods or documented stubs

- [ ] **Step 1: Implement or guard AuthenticationService.IsSessionValidAsync**

At line 67, replace `throw new NotImplementedException()` with either:
- A proper implementation using existing authentication logic, OR
- A `throw new NotSupportedException("Session validation is handled server-side")` with XML doc explaining why

- [ ] **Step 2: Implement or guard DotnetVersionService.IsValidVersionFormat**

At line 58, replace `throw new NotImplementedException()` with either:
- A regex-based version format validation, OR
- A `NotSupportedException` with explanation

- [ ] **Step 3: Implement or guard FileSystemService method**

At line 36, replace `throw new NotImplementedException()` with implementation or `NotSupportedException`.

- [ ] **Step 4: Format and build**

Run: `dotnet format ./src/Askyl.Dsm.WebHosting.slnx --verbosity quiet && dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx`
Expected: Build succeeds with zero errors and zero warnings.

- [ ] **Step 5: Commit**

```bash
git add src/Askyl.Dsm.WebHosting.Ui.Client/Services/AuthenticationService.cs src/Askyl.Dsm.WebHosting.Ui.Client/Services/DotnetVersionService.cs src/Askyl.Dsm.WebHosting.Ui.Client/Services/FileSystemService.cs
git commit -m "fix: replace NotImplementedException with proper implementations or NotSupportedException

Prevents runtime crashes from unimplemented stub methods."
```

---

### Task 15: Narrow Broad Exception Catches (S9)

**Files:**
- Modify: `src/Askyl.Dsm.WebHosting.Ui/Services/WebSiteHostingService.cs:110,167`

**Interfaces:**
- Consumes: Existing catch blocks
- Produces: Narrowed exception handling

- [ ] **Step 1: Narrow catch blocks**

At lines 110 and 167, replace `catch (Exception ex)` with specific exception types that the operation can actually throw (e.g., `IOException`, `OperationCanceledException`, custom domain exceptions).

Keep a final `catch (Exception ex)` only if the service contract requires returning a result (not rethrowing), and add a comment explaining why.

- [ ] **Step 2: Format and build**

Run: `dotnet format ./src/Askyl.Dsm.WebHosting.slnx --verbosity quiet && dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx`
Expected: Build succeeds with zero errors and zero warnings.

- [ ] **Step 3: Commit**

```bash
git add src/Askyl.Dsm.WebHosting.Ui/Services/WebSiteHostingService.cs
git commit -m "fix: narrow broad catch(Exception) to specific exception types

Prevents catching OutOfMemoryException, StackOverflowException, etc."
```

---

### Task 16: Fix Thread-Safety Issues (S11, S14, S15)

**Files:**
- Modify: `src/Askyl.Dsm.WebHosting.Tools/Runtime/VersionsDetectorService.cs:73`
- Modify: `src/Askyl.Dsm.WebHosting.Ui/Services/DsmSession.cs:28-29`
- Modify: `src/Askyl.Dsm.WebHosting.Ui/Services/WebSitesConfigurationService.cs:24`

**Interfaces:**
- Consumes: Existing fields
- Produces: Thread-safe fields

- [ ] **Step 1: Fix VersionsDetectorService._cachedFrameworks**

Ensure `_cachedFrameworks` is initialized with a thread-safe pattern. If it's lazily initialized, use `Lazy<T>` or `volatile` + double-check locking.

- [ ] **Step 2: Fix DsmSession thread-safety**

Make `_sessionValid` and `_lastSessionValidation` thread-safe:
```csharp
volatile bool _sessionValid;
DateTime _lastSessionValidation;
```

Or use `Interlocked` for the boolean and a lock for the DateTime.

- [ ] **Step 3: Fix WebSitesConfigurationService._initialized**

Make `_initialized` volatile:
```csharp
volatile bool _initialized;
```

- [ ] **Step 4: Format and build**

Run: `dotnet format ./src/Askyl.Dsm.WebHosting.slnx --verbosity quiet && dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx`
Expected: Build succeeds with zero errors and zero warnings.

- [ ] **Step 5: Commit**

```bash
git add src/Askyl.Dsm.WebHosting.Tools/Runtime/VersionsDetectorService.cs src/Askyl.Dsm.WebHosting.Ui/Services/DsmSession.cs src/Askyl.Dsm.WebHosting.Ui/Services/WebSitesConfigurationService.cs
git commit -m "fix: add thread-safety to shared state fields

Uses volatile and Lazy<T> to prevent race conditions in concurrent scenarios."
```

---

### Task 17: Extract WorkingStateBase (UI-H1)

**Files:**
- Create: `src/Askyl.Dsm.WebHosting.Ui.Client/Components/Patterns/WorkingState/WorkingStateBase.cs`
- Modify: `src/Askyl.Dsm.WebHosting.Ui.Client/Components/Pages/Login.razor`
- Modify: `src/Askyl.Dsm.WebHosting.Ui.Client/Components/Pages/Home.razor`
- Modify: `src/Askyl.Dsm.WebHosting.Ui.Client/Components/Dialogs/FileSelectionDialog.razor`
- Modify: `src/Askyl.Dsm.WebHosting.Ui.Client/Components/Dialogs/WebSiteConfigurationDialog.razor`
- Modify: `src/Askyl.Dsm.WebHosting.Ui.Client/Components/Dialogs/AspNetReleasesDialog.razor`
- Modify: `src/Askyl.Dsm.WebHosting.Ui.Client/Components/Dialogs/LicensesDialog.razor`

**Interfaces:**
- Consumes: `IWorkingState` interface
- Produces: `WorkingStateBase : ComponentBase, IWorkingState` with shared implementation

- [ ] **Step 1: Create WorkingStateBase**

```csharp
namespace Askyl.Dsm.WebHosting.Ui.Client.Components.Patterns.WorkingState;

/// <summary>
/// Base component that implements IWorkingState to eliminate boilerplate duplication.
/// </summary>
public abstract class WorkingStateBase : ComponentBase, IWorkingState
{
    public bool IsWorking { get; set; }
    public string Message { get; set; } = String.Empty;
    public abstract void NotifyStateChanged();
}
```

- [ ] **Step 2: Update components to inherit WorkingStateBase**

For each of the 6 components:
1. Replace `@implements IWorkingState` with `@inherits WorkingStateBase`
2. Remove the `#region IWorkingState Implementation` block (the 3 properties/methods)

- [ ] **Step 3: Format and build**

Run: `dotnet format ./src/Askyl.Dsm.WebHosting.slnx --verbosity quiet && dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx`
Expected: Build succeeds with zero errors and zero warnings.

- [ ] **Step 4: Commit**

```bash
git add src/Askyl.Dsm.WebHosting.Ui.Client/Components/Patterns/WorkingState/WorkingStateBase.cs src/Askyl.Dsm.WebHosting.Ui.Client/Components/Pages/ src/Askyl.Dsm.WebHosting.Ui.Client/Components/Dialogs/
git commit -m "refactor: extract WorkingStateBase to eliminate IWorkingState boilerplate

Removes ~42 lines of duplicated code from 6 components. Components now
inherit WorkingStateBase instead of implementing IWorkingState directly."
```

---

### Task 18: Fix UI Medium Issues (UI-H3, UI-H4, UI-H5, UI-M1, UI-M7)

**Files:**
- Modify: `src/Askyl.Dsm.WebHosting.Ui.Client/Components/Controls/LoadingOverlay.razor`
- Modify: `src/Askyl.Dsm.WebHosting.Ui.Client/Components/Controls/AutoDataGrid.razor:133-153`
- Modify: `src/Askyl.Dsm.WebHosting.Ui.Client/Components/Pages/Login.razor:22`
- Modify: `src/Askyl.Dsm.WebHosting.Ui.Client/Components/Controls/RealTimeTextField.razor`
- Modify: `src/Askyl.Dsm.WebHosting.Ui.Client/Components/Controls/RealTimeNumberField.razor`
- Modify: `src/Askyl.Dsm.WebHosting.Ui.Client/Components/Dialogs/AspNetReleasesDialog.razor:203`

**Interfaces:**
- Consumes: Existing components
- Produces: Fixed render coupling, double-click, accessibility, editor required, async state

- [ ] **Step 1: Fix LoadingOverlay render coupling (UI-H3)**

Ensure `LoadingOverlay` explicitly receives state changes rather than relying on parent `StateHasChanged()` cascade. Use a callback or explicit parameter binding.

- [ ] **Step 2: Fix AutoDataGrid double-click emulation (UI-H4)**

At lines 133-153, fix the double-click detection to suppress the single-click when a double-click is detected. Use a timer-based approach: defer single-click handler, cancel it if double-click fires within threshold.

- [ ] **Step 3: Add Open parameter to Login FluentDialog (UI-H5)**

At line 22, add `Open="true"` to the `FluentDialog` to ensure proper focus-trap behavior.

- [ ] **Step 4: Add [EditorRequired] to ValueChanged (UI-M1)**

In `RealTimeTextField.razor` and `RealTimeNumberField.razor`, add `[EditorRequired]` to the `ValueChanged` parameter.

- [ ] **Step 5: Fix StateHasChanged in finally (UI-M7)**

In `AspNetReleasesDialog.razor:203`, replace `StateHasChanged()` with `await InvokeAsync(StateHasChanged);`.

- [ ] **Step 6: Format and build**

Run: `dotnet format ./src/Askyl.Dsm.WebHosting.slnx --verbosity quiet && dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx`
Expected: Build succeeds with zero errors and zero warnings.

- [ ] **Step 7: Commit**

```bash
git add src/Askyl.Dsm.WebHosting.Ui.Client/Components/Controls/LoadingOverlay.razor src/Askyl.Dsm.WebHosting.Ui.Client/Components/Controls/AutoDataGrid.razor src/Askyl.Dsm.WebHosting.Ui.Client/Components/Pages/Login.razor src/Askyl.Dsm.WebHosting.Ui.Client/Components/Controls/RealTimeTextField.razor src/Askyl.Dsm.WebHosting.Ui.Client/Components/Controls/RealTimeNumberField.razor src/Askyl.Dsm.WebHosting.Ui.Client/Components/Dialogs/AspNetReleasesDialog.razor
git commit -m "fix: UI medium priority fixes

- LoadingOverlay: explicit state change notification
- AutoDataGrid: suppress single-click on double-click detection
- Login: add Open parameter for accessibility
- RealTime fields: add [EditorRequired] on ValueChanged
- AspNetReleasesDialog: use InvokeAsync for thread-safe StateHasChanged"
```

---

### Task 19: Fix Data Layer Medium Issues (D4, D5, D6, D7, D10)

**Files:**
- Modify: `src/Askyl.Dsm.WebHosting.Data/Exceptions/LastReleaseUninstallException.cs`
- Modify: `src/Askyl.Dsm.WebHosting.Data/Domain/WebSites/WebSiteConfiguration.cs`
- Modify: `src/Askyl.Dsm.WebHosting.Data/Domain/Runtime/AspNetCoreReleaseInfo.cs`
- Modify: `src/Askyl.Dsm.WebHosting.Data/DsmApi/Models/ReverseProxy/ReverseProxy.cs:27-38`
- Modify: `src/Askyl.Dsm.WebHosting.Data/DsmApi/Responses/ApiResponseBase.cs`

**Interfaces:**
- Consumes: Existing data types
- Produces: Immutable, correctly structured data types

- [ ] **Step 1: Fix LastReleaseUninstallException (D4)**

Update generic constructors to require `Version` and `Channel` parameters instead of defaulting to `String.Empty`.

- [ ] **Step 2: Make WebSiteConfiguration immutable (D5)**

Change all properties from `get; set;` to `get; init;`.

- [ ] **Step 3: Convert AspNetCoreReleaseInfo to primary constructor (D6)**

Replace `[SetsRequiredMembers]` pattern with primary constructor.

- [ ] **Step 4: Replace magic numbers in ReverseProxy (D7)**

Replace `60` and `1` with references from `ReverseProxyConstants`.

- [ ] **Step 5: Make ApiResponseBase immutable (D10)**

Change properties from `get; set;` to `get; init;`.

- [ ] **Step 6: Format and build**

Run: `dotnet format ./src/Askyl.Dsm.WebHosting.slnx --verbosity quiet && dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx`
Expected: Build succeeds with zero errors and zero warnings.

- [ ] **Step 7: Commit**

```bash
git add src/Askyl.Dsm.WebHosting.Data/Exceptions/ src/Askyl.Dsm.WebHosting.Data/Domain/ src/Askyl.Dsm.WebHosting.Data/DsmApi/
git commit -m "refactor: data layer medium fixes

- LastReleaseUninstallException: require Version/Channel in constructors
- WebSiteConfiguration: immutable properties
- AspNetCoreReleaseInfo: primary constructor instead of [SetsRequiredMembers]
- ReverseProxy: magic numbers replaced with constants
- ApiResponseBase: immutable properties"
```

---

### Task 20: Fix Test Medium Issues (T-H1, T-H2, T-H3, T-M2, T-M4)

**Files:**
- Modify: `src/Askyl.Dsm.WebHosting.Tests/Ui/Services/DsmSessionTests.cs`
- Modify: `src/Askyl.Dsm.WebHosting.Tests/Tools/Network/DsmApiClientTests.cs`
- Modify: `src/Askyl.Dsm.WebHosting.Tests/Ui/Services/FileSystemServiceTests.cs:264-313`
- Modify: `src/Askyl.Dsm.WebHosting.Tests/Ui/Services/SiteLifecycleManagerTests.cs:39-43`
- Modify: `src/Askyl.Dsm.WebHosting.Tests/Globalization/CultureManagerTests.cs:252-275`
- Modify: `src/Askyl.Dsm.WebHosting.Tests/Tools/Threading/SemaphoreLockTests.cs:119-160`

**Interfaces:**
- Consumes: Existing test infrastructure
- Produces: Reliable, non-flaky tests

- [ ] **Step 1: Fix HttpClient disposal in tests (T-H1)**

In `DsmSessionTests.cs` and `DsmApiClientTests.cs`, ensure shared `HttpClient` is disposed between tests. Use `using` declaration or implement `IDisposable` on the test class.

- [ ] **Step 2: Add parameter validation to FakeDsmSession (T-H2)**

In `FileSystemServiceTests.cs`, update `FakeDsmSession` to validate API method names and parameters. Throw `ArgumentException` if unexpected values are passed.

- [ ] **Step 3: Fix temp directory leak (T-H3)**

In `SiteLifecycleManagerTests.cs`, wrap temp directory creation in a `try/finally` or use `using` pattern to ensure cleanup on construction failure.

- [ ] **Step 4: Fix order-dependent static state tests (T-M2)**

In `CultureManagerTests.cs`, isolate static state tests so they don't depend on execution order. Reset static state in `[Fact]` setup or use `[Collection]` for serialization.

- [ ] **Step 5: Fix flaky concurrency test (T-M4)**

In `SemaphoreLockTests.cs`, replace `Task.Delay(10)` with a more robust synchronization mechanism (e.g., `TaskCompletionSource` or `ManualResetEventSlim`).

- [ ] **Step 5b: Add async navigation tests (T-M8)**

In `AuthenticationNavigationGuardTests.cs`, add tests for async navigation scenarios beyond the existing 2 tests. Cover: authenticated navigation, unauthenticated redirect, pending authentication.

- [ ] **Step 5c: Add mock verification for ReverseProxy tests (T-H4)**

In `ReverseProxyManagerServiceTests.cs`, add `Mock.Verify()` calls for List operations to ensure correct API parameters are passed.

- [ ] **Step 6: Run all tests**

Run: `dotnet test ./src/Askyl.Dsm.WebHosting.Tests/Askyl.Dsm.WebHosting.Tests.csproj --verbosity normal`
Expected: All tests pass.

- [ ] **Step 7: Commit**

```bash
git add src/Askyl.Dsm.WebHosting.Tests/
git commit -m "fix: test reliability improvements

- HttpClient disposal between tests prevents socket leaks
- FakeDsmSession validates parameters for meaningful failures
- Temp directory cleanup on construction failure
- Static state tests isolated from execution order
- Concurrency test uses deterministic synchronization
- AuthenticationNavigationGuard: added async navigation tests
- ReverseProxyManagerServiceTests: added mock verification for List calls"
```

---

### Task 21: Add Missing API Constants (CO-1, CO-3, CO-L1)

**Files:**
- Modify: `src/Askyl.Dsm.WebHosting.Constants/DSM/API/ApiConstants.cs`
- Modify: `src/Askyl.Dsm.WebHosting.Data/DsmApi/Parameters/FileStationListParameters.cs:13`
- Modify: `src/Askyl.Dsm.WebHosting.Data/DsmApi/Parameters/CoreUserGetParameters.cs:17`
- Modify: `src/Askyl.Dsm.WebHosting.Data/DsmApi/Parameters/AuthLoginParameters.cs:13`
- Modify: `src/Askyl.Dsm.WebHosting.Data/DsmApi/Parameters/FileStationListShareParameters.cs:13`
- Modify: `src/Askyl.Dsm.WebHosting.Data/DsmApi/Parameters/CoreAclSetParameters.cs:13`
- Modify: `src/Askyl.Dsm.WebHosting.Data/DsmApi/Parameters/InformationsQueryParameters.cs:13`
- Modify: `src/Askyl.Dsm.WebHosting.Ui.Client/Program.cs:16`

**Interfaces:**
- Consumes: Hardcoded strings
- Produces: References to named constants

- [ ] **Step 1: Add missing method constants to ApiConstants**

Add to the `#region Methods` section:
```csharp
public const string MethodLogin = "login";
public const string MethodListShare = "list_share";
public const string MethodSet = "set";
public const string MethodQuery = "query";
```

- [ ] **Step 2: Replace hardcoded strings in parameter files**

Replace hardcoded method strings with `ApiConstants` references:
- `FileStationListParameters.cs:13` — `"list"` → `ApiConstants.MethodList`
- `CoreUserGetParameters.cs:17` — `"get"` → `ApiConstants.MethodGet`
- `AuthLoginParameters.cs:13` — `"login"` → `ApiConstants.MethodLogin`
- `FileStationListShareParameters.cs:13` — `"list_share"` → `ApiConstants.MethodListShare`
- `CoreAclSetParameters.cs:13` — `"set"` → `ApiConstants.MethodSet`
- `InformationsQueryParameters.cs:13` — `"query"` → `ApiConstants.MethodQuery`

- [ ] **Step 3: Fix Program.cs hardcoded settings file**

Replace `"appsettings.json"` with `ApplicationConstants.SettingsFileName`.

- [ ] **Step 4: Resolve duplicate ConfigurationFileName (CO-L1)**

Identify the duplicate in `Application` and `DSM.System` namespaces. Consolidate to a single constant and update references.

- [ ] **Step 5: Format and build**

Run: `dotnet format ./src/Askyl.Dsm.WebHosting.slnx --verbosity quiet && dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx`
Expected: Build succeeds with zero errors and zero warnings.

- [ ] **Step 6: Commit**

```bash
git add src/Askyl.Dsm.WebHosting.Constants/ src/Askyl.Dsm.WebHosting.Data/DsmApi/Parameters/ src/Askyl.Dsm.WebHosting.Ui.Client/Program.cs
git commit -m "refactor: replace hardcoded API method strings with constants

Adds MethodLogin, MethodListShare, MethodSet, MethodQuery to ApiConstants.
Replaces 6 hardcoded strings in parameter files. Resolves duplicate
ConfigurationFileName constant."
```

---

## P3 — Low Priority (Polish & Cleanup)

### Task 22: Fix Services P3 Issues (S10, S12, S13)

**Files:**
- Modify: `src/Askyl.Dsm.WebHosting.Ui/Services/LogDownloadService.cs`
- Modify: `src/Askyl.Dsm.WebHosting.Ui/Services/ReverseProxyManagerService.cs:194-218`
- Modify: `src/Askyl.Dsm.WebHosting.Ui.Client/Services/FileSystemService.cs:103-143`

**Interfaces:**
- Consumes: Existing service implementations
- Produces: Improved services

- [ ] **Step 1: Add CancellationToken to LogDownloadService (S10)**

Add `CancellationToken` to file I/O operations in `LogDownloadService`.

- [ ] **Step 2: Fix IsNotFoundError overloading (S12)**

In `ReverseProxyManagerService.cs`, consolidate the overloaded `IsNotFoundError` methods. Make the string version use proper JSON parsing or regex instead of fragile string matching.

- [ ] **Step 3: Extract ACL factory method (S13)**

In `FileSystemService.cs:103-143`, extract the ~40-line ACL object initialization to a private factory method.

- [ ] **Step 4: Format and build**

Run: `dotnet format ./src/Askyl.Dsm.WebHosting.slnx --verbosity quiet && dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx`
Expected: Build succeeds with zero errors and zero warnings.

- [ ] **Step 5: Commit**

```bash
git add src/Askyl.Dsm.WebHosting.Ui/Services/LogDownloadService.cs src/Askyl.Dsm.WebHosting.Ui/Services/ReverseProxyManagerService.cs src/Askyl.Dsm.WebHosting.Ui.Client/Services/FileSystemService.cs
git commit -m "refactor: P3 service improvements

- LogDownloadService: CancellationToken on file I/O
- ReverseProxyManagerService: robust IsNotFoundError
- FileSystemService: extracted ACL factory method"
```

---

### Task 23: Fix UI P3 Issues (UI-H2, UI-M2, UI-M3, UI-M4, UI-M5, UI-M6, UI-L*)

**Files:**
- Modify: `src/Askyl.Dsm.WebHosting.Ui.Client/Components/Dialogs/WebSiteConfigurationDialog.razor:141,206`
- Modify: `src/Askyl.Dsm.WebHosting.Ui.Client/Components/Pages/Home.razor:25-57`
- Modify: `src/Askyl.Dsm.WebHosting.Ui.Client/Components/Dialogs/FileSelectionDialog.razor`
- Modify: `src/Askyl.Dsm.WebHosting.Ui.Client/Components/Dialogs/DotnetVersionsDialog.razor`
- Modify: `src/Askyl.Dsm.WebHosting.Ui.Client/wwwroot/css/aspnet-releases.css:2-3`
- Modify: Various UI files for low-priority cleanup

**Interfaces:**
- Consumes: Existing components
- Produces: Polished UI components

- [ ] **Step 1: Replace magic string in WebSiteConfigurationDialog (UI-H2)**

Replace `"80%"` at lines 141 and 206 with `DialogConstants` reference.

- [ ] **Step 2: Cache event handlers in Home.razor (UI-M2)**

Cache the 10+ event handlers as private fields instead of creating new delegates on every render.

- [ ] **Step 3: Cache Icon instances (UI-M3)**

In `FileSelectionDialog.razor` and `DotnetVersionsDialog.razor`, convert `GetFileIcon`/`GetFrameworkIcon` to `static readonly` dictionary lookups.

- [ ] **Step 4: Handle Enum.Parse exception (UI-M4)**

In `WebSiteConfigurationDialog.razor:114`, wrap `Enum.Parse<ProtocolType>` in a try/catch or use `Enum.TryParse`.

- [ ] **Step 5: Remove <strong> from FluentLabel (UI-M5)**

In `DotnetVersionsDialog.razor:38`, replace `<strong>` inside `FluentLabel` with FluentUI-compliant markup.

- [ ] **Step 6: Make CSS responsive (UI-M6)**

In `aspnet-releases.css:2-3`, replace hardcoded pixel dimensions with relative units or media queries.

- [ ] **Step 7: Low-priority cleanup (UI-L1/L2/L4/L5/L6/L7/L8)**

- Remove unused `CanInstallSelected` field in `AspNetReleasesDialog.razor:97`
- Remove dead code `EditWebSite` in `Home.razor:131`
- Add `aria-label` to icon-only buttons in `Home.razor:35-40` and `AutoDataGrid.razor:51`
- Fix `FocusOnNavigate` selector in `Routes.razor:6` (target element that exists)
- Namespace JS in `tree-navigation.js` under `window.ADWH`
- Add explicit `SelectedIndex` binding to `FluentTabs` in `LicensesDialog.razor:23`

- [ ] **Step 8: Format and build**

Run: `dotnet format ./src/Askyl.Dsm.WebHosting.slnx --verbosity quiet && dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx`
Expected: Build succeeds with zero errors and zero warnings.

- [ ] **Step 9: Commit**

```bash
git add src/Askyl.Dsm.WebHosting.Ui.Client/Components/ src/Askyl.Dsm.WebHosting.Ui.Client/wwwroot/
git commit -m "refactor: P3 UI polish and cleanup

Caches event handlers and Icon instances, replaces magic strings,
adds accessibility attributes, removes dead code, namespaces JS."
```

---

### Task 24: Fix Data Layer P3 Issues (D8, D9, D11, D-L*)

**Files:**
- Modify: `src/Askyl.Dsm.WebHosting.Data/DsmApi/Models/FileStation/FileStationFile.cs:14`
- Modify: `src/Askyl.Dsm.WebHosting.Data/Results/*.cs`
- Modify: `src/Askyl.Dsm.WebHosting.Data/DsmApi/Models/ReverseProxy/ReverseProxyFrontend.cs`
- Modify: `src/Askyl.Dsm.WebHosting.Data/DsmApi/Models/ReverseProxy/ReverseProxyBackend.cs`
- Modify: `src/Askyl.Dsm.WebHosting.Data/DsmApi/Models/ReverseProxy/ReverseProxyHttps.cs`
- Modify: `src/Askyl.Dsm.WebHosting.Data/DsmApi/Exceptions/ReverseProxyNotFoundException.cs:7`
- Modify: `src/Askyl.Dsm.WebHosting.Data/DsmApi/Exceptions/FileStationApiException.cs:28-29`

**Interfaces:**
- Consumes: Existing data types
- Produces: Polished data types

- [ ] **Step 1: Convert FileStationFile.Type to enum (D8)**

Create a `FileType` enum with `File` and `Directory` values. Update `FileStationFile.Type` from `string` to `FileType`.

- [ ] **Step 2: Add consistent factory methods to Results (D9)**

Add missing factory methods (`CreateSuccess`, `CreateFailure`) to Result subclasses that don't have them. Consider adding to abstract base class.

- [ ] **Step 3: Remove redundant primary constructor parameters (D11)**

In `ReverseProxyFrontend.cs`, `ReverseProxyBackend.cs`, `ReverseProxyHttps.cs`, remove properties that are already handled by primary constructor auto-properties.

- [ ] **Step 4: Low-priority data fixes (D-L1/L2/L3)**

- Move `"Running"`/`"Stopped"` magic strings in `WebSiteInstance.cs:46` to constants or enum
- Add `sealed` to `ReverseProxyNotFoundException`
- Handle null `Message` in `FileStationApiException.FormattedMessage`

- [ ] **Step 5: Format and build**

Run: `dotnet format ./src/Askyl.Dsm.WebHosting.slnx --verbosity quiet && dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx`
Expected: Build succeeds with zero errors and zero warnings.

- [ ] **Step 6: Commit**

```bash
git add src/Askyl.Dsm.WebHosting.Data/
git commit -m "refactor: P3 data layer polish

FileStationFile.Type converted to enum, consistent Result factory methods,
removed redundant primary constructor parameters, sealed exception type."
```

---

### Task 25: Fix Tests P3 Issues (T-M1, T-M3, T-M5, T-M6, T-L*)

**Files:**
- Modify: `src/Askyl.Dsm.WebHosting.Tests/Ui/Services/LogDownloadServiceTests.cs`
- Modify: `src/Askyl.Dsm.WebHosting.Tests/Tools/Diagnostics/OperationTimerTests.cs:53-60`
- Modify: Multiple test files for `[Trait]` categorization
- Modify: `src/Askyl.Dsm.WebHosting.Tests/Tools/Infrastructure/DsmSettingsServiceTests.cs:37-68`

**Interfaces:**
- Consumes: Existing test infrastructure
- Produces: Reliable, well-categorized tests

- [ ] **Step 1: Convert LogDownloadServiceTests to unit tests (T-M1)**

Replace file-system-dependent tests with mocked file I/O. Use `TestPath` or in-memory streams.

- [ ] **Step 2: Fix OperationTimerTests assertion (T-M3)**

Replace `>= 0` assertion with a meaningful elapsed time check (e.g., `Assert.InRange(elapsed, expectedMin, expectedMax)`).

- [ ] **Step 3: Add [Trait] categorization (T-M5)**

Add `[Trait("Category", "FileSystem")]` to tests that depend on the file system.

- [ ] **Step 4: Fix DsmSettingsServiceTests (T-M6)**

Rewrite to test actual service methods instead of inline parsing logic.

- [ ] **Step 5: Low-priority test fixes (T-L1/L2/L3/L4/L5)**

- Replace magic numbers in `WebSiteConfigurationTests.cs:8-14` with production constants
- Replace `dynamic` assertions in `ResultTypesTests.cs:173-206` with strongly-typed assertions
- Remove redundant constant value test in `DsmLanguageToCultureConverterTests.cs:156-161`
- Standardize naming conventions across test files
- Extend `MockHttpMessageHandler` to support multiple responses

- [ ] **Step 6: Run all tests**

Run: `dotnet test ./src/Askyl.Dsm.WebHosting.Tests/Askyl.Dsm.WebHosting.Tests.csproj --verbosity normal`
Expected: All tests pass.

- [ ] **Step 7: Commit**

```bash
git add src/Askyl.Dsm.WebHosting.Tests/
git commit -m "refactor: P3 test polish

Unit-tested file I/O, meaningful time assertions, [Trait] categorization,
strongly-typed assertions, standardized naming."
```

---

### Task 26: Fix Compliance P3 Issues (CO-L2, CO-L3, CO-L4, CO-L5)

**Files:**
- Modify: `src/Askyl.Dsm.WebHosting.Constants/DSM/API/DsmConstants.cs`
- Modify: `src/Askyl.Dsm.WebHosting.Constants/DSM/API/ReverseProxyConstants.cs`
- Modify: `src/Askyl.Dsm.WebHosting.Ui.Client/Services/DotnetVersionService.cs:85`
- Modify: `src/Askyl.Dsm.WebHosting.Ui/Services/SiteLifecycleManager.cs:63,85,105`
- Modify: `src/Askyl.Dsm.WebHosting.Data/Results/InstallationResult.cs:29`

**Interfaces:**
- Consumes: Existing constants and code
- Produces: Compliant code

- [ ] **Step 1: Resolve duplicate error code -4 (CO-L2)**

In `DsmConstants.cs` and `ReverseProxyConstants.cs`, one of the `-4` error codes should use a different value. Update the duplicate and all references.

- [ ] **Step 2: Replace .ToList() with collection expression (CO-L3)**

In `DotnetVersionService.cs:85`, replace `channels.ToList()` with `[.. channels]`.

- [ ] **Step 3: Use target-typed new() (CO-L4)**

In `SiteLifecycleManager.cs:63,85,105`, replace `new TaskCompletionSource<ApiResult>()` with `new()`.

- [ ] **Step 4: Extract hardcoded message (CO-L5)**

In `InstallationResult.cs:29`, replace `"Installation completed successfully."` with a constant from the Constants project.

- [ ] **Step 5: Format and build**

Run: `dotnet format ./src/Askyl.Dsm.WebHosting.slnx --verbosity quiet && dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx`
Expected: Build succeeds with zero errors and zero warnings.

- [ ] **Step 6: Commit**

```bash
git add src/Askyl.Dsm.WebHosting.Constants/ src/Askyl.Dsm.WebHosting.Ui.Client/Services/DotnetVersionService.cs src/Askyl.Dsm.WebHosting.Ui/Services/SiteLifecycleManager.cs src/Askyl.Dsm.WebHosting.Data/Results/InstallationResult.cs
git commit -m "refactor: P3 compliance fixes

Resolved duplicate error code, collection expression, target-typed new(),
extracted hardcoded message to constant."
```

---

### Task 27: Final Verification

**Files:** All modified files

- [ ] **Step 1: Full format and clean build**

Run: `dotnet clean /nr:false ./src/Askyl.Dsm.WebHosting.slnx && dotnet format ./src/Askyl.Dsm.WebHosting.slnx --verbosity quiet && dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx`
Expected: Build succeeds with zero errors and zero warnings.

- [ ] **Step 2: Run all tests**

Run: `dotnet test ./src/Askyl.Dsm.WebHosting.Tests/Askyl.Dsm.WebHosting.Tests.csproj --verbosity normal`
Expected: All tests pass.

- [ ] **Step 3: Verify no magic strings/numbers remain (manual check)**

Scan modified files for hardcoded strings that should reference constants.

- [ ] **Step 4: Verify commit history**

Run: `git log --oneline -30`
Expected: Commits are ordered P0 → P1 → P2 → P3, each with a clear conventional commit message.
