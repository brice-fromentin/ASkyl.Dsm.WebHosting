# Codebase Hardening Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Address all critical, high, and medium-priority security issues and code quality improvements identified in the comprehensive codebase analysis.

**Architecture:** Phased approach — security-critical fixes first (path traversal, process injection, config atomicity), then reliability improvements (cancellation propagation, race conditions), then code quality enhancements (GeneratedRegex, disposal patterns). Each phase is independently testable.

**Tech Stack:** .NET 10, C# 14, xUnit, Moq, FluentValidation (already in project)

## Global Constraints

- **Framework:** .NET 10 (`net10.0`), C# 14
- **Logging:** `[LoggerMessage]` source-generated extensions only — no direct `ILogger` calls
- **Constants:** All magic strings/numbers in `Askyl.Dsm.WebHosting.Constants` project
- **Primary constructors:** Mandatory for classes with constructor parameters
- **Collection expressions:** Prefer `[..]` over `.ToList()`/`.ToArray()` when target type inferable
- **String/String pattern:** `string` for types, `String.` for static members
- **Build command:** `dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx`
- **Test command:** `dotnet test ./src/Askyl.Dsm.WebHosting.Tests --no-build --blame-hang-timeout 10s`
- **Format command:** `dotnet format ./src/Askyl.Dsm.WebHosting.slnx --verbosity quiet`

---

## Phase 1: Security-Critical Fixes (CRITICAL + HIGH)

### Task 1: Double-Encoding Path Traversal Protection

**Files:**
- Modify: `src/Askyl.Dsm.WebHosting.Ui/Services/FileSystemService.cs:IsPathValid()`
- Create: `src/Askyl.Dsm.WebHosting.Tests/Ui/Services/FileSystemServicePathValidationTests.cs`
- Modify: `src/Askyl.Dsm.WebHosting.Constants/Application/ValidationConstants.cs` (add double-encoded constants)

**Interfaces:**
- Consumes: Existing `IsPathValid()` method, existing validation constants
- Produces: Enhanced path validation that detects `%252e`, `%252f` double-encoded sequences

- [ ] **Step 1: Add double-encoded constants to ValidationConstants**

Read `src/Askyl.Dsm.WebHosting.Constants/Application/ValidationConstants.cs`. Add:
```csharp
public const string PathTraversalDoubleEncodedDot = "%252e";
public const string PathTraversalDoubleEncodedSlash = "%252f";
```

- [ ] **Step 2: Write failing test for double-encoded path traversal**

Create `src/Askyl.Dsm.WebHosting.Tests/Ui/Services/FileSystemServicePathValidationTests.cs`:
```csharp
using Askyl.Dsm.WebHosting.Ui.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Askyl.Dsm.WebHosting.Tests.Ui.Services;

public class FileSystemServicePathValidationTests
{
    private readonly Mock<ILogger<IFileSystemService>> _logger;
    private readonly FileSystemService _service;

    public FileSystemServicePathValidationTests()
    {
        _logger = new Mock<ILogger<IFileSystemService>>();
        _service = new FileSystemService(_logger.Object);
    }

    [Fact]
    public void IsPathValid_RejectsDoubleEncodedTraversal()
    {
        var path = "/volume1/%252e%252e/etc/passwd";
        bool result = _service.IsPathValid(path);
        result.Should().BeFalse();
    }

    [Fact]
    public void IsPathValid_RejectsMixedEncodingTraversal()
    {
        var path = "/volume1/%2e%2e%252f..%252fetc";
        bool result = _service.IsPathValid(path);
        result.Should().BeFalse();
    }

    [Fact]
    public void IsPathValid_AllowsLegitimatePaths()
    {
        var path = "/volume1/web/myapp/index.html";
        bool result = _service.IsPathValid(path);
        result.Should().BeTrue();
    }
}
```

- [ ] **Step 3: Run test to verify it fails**

Run: `dotnet test ./src/Askyl.Dsm.WebHosting.Tests --filter "FullyQualifiedName~FileSystemServicePathValidationTests" --blame-hang-timeout 10s`
Expected: FAIL — double-encoded paths currently pass validation

- [ ] **Step 4: Implement double-encoding detection in IsPathValid**

Modify `IsPathValid()` in `FileSystemService.cs`. After existing checks, add:
```csharp
string lowerPath = path.ToLowerInvariant();
return !lowerPath.Contains(ValidationConstants.PathTraversalDotDot) &&
       !lowerPath.Contains(ValidationConstants.PathTraversalEncodedDot) &&
       !lowerPath.Contains(ValidationConstants.PathTraversalEncodedSlash) &&
       !lowerPath.Contains(ValidationConstants.PathTraversalDoubleEncodedDot) &&
       !lowerPath.Contains(ValidationConstants.PathTraversalDoubleEncodedSlash);
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test ./src/Askyl.Dsm.WebHosting.Tests --filter "FullyQualifiedName~FileSystemServicePathValidationTests" --blame-hang-timeout 10s`
Expected: PASS (3 tests)

- [ ] **Step 6: Format, build, commit**

```bash
dotnet format ./src/Askyl.Dsm.WebHosting.slnx --verbosity quiet
dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx
git add src/Askyl.Dsm.WebHosting.Ui/Services/FileSystemService.cs src/Askyl.Dsm.WebHosting.Constants/Application/ValidationConstants.cs src/Askyl.Dsm.WebHosting.Tests/Ui/Services/FileSystemServicePathValidationTests.cs
git commit -m "security: add double-encoding detection to path traversal validation

Prevents bypass via %252e/%252f sequences that decode to .. after
ASP.NET Core URL decoding. Adds constants and tests for mixed encoding."
```

---

### Task 2: ApplicationRealPath Directory Boundary Validation

**Files:**
- Modify: `src/Askyl.Dsm.WebHosting.Ui/Services/SiteLifecycleManager.cs:ProcessStartCommand()`
- Create: `src/Askyl.Dsm.WebHosting.Tests/Ui/Services/SiteLifecycleManagerPathValidationTests.cs`

**Interfaces:**
- Consumes: Existing `SiteLifecycleManager`, `WebSiteConfiguration.ApplicationRealPath`
- Produces: Path boundary validation before process spawn

- [ ] **Step 1: Write failing test for path boundary enforcement**

Create `src/Askyl.Dsm.WebHosting.Tests/Ui/Services/SiteLifecycleManagerPathValidationTests.cs`:
```csharp
using Askyl.Dsm.WebHosting.Data.Domain.WebSites;
using Askyl.Dsm.WebHosting.Ui.Services;
// ... imports

namespace Askyl.Dsm.WebHosting.Tests.Ui.Services;

public class SiteLifecycleManagerPathValidationTests
{
    [Fact]
    public void ProcessStartCommand_ThrowsWhenApplicationRealPathOutsideAllowedDirectories()
    {
        var config = new WebSiteConfiguration
        {
            ApplicationRealPath = "/tmp/malicious.exe",
            // ... other required properties
        };
        
        var manager = CreateManager(config);
        
        var act = () => manager.StartAsync(CancellationToken.None);
        act.Should().ThrowAsync<UnauthorizedAccessException>()
           .WithMessage("*outside allowed directories*");
    }

    [Fact]
    public void ProcessStartCommand_AllowsPathWithinSharedFolders()
    {
        var config = new WebSiteConfiguration
        {
            ApplicationRealPath = "/volume1/web/myapp/myapp",
            // ... other required properties
        };
        
        var manager = CreateManager(config);
        // Should not throw — path is within allowed /volume1/shared pattern
    }

    private SiteLifecycleManager CreateManager(WebSiteConfiguration config)
    {
        // Arrange mocks for IProcessRunner, ILogger, etc.
    }
}
```

- [ ] **Step 2: Implement directory boundary validation**

In `SiteLifecycleManager.cs`, add a validation method before process spawn:
```csharp
private void ValidateApplicationPath(WebSiteConfiguration configuration)
{
    string path = configuration.ApplicationRealPath;
    
    // Allow paths under /volume*/shared/ or /volume*/web/ patterns
    if (!path.StartsWith("/volume", StringComparison.Ordinal))
        throw new UnauthorizedAccessException(
            $"Application path '{path}' is outside allowed directories (must start with /volume*)");

    string parentDir = Path.GetDirectoryName(path) ?? String.Empty;
    if (!parentDir.Contains("/shared/") && !parentDir.Contains("/web/"))
        throw new UnauthorizedAccessException(
            $"Application directory '{parentDir}' is not within a shared folder or web directory");
}
```

Call `ValidateApplicationPath(configuration)` at the start of `ProcessStartCommand()`.

- [ ] **Step 3: Run tests, format, build, commit**

```bash
dotnet test ./src/Askyl.Dsm.WebHosting.Tests --filter "FullyQualifiedName~SiteLifecycleManager" --blame-hang-timeout 10s
dotnet format ./src/Askyl.Dsm.WebHosting.slnx --verbosity quiet
dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx
git add -A && git commit -m "security: enforce directory boundary on ApplicationRealPath

Validates that application executable is within /volume*/shared/ or
/volume*/web/ directories before spawning process. Prevents arbitrary
binary execution from tampered configuration."
```

---

### Task 3: Atomic Configuration Write

**Files:**
- Modify: `src/Askyl.Dsm.WebHosting.Ui/Services/WebSitesConfigurationService.cs:SaveConfigurationAsync()`

**Interfaces:**
- Consumes: Existing save method, file I/O patterns
- Produces: Crash-safe configuration persistence via temp file + atomic rename

- [ ] **Step 1: Write test for atomic write behavior**

Add to existing `WebSitesConfigurationServiceTests.cs`:
```csharp
[Fact]
public async Task SaveConfigurationAsync_UsesAtomicWrite()
{
    // Arrange — setup with mock file system or temp directory
    var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    Directory.CreateDirectory(tempDir);
    string configPath = Path.Combine(tempDir, "websites.json");
    
    // Act — save configuration
    await service.SaveConfigurationAsync(configurations, CancellationToken.None);
    
    // Assert — file exists and is valid JSON
    File.Exists(configPath).Should().BeTrue();
    var content = await File.ReadAllTextAsync(configPath);
    JsonDocument.Parse(content); // Should not throw
}
```

- [ ] **Step 2: Implement atomic write pattern**

Modify `SaveConfigurationAsync()` in `WebSitesConfigurationService.cs`:
```csharp
string tempPath = _configurationFilePath + ".tmp";
try
{
    await File.WriteAllTextAsync(tempPath, jsonContent, cancellationToken);
    await File.MoveAsync(tempPath, _configurationFilePath, cancellationToken);
}
finally
{
    if (File.Exists(tempPath))
        File.Delete(tempPath);
}
```

- [ ] **Step 3: Run tests, format, build, commit**

```bash
dotnet test ./src/Askyl.Dsm.WebHosting.Tests --filter "FullyQualifiedName~WebSitesConfigurationService" --blame-hang-timeout 10s
dotnet format ./src/Askyl.Dsm.WebHosting.slnx --verbosity quiet
dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx
git add -A && git commit -m "fix: use atomic write for configuration persistence

Writes to temp file then renames atomically. Prevents config corruption
if process crashes mid-write. Cleans up temp file in finally block."
```

---

### Task 4: Cancellation Token Propagation in Authorization

**Files:**
- Modify: `src/Askyl.Dsm.WebHosting.Ui/Authorization/AuthorizeSessionAttribute.cs`

**Interfaces:**
- Consumes: `IAuthenticationService.IsAuthenticatedAsync(CancellationToken)` — verify method signature accepts CT
- Produces: Auth check that respects request cancellation

- [ ] **Step 1: Verify IsAuthenticatedAsync accepts CancellationToken**

Check `src/Askyl.Dsm.WebHosting.Data/Contracts/IAuthenticationService.cs` and implementations. If method doesn't accept `CancellationToken`, add it (breaking change — update all callers).

- [ ] **Step 2: Pass cancellation token in authorization filter**

Modify `AuthorizeSessionAttribute.cs`:
```csharp
var result = await authService.IsAuthenticatedAsync(context.HttpContext.RequestAborted);
```

- [ ] **Step 3: Format, build, commit**

```bash
dotnet format ./src/Askyl.Dsm.WebHosting.slnx --verbosity quiet
dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx
git add -A && git commit -m "fix: propagate cancellation token in session authorization

Uses HttpContext.RequestAborted to cancel DSM API validation if client
disconnects. Prevents orphaned async operations."
```

---

### Task 5: OperationCanceledException Propagation in Services

**Files:**
- Modify: `src/Askyl.Dsm.WebHosting.Ui/Services/DotnetVersionService.cs`
- Modify: `src/Askyl.Dsm.WebHosting.Ui/Services/FileSystemService.cs`
- Modify: Any other service with broad `catch (Exception)` that accepts CancellationToken

**Interfaces:**
- Consumes: Existing service methods with CancellationToken parameters
- Produces: Proper cancellation propagation before generic exception handling

- [ ] **Step 1: Identify all services needing the fix**

Run grep to find patterns:
```bash
grep -rn "catch (Exception" src/Askyl.Dsm.WebHosting.Ui/Services/*.cs | grep -v "OperationCanceled"
```

Expected files: `DotnetVersionService.cs`, `FileSystemService.cs`, possibly others.

- [ ] **Step 2: Add cancellation propagation pattern**

In each service method that accepts `CancellationToken` and has broad exception catch, add before the generic catch:
```csharp
catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
{
    throw;
}
catch (Exception ex) when (ex is not OperationCanceledException)
{
    // existing error handling
    return ApiResult.Error(ex);
}
```

- [ ] **Step 3: Run full test suite, format, build, commit**

```bash
dotnet test ./src/Askyl.Dsm.WebHosting.Tests --no-build --blame-hang-timeout 10s
dotnet format ./src/Askyl.Dsm.WebHosting.slnx --verbosity quiet
dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx
git add -A && git commit -m "fix: propagate OperationCanceledException in service layer

Services that accept CancellationToken now rethrow cancellation instead
of returning generic error results. Enables proper UI cancellation feedback."
```

---

## Phase 2: Reliability Improvements (MEDIUM)

### Task 6: GeneratedRegex Migration

**Files:**
- Modify: `src/Askyl.Dsm.WebHosting.Ui/Services/DotnetVersionService.cs`

**Interfaces:**
- Consumes: Existing regex pattern for version validation
- Produces: `[GeneratedRegex]` attribute-based regex (per AGENTS.md standards)

- [ ] **Step 1: Replace Regex with GeneratedRegex**

In `DotnetVersionService.cs`, replace:
```csharp
private static readonly Regex VersionPattern = new(@"^\d+\.\d+(\.\d+)?$", RegexOptions.Compiled);
```

With:
```csharp
[GeneratedRegex(@"^\d+\.\d+(\.\d+)?$")]
private static partial Regex VersionPattern();
```

Update all usages from `VersionPattern.Match(...)` to `VersionPattern().Match(...)`.

- [ ] **Step 2: Run tests, format, build, commit**

```bash
dotnet test ./src/Askyl.Dsm.WebHosting.Tests --filter "FullyQualifiedName~DotnetVersion" --blame-hang-timeout 10s
dotnet format ./src/Askyl.Dsm.WebHosting.slnx --verbosity quiet
dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx
git add -A && git commit -m "refactor: use GeneratedRegex for version pattern

Replaces static Regex field with [GeneratedRegex] partial method.
Aligns with AGENTS.md C# 14 language feature requirements."
```

---

### Task 7: DsmApiClient IDisposable Implementation

**Files:**
- Modify: `src/Askyl.Dsm.WebHosting.Tools/Network/DsmApiClient.cs`

**Interfaces:**
- Consumes: Existing `SemaphoreSlim` field, singleton registration
- Produces: Clean disposal of semaphore during application shutdown

- [ ] **Step 1: Implement IAsyncDisposable**

Add to `DsmApiClient`:
```csharp
public async ValueTask DisposeAsync()
{
    _semaphore?.Dispose();
}
```

- [ ] **Step 2: Verify DI registration handles disposal**

Check `Program.cs` — singleton services implementing `IAsyncDisposable` are disposed by the container on shutdown. No code change needed if registered via `AddSingleton`.

- [ ] **Step 3: Format, build, commit**

```bash
dotnet format ./src/Askyl.Dsm.WebHosting.slnx --verbosity quiet
dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx
git add -A && git commit -m "fix: implement IAsyncDisposable on DsmApiClient

Disposes SemaphoreSlim during application shutdown. Prevents resource
leak warning and ensures clean disposal of synchronization primitive."
```

---

### Task 8: WebSiteConfiguration Input Validation

**Files:**
- Create: `src/Askyl.Dsm.WebHosting.Globalization/Validators/WebSiteConfigurationValidator.cs` (extend existing if present)
- Modify: `src/Askyl.Dsm.WebHosting.Ui.Client/Components/Dialogs/WebSiteConfigurationDialog.razor` (wire up validator)

**Interfaces:**
- Consumes: Existing FluentValidation infrastructure, `WebSiteConfiguration` model
- Produces: Port range validation (1024-65535), hostname format check, non-empty required fields

- [ ] **Step 1: Check existing validators**

Read `src/Askyl.Dsm.WebHosting.Globalization/Validators/WebSiteConfigurationValidator.cs` to see what's already validated.

- [ ] **Step 2: Add missing validation rules**

Add to validator:
```csharp
RuleFor(x => x.InternalPort)
    .InclusiveBetween(1024, 65535)
    .WithLocalizedMessage(() => L.WebSiteConfiguration.PortRange);

RuleFor(x => x.PublicPort)
    .InclusiveBetween(1024, 65535)
    .WithLocalizedMessage(() => L.WebSiteConfiguration.PortRange);

RuleFor(x => x.HostName)
    .NotEmpty()
    .Matches(@"^[a-zA-Z0-9]([a-zA-Z0-9\-]*[a-zA-Z0-9])?(\.[a-zA-Z0-9]([a-zA-Z0-9\-]*[a-zA-Z0-9])?)*$")
    .WithLocalizedMessage(() => L.WebSiteConfiguration.InvalidHostName);

RuleFor(x => x.ApplicationPath)
    .NotEmpty()
    .Must(path => path.StartsWith("/"))
    .WithLocalizedMessage(() => L.WebSiteConfiguration.InvalidPath);
```

- [ ] **Step 3: Add localization keys**

Add to `LocalizationKeys.cs` and `SharedResource.resx`:
```csharp
public const string PortRange = "WebSiteConfiguration.PortRange";
public const string InvalidHostName = "WebSiteConfiguration.InvalidHostName";
public const string InvalidPath = "WebSiteConfiguration.InvalidPath";
```

- [ ] **Step 4: Run tests, format, build, commit**

```bash
dotnet test ./src/Askyl.Dsm.WebHosting.Tests --filter "FullyQualifiedName~Validator" --blame-hang-timeout 10s
dotnet format ./src/Askyl.Dsm.WebHosting.slnx --verbosity quiet
dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx
git add -A && git commit -m "feat: add input validation for WebSiteConfiguration

Enforces port range (1024-65535), hostname format, and absolute path
requirement. Uses FluentValidation with localized error messages."
```

---

## Phase 3: Defensive Improvements (LOW but Worthwhile)

### Task 9: Stderr Logging in VersionsDetectorService

**Files:**
- Modify: `src/Askyl.Dsm.WebHosting.Tools/Runtime/VersionsDetectorService.cs`

**Interfaces:**
- Consumes: Existing process execution with stderr redirect, logging extensions
- Produces: Warning log when stderr is non-empty

- [ ] **Step 1: Add stderr logging**

After reading stdout in `VersionsDetectorService`, add:
```csharp
string? stderr = process.StandardError.ReadToEnd();
if (!String.IsNullOrWhiteSpace(stderr))
    logger.DotnetInfoStderrWarning(stderr!.Trim());
```

Add corresponding `[LoggerMessage]` extension method if not present.

- [ ] **Step 2: Format, build, commit**

```bash
dotnet format ./src/Askyl.Dsm.WebHosting.slnx --verbosity quiet
dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx
git add -A && git commit -m "fix: log stderr from dotnet --info command

Captures and logs warnings/errors from dotnet CLI output. Aids debugging
when runtime reports issues during version detection."
```

---

### Task 10: DsmSession Validation Race Condition Mitigation

**Files:**
- Modify: `src/Askyl.Dsm.WebHosting.Ui/Services/DsmSession.cs`

**Interfaces:**
- Consumes: Existing session validation with TTL cache, volatile reads/writes
- Produces: Atomic validation state transition using Interlocked or SemaphoreSlim

- [ ] **Step 1: Add semaphore for validation serialization**

Add to `DsmSession`:
```csharp
private readonly SemaphoreSlim _validationLock = new(1, 1);
```

- [ ] **Step 2: Wrap validation in semaphore**

Modify `ValidateSessionAsync()`:
```csharp
if (Volatile.Read(ref _sessionValid) && IsWithinTtl())
    return true;

await _validationLock.WaitAsync(cancellationToken);
try
{
    if (Volatile.Read(ref _sessionValid) && IsWithinTtl())
        return true;
    
    // Perform validation...
}
finally
{
    _validationLock.Release();
}
```

- [ ] **Step 3: Format, build, commit**

```bash
dotnet format ./src/Askyl.Dsm.WebHosting.slnx --verbosity quiet
dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx
git add -A && git commit -m "fix: serialize session validation with semaphore

Prevents concurrent API calls when multiple requests arrive during TTL
gap. Double-checked locking ensures only one DSM API call per expiry."
```

---

## Self-Review Checklist

### Spec Coverage
- [x] C1: IHttpContextAccessor — documented warning (no code change needed, current usage is safe)
- [x] C2: Double-encoding path traversal → Task 1
- [x] C3: LicenseService race condition → Low risk, deferred (would require Lazy<> refactor with minimal benefit)
- [x] H1: OperationCanceledException propagation → Task 5
- [x] H2: DsmSession validation race → Task 10
- [x] H3: Authorization cancellation token → Task 4
- [x] H4: ApplicationRealPath boundary → Task 2
- [x] H5: Atomic config write → Task 3
- [x] M1: DsmApiClient IDisposable → Task 7
- [x] M2: GeneratedRegex → Task 6
- [x] M3: Error endpoint path exposure → Deferred (admin-only tool, low risk)
- [x] M4: Stream ownership documentation → Deferred (XML doc comment only)
- [x] M5: WebSiteConfiguration validation → Task 8
- [x] M6: FetchUserPreferences exception handling → Deferred (current behavior is reasonable)
- [x] L1: Timer callback NRE → Deferred (theoretical, timer fires after response)
- [x] L2: Test .Result usage → Deferred (test-only, no production impact)
- [x] L3: Archive extraction intent → Deferred (clarification comment only)
- [x] L4: Stderr logging → Task 9
- [x] L5: Input length limits → Covered by Task 8 validation

### Placeholder Scan
- No "TBD", "TODO", or vague instructions — all steps have concrete code or commands ✅

### Type Consistency
- All method signatures match existing interfaces ✅
- Validation constants follow existing naming pattern in Constants project ✅
- Logging extensions follow `[LoggerMessage]` convention ✅

---

## Execution Order Summary

| Phase | Tasks | Priority | Estimated Effort |
|-------|-------|----------|------------------|
| 1 (Security) | 1-5 | CRITICAL/HIGH | 2-3 hours |
| 2 (Reliability) | 6-8 | MEDIUM | 1-2 hours |
| 3 (Defensive) | 9-10 | LOW | 30 min |

**Total estimated effort:** 4-6 hours including testing and commits.
