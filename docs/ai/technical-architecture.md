# ASkyl.Dsm.WebHosting - Technical Architecture Document

**Target Framework:** .NET 10 (net10.0)
**Last Updated:** June 26, 2026

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
9. [Performance Optimization](#performance-optimization)

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
├── Askyl.Dsm.WebHosting.Tests              # Unit tests (xUnit, Moq, FluentAssertions)
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

**Files:** `AnalyzerConstants.cs`, `BlankLineAnalyzer.cs`, `BlankLineCodeFixProvider.cs`, `StringStaticMemberAnalyzer.cs`, `StringStaticMemberCodeFixProvider.cs`, `LoggerDirectCallAnalyzer.cs`

### 2. Askyl.Dsm.WebHosting.Constants

**Purpose:** Centralized constants, defaults, and enums for the entire solution

**Complete Inventory:**

```text
Constants/
├── Application/
│   ├── ApplicationConstants.cs             # App paths, URLs, HTTP client names, session (DsmSid, DsmUsername, TTL)
│   ├── DotnetInfoParserConstants.cs        # dotnet --info section headers and framework identifiers
│   ├── InfrastructureConstants.cs          # Directory names (Downloads)
│   ├── LogConstants.cs                     # Log directory and file paths
│   ├── SecurityHeaders.cs                  # HTTP security header values (CSP, X-Frame-Options, etc.)
│   ├── ValidationConstants.cs              # Validation message constants
│   └── WebSiteConstants.cs                 # Website config, process lifecycle, port validation
├── Globalization/
│   └── GlobalizationConstants.cs           # Default culture, text direction (LTR/RTL), env var names
├── DSM/
│   ├── API/
│   │   ├── ApiConstants.cs                 # Merged API names, methods, version ranges
│   │   ├── DsmConstants.cs                 # Shared DSM error codes
│   │   ├── PhpDotNetFormatTokens.cs        # PHP → .NET format token mappings (ImmutableDictionary)
│   │   ├── ReverseProxyConstants.cs        # Proxy error codes
│   │   └── SerializationFormats.cs         # Enum: Form, Json
│   ├── FileStation/
│   │   └── FileStationDefaults.cs          # Listing patterns, sorting, file types
│   └── System/
│       ├── DsmLanguageCodes.cs             # DSM 3-letter language code data
│       └── SystemDefaults.cs               # Config paths, external ports
├── JSON/
│   └── JsonOptionsCache.cs                 # Static JsonSerializerOptions (camelCase, ignore nulls)
├── Network/
│   ├── NetworkConstants.cs                 # Cookie headers, localhost, MIME types
│   └── ProtocolTypes.cs                    # Enum: HTTP (0), HTTPS (1)
├── Runtime/
│   ├── DotNetFrameworkTypes.cs             # Framework type strings
│   └── RuntimeConstants.cs                 # Architecture (x64/arm/arm64), OS (linux/osx/windows)
├── Logging/
│   └── LogEventIds.cs                      # EventId range bases for [LoggerMessage] extensions
├── UI/
│   ├── DialogConstants.cs                  # Dialog widths
│   └── FileSizeConstants.cs                # Byte calculations (KiB/MiB/GiB)
└── WebApi/
    ├── AuthenticationRoutes.cs             # /api/v1/authentication/*
    ├── FileManagementRoutes.cs             # /api/v1/files/*
    ├── FrameworkManagementRoutes.cs        # /api/v1/frameworks/*
    ├── LogDownloadRoutes.cs                # /api/v1/logdownload/*
    ├── RuntimeManagementRoutes.cs          # /api/v1/runtime/*
    └── WebsiteHostingRoutes.cs             # /api/v1/websites/*
```

### 3. Askyl.Dsm.WebHosting.Data

**Purpose:** Core data layer, API definitions, domain services, and result types

**Complete Service Contracts Inventory:**

| Interface | Key Methods | Implemented By |
|-----------|-------------|----------------|
| **IAuthenticationService** | LoginAsync(), LogoutAsync(), IsAuthenticatedAsync() | Ui + Ui.Client |
| **ICultureManager** | InitializeFromLogin(), ResetToSystem(), CurrentCulture | Ui.Client.CultureManager |
| **IDotnetVersionService** | GetInstalledVersionsAsync(), GetChannelsAsync() | Ui + Ui.Client |
| **IFileSystemService** | GetSharedFoldersAsync(), GetDirectoryContentsAsync() | Ui + Ui.Client |
| **IFrameworkManagementService** | InstallFrameworkAsync(), UninstallFrameworkAsync() | Ui.Services |
| **IGlobalizationSettings** | SupportedCultures, SupportedCultureNamesJson, SystemCulture | Ui.Infrastructure.GlobalizationSettings |
| **ILogDownloadService** | CreateLogZipStreamAsync() | Ui.Services |
| **IReverseProxyManagerService** | CreateAsync(), UpdateAsync(), DeleteAsync() | Ui.Services |
| **IWebSiteHostingService** | GetAllWebsitesAsync(), AddWebsiteAsync() | Ui + Ui.Client |
| **IFileManagerService** | Initialize(), GetDirectory(), DeleteDirectory() | Tools.Infrastructure |
| **IArchiveExtractorService** | Decompress(inputFile, exclude) | Tools.Infrastructure |
| **IDownloaderService** | DownloadToAsync(), DownloadVersionToAsync() | Tools.Runtime |
| **IVersionsDetectorService** | GetInstalledVersionsAsync(), RefreshCacheAsync() | Tools.Runtime (Singleton) |
| **IAssemblyRuntimeDetector** | Detect() | Tools.Runtime (Singleton) |
| **IDsmSession** | ConnectAsync(), ValidateSessionAsync(), ExecuteAsync() | Ui.Services.DsmSession |
| **IDsmSettingsService** | Server, Port, Language | Tools.Infrastructure |

**Structure:**

```text
Data/
├── Contracts/                              # Service interfaces
├── Domain/                                 # Domain models
│   ├── Authentication/                     # LoginCredentials
│   ├── FileSystem/                         # FsEntry
│   ├── Licensing/                          # LicenseInfo
│   ├── Runtime/                            # AspNetCoreReleaseInfo, AssemblyRuntimeInfo, FrameworkInfo, InstallFramework
│   └── WebSites/                           # ProcessInfo, WebSiteConfiguration, WebSiteInstance, WebSiteRuntimeState, WebSitesConfiguration
├── DsmApi/                                 # DSM API integration
│   ├── Models/                             # API models (records with init setters)
│   │   ├── Auth/                           # AuthenticateLogin
│   │   ├── Core/                           # ApiInformation, ACL models, User models, UserSettings models
│   │   ├── FileStation/                    # File operation models
│   │   └── ReverseProxy/                   # Proxy configuration models
│   ├── Parameters/                         # Request parameter classes
│   │   ├── Auth/, Core/, FileStation/, Info/
│   │   ├── ApiParametersBase.cs            # Base parameter class
│   │   ├── ApiParametersNone.cs            # No-parameters wrapper
│   │   └── IApiParameters.cs               # Parameter interface
│   └── Responses/                          # API response wrappers
│       ├── ApiResponseBase.cs              # Generic response base with Error model
│       ├── Auth/, Core/, FileStation/
│       └── ApiInformationResponse.cs
├── Exceptions/                             # Custom exception types
└── Results/                                # Result pattern implementations
    ├── ApiResult.cs, ApiResultBool.cs, ApiResultData<T>.cs, ApiResultItems<T>.cs, ApiResultValue<T>.cs
    ├── ApiErrorCode.cs, AuthenticationResult.cs, ChannelsResult.cs
    ├── DirectoryContentsResult.cs, InstallationResult.cs, InstalledVersionsResult.cs
    ├── ReleasesResult.cs, SharedFoldersResult.cs, WebSiteInstanceResult.cs, WebSiteInstancesResult.cs
```

### 4. Askyl.Dsm.WebHosting.Globalization

**Purpose:** Localization resources, shared validators, culture management, C# 14 scoped extensions

```text
Globalization/
├── Extensions/                             # C# 14 scoped extensions
│   ├── CultureInfoExtensions.cs            # `extension(CultureInfo)` — GetTextDirection()
│   └── GlobalizationServiceCollectionExtensions.cs # `extension(IServiceCollection)` — AddGlobalization()
├── Resources/                              # Localization resources
│   ├── SharedResource.cs + .resx           # English (default)
│   └── SharedResource.fr-FR.resx           # French
├── Validators/                             # FluentValidation shared validators
│   ├── DeferredMessageExtensions.cs        # WithLocalizedMessage() — defers key resolution to validation time
│   ├── LoginCredentialsValidator.cs        # Login rules
│   └── WebSiteConfigurationValidator.cs    # Website config rules, separate port messages
├── Localizer.cs                            # ILocalizer — wraps ResourceManager, reads CurrentUICulture at call time
└── LocalizationKeys.cs                     # Strongly-typed keys (L.WebSiteConfiguration.*, L.LoginCredentials.*)
```

**Key Design Decisions:**

- **Shared validators** — Single source of truth; server auto-validation uses same FluentValidation rules
- **`ILocalizer` abstraction** — Returns `string` directly, hides `ResourceManager` from consumers
- **`ResourceManager` over `IStringLocalizer<T>`** — `IStringLocalizer<T>` caches culture at construction in WASM; `ResourceManager` reads `CurrentUICulture` at call time
- **No DataAnnotations** — All validation migrated to FluentValidation (cannot use runtime-localized messages)

### 5. Askyl.Dsm.WebHosting.Tools

**Purpose:** Utility services, DSM API client, and runtime management tools

```text
├── Converters/                             # Format/language converters
│   ├── DsmLanguageToCultureConverter.cs    # DSM 3-letter language code → .NET culture name
│   └── PhpFormatToDotNetConverter.cs       # PHP date/time tokens → .NET format strings
├── Extensions/                             # Extension methods
│   ├── ApiResponseExtensions.cs            # Response mapping helpers
│   └── HttpClientExtensions.cs             # HTTP client helpers (C# 14 scoped `extension(HttpClient)`)
├── Infrastructure/                         # Infrastructure utilities
│   ├── ArchiveExtractorService.cs          # gzip + tar extraction
│   ├── FileManagerService.cs               # File system initialization
│   ├── PlatformInfoService.cs              # Platform detection (no interface)
│   ├── ProcessHandle.cs                    # IProcessHandle + SystemProcessHandle (co-located)
│   ├── ProcessRunner.cs                    # IProcessRunner + SystemProcessRunner (co-located)
│   └── ProcessTerminator.cs                # Cross-platform process termination (SIGTERM/CloseMainWindow)
├── Network/                                # Network communication
│   └── DsmApiClient.cs                     # Centralized DSM API client
├── Diagnostics/                            # Diagnostic utilities
│   └── OperationTimer.cs                   # Disposable scope timer (Stopwatch + callback on Dispose)
├── Runtime/                                # .NET runtime management (DI-based)
│   ├── DownloaderService.cs                # Binary download utility
│   ├── VersionsDetectorService.cs          # Version detection with smart caching
│   └── AssemblyRuntimeDetector.cs          # Runtime detection from *.runtimeconfig.json
└── Threading/                              # Async coordination utilities
    └── SemaphoreLock.cs                    # Semaphore-based async locking utility
```

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

**DsmApiClient Key Features:**

- Singleton, implements `ISemaphoreOwner` for thread-safe lazy `ApiInformations` initialization
- `ExecuteAsync<R>` with compile-time `where R : IApiResponse` constraint (no reflection)
- Strategy pattern for Form vs JSON serialization
- Structured logging: request timing, auth failures, API errors via `[LoggerMessage]` extensions

**Process Lifecycle:** `SystemProcessRunner` requires `ILoggerFactory` to create correctly-typed child loggers for `SystemProcessHandle` instances (distinct closed generic types cannot be cast).

### 6. Askyl.Dsm.WebHosting.Ui

**Purpose:** Main Blazor hybrid application (Server + WebAssembly rendering)

```text
Ui/
├── Authorization/                          # Custom authorization
│   └── AuthorizeSessionAttribute.cs        # Session-based authorization
├── Controllers/                            # API controllers
│   ├── AuthenticationController.cs
│   ├── FileManagementController.cs
│   ├── FrameworkManagementController.cs
│   ├── LogDownloadController.cs
│   ├── RuntimeManagementController.cs
│   └── WebsiteHostingController.cs
├── Infrastructure/                         # Server-side infrastructure
│   └── GlobalizationSettings.cs            # IGlobalizationSettings — discovers cultures from satellite assemblies
├── Extensions/                             # Server-side extensions
│   └── GlobalizationExtensions.cs          # ApplyDsmSystemCulture(), UseGlobalizationRequestLocalization()
├── Services/                               # Business logic services
│   ├── AuthenticationService.cs            # Auth façade over DsmApiClient
│   ├── DotnetVersionService.cs
│   ├── FileSystemService.cs
│   ├── FrameworkManagementService.cs
│   ├── LogDownloadService.cs
│   ├── ReverseProxyManagerService.cs
│   ├── SiteLifecycleManager.cs             # Per-site process management (Channel-based command queue)
│   ├── WebSiteHostingService.cs            # Orchestrator (BackgroundService, ConcurrentDictionary<Guid, SiteEntry>)
│   └── WebSitesConfigurationService.cs
└── Program.cs                              # Entry point, DI registration, middleware pipeline
```

**Middleware Pipeline:**

1. `UsePathBase("/adwh")` — Sub-path support
2. `UseSession()` — Session before antiforgery
3. `UseRouting()` + `MapControllers()` — API endpoints
4. `UseAntiforgery()` — CSRF protection
5. `MapRazorComponents` with InteractiveWebAssembly render mode

### 7. Askyl.Dsm.WebHosting.Ui.Client

**Purpose:** Blazor WebAssembly client library (shared components and HTTP service proxies)

```text
Ui.Client/
├── Components/
│   ├── Controls/                           # Custom UI controls
│   │   ├── AutoDataGrid.razor              # Generic data grid with sorting, reload, row click/double-click
│   │   ├── LoadingOverlay.razor            # Full-screen overlay for IWorkingState
│   │   ├── RealTimeNumberField.razor       # Numeric input with real-time validation
│   │   └── RealTimeTextField.razor         # Text/password input with real-time validation
│   ├── Dialogs/                            # FluentUI dialog wrappers
│   │   ├── AspNetReleasesDialog.razor      # Channel selection, version grid, install/uninstall
│   │   ├── DotnetVersionsDialog.razor      # Installed .NET frameworks display
│   │   ├── FileSelectionDialog.razor       # Dual-pane file browser (tree + grid) with lazy loading
│   │   ├── LicensesDialog.razor            # Tabbed license viewer (parallel HTTP fetches)
│   │   └── WebSiteConfigurationDialog.razor # Add/edit website form
│   ├── Layout/
│   │   └── MainLayout.razor                # FluentMainLayout with global providers (Toast, Dialog, Tooltip)
│   ├── Pages/                              # Blazor pages
│   │   ├── Home.razor                      # Dashboard with website grid
│   │   ├── Login.razor                     # Authentication form
│   │   └── NotFound.razor                  # 404 handler
│   └── Patterns/WorkingState/              # IWorkingState interface + CreateWorkingState extension
├── Contracts/
│   └── INavigationGuard.cs                 # Router navigation guard interface
├── Services/                               # HTTP client wrappers + culture management
│   ├── AcceptLanguageHandler.cs            # DelegatingHandler — attaches Accept-Language from ICultureManager
│   ├── AuthenticationService.cs            # Singleton — POST /api/authentication/*
│   ├── AuthenticationNavigationGuard.cs    # Singleton — Router OnNavigateAsync guard
│   ├── CultureManager.cs                   # ICultureManager — resolves culture at login, clones with date/time formats
│   ├── DotnetVersionService.cs             # GET /api/runtime-management/*
│   ├── FileSystemService.cs                # GET /api/file-management/*
│   ├── FrameworkManagementService.cs       # POST /api/framework-management/*
│   ├── LicenseService.cs                   # Parallel HTTP fetches from server licenses/
│   ├── TreeContentService.cs               # FsEntry → TreeViewItem with lazy loading
│   └── WebSiteHostingService.cs            # GET/POST/DELETE /api/website-hosting/*
├── Routes.razor                            # Router with OnNavigateAsync auth guard
└── Program.cs                              # WASM entry point, service registration
```

**JavaScript Interop:** Single usage in FileSelectionDialog — `selectChildItem` for tree navigation after folder double-click.

### 8. Askyl.Dsm.WebHosting.Logging

**Purpose:** Logging extensions with source-generated logger methods

**Key Features:**

- **Source-generated log methods** for compile-time message validation
- **Zero-allocation logging** for performance-critical paths
- **Namespace-level category interfaces** — empty marker interfaces (e.g., `ILogAuthenticationService`) for `ILogger<T>` categorization
- **Server/Client folder separation**

```text
Logging/
├── Server/                                 # Server-side logging extensions
│   ├── Authentication/                     # AuthenticationService
│   ├── DsmApi/                             # DsmApiClient + DsmSession
│   ├── FileManagement/                     # FileManagerService, FileSystemService, LogDownloadService
│   ├── Framework/                          # DotnetVersionService, FrameworkManagementService
│   ├── Infrastructure/                     # ArchiveExtractor, AssemblyRuntimeDetector, Downloader, DsmSettingsService, GlobalizationSettings, PlatformInfo, VersionsDetector
│   ├── ProcessLifecycle/                   # ProcessHandle, ProcessLoggingExtensions, ProcessRunner
│   ├── ReverseProxy/                       # ReverseProxyManagerService
│   └── WebsiteHosting/                     # ConfigurationLoggingExtensions, WebsiteLoggingExtensions
└── Client/                                 # Client-side (WASM) logging extensions
    └── ClientLoggingExtensions.cs          # Home, dialogs, license service
```

**EventId Management:**

All `[LoggerMessage]` attributes use inline `int` literals. EventId ranges documented in `Constants/Logging/LogEventIds.cs`. Each service owns a 100K range at 1M spacing:

| Range | Service | Extension File |
|-------|---------|----------------|
| `1000001–1000007` | AuthenticationService | `AuthenticationLoggingExtensions.cs` |
| `1100001–1100012` | FileSystemService | `FileSystemServiceLoggingExtensions.cs` |
| `1200001–1200006` | FileManagerService | `FileManagerServiceLoggingExtensions.cs` |
| `1300001–1300007` | LogDownloadService | `LogDownloadServiceLoggingExtensions.cs` |
| `1400001–1400007` | FrameworkManagementService | `FrameworkManagementLoggingExtensions.cs` |
| `1500001–1500007` | DotnetVersionService | `DotnetVersionServiceLoggingExtensions.cs` |
| `1600001–1600019` | SiteLifecycleManager | `ProcessLoggingExtensions.cs` |
| `1700001–1700013` | ReverseProxyManagerService | `ReverseProxyLoggingExtensions.cs` |
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
| `7000001` | LicenseService (WASM) | `ClientLoggingExtensions.cs` |

**Total:** All services use `[LoggerMessage]` extensions, zero CA2254 warnings.

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
- **Client:** HTTP client wrappers, local component state, FluentUI Dialog state, `IWorkingState` pattern

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

### Caching Strategy

- **ApiInformations Cache:** Lazy-init with `SemaphoreLock` double-checked locking in `DsmApiClient`; fetched once, cached forever
- **Session Validation Cache:** 1-minute TTL for DSM session validation
- **Instance Cache:** In-memory `ConcurrentDictionary` for website instances
- **Configuration Cache:** JSON file read on startup, in-memory during runtime

---

## Appendix

### A. API Route Summary

| Controller | Route | Method | Purpose |
|------------|-------|--------|---------|
| AuthenticationController | `/api/authentication/status` | GET | Check auth state |
| AuthenticationController | `/api/authentication/login` | POST | Authenticate user |
| AuthenticationController | `/api/authentication/logout` | POST | Clear session |
| WebsiteHostingController | `/api/websites/all` | GET | List all websites |
| WebsiteHostingController | `/api/websites/add` | POST | Create website |
| WebsiteHostingController | `/api/websites/update` | POST | Update website |
| WebsiteHostingController | `/api/websites/remove/{id}` | DELETE | Remove website |
| WebsiteHostingController | `/api/websites/start/{id}` | POST | Start website |
| WebsiteHostingController | `/api/websites/stop/{id}` | POST | Stop website |
| FileManagementController | `/api/filemanagement/*` | * | File operations |
| FrameworkManagementController | `/api/frameworkmanagement/*` | * | .NET installation |
| RuntimeManagementController | `/api/runtime/*` | * | Version detection |
| LogDownloadController | `/api/logdownload/*` | * | Log retrieval |

### B. DSM API Reference

**Authentication:** `auth.login`, `auth.logout`, `auth.multifactor.login`
**FileStation:** `util.list`, `file.download`, `core.acl.set`
**ReverseProxy:** `list`, `add`, `set`, `delete`
