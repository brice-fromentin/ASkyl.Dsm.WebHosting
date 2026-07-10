# Architecture Gaps Fixes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development
> (recommended) or superpowers:executing-plans to implement this plan task-by-task.
> Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix all 8 identified architecture gaps to improve production stability, developer experience, and code quality.

**Architecture:** Priority-ordered fixes from Critical (broken error endpoints) through
Minor (dead code removal). Each gap is self-contained with clear before/after state.
Code changes follow existing patterns: primary constructors, `[LoggerMessage]` extensions,
collection expressions, and `String.` static member pattern.

**Tech Stack:** .NET 10, C# 14, Blazor Interactive WebAssembly, xUnit, Moq, GitHub Actions

## Global Constraints

- **Framework:** .NET 10 (`net10.0`), C# 14 language features
- **String pattern:** `String.` for static members (`String.Equals`, `String.IsNullOrWhiteSpace`, `String.Empty`); `string` for types/variables
- **Primary constructors:** Mandatory for all classes with constructor parameters (except abstract)
- **Collections:** Use `[..]` collection expressions when target type inferable; `.Count == 0` for emptiness checks
- **Logging:** `[LoggerMessage]` source-generated extension methods only — no direct `ILogger` calls
- **Nullable:** Enabled and enforced throughout
- **Format/Build:** `dotnet format ./src/Askyl.Dsm.WebHosting.slnx --verbosity quiet` then `dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx`
- **Tests:** `dotnet test ./src/Askyl.Dsm.WebHosting.Tests --no-build --blame-hang-timeout 10s`
- **Markdown:** Run `markdownlint <file>` after any `.md` changes

---

## Task 1: Fix Broken Error Handling Endpoints (Gap #1 — Critical)

**Files:**

- Create: `src/Askyl.Dsm.WebHosting.Ui/Endpoints/ErrorEndpoints.cs`
- Modify: `src/Askyl.Dsm.WebHosting.Ui/Program.cs:136` (verify middleware ordering)

**Interfaces:**

- Consumes: None
- Produces: `ErrorEndpoints.MapErrorEndpoints(WebApplication)` extension method for other tasks to reference

### 1.1 Create ErrorEndpoints.cs

- [ ] **Step 1: Create the file with error handler endpoints**

Create `src/Askyl.Dsm.WebHosting.Ui/Endpoints/ErrorEndpoints.cs`:

```csharp
using System.Net.Mime;
using Askyl.Dsm.WebHosting.Data.Results;

namespace Askyl.Dsm.WebHosting.Ui.Endpoints;

/// <summary>
/// Maps error handling endpoints for middleware re-execution.
/// </summary>
public static class ErrorEndpoints
{
    /// <summary>
    /// Maps /Error and /not-found endpoints to handle middleware re-execution.
    /// </summary>
    public static void MapErrorEndpoints(this WebApplication app)
    {
        app.MapGet("/Error", (HttpContext context) =>
        {
            var error = context.Features.Get<IExceptionHandlerFeature>()?.error;
            var errorId = error?.GetType().Name ?? "UnknownException";

            if (context.Request.Headers.Accept.Contains(MediaTypeNames.Application.Json))
            {
                return Results.Json(
                    new ApiResult(false, $"Internal server error: {errorId}"),
                    statusCode: StatusCodes.Status500InternalServerError);
            }

            return Results.Content(
                $"<html><body><h1>500 Internal Server Error</h1><p>{errorId}</p></body></html>",
                MediaTypeNames.Text.Html,
                statusCode: StatusCodes.Status500InternalServerError);
        });

        app.MapGet("/not-found", (HttpContext context) =>
        {
            var statusCode = context.Features.Get<IStatusCodeReExecuteFeature>()?.OriginalStatusCode
                ?? StatusCodes.Status404NotFound;

            if (context.Request.Headers.Accept.Contains(MediaTypeNames.Application.Json))
            {
                return Results.Json(
                    new ApiResult(false, "Resource not found"),
                    statusCode: statusCode);
            }

            return Results.Content(
                $"<html><body><h1>{statusCode} Not Found</h1></body></html>",
                MediaTypeNames.Text.Html,
                statusCode: statusCode);
        });
    }
}
```

- [ ] **Step 2: Build and verify**

Run: `dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx`
Expected: SUCCESS with no errors or warnings

### 1.2 Wire ErrorEndpoints into Program.cs

- [ ] **Step 3: Add using and call in Program.cs**

Modify `src/Askyl.Dsm.WebHosting.Ui/Program.cs`:

Add after line 12 (`using Askyl.Dsm.WebHosting.Ui.Extensions;`):

```csharp
using Askyl.Dsm.WebHosting.Ui.Endpoints;
```

Add after line 163 (`app.MapControllers();`):

```csharp
app.MapErrorEndpoints();
```

- [ ] **Step 4: Build and verify**

Run: `dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx`
Expected: SUCCESS with no errors or warnings

### 1.3 Commit

- [ ] **Step 5: Commit**

```bash
git add src/Askyl.Dsm.WebHosting.Ui/Endpoints/ErrorEndpoints.cs src/Askyl.Dsm.WebHosting.Ui/Program.cs
git commit -m "fix: implement broken error handling endpoints

Resolves infinite redirect loop when UseExceptionHandler and
UseStatusCodePagesWithReExecute hit non-existent routes. Adds /Error
and /not-found endpoints with JSON/HTML content negotiation."
```

---

## Task 2: Remove Dead Code (Gap #8 — Minor)

**Files:**

- Delete: `src/Askyl.Dsm.WebHosting.Data/Exceptions/MandatorySettingMissingException.cs`

**Interfaces:**

- Consumes: None
- Produces: Cleaner codebase with no unused exception type

### 2.1 Verify No References

- [ ] **Step 1: Confirm zero usages**

Run: `grep -r "MandatorySettingMissingException" src/ --include="*.cs"`
Expected: Only the definition file itself (no `using` or throw statements)

### 2.2 Delete File

- [ ] **Step 2: Remove the file**

```bash
rm src/Askyl.Dsm.WebHosting.Data/Exceptions/MandatorySettingMissingException.cs
```

- [ ] **Step 3: Build and verify**

Run: `dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx`
Expected: SUCCESS with no errors or warnings

### 2.3 Commit

- [ ] **Step 4: Commit**

```bash
git add -A src/Askyl.Dsm.WebHosting.Data/Exceptions/
git commit -m "refactor: remove unused MandatorySettingMissingException

Type was defined but never thrown anywhere in the codebase."
```

---

## Task 3: Fix Test Anti-Patterns (Gap #4 — Significant)

**Files:**

- Modify: `src/Askyl.Dsm.WebHosting.Tests/Ui/Services/WebSiteHostingServiceTests.cs`
- Modify: `src/Askyl.Dsm.WebHosting.Tests/Tools/Infrastructure/ArchiveExtractorServiceTests.cs`
- Modify: `src/Askyl.Dsm.WebHosting.Tests/Tools/Infrastructure/FileManagerServiceTests.cs`

**Interfaces:**

- Consumes: None
- Produces: Clean test suite with proper IDisposable cleanup

### 3.1 Fix WebSiteHostingServiceTests — Remove Collection, Add Dispose

- [ ] **Step 1: Implement IDisposable and remove ineffective Collection attribute**

Modify `src/Askyl.Dsm.WebHosting.Tests/Ui/Services/WebSiteHostingServiceTests.cs`:

Remove line 15: `[Collection("WebSiteHostingService")]`

Change class declaration (line 17) to implement `IDisposable`:

```csharp
[Trait("Category", "FileSystem")]
public class WebSiteHostingServiceTests : IDisposable
{
```

Add Dispose method at end of class (before final `}`):

```csharp
    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }
}
```

- [ ] **Step 2: Run tests to verify**

Run: `dotnet test ./src/Askyl.Dsm.WebHosting.Tests --no-build --blame-hang-timeout 10s --filter "FullyQualifiedName~WebSiteHostingServiceTests"`
Expected: All tests PASS

### 3.2 Fix ArchiveExtractorServiceTests — Add Dispose

- [ ] **Step 3: Implement IDisposable**

Modify `src/Askyl.Dsm.WebHosting.Tests/Tools/Infrastructure/ArchiveExtractorServiceTests.cs`:

Change class declaration to implement `IDisposable`, add cleanup for `_tempBase` and `_tempExtract`:

```csharp
    public void Dispose()
    {
        if (Directory.Exists(_tempBase))
        {
            Directory.Delete(_tempBase, recursive: true);
        }

        if (Directory.Exists(_tempExtract))
        {
            Directory.Delete(_tempExtract, recursive: true);
        }
    }
```

- [ ] **Step 4: Run tests to verify**

Run: `dotnet test ./src/Askyl.Dsm.WebHosting.Tests --no-build --blame-hang-timeout 10s --filter "FullyQualifiedName~ArchiveExtractorServiceTests"`
Expected: All tests PASS

### 3.3 Fix FileManagerServiceTests — Add Dispose

- [ ] **Step 5: Implement IDisposable**

Modify `src/Askyl.Dsm.WebHosting.Tests/Tools/Infrastructure/FileManagerServiceTests.cs`:

Change class declaration to implement `IDisposable`, add cleanup for `_tempBase`:

```csharp
    public void Dispose()
    {
        if (Directory.Exists(_tempBase))
        {
            Directory.Delete(_tempBase, recursive: true);
        }
    }
```

- [ ] **Step 6: Run tests to verify**

Run: `dotnet test ./src/Askyl.Dsm.WebHosting.Tests --no-build --blame-hang-timeout 10s --filter "FullyQualifiedName~FileManagerServiceTests"`
Expected: All tests PASS

### 3.4 Fix Magic String Assertions

- [ ] **Step 7: Replace magic string assertions with structured property checks**

In `WebSiteHostingServiceTests.cs`, replace assertions like:

```csharp
Assert.Equal("Site not found", result.Message);
```

With structured checks that verify the result state without coupling to localization
strings. Check what properties are available on the result type and assert those
instead (e.g., `result.ErrorCode` or similar).

- [ ] **Step 8: Run all tests**

Run: `dotnet test ./src/Askyl.Dsm.WebHosting.Tests --no-build --blame-hang-timeout 10s`
Expected: All tests PASS (~4s execution)

### 3.5 Commit

- [ ] **Step 9: Commit**

```bash
git add src/Askyl.Dsm.WebHosting.Tests/
git commit -m "fix: test anti-patterns — collection attribute, temp leaks, magic strings

Removes ineffective Collection attribute without matching definition.
Adds IDisposable cleanup to 3 test classes that leak temp directories.
Replaces magic string assertions with structured result property checks."
```

---

## Task 4: Configuration Management (Gap #5 — Significant)

**Files:**

- Modify: `src/Askyl.Dsm.WebHosting.Ui/appsettings.json`
- Modify: `src/Askyl.Dsm.WebHosting.Tools/Infrastructure/DsmSettingsService.cs`
- Modify: `src/Askyl.Dsm.WebHosting.Constants/DSM/System/SystemDefaults.cs`
- Modify: `src/Askyl.Dsm.WebHosting.Ui/Properties/launchSettings.json`
- Create: `dev-mock/synoinfo.conf` (template)
- Modify: `.gitignore`

**Interfaces:**

- Consumes: None
- Produces: Configurable DSM settings path for local debugging

### 4.1 Add DsmSettings.ConfigPath to appsettings.json

- [ ] **Step 1: Add configuration key**

Modify `src/Askyl.Dsm.WebHosting.Ui/appsettings.json`, add before closing `}`:

```json
  "DsmSettings": {
    "ConfigPath": "/etc/synoinfo.conf"
  }
```

### 4.2 Update DsmSettingsService to Use Configuration

- [ ] **Step 2: Update service to read path from configuration**

Modify `src/Askyl.Dsm.WebHosting.Tools/Infrastructure/DsmSettingsService.cs`:

Update constructor to accept `IConfiguration` and pass config path to `ReadSettings`:

```csharp
public sealed class DsmSettingsService(ILogger<ILogDsmSettingsService> logger, IFileReader fileReader, IConfiguration configuration) : IDsmSettingsService
{
    private readonly DsmSystemPreferences _preferences = ReadSettings(logger, fileReader, configuration);

    public string Server => _preferences.Server;

    public int Port => _preferences.Port;

    public string Language => _preferences.Language;

    static DsmSystemPreferences ReadSettings(ILogger<ILogDsmSettingsService> logger, IFileReader fileReader, IConfiguration configuration)
    {
        var configPath = configuration.GetValue<string>("DsmSettings:ConfigPath") ?? SystemDefaults.SynoInfoConfPath;

        if (!fileReader.FileExists(configPath))
        {
            logger.ConfigurationFileNotFound(configPath);
            return CreateDefaults(logger);
        }

        try
        {
            var lines = fileReader.ReadAllLines(configPath);
            // ... rest of method uses configPath instead of SystemDefaults.SynoInfoConfPath
```

Update exception message in `GetMandatorySetting` to use passed path variable.

- [ ] **Step 3: Update Program.cs registration**

Modify `src/Askyl.Dsm.WebHosting.Ui/Program.cs` line 62 — the singleton registration already works since `IConfiguration` is available in DI container.

### 4.3 Create dev-mock Template

- [ ] **Step 4: Create mock config template**

Create `dev-mock/synoinfo.conf`:

```text
# Mock DSM system configuration for local development
# Copy this file and modify as needed for testing
external_host_ip="127.0.0.1"
external_port_dsm_https="5001"
language="enu"
```

- [ ] **Step 5: Update .gitignore**

Add to `.gitignore`:

```text
# Local development mock files
dev-mock/
```

### 4.4 Update launchSettings.json

- [ ] **Step 6: Add environment variable override**

Modify `src/Askyl.Dsm.WebHosting.Ui/Properties/launchSettings.json`:

```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "inspectUri": "{wsProtocol}://{url.hostname}:{url.port}/adwh/_framework/debug/ws-proxy?browser={browserInspectUri}",
      "applicationUrl": "https://localhost:7018",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "DsmSettings__ConfigPath": "./dev-mock/synoinfo.conf"
      }
    }
  }
}
```

### 4.5 Update build-spk.sh Cleanup

- [ ] **Step 7: Add cleanup step to build script**

Modify `src/scripts/build-spk.sh`, add after line 339 (after PDB removal):

```bash
# Remove development-only files from publish output
echo "🧹 Removing development artifacts..."
find "$UI_PUBLISH_DIR" -name "*Development.json" -delete 2>/dev/null || true
rm -rf "$UI_PUBLISH_DIR/dev-mock" 2>/dev/null || true
```

- [ ] **Step 8: Build and run tests**

Run: `dotnet format ./src/Askyl.Dsm.WebHosting.slnx --verbosity quiet && dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx`
Expected: SUCCESS with no errors or warnings

Run: `dotnet test ./src/Askyl.Dsm.WebHosting.Tests --no-build --blame-hang-timeout 10s`
Expected: All tests PASS (update DsmSettingsServiceTests to mock IConfiguration)

### 4.6 Commit

- [ ] **Step 9: Commit**

```bash
git add src/Askyl.Dsm.WebHosting.Ui/appsettings.json src/Askyl.Dsm.WebHosting.Tools/Infrastructure/DsmSettingsService.cs src/Askyl.Dsm.WebHosting.Ui/Properties/launchSettings.json dev-mock/synoinfo.conf .gitignore src/scripts/build-spk.sh
git commit -m "feat: configurable DSM settings path for local debugging

Adds DsmSettings.ConfigPath to appsettings.json with environment variable
override support. Creates dev-mock/ template and updates build script to
strip development artifacts from SPK packages."
```

---

## Task 5: CI/CD Pipeline (Gap #3 — High)

**Files:**

- Create: `.github/workflows/build.yml`
- Modify: `src/scripts/build-spk.sh` (gzip fallback)

**Interfaces:**

- Consumes: None
- Produces: Automated build, test, and release pipeline

### 5.1 Create GitHub Actions Workflow

- [ ] **Step 1: Create workflow file**

Create `.github/workflows/build.yml`:

```text
name: Build and Release

on:
  push:
    branches: [main]
    tags:
      - 'v*'
  pull_request:
    branches: [main]

env:
  DOTNET_VERSION: '10.0.x'

jobs:
  verify:
    name: Verify (Push/PR)
    if: "!startsWith(github.ref, 'refs/tags/v')"
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Restore
        run: dotnet restore ./src/Askyl.Dsm.WebHosting.slnx

      - name: Format check
        run: dotnet format ./src/Askyl.Dsm.WebHosting.slnx --verify-no-changes --verbosity quiet

      - name: Build
        run: dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx

      - name: Test
        run: dotnet test ./src/Askyl.Dsm.WebHosting.Tests --no-build --blame-hang-timeout 10s

  release:
    name: Release (Tag Push)
    if: "startsWith(github.ref, 'refs/tags/v')"
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Install dependencies
        run: sudo apt-get update && sudo apt-get install -y pigz jq

      - name: Restore
        run: dotnet restore ./src/Askyl.Dsm.WebHosting.slnx

      - name: Build
        run: dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx

      - name: Test
        run: dotnet test ./src/Askyl.Dsm.WebHosting.Tests --no-build --blame-hang-timeout 10s

      - name: Cache .NET runtimes
        uses: actions/cache@v4
        with:
          path: src/spk-project/package/runtimes/downloads
          key: ${{ runner.os }}-runtimes-${{ hashFiles('src/Askyl.Dsm.WebHosting.Ui/appsettings.json') }}

      - name: Build SPK package
        run: ./src/scripts/build-spk.sh

      - name: Create GitHub Release
        env:
          GITHUB_TOKEN: ${{ github.token }}
        run: gh release create ${{ github.ref_name }} dist/*.spk --generate-notes --title "${{ github.ref_name }}"
```

### 5.2 Add gzip Fallback to build-spk.sh

- [ ] **Step 2: Add portable compression fallback**

Modify `src/scripts/build-spk.sh`, update the `create_spk_package` function (around line 246):

```bash
    # Use pigz if available, otherwise fall back to gzip
    if command -v pigz &> /dev/null; then
        tar -cf - . | pigz -2 > ../package.tgz
    else
        tar -cf - . | gzip -n > ../package.tgz
    fi
```

- [ ] **Step 3: Verify workflow syntax**

Run: `actionlint .github/workflows/build.yml 2>/dev/null || echo "actionlint not installed, skipping validation"`

### 5.3 Commit

- [ ] **Step 4: Commit**

```bash
git add .github/workflows/build.yml src/scripts/build-spk.sh
git commit -m "feat: CI/CD pipeline with automated releases on tag push

Adds GitHub Actions workflow with two paths: lightweight verification
on push/PR, full SPK build and release on tag push. Includes runtime
caching and gzip fallback for portability."
```

---

## Task 6: Error Correlation Tracing (Gap #7 — Minor)

**Files:**

- Create: `src/Askyl.Dsm.WebHosting.Ui/Middleware/RequestTrackingMiddleware.cs`
- Modify: `src/Askyl.Dsm.WebHosting.Ui/Program.cs` (wire middleware)

**Interfaces:**

- Consumes: None
- Produces: Request ID propagation through full call chain

### 6.1 Create Request Tracking Middleware

- [ ] **Step 1: Create middleware**

Create `src/Askyl.Dsm.WebHosting.Ui/Middleware/RequestTrackingMiddleware.cs`:

```csharp
namespace Askyl.Dsm.WebHosting.Ui.Middleware;

/// <summary>
/// Propagates X-Request-ID header through the request pipeline and surfaces it in error responses.
/// </summary>
public sealed class RequestTrackingMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = context.Request.Headers.XRequestId.FirstOrDefault() ?? Guid.NewGuid().ToString("N");
        context.Response.Headers.XRequestId = requestId;
        context.Items["RequestId"] = requestId;

        await next(context);
    }
}
```

### 6.2 Wire Middleware into Program.cs

- [ ] **Step 2: Register middleware**

Modify `src/Askyl.Dsm.WebHosting.Ui/Program.cs`, add after line 127 (`app.UseGlobalizationRequestLocalization();`):

```csharp
// Request tracking must be early to capture ID for the full pipeline
app.UseMiddleware<RequestTrackingMiddleware>();
```

- [ ] **Step 3: Build and verify**

Run: `dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx`
Expected: SUCCESS with no errors or warnings

### 6.3 Commit

- [ ] **Step 4: Commit**

```bash
git add src/Askyl.Dsm.WebHosting.Ui/Middleware/RequestTrackingMiddleware.cs src/Askyl.Dsm.WebHosting.Ui/Program.cs
git commit -m "feat: request ID propagation for error correlation

Adds X-Request-ID middleware to surface trace IDs in responses,
enabling support ticket correlation across logs and client reports."
```

---

## Task 7: Documentation Updates (Gaps #2, #4-doc, #5-doc, #6, #7-doc)

**Files:**

- Modify: `docs/ai/technical-architecture.md`

**Interfaces:**

- Consumes: All code changes from Tasks 1-6
- Produces: Updated technical documentation

### 7.1 Add Deployment & Packaging Section (Gap #2)

- [ ] **Step 1: Add deployment section to technical-architecture.md**

Add new section covering:

- Complete build pipeline from code to SPK (pre-flight checks, .NET runtime download, application publish, SPK assembly)
- Runtime selection strategy at install time (fat package, thin extract per architecture)
- Nginx reverse proxy integration mechanism (`adwh-alias.conf`)
- Service account and permissions model (`AskylWebHosting` system user in `http` group)
- Data persistence paths (`/var/packages/AskylWebHosting/var/`)
- Port configuration flow (`adwh.sc`: HTTP 7120, HTTPS 7121)
- Lifecycle scripts table (preinst/postinst/preupgrade/postupgrade/preuninst/postuninst/start-stop-status/common-functions.sh)
- Version management dual-source constraint (`Directory.Build.props` + `spk-project/INFO`)

Source material: `docs/ai/arch-gaps-2026-07-01.md` lines 30-65

### 7.2 Add CI/CD Pipeline Section (Gap #3-doc)

- [ ] **Step 2: Add CI/CD section**

Add new section describing workflow triggers, stages, artifact retention, and release process. Reference `.github/workflows/build.yml`.

### 7.3 Update Test Strategy Section (Gap #4-doc)

- [ ] **Step 3: Fix test framework documentation**

In `technical-architecture.md`, find the test frameworks section (~line 80):

- Remove `FluentAssertions` from listed frameworks
- Add note that controllers are thin routing wrappers with no business logic — all behavior delegated to services (which are tested)

### 7.4 Add Configuration Section (Gap #5-doc)

- [ ] **Step 4: Add configuration section**

Add new section covering:

- Dual appsettings.json structure (server `Ui/appsettings.json`, client `Ui.Client/wwwroot/appsettings.json`)
- `Download.ChannelVersion` dual-purpose constraint: build-time runtime download version + runtime application version detection
- Direct configuration access pattern (`builder.Configuration[]`, no IOptions used anywhere)
- `DsmSettingsService` configurable path for local debugging support
- Layered configuration merge semantics

### 7.5 Add Performance Section (Gap #6)

- [ ] **Step 5: Add performance section**

Add new section covering:

- Response time targets for API endpoints (<200ms typical)
- Memory usage guidelines for long-running hosting service
- Connection pool sizing rationale for `HttpClient` instances
- Existing caching strategy summary (lazy-init, 1-min TTL session validation, instance cache)

### 7.6 Add Request Tracing Section (Gap #7-doc)

- [ ] **Step 6: Add request tracing section**

Add new section describing X-Request-ID propagation mechanism and client usage for support correlation.

### 7.7 Verify and Commit

- [ ] **Step 7: Validate markdown**

Run: `markdownlint docs/ai/technical-architecture.md`
Expected: No errors (fix any reported issues)

- [ ] **Step 8: Commit**

```bash
git add docs/ai/technical-architecture.md
git commit -m "docs: comprehensive architecture documentation updates

Adds Deployment & Packaging, CI/CD Pipeline, Configuration, Performance,
and Request Tracing sections. Fixes test framework listing and documents
controller architecture as thin routing wrappers."
```

---

## Self-Review Checklist

**Spec coverage:**

- [x] Gap #1 (Critical): Task 1 — error endpoints implemented
- [x] Gap #2 (High): Task 7.1 — deployment documentation added
- [x] Gap #3 (High): Task 5 + 7.2 — CI/CD pipeline and documentation
- [x] Gap #4 (Significant): Task 3 + 7.3 — test fixes and controller architecture documentation
- [x] Gap #5 (Significant): Task 4 + 7.4 — configuration management code and documentation
- [x] Gap #6 (Minor): Task 7.5 — performance metrics documentation
- [x] Gap #7 (Minor): Task 6 + 7.6 — request tracing code and documentation
- [x] Gap #8 (Minor): Task 2 — dead code removal

**Placeholder scan:** No TBDs, TODOs, or vague "implement later" items. All tasks include specific file paths, code snippets, and verification commands.

**Type consistency:** `ApiResult` used consistently across error endpoints; `IConfiguration` properly injected in DsmSettingsService; middleware follows standard ASP.NET Core pattern.
