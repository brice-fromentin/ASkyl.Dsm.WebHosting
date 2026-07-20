# ASkyl.Dsm.WebHosting - Technical Architecture Document

**Target Framework:** .NET 10 (net10.0)
**Last Updated:** July 2, 2026

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Solution Overview](#solution-overview)
3. [Project Architecture](#project-architecture)
4. [Design Patterns & Principles](#design-patterns--principles)
5. [Technical Stack](#technical-stack)
6. [Data Models & API Integration](#data-models--api-integration)
7. [UI Architecture](#ui-architecture)
8. [Security Considerations](#security-considerations)
9. [Globalization & Localization](#globalization--localization)
10. [Performance Optimization](#performance-optimization)
11. [Request Tracing](#request-tracing)
12. [Deployment & Packaging](#deployment--packaging)
13. [Build and Release Pipeline](#build-and-release-pipeline)
14. [Configuration Management](#configuration-management)
15. [Appendix](#appendix)

---

## Executive Summary

**ASkyl.Dsm.WebHosting** is a comprehensive web-based management solution for .NET web applications on Synology DSM 7.2+ devices. The application provides:

- Web application lifecycle management (start/stop/restart)
- Reverse proxy configuration via Synology's API
- File system operations through FileStation API
- Framework/runtime installation and management
- Centralized logging with Serilog
- Immutable C# record types for DSM API models with `init` setters

The solution follows modern .NET 10 best practices, utilizing Blazor Hybrid architecture (Interactive WebAssembly), FluentUI components, and a clean layered architecture pattern.

**Key Architectural Decisions:**

- **Hybrid Rendering Mode:** Server-side authentication with WebAssembly interactive components
- **Result Pattern:** Strongly-typed success/failure results instead of exceptions for control flow
- **C# Records (init setters):** DSM API model classes converted from source-generated clone methods to immutable records
- **Centralized Constants:** All magic strings/numbers extracted to dedicated Constants project
- **Background Service:** WebSiteHostingService orchestrates website instances; per-site process lifecycle delegated to SiteLifecycleManager (SIGTERM graceful shutdown, force kill fallback)
- **Cross-platform Process Termination:** `ProcessTerminator` sends SIGTERM on Unix/Linux/macOS (P/Invoke `libc.kill`) and CloseMainWindow on Windows — enables ~1-3 second graceful drain

**Current Status:**

- ✅ Blazor Server + Interactive WebAssembly hybrid rendering
- ✅ DSM API integration (Authentication, FileStation, ReverseProxy)
- ✅ Website lifecycle management with process control
- ✅ JSON-based configuration persistence
- ✅ Infrastructure services refactored to DI-based architecture
- ✅ Smart caching strategy for expensive operations (VersionsDetectorService with lazy initialization)
- ✅ Full CancellationToken support across all async operations
- ✅ All static classes converted to injectable services for testability
- ✅ Critical security issues resolved (all security phases complete)
- ✅ SIGTERM process termination fix (cross-platform `ProcessTerminator`)
- ✅ Unit test implementation
- ✅ IProcessRunner abstraction for SiteLifecycleManager — co-located interface + implementation
- ✅ LoggerMessage migration — source-generated `[LoggerMessage]` extension methods across all services
- ✅ Runtime detection — `AssemblyRuntimeDetector` parses `*.runtimeconfig.json`
- ✅ Session validation — async authorization filter validates against DSM server with 1-minute TTL cache
- ⏳ Certificate management for reverse proxy
- ⏳ Multi-language end-to-end testing

**Status:** Production-ready

---

## Solution Overview

### Solution Structure

```text
Askyl.Dsm.WebHosting.slnx
├── Askyl.Dsm.WebHosting.Analyzers          # Custom Roslyn analyzers (ADWH01001-03001)
├── Askyl.Dsm.WebHosting.Constants          # Centralized constants & enums
├── Askyl.Dsm.WebHosting.Data               # Core data layer, API definitions, services
├── Askyl.Dsm.WebHosting.Globalization      # Localization resources, validators, culture management
├── Askyl.Dsm.WebHosting.Logging            # Logging extensions (source-generated log methods)
├── Askyl.Dsm.WebHosting.Tools              # Utility tools & DSM API client
├── Askyl.Dsm.WebHosting.Tests              # Unit tests (xUnit, Moq)
├── Askyl.Dsm.WebHosting.Ui                 # Main Blazor Server-WASM hybrid UI
└── Askyl.Dsm.WebHosting.Ui.Client          # Blazor WebAssembly client library
```

### Key Characteristics

- **Multi-project solution** with clear separation of concerns
- **Custom Roslyn analyzers** for enforcing project-specific code standards (String/String pattern, Logger calls, blank lines)
- **Source generators** for reducing boilerplate code (Serilog logging methods)
- **Hybrid rendering mode** (InteractiveServer + InteractiveWebAssembly)
- **Background services** for long-running operations
- **Centralized versioning** via Directory.Build.props

### Test Project (`Askyl.Dsm.WebHosting.Tests`)

**Purpose:** Unit tests for analyzers, domain models, globalization, tools, and UI services. 45 test files across 5 categories.

**Frameworks:** xUnit (v2.9.3), Moq (v4.20.72), coverlet (code coverage), bunit (BunitContext). Analyzer testing via Microsoft.CodeAnalysis.Analyzer.Testing ecosystem (v1.1.x).

**Test organization by subsystem:**

- **Analyzers/** — 3 tests: BlankLineAnalyzer, LoggerDirectCallAnalyzer, StringStaticMemberAnalyzer
- **Data/Domain/** — 5 tests: LoginCredentials, AspNetCoreReleaseInfo, AspNetRelease, WebSiteConfiguration, WebSiteInstance
- **Data/Results/** — 2 tests: result types + serialization
- **Globalization/** — 8 tests: AcceptLanguageHandler, CultureInfoExtensions, CultureManager,
  DeferredMessageExtensions, GlobalizationServiceCollectionExtensions, GlobalizationSettings, Localizer, ResourceCompleteness
- **Tools/** — 11 tests across Converters (language/date format), Diagnostics (OperationTimer),
  Extensions, Infrastructure (ArchiveExtractor, DsmSettingsService, FileManagerService),
  Network (DsmApiClient), Runtime (AssemblyRuntimeDetector, VersionsDetectorService), Threading (SemaphoreLock)
- **Ui/Services/** — 13 tests: AuthenticationNavigationGuard, AuthenticationService, DotnetVersionService,
  DsmSession, FileSystemService, FrameworkManagementService, LicenseService, LogDownloadService,
  ReverseProxyManagerService, SiteLifecycleManager, TreeContentService, WebSiteHostingService, WebSitesConfigurationService

**Design:** controllers are thin routing wrappers with no business logic — all behavior delegated to services which are tested directly.  
bunit is referenced for `BunitContext` usage in navigation guard tests; no Blazor component rendering tests currently exist.

### Build Configuration

All projects share common build settings from `Directory.Build.props`:

```xml
<EnableNETAnalyzers>true</EnableNETAnalyzers>
<AnalysisLevel>latest</AnalysisLevel>
<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
<RunAnalyzersDuringBuild>true</RunAnalyzersDuringBuild>
<EnablePreviewFeatures>true</EnablePreviewFeatures>  <!-- C# 14 scoped extension keyword -->
```

**Analyzer Packages:**

- **Roslynator.Analyzers** - Enhanced code style enforcement
- **Roslynator.Formatting.Analyzers** - Formatting rules
- **Askyl.Dsm.WebHosting.Analyzers** - Custom analyzers (ADWH01001-03001)

**.editorconfig Rule Severities:**

| Category | Rule ID | Severity | Purpose |
|----------|---------|----------|---------|
| Collection Expression | dotnet_style_prefer_collection_expression | error | Prefer `[..]` over `.ToList()`/`.ToArray()` |
| String/String Pattern | IDE0049 | error | Use `string` for types, `String.` for static methods |
| Primary Constructors | IDE0290, dotnet_style_primary_constructors | warning | MANDATORY for classes with parameters |
| Magic String Prevention | IDE0280 | warning | Use `nameof()` instead of string literals |
| Var Usage | dotnet_style_var_for_built_in_types | error | Use explicit types for built-in types |
| Var When Apparent | dotnet_style_var_when_type_is_apparent | warning | Use `var` when type is obvious |
| Cleanup | IDE0005 | warning | Remove unnecessary using directives |

### Nullable Reference Types

All projects have `<Nullable>enable</Nullable>`. Blazor `@inject` and constructor-injected services do NOT require null-forgiving operators (`!`) — DI container guarantees non-null instances.

**Standardized Build Command:**

```bash
dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx
```

---

## Project Architecture

### 1. Askyl.Dsm.WebHosting.Analyzers

**Purpose:** Custom Roslyn analyzers for enforcing project-specific code standards

**Target:** `netstandard2.0` (DevelopmentDependency, no build output)

| Analyzer | ID | Severity | Purpose | Code Fix |
|----------|-----|----------|---------|----------|
| `BlankLineAnalyzer` | ADWH01001/01002 | Error | Blank lines before/after control flow | ✅ |
| `StringStaticMemberAnalyzer` | ADWH02001 | Error | `String.` for static, `string` for types | ✅ |
| `LoggerDirectCallAnalyzer` | ADWH03001 | Error | No direct `ILogger.LogXxx()` calls | ❌ |

**Files:** `AnalyzerConstants.cs`, `BlankLineAnalyzer.cs`, `BlankLineCodeFixProvider.cs`,
`StringStaticMemberAnalyzer.cs`, `StringStaticMemberCodeFixProvider.cs`, `LoggerDirectCallAnalyzer.cs`,
`Askyl.Dsm.WebHosting.Analyzers.cs` (assembly definition), `Resources.resx` + `Resources.Designer.cs`

### 2. Askyl.Dsm.WebHosting.Constants

**Purpose:** Centralized constants, defaults, and enums for the entire solution. Eliminates magic strings and numbers across all projects.

**Organization by domain:**

- **Application/** — app paths, URLs, HTTP client names, session keys (DsmSid, DsmUsername), security headers, validation messages, website lifecycle defaults
- **DSM/API/** — DSM API names, methods, version ranges, error codes, PHP→.NET format token mappings, serialization formats (Form/Json enum)
- **DSM/FileStation/** — FileStation listing defaults, sorting, file type enum (File/Directory)
- **DSM/System/** — DSM 3-letter language code data, config paths, external ports
- **Globalization/** — default culture, text direction (LTR/RTL), environment variable names
- **JSON/** — static `JsonSerializerOptions` cache (camelCase, ignore nulls)
- **Logging/** — EventId range bases for `[LoggerMessage]` extensions (100K ranges at 1M spacing)
- **Network/** — cookie headers, localhost addresses, MIME types, protocol type enum (HTTP/HTTPS)
- **Runtime/** — .NET framework type strings, architecture identifiers (x64/arm/arm64), OS identifiers (linux/osx/windows)
- **UI/** — dialog dimensions, byte calculation constants (KiB/MiB/GiB)
- **WebApi/** — route constants per controller (`/api/v1/authentication/*`, `/api/v1/websites/*`, etc.)

**Rule:** Any hardcoded string or number used in more than one place belongs here. New domain? Add a subdirectory.

### 3. Askyl.Dsm.WebHosting.Data

**Purpose:** Core data layer, API definitions, domain services, and result types

**Complete Service Contracts Inventory:**

| Interface | Key Methods | Implemented By |
|-----------|-------------|----------------|
| **IAuthenticationService** | LoginAsync(), LogoutAsync(), IsAuthenticatedAsync() | Ui + Ui.Client |
| **ICultureManager** | InitializeFromLogin(string? culture, string? dateFormat, string? timeFormat), ResetToSystem(), CurrentCulture, CurrentUICulture | Ui.Client.CultureManager |
| **IDotnetVersionService** | GetInstalledVersionsAsync(), GetChannelsAsync(), IsChannelInstalledAsync(), IsVersionInstalledAsync(), GetReleasesWithStatusAsync(), RefreshCacheAsync(), IsValidVersionFormat() | Ui + Ui.Client |
| **IFileSystemService** | GetSharedFoldersAsync(), GetDirectoryContentsAsync(), SetHttpGroupPermissionsAsync() | Ui + Ui.Client |
| **IFrameworkManagementService** | InstallFrameworkAsync(), UninstallFrameworkAsync() | Ui.Services |
| **IGlobalizationSettings** | SupportedCultures, SupportedCultureNamesJson, SystemCulture | Ui.Infrastructure.GlobalizationSettings |
| **ILogDownloadService** | CreateLogZipStreamAsync() | Ui.Services |
| **IReverseProxyManagerService** | CreateAsync(), UpdateAsync(), DeleteAsync() | Ui.Services |
| **IWebSiteHostingService** | GetAllWebsitesAsync(), AddWebsiteAsync() | Ui + Ui.Client |
| **IFileManagerService** | Initialize(), GetDirectory(), DeleteDirectory(), GetFullName(string directory, string file) | Tools.Infrastructure |
| **IArchiveExtractorService** | Decompress(inputFile, exclude) | Tools.Infrastructure |
| **IDownloaderService** | DownloadVersionToAsync(), GetAspNetCoreReleasesAsync(), GetAspNetCoreChannelsAsync() | Tools.Runtime |
| **IVersionsDetectorService** | GetInstalledVersionsAsync(), RefreshCacheAsync(), IsChannelInstalled(), IsVersionInstalled() | Tools.Runtime (Singleton) |
| **IAssemblyRuntimeDetector** | Detect(string assemblyPath) | Tools.Runtime (Singleton) |
| **IDsmSession** | ConnectAsync(LoginCredentials, CancellationToken), ValidateSessionAsync(), ExecuteAsync(), ExecuteSimpleAsync(), Disconnect(); properties: UserLanguage, UserDateFormat, UserTimeFormat | Ui.Services.DsmSession |
| **IDsmSettingsService** | Server, Port, Language | Tools.Infrastructure |
| **ILicenseService** | GetLicensesAsync() → IReadOnlyList&lt;LicenseInfo&gt; | Ui.Client.Services |
| **ITreeContentService** | LoadChildDirectoriesAsync(string path, Func&lt;string, Task&gt; errorHandler, Func&lt;string, Task&lt;List&lt;TreeViewItem&gt;&gt;&gt; loadChildrenAsync) | Ui.Client.Services |

**Structure:**

- **Contracts/** — service interfaces shared between server and WASM client. 16 interfaces defining the boundary
  between Data (contracts) and Ui/Tools (implementations). See "Complete Service Contracts Inventory" table above for full method signatures.
- **Domain/** — model classes organized by subsystem: Authentication (login credentials), FileSystem (FsEntry),
  Licensing (license info), Runtime (.NET framework/release models), System (DSM preferences from synoinfo.conf),
  WebSites (website configuration, instances, process state). New domain? Add a subdirectory.
- **DsmApi/** — DSM API integration layer:
  - **Models/** — immutable C# records with `init` setters for every DSM API type, organized by API namespace
    (Auth, Core/Acl, Core/User, Core/UserSettings, FileStation, ReverseProxy)
  - **Parameters/** — request parameter classes mirroring Models structure; inherit from `ApiParametersBase` or implement
    `IApiParameters`; serialization format determined by `SerializationFormat` property (Form vs Json strategy pattern)
  - **Responses/** — response wrappers per API endpoint inheriting from `ApiResponseBase<T>`  
    with embedded error model.
- **Results/** — strongly-typed success/failure types replacing exceptions for control flow.  
  Generic variants (`ApiResultData<T>`, `ApiResultItems<T>`) and domain-specific results (InstallationResult, WebSiteInstanceResult, etc.).
- **Exceptions/** — 4 custom exception types for unrecoverable failures: FileStationApiException, LastReleaseUninstallException, MissingChannelConfigurationException, ReverseProxyNotFoundException

### 4. Askyl.Dsm.WebHosting.Globalization

**Purpose:** Localization resources, shared validators, culture management, C# 14 scoped extensions.

**Structure:**

- **Extensions/** — C# 14 scoped `extension` methods: `CultureInfo.GetTextDirection()` for RTL support,
  `IServiceCollection.AddGlobalization()` for DI registration
- **Resources/** — `SharedResource.resx` (English default) + culture-specific variants (`fr-FR`, etc.).  
  Adding a new culture = dropping a `.resx` file;  
  SDK auto-generates satellite assemblies; zero code changes needed.
- **Validators/** — FluentValidation shared validators with deferred message resolution  
  (`WithLocalizedMessage()` resolves keys at validation time, not construction).  
  Covers login credentials and website configuration rules.
- **Localizer.cs** — `ILocalizer` abstraction wrapping `ResourceManager`; returns `string` directly,  
  reads `CurrentUICulture` at call time (not cached at construction like `IStringLocalizer<T>`).
- **LocalizationKeys.cs** — strongly-typed resource keys (`L.WebSiteConfiguration.*`, `L.LoginCredentials.*`)

**Key design decisions:** shared validators are single source of truth (server auto-validation uses same FluentValidation rules); no DataAnnotations (cannot use runtime-localized messages).

### 5. Askyl.Dsm.WebHosting.Tools

**Purpose:** Utility services, DSM API client, and runtime management tools.

**Structure:**

- **Converters/** — format/language converters: DSM 3-letter language code → .NET culture name, PHP date/time tokens → .NET format strings.
- **Extensions/** — C# 14 scoped `extension` methods on `ApiResponse` (mapping helpers) and `HttpClient` (HTTP client helpers).
- **Infrastructure/** — core utilities: archive extraction (tar.gz), file management with configurable root path,  
  platform detection, process lifecycle (`IProcessRunner`/`IProcessHandle` co-located with implementations for testability),  
  cross-platform termination (SIGTERM on Unix, CloseMainWindow on Windows), file system abstraction (`IFileReader`/`SystemFileReader`),  
  DSM settings service (reads `/etc/synoinfo.conf`).
- **Network/** — `DsmApiClient`: centralized HTTP client for all DSM API calls;  
  singleton with lazy-initialized `ApiInformations`,  
  compile-time generic constraints, Form vs JSON serialization strategy.
- **Diagnostics/** — `OperationTimer`: disposable scope timer (`struct`) that fires callback on Dispose (success or exception);  
  used across ReverseProxyManagerService, FrameworkManagementService, WebSiteHostingService, SiteLifecycleManager, DownloaderService.
- **Runtime/** — .NET runtime management: binary downloads with cancellation, version detection with smart caching (singleton), assembly runtime detection from `*.runtimeconfig.json`
- **Threading/** — `SemaphoreLock`: semaphore-based async locking utility for thread-safe lazy initialization

**Infrastructure Services:**

| Service | Interface | Lifetime | Key Features | Dependencies |
|---------|-----------|----------|--------------|--------------|
| **PlatformInfoService** | _(none)_ | Singleton | Platform detection, config loading | ILogger |
| **FileManagerService** | `IFileManagerService` | Scoped | Directory management, configurable root | ILogger, string rootPath |
| **ArchiveExtractorService** | `IArchiveExtractorService` | Scoped | tar.gz extraction | IFileManagerService |
| **DownloaderService** | `IDownloaderService` | Scoped | .NET runtime downloads with cancellation | PlatformInfoService, IFileManagerService |
| **VersionsDetectorService** | `IVersionsDetectorService` | Singleton | Smart caching for dotnet --info | ILogger, ISemaphoreOwner |
| **SystemProcessRunner** | `IProcessRunner` | Singleton | Spawns OS processes | ILogger, ILoggerFactory |
| **SystemProcessHandle** | `IProcessHandle` | Transient | Wraps `Process` for testability | ILogger<ILogSystemProcessHandle> |
| **DsmSettingsService** | `IDsmSettingsService` | Singleton | Reads /etc/synoinfo.conf via IFileReader | ILogger, IFileReader |

**DsmApiClient Key Features:**

- Singleton, implements `ISemaphoreOwner` for thread-safe lazy `ApiInformations` initialization
- `ExecuteAsync<R>` with compile-time `where R : IApiResponse` constraint (no reflection)
- Strategy pattern for Form vs JSON serialization
- Structured logging: request timing, auth failures, API errors via `[LoggerMessage]` extensions

**Process Lifecycle:** `SystemProcessRunner` requires `ILoggerFactory` to create correctly-typed child loggers for `SystemProcessHandle` instances (distinct closed generic types cannot be cast).

### 6. Askyl.Dsm.WebHosting.Ui

**Purpose:** Main Blazor hybrid application (Server + WebAssembly rendering). Entry point, DI registration, middleware pipeline, API controllers, and server-side business logic services.

**Structure:**

- **Authorization/** — `[AuthorizeSession]` attribute: session-based authorization for API controllers; validates against DSM server with 1-minute TTL cache.
- **Controllers/** — thin routing wrappers (Authentication, FileManagement, FrameworkManagement, LogDownload,  
  RuntimeManagement, WebsiteHosting). No business logic — all delegated to services.  
  Protected by `[AuthorizeSession]` except AuthenticationController.
- **Endpoints/** — minimal API endpoints: `MapErrorEndpoints()` maps `/Error` and `/not-found` with JSON vs HTML content negotiation.
- **Extensions/** — server-side globalization extensions: `ApplyDsmSystemCulture()`, `UseGlobalizationRequestLocalization()`.
- **Infrastructure/** — `GlobalizationSettings`: discovers supported cultures from satellite assemblies at construction (server-only; avoids WASM file system API crashes).
- **Middleware/** — `RequestTrackingMiddleware`: propagates `X-Request-ID` through HTTP pipeline via `HttpContext.Items` for support ticket correlation.
- **Services/** — business logic implementations of Data.Contracts interfaces: authentication façade, file system operations,  
  framework management, reverse proxy CRUD, website hosting orchestrator (BackgroundService with ConcurrentDictionary),  
  per-site lifecycle manager (Channel-based command queue, SIGTERM graceful shutdown),  
  DSM session management (per-user, 1-min TTL cache).

**Middleware pipeline:** `UsePathBase("/adwh")` → `UseSession()` → `UseRouting()` + `MapControllers()` → `UseAntiforgery()` → `MapRazorComponents` with InteractiveWebAssembly render mode.

### 7. Askyl.Dsm.WebHosting.Ui.Client

**Purpose:** Blazor WebAssembly client library (shared components and HTTP service proxies).

**Structure:**

- **Components/Controls/** — custom UI controls: `AutoDataGrid` (generic data grid with sorting, reload, row click/double-click),  
  `LoadingOverlay` (full-screen overlay for WorkingState disposable pattern),  
  `RealTimeNumberField` and `RealTimeTextField` (numeric/text input with real-time validation).
- **Components/Dialogs/** — FluentUI dialog wrappers: AspNetReleases (channel selection, version grid, install/uninstall),  
  DotnetVersions (installed frameworks display), FileSelection (dual-pane file browser with lazy loading),  
  Licenses (tabbed viewer with parallel HTTP fetches), WebSiteConfiguration (add/edit website form).
- **Components/Layout/** — `MainLayout`: FluentMainLayout with global providers (Toast, Dialog, Tooltip).
- **Components/Pages/** — Home (dashboard with website grid), Login (authentication form), NotFound (404 handler).
- **Components/Patterns/WorkingState/** — 3-class system: `WorkingStateBase` (abstract base),  
  `WorkingState` (disposable wrapper for start/stop transitions),  
  `WorkingStateExtensions` (`CreateWorkingState()` extension method). No interface.
- **Contracts/** — `INavigationGuard`: router navigation guard interface for async auth checks before component render.
- **Interfaces/** — client-side service interfaces: `ILicenseService` (GetLicensesAsync),  
  `ITreeContentService` (LoadChildDirectoriesAsync for FluentTreeView lazy loading).
- **Extensions/** — C# 14 scoped extensions on `List<FsEntry>`/`FsEntry` → TreeViewItem conversion with lazy loading.
- **Services/** — HTTP client wrappers that call server API endpoints: authentication, file system, framework management,  
  runtime versions, website hosting. Plus `AcceptLanguageHandler` (DelegatingHandler attaches Accept-Language from ICultureManager),  
  `AuthenticationNavigationGuard` (Router OnNavigateAsync guard),  
  `CultureManager` (resolves culture at login, clones with date/time formats).
- **Routes.razor** — Router with OnNavigateAsync auth guard
- **Program.cs** — WASM entry point, service registration

**JavaScript interop:** single usage in FileSelectionDialog — `selectChildItem` for tree navigation after folder double-click.

### 8. Askyl.Dsm.WebHosting.Logging

**Purpose:** Logging extensions with source-generated `[LoggerMessage]` logger methods.

Enforced by `LoggerDirectCallAnalyzer` (ADWH03001) — no direct `ILogger.LogXxx()` calls allowed.

**Key features:** compile-time message validation, zero-allocation logging for performance-critical paths,  
namespace-level category interfaces (`ILogAuthenticationService`, etc.) for `ILogger<T>` categorization,  
server/client folder separation.

**Structure:**

- **Server/** — one extension file per service domain, organized by subsystem:
  - _Authentication/_ — AuthenticationService
  - _DsmApi/_ — DsmApiClient + DsmSession (2 files)
  - _FileManagement/_ — FileManagerService, FileSystemService, LogDownloadService (3 files)
  - _Framework/_ — DotnetVersionService, FrameworkManagementService (2 files)
  - _Infrastructure/_ — ArchiveExtractor, AssemblyRuntimeDetector, Downloader, DsmSettingsService, GlobalizationSettings, PlatformInfo, VersionsDetector (7 files)
  - _ProcessLifecycle/_ — ProcessHandle, SiteLifecycleManager, ProcessRunner (3 files)
  - _ReverseProxy/_ — ReverseProxyManagerService (1 file)
  - _WebsiteHosting/_ — WebSitesConfigurationService, WebSiteHostingService (2 files)
- **Client/** — `ClientLoggingExtensions.cs`: WASM-side logging for Home page, dialogs, license service

**Naming convention:** `{ServiceName}LoggingExtensions.cs`.

New service? Add a `[LoggerMessage]` extension method with XML doc comment; consult `Constants/Logging/LogEventIds.cs` for next available EventId in the service's range.

**EventId Management:**

All `[LoggerMessage]` attributes use inline `int` literals. EventId ranges documented in `Constants/Logging/LogEventIds.cs`.  
Each service owns a 100K range at 1M spacing:

| Range | Service | Extension File |
|-------|---------|----------------|
| `1000001–1000007` | AuthenticationService | `AuthenticationLoggingExtensions.cs` |
| `1100001–1100012` | FileSystemService | `FileSystemServiceLoggingExtensions.cs` |
| `1200001–1200006` | FileManagerService | `FileManagerServiceLoggingExtensions.cs` |
| `1300001–1300007` | LogDownloadService | `LogDownloadServiceLoggingExtensions.cs` |
| `1400001–1400007` | FrameworkManagementService | `FrameworkManagementLoggingExtensions.cs` |
| `1500001–1500007` | DotnetVersionService | `DotnetVersionServiceLoggingExtensions.cs` |
| `1600001–1600019` | SiteLifecycleManager | `ProcessLoggingExtensions.cs` |
| `1700001–1700014` | ReverseProxyManagerService | `ReverseProxyLoggingExtensions.cs` |
| `1800001–1800031` | WebSiteHostingService | `WebsiteLoggingExtensions.cs` |
| `1900001–1900012` | WebSitesConfigurationService | `ConfigurationLoggingExtensions.cs` |
| `2000001–2000013` | DsmApiClient | `DsmApiLoggingExtensions.cs` |
| `2100001–2100006` | ArchiveExtractorService | `ArchiveExtractorLoggingExtensions.cs` |
| `2200001–2200004` | VersionsDetectorService | `VersionsDetectorLoggingExtensions.cs` |
| `2250001–2250005` | AssemblyRuntimeDetector | `AssemblyRuntimeDetectorLoggingExtensions.cs` |
| `2300001–2300002` | PlatformInfoService | `PlatformInfoLoggingExtensions.cs` |
| `2400001–2400004` | DownloaderService | `DownloaderLoggingExtensions.cs` |
| `2500001` | SystemProcessRunner | `ProcessRunnerLoggingExtensions.cs` |
| `2600001–2600005` | SystemProcessHandle | `ProcessHandleLoggingExtensions.cs` |
| `2700001–2700004` | GlobalizationSettings | `GlobalizationSettingsLoggingExtensions.cs` |
| `2800001–2800005` | DsmSettingsService | `DsmSettingsServiceLoggingExtensions.cs` |
| `2900001–2900007` | DsmSession | `DsmSessionLoggingExtensions.cs` |
| `7000001` | LicenseService (WASM) | `ClientLoggingExtensions.cs` |
| `7100001` | Client utilities (JS interop) | `ClientLoggingExtensions.cs` |
| `7600001–7600010` | CultureManager (client) | `ClientLoggingExtensions.cs` |

**Total:** All services use `[LoggerMessage]` extensions — 24 EventId ranges across server and client. Zero CA2254 warnings.

**Serilog Configuration:**

- Output template: `{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [EventId:{EventId}] {Message:lj}{NewLine}{Exception}`
- Graceful flush: `Log.CloseAndFlush()` via `ApplicationStopping` lifetime hook
- Activity correlation: `WithActivity` enricher adds `ActivityId`, `ActivityTraceId`, `ActivitySpanId`

---

## Design Patterns & Principles

### 1. Dependency Injection (DI)

**Patterns Used:**

- **Singleton:** DsmApiClient, PlatformInfoService, VersionsDetectorService, WebSiteHostingService, IAssemblyRuntimeDetector
- **Scoped:** FileManagerService (factory lambda for root path), ArchiveExtractorService, DownloaderService, UI services
- **Background Service:** WebSiteHostingService implements IHostedService

**Architectural Trade-off — Singleton `DsmApiClient`:**

`DsmApiClient` is a pure HTTP client with no per-session state (SID passed per-call via `HttpRequestMessage` cookie). Singleton because:

1. **Shared `ApiInformations`:** API metadata cached via lazy-init with `SemaphoreLock` — fetched once, forever
2. **`HttpClient` reuse:** Named client with connection pooling
3. **`BackgroundService` anchor:** `WebSiteHostingService` (Singleton) depends on services using `DsmApiClient`

**Mitigation:** `SetSid()` updates `_sid` + cookie header. Session validation cache: 1-minute TTL.

**Service Lifetime Hierarchy:**

```text
Singleton
├── DsmApiClient
├── PlatformInfoService
├── VersionsDetectorService (smart caching)
├── AssemblyRuntimeDetector
└── WebSiteHostingService (BackgroundService)
    └── SiteLifecycleManager (per-instance)

Scoped
├── FileManagerService (factory with root path)
│   ├── ArchiveExtractorService
│   └── DownloaderService
│       ├── DotnetVersionService
│       └── FrameworkManagementService
├── AuthenticationService
└── LogDownloadService
```

### 2. Result Pattern

Strongly-typed success/failure results instead of exceptions for control flow. Eliminates null checks, provides cleaner UI error handling.

### 3. Repository/Service Facade Pattern

```text
Contracts (Data layer)          →  Implementations (Ui.Services)
─────────────────────────          ───────────────────────────┬───────
IWebSiteHostingService            WebSiteHostingService       │
IAuthenticationService            AuthenticationService        │ (Server-side)
IReverseProxyManagerService       ReverseProxyManagerService   │
IFileSystemService                FileSystemService            │
                                                                        ↓
                                                              DsmApiClient (Infrastructure)
```

### 4. Background Service Pattern

```text
WebSiteHostingService (BackgroundService, Singleton)
├── Orchestrates instances via ConcurrentDictionary<Guid, SiteEntry>
├── SiteEntry pairs WebSiteInstance + SiteLifecycleManager
├── Loads configurations from JSON on startup
├── Detects required framework on init (sets RequiredFramework — not persisted)
└── Delegates per-site process management to SiteLifecycleManager

SiteLifecycleManager (Per-instance, Thread-safe)
├── Starts/stops processes via IProcessRunner abstraction (unit-testable)
├── Validates framework compatibility on start
├── IProcessHandle? delegates to SystemProcessHandle
├── Configures environment variables (ASPNETCORE_URLS, ASPNETCORE_ENVIRONMENT, custom vars)
├── Graceful shutdown: ProcessTerminator (SIGTERM on Unix, CloseMainWindow on Windows)
├── Async WaitForExitAsync with linked cancellation token + timeout
├── Force kill fallback
└── Thread-safe via Channel-based command queue (eliminates TOCTOU races)
```

### 5. Strategy Pattern (Serialization)

`DsmApiClient.ExecuteAsync<R>` dispatches on `IApiParameters.SerializationFormat`:

- `Form` → `ExecuteFormAsync<R>`
- `Json` → `ExecuteJsonAsync<R>`

### 6. Disposable Scope Pattern (OperationTimer)

`OperationTimer` — value-type (`struct`) disposable timer in `Tools/Diagnostics/OperationTimer.cs`.

```csharp
using var timer = new OperationTimer(elapsed => logger.FrameworkInstalledDuration(elapsed, version));
// ... method body ... callback fires on Dispose (success or exception)
```

**Usage:** ReverseProxyManagerService (Create/Update/Delete), FrameworkManagementService (Install/Uninstall),
WebSiteHostingService (Add/Update/Start/Stop/Remove), SiteLifecycleManager, DownloaderService,
DotnetVersionService, WebSitesConfigurationService.

---

## Technical Stack

| Component | Technology | Purpose |
|-----------|------------|---------|
| **Runtime** | .NET 10 | Application framework |
| **UI Framework** | Blazor Hybrid (Interactive WebAssembly) | Server + client rendering |
| **UI Components** | FluentUI Blazor | Modern UI component library |
| **Logging** | Serilog | Structured logging |
| **HTTP Client** | Microsoft.Extensions.Http | HttpClient factory |
| **.NET Releases** | Microsoft.Deployment.DotNet.Releases | Version detection |
| **WASM Server** | Microsoft.AspNetCore.Components.WebAssembly.Server | Blazor WASM hosting |
| **Analyzer Rules** | Roslynator.Analyzers + Formatting.Analyzers | Code style enforcement |

---

## Data Models & API Integration

### Core Domain Models

- **WebSiteConfiguration** — main config model (name, path, port, SSL, environment variables)
- **WebSiteInstance** — runtime instance wrapping configuration + process lifecycle
- **ProcessInfo** — immutable process snapshot (Id, IsResponding) — captures at construction to avoid `InvalidOperationException` if process exits during serialization

### DSM API Integration

#### Authentication Flow

```text
1. Client → LoginCredentials { Username, Password, [LotP] }
2. DsmSettingsService → Load /etc/synoinfo.conf (graceful fallback defaults)
3. DsmApiClient.EnsureInitializedAsync() → SYNO.API.Info query (lazy-init, SemaphoreLock)
4. DsmSession.AuthenticateAsync() → auth.login API call
5. Response: SID stored per-request via HttpRequestMessage cookie header
6. Session persisted in ASP.NET Core session (DsmSid + DsmUsername)
```

#### Session Validation

`IsAuthenticatedAsync()` validates against DSM server to detect expired/revoked sessions:

1. Check local session keys (DsmSid + DsmUsername) exist
2. Check 1-minute TTL validation cache
3. If expired: call `SYNO.Core.User.get` with cached username
4. Error `-4` = invalid/expired SID → clear session keys, return false
5. Cache result for 1 minute

**API Choice:** `SYNO.API.Auth` only has `login`/`logout`. `SYNO.Core.User.get` is the lightest API that validates session state.

#### FileStation Operations

`util.list`, `util.upload`, `util.download`, `util.delete`, `util.mkdir`, `file.move`, `file.copy`, `core.acl.set`

**HTTP Group Permissions:** Uses `SYNO.Core.ACL` API to grant `http` group read/execute on deployment directories — called after framework installation.

---

## UI Architecture

### Rendering Strategy

**Hybrid Mode:** `AddRazorComponents().AddInteractiveWebAssemblyComponents()` — Server-side authentication + client-side interactivity.

### Component Hierarchy

```text
App.razor (Root — server-rendered shell)
├── FluentDesignTheme (System mode)
└── FluentLayout
    └── Routes (InteractiveWebAssembly)
        └── MainLayout.razor (FluentMainLayout with Header/Body)
            ├── Home.razor (Dashboard with website grid)
            ├── Login.razor (Authentication)
            └── NotFound.razor (404)

Dialogs (Overlay)
├── WebSiteConfigurationDialog.razor
├── FileSelectionDialog.razor
├── DotnetVersionsDialog.razor
├── AspNetReleasesDialog.razor
└── LicensesDialog.razor
```

### State Management

- **Server:** ASP.NET Core Session (DSM SID), `WebSiteHostingService` singleton, `WebSitesConfigurationService`
- **Client:** HTTP client wrappers, local component state, FluentUI Dialog state, WorkingStateBase/WorkingState disposable pattern (no interface — abstract base class + disposable wrapper)

---

## Security Considerations

### Authentication & Session Management

1. **Router-Level Navigation Guard** — `AuthenticationNavigationGuard` intercepts all navigation via `<Router OnNavigateAsync>`; async auth check before any component renders; no cached state
2. **Server-Side Session Storage** — DSM SID in server session (not client); HttpOnly cookies; SameSite=Strict
3. **Server-Side Session Validation** — `IsAuthenticatedAsync()` validates session keys + calls `SYNO.Core.User.get`; 1-minute TTL cache
4. **Antiforgery & CSRF Protection** — Enabled for all Blazor components and API endpoints
5. **HTTPS & HSTS Enforcement** — `UseHttpsRedirection()`, `UseHsts()` (30-day max-age non-dev)

### API Security

1. **Authorization Coverage** — `[AuthorizeSession]` on all API controllers; `AuthenticationController` intentionally public
2. **Input Validation** — Path traversal prevention (`IsPathValid()` rejects `..`), version format validation, environment variable limits (256 key, 4096 value)
3. **Rate Limiting** — Login: 5 attempts/minute/IP
4. **Error Handling** — Generic messages to clients; full details server-side via `[LoggerMessage]`
5. **No Client-Side Secrets** — All DSM API calls through server controllers

### File System Security

- HTTP group permissions set before deployment
- Path validation against allowed directories via `IsPathValid()`

---

## Globalization & Localization

### Architecture Overview

Culture is **DSM-controlled** — resolved once at login, locked for the session. No runtime switching.

### Culture Flow

1. **Server discovers cultures** — `GlobalizationSettings` scans satellite assembly directories at construction
2. **Server reads DSM culture** — `ApplyDsmSystemCulture()` extracts `language` from DSM, converts via `DsmLanguageToCultureConverter`
3. **Server injects to WASM** — Supported cultures as JSON + system culture via `Blazor.start()` `dotnet.withEnvironmentVariable()`
4. **WASM parses cultures** — `CultureManager` static initializer deserializes env vars
5. **Early resolution** — `Program.cs` forces DI resolution of `ICultureManager` before `host.RunAsync()`
6. **Login resolves culture** — Priority: login response `Culture` → system culture → browser culture → `en-US`
7. **WASM propagates to server** — `AcceptLanguageHandler` attaches `Accept-Language` header
8. **Server reads header** — `RequestLocalization` middleware sets thread culture per request
9. **Logout** — `forceLoad: true` resets culture to system/browser

**html lang:** Set server-side in `App.razor` via `GetLanguageTag()` (DSM system culture → Accept-Language header → `en`).

### Date/Time Format Flow

1. Server fetches `SYNO.Core.UserSettings.get` (best-effort, post-auth) — extracts `Personal.dateFormat`, `Personal.timeFormat`
2. `PhpFormatToDotNetConverter` converts PHP tokens to .NET format strings
3. `AuthenticationResult` carries `DateFormat`/`TimeFormat` to WASM
4. `CultureManager.InitializeFromLogin()` clones `CultureInfo`, overrides `DateTimeFormat` patterns
5. UI uses `Format="d"` / `Format="g"` — automatically respects user patterns

**Defensive:** `CultureNotFoundException`/`ArgumentException` → system culture fallback; `FormatException` → keep defaults, log warning.

### Culture Resolution Priority

**At construction (login page, post-logout):** DSM system culture → Browser culture → `en-US`
**After login:** Login response culture → DSM system culture → Browser culture → `en-US`

### Adding a New Culture

1. Add `SharedResource.<culture>.resx` to `Globalization/Resources/`
2. Build — SDK auto-generates satellite assembly
3. Server auto-discovers → injects to WASM
4. **Zero code changes needed**

### Key Design Decisions

- **`BlazorWebAssemblyLoadAllGlobalizationData`** — Required for dynamic culture changes at WASM startup
- **`CultureManager` updates `html lang` and `dir` via `IJSRuntime`** — Enables RTL support
- **`DsmLanguageToCultureConverter` returns `null` for `"def"`** — Means "use browser default", not English
- **`GlobalizationSettings` as singleton in Ui/Infrastructure/** — Server-only; avoids WASM file system API crashes
- **`IRequestCultureFeature` doesn't match neutral languages** (`fr` → `fr-FR`) — `GetLanguageTag()` parses header directly
- **Safe static initialization** — Each static field uses a `Safe*` wrapper catching `CultureNotFoundException`, `ArgumentException`, `JsonException`
- **`NotSupportedException` on pattern setters** — Defensive against rare immutable culture variants

---

## Performance Optimization

### Response Time Targets

- API endpoints targeting <200ms typical response time for local DSM operations
- FileStation list operations may exceed target depending on directory size
- Framework installation and runtime download are long-running operations with progress feedback via WorkingState disposable pattern

### Memory Usage Guidelines

- Long-running hosting service (`WebSiteHostingService`) maintains per-site state in `ConcurrentDictionary<Guid, SiteEntry>`
- Each site entry holds a `SiteLifecycleManager` instance with a Channel-based command queue
- Memory footprint scales linearly with number of managed websites; typical deployment manages <10 sites

### Connection Pool Sizing

- Single named `HttpClient` instance for DSM API calls via `DsmApiClient` (Singleton)
- Default connection pool sizing from .NET runtime defaults (2 connections per server)
- No custom `SocketsHttpHandler` configuration — relies on framework defaults for local DSM communication

### Caching Strategy

- **ApiInformations Cache:** Lazy-init with `SemaphoreLock` double-checked locking in `DsmApiClient`; fetched once, cached forever
- **Session Validation Cache:** 1-minute TTL for DSM session validation
- **Instance Cache:** In-memory `ConcurrentDictionary` for website instances
- **Configuration Cache:** JSON file read on startup, in-memory during runtime

---

## Request Tracing

### X-Request-ID Propagation

Serilog's `WithActivity` enricher adds `ActivityId`, `ActivityTraceId`, and `ActivitySpanId` to log entries. These correlate with .NET's built-in `System.Diagnostics.Activity` infrastructure.

**Current State:** `RequestTrackingMiddleware` propagates `X-Request-ID` through the HTTP pipeline via `HttpContext.Items`.
Serilog's `WithActivity` enricher captures `ActivityId`, `ActivityTraceId`, and `ActivitySpanId` in server-side logs.
The Blazor WebAssembly client does not include request ID headers on outgoing API calls, and the server does not expose trace identifiers in API responses for support ticket correlation.

**Pipeline Flow (when Activities are active):**

1. Incoming HTTP request creates `Activity` via ASP.NET Core hosting
2. Serilog enricher attaches `ActivityId`, `ActivityTraceId`, `ActivitySpanId` to log context
3. Outgoing DSM API calls inherit Activity scope via `HttpClient` diagnostics handler
4. All logs within the request scope share the same trace identifiers

**For Support Correlation:** Currently relies on timestamp + EventId correlation. Future enhancement could surface `X-Request-ID` in API response headers for client-side support ticket inclusion.

---

## Deployment & Packaging

### Build Pipeline

The SPK build pipeline (`src/scripts/build-spk.sh`) assembles the Synology package through four phases:

1. **Pre-flight Checks:** Verifies availability of `curl`, `tar`, `dotnet`, `jq`, `awk`, `pigz`
2. **.NET Runtime Download:** Reads `ChannelVersion` from `appsettings.json`, fetches Microsoft
   releases metadata, downloads aspnetcore-runtime for `linux-arm`, `linux-arm64`, `linux-x64`
   with SHA512 verification
3. **Application Publish:** Framework-dependent publish (`--self-contained false`) to `spk-project/package/admin-ui/`
4. **SPK Assembly:** Compresses via `pigz -2`, creates tar archive containing `INFO`, `package.tgz`, lifecycle scripts, configuration, and icons

### Runtime Selection Strategy

The SPK is a fat package containing runtimes for all three architectures. At install time,
the `postinst` script detects the NAS architecture and extracts only the matching runtime,
keeping the installed footprint minimal.

### Nginx Reverse Proxy Integration

`adwh-alias.conf` provides reverse proxy from `/adwh` to `localhost:7120`. The configuration
is injected into DSM's built-in Nginx via the package's `web-config` resource declaration in `INFO`.
All application traffic flows through this alias, enabling sub-path access without port conflicts.

### Service Account and Permissions

- Dedicated system user `AskylWebHosting` created during installation
- Member of `http` group for web server compatibility
- Defined in `conf/privilege` within the SPK structure
- Process spawning and file operations execute under this account

### Data Persistence

All persistent data resides under `/var/packages/AskylWebHosting/var/`:

- Website configurations (JSON)
- Application logs (Serilog rolling files)
- Downloaded .NET runtimes
- User-specific state

This path survives package upgrades per Synology's package data directory conventions.

### Port Configuration

`adwh.sc` defines the application listening ports:

| Protocol | Port | Purpose |
|----------|------|---------|
| HTTP | 7120 | Primary application port (proxied via Nginx `/adwh`) |
| HTTPS | 7121 | SSL-enabled alternative |

Port `7120` is declared in the SPK `INFO` file for conflict detection during installation.

### Lifecycle Scripts

| Script | Purpose |
|--------|---------|
| `preinst` | Environment setup, architecture detection |
| `postinst` | .NET runtime installation for detected architecture |
| `preupgrade` | Service stop, configuration backup |
| `postupgrade` | Configuration restore, runtime reinstall, service start |
| `preuninst` | Service stop, PID file cleanup |
| `postuninst` | Final cleanup |
| `start-stop-status` | Service lifecycle management with PID tracking |
| `common-functions.sh` | Shared utilities: logging, process management, runtime install/verify |

### Version Management

Dual sources of truth require manual synchronization:

- **`Directory.Build.props`:** Controls .NET assembly version and informational version
- **`spk-project/INFO`:** Controls SPK package version displayed in Package Center

Use `scripts/update-version.sh` to synchronize both simultaneously.

---

## Build and Release Pipeline

### Current State

Deployment is entirely manual: developer runs `build-spk.sh` locally, then copies the resulting `.spk` from `dist/` to the target NAS via Package Center.

### Planned Workflow

A GitHub Actions pipeline would operate with two job paths triggered by repository events:

**Triggers:** Push to `main`, pull requests, and tag pushes (`v*.*.*`)

| Job Path | Trigger | Steps |
|----------|---------|-------|
| **Verify** (lightweight) | Push to `main`, PRs | Format check, build, unit tests, markdown lint |
| **Release** (full) | Tag push | Verify steps + SPK assembly + GitHub release with artifact attachment |

### Artifact Strategy

- Release artifacts: `.spk` package attached to GitHub release
- Runtime binaries cached in Actions cache keyed by architecture and ChannelVersion to avoid redundant downloads
- Artifact retention aligned with GitHub default policies (90 days for workflow artifacts, indefinite for releases)

---

## Configuration Management

### Dual appsettings.json Structure

| Location | Purpose |
|----------|---------|
| `Ui/appsettings.json` | Server-side: runtime download version, Serilog sinks, allowed hosts |
| `Ui.Client/wwwroot/appsettings.json` | Client-side (WASM): BrowserConsole Serilog sink with Debug minimum level for `Askyl.Dsm` namespace |

### Download.ChannelVersion Dual-Purpose Constraint

`Download.ChannelVersion` in the server `appsettings.json` serves two roles:

1. **Build-time:** `build-spk.sh` reads this value to determine which .NET runtime version to download and package
2. **Runtime:** The application uses it for version detection and release fetching via `Microsoft.Deployment.DotNet.Releases`

This coupling means the packaged SPK's bundled runtime version is determined by the same
configuration value the running application consults for available releases. Changing this value
requires a full rebuild to maintain consistency between bundled runtime and application expectations.

### Direct Configuration Access Pattern

The codebase does not use `IOptions<T>` or `IConfiguration` binding anywhere. All configuration
values are accessed directly via `builder.Configuration["Section:Key"]` or through dedicated
services like `DsmSettingsService`.

**`DsmSettingsService`:** Reads `/etc/synoinfo.conf` for DSM-specific settings (server address, port, language). The file path is configurable to support local debugging against a remote DSM instance.

### Layered Configuration Merge

Standard .NET configuration layering applies: `appsettings.json` → `appsettings.{Environment}.json`
→ environment variables → command-line arguments. Currently only the base `appsettings.json` files
are used in production; no environment-specific overrides are packaged with the SPK.

---

## Appendix

### A. API Route Summary

| Controller | Route | Method | Purpose |
|------------|-------|--------|---------|
| AuthenticationController | `/api/v1/authentication/status` | GET | Check auth state |
| AuthenticationController | `/api/v1/authentication/login` | POST | Authenticate user |
| AuthenticationController | `/api/v1/authentication/logout` | POST | Clear session |
| WebsiteHostingController | `/api/v1/websites/all` | GET | List all websites |
| WebsiteHostingController | `/api/v1/websites/add` | POST | Create website |
| WebsiteHostingController | `/api/v1/websites/update` | POST | Update website |
| WebsiteHostingController | `/api/v1/websites/remove/{id}` | DELETE | Remove website |
| WebsiteHostingController | `/api/v1/websites/start/{id}` | POST | Start website |
| WebsiteHostingController | `/api/v1/websites/stop/{id}` | POST | Stop website |
| FileManagementController | `/api/v1/files/shared-folders` | GET | List shared folders via FileStation API |
| FileManagementController | `/api/v1/files/directory?path={path}&directoryOnly={bool}` | GET | List directory contents; `path` is a query parameter, not a route segment |
| FrameworkManagementController | `/api/v1/frameworks/install` | POST | Install .NET framework/runtime |
| FrameworkManagementController | `/api/v1/frameworks/uninstall/{version}` | POST | Uninstall specific framework version |
| RuntimeManagementController | `/api/v1/runtime/versions` | GET | List installed .NET versions |
| RuntimeManagementController | `/api/v1/runtime/channels/installed/{productVersion}` | GET | Check if channel is installed |
| RuntimeManagementController | `/api/v1/runtime/versions/installed/{version}` | GET | Check if specific version is installed |
| RuntimeManagementController | `/api/v1/runtime/channels` | GET | List available .NET channels |
| RuntimeManagementController | `/api/v1/runtime/releases/status/{productVersion}` | GET | List releases with installation status |
| LogDownloadController | `/api/v1/logdownload/logs` | GET | Download log files as ZIP archive |

### B. DSM API Reference

**Authentication:** `auth.login`, `auth.logout`, `auth.multifactor.login`
**FileStation:** `util.list`, `file.download`, `core.acl.set`
**ReverseProxy:** `list`, `add`, `set`, `delete`
