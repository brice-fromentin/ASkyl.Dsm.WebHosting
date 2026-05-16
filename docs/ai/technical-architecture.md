# ASkyl.Dsm.WebHosting - Technical Architecture Document

**Version:** 0.5.8
**Target Framework:** .NET 10 (net10.0)
**Last Updated:** May 15, 2026 (Phases 5+6 — DSM API logging, `IApiResponse`, Serilog template, graceful flush, ActivityId correlation)

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
10. [Build & Deployment](#build--deployment)
11. [Recommendations](#recommendations)

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
- **C# Records (init setters):** 22 DSM API model classes converted from source-generated clone methods to immutable records
- **Centralized Constants:** All magic strings/numbers extracted to dedicated Constants project
- **Background Service:** WebSiteHostingService orchestrates website instances; per-site process lifecycle delegated to SiteLifecycleManager (SIGTERM graceful shutdown, force kill fallback)
- **Cross-platform Process Termination:** `ProcessTerminator` sends SIGTERM on Unix/Linux/macOS (P/Invoke `libc.kill`) and CloseMainWindow on Windows — enables ~1-3 second graceful drain

**Current Status:**

- ✅ Blazor Server + Interactive WebAssembly hybrid rendering
- ✅ DSM API integration (Authentication, FileStation, ReverseProxy)
- ✅ Website lifecycle management with process control
- ✅ JSON-based configuration persistence
- ✅ **Infrastructure services refactored to DI-based architecture** (PlatformInfoService, FileManagerService, ArchiveExtractorService, DownloaderService, VersionsDetectorService)
- ✅ **Smart caching strategy** for expensive operations (VersionsDetectorService with lazy initialization)
- ✅ **Full CancellationToken support** across all async operations
- ✅ **All static classes converted** to injectable services for testability
- ✅ **Critical security issues resolved** (April 8, 2026):
  - ✅ Path traversal vulnerability fixed in FileManagerService with input sanitization
  - ✅ Blocking calls removed from async context in DotnetVersionService
  - ✅ HttpClient content disposal race condition resolved
  - ✅ All Console.WriteLine replaced with structured ILogger logging
- ✅ **SIGTERM process termination fix** (April 29, 2026):
  - ✅ Cross-platform `ProcessTerminator` utility replaces Windows-only `CloseMainWindow()`
  - ✅ SIGTERM sent via P/Invoke (`libc.kill`) on Unix/Linux/macOS for ~1-3 second graceful drain
  - ✅ Async `WaitForExitAsync` with linked cancellation token replaces blocking `WaitForExit(timeoutMs)`
  - ✅ Reduced timeouts: HttpClient (90→15s), Process (60→10s) — eliminates DSM reverse proxy 504 errors
- ⏳ TODO: Certificate management for reverse proxy
- ⏳ TODO: Multi-language support
- ✅ Unit test implementation (10 phases complete — May 2026)
- ✅ **IProcessRunner abstraction** for SiteLifecycleManager — co-located interface + implementation (ProcessRunner.cs, ProcessHandle.cs), enables full unit testing of process lifecycle
- ✅ **LoggerMessage migration** — 126 logger calls migrated to 145 source-generated `[LoggerMessage]` extension methods across 19 files; zero CA2254 warnings
- ✅ **DSM API logging** — request timing, authentication failures, and API errors logged via `[LoggerMessage]` extensions; compile-time `IApiResponse` constraint replaces reflection
- ✅ **Serilog configuration** — output template with `{EventId}`, `Log.CloseAndFlush()` on graceful shutdown, `WithActivity` enricher for correlation tracking
- ✅ **OperationTimer** — value-type disposable timer for scope-based duration logging across all services; replaced manual `Stopwatch` boilerplate with single-line `using var` pattern

**Security Score:** ⭐⭐⭐⭐☆ (4/5) - Production-ready after critical fixes

---

## Solution Overview

### Solution Structure

```text
Askyl.Dsm.WebHosting.slnx (Version 0.5.3)
├── Askyl.Dsm.WebHosting.Benchmarks         # Performance benchmarks (BenchmarkDotNet)
├── Askyl.Dsm.WebHosting.Constants          # Centralized constants & enums
├── Askyl.Dsm.WebHosting.Data               # Core data layer, API definitions, services
├── Askyl.Dsm.WebHosting.Logging            # Logging extensions (source-generated log methods)
├── Askyl.Dsm.WebHosting.Tools              # Utility tools & DSM API client
├── Askyl.Dsm.WebHosting.Tests              # Unit tests (xUnit, Moq, FluentAssertions)
├── Askyl.Dsm.WebHosting.Ui                 # Main Blazor Server-WASM hybrid UI
└── Askyl.Dsm.WebHosting.Ui.Client          # Blazor WebAssembly client library
```

### Key Characteristics

- **Multi-project solution** with clear separation of concerns
- **Shared constants** across all projects for maintainability
- **Source generators** for reducing boilerplate code (Serilog logging methods)
- **Hybrid rendering mode** (InteractiveServer + InteractiveWebAssembly)
- **Background services** for long-running operations
- **Centralized versioning** via Directory.Build.props

### Build Configuration

All projects share common build settings from `Directory.Build.props`:

```xml
<!-- Centralized versioning -->
<Version>0.5.8</Version>
<AssemblyVersion>0.5.8.0</AssemblyVersion>
<FileVersion>0.5.8.0</FileVersion>
<InformationalVersion>0.5.8</InformationalVersion>
<PackageVersion>0.5.8</PackageVersion>

<!-- Debug settings -->
<DebugType Condition="'$(Configuration)' == 'Release'">None</DebugType>
<DebugSymbols Condition="'$(Configuration)' == 'Release'">false</DebugSymbols>
<GenerateDocumentationFile>false</GenerateDocumentationFile>

<!-- .NET Analyzers for code quality and style enforcement -->
<EnableNETAnalyzers>true</EnableNETAnalyzers>
<AnalysisLevel>latest</AnalysisLevel>
<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
<RunAnalyzersDuringBuild>true</RunAnalyzersDuringBuild>
<RunAnalyzersDuringLiveAnalysis>true</RunAnalyzersDuringLiveAnalysis>
```

**Analyzer Packages:**

- **Roslynator.Analyzers** - Enhanced code style enforcement
- **Roslynator.Formatting.Analyzers** - Formatting rules

**.editorconfig Rule Severities (Updated April 3, 2026):**

| Category | Rule ID | Severity | Purpose |
|----------|---------|----------|---------|
| **AGENTS.md Mandatory** | dotnet_style_prefer_collection_expression | error | Prefer `[..]` over `.ToList()`/`.ToArray()` |
| **String/String Pattern** | IDE0049 | error | Use `string` (keyword) for types, `String.` for static methods |
| **Primary Constructors** | IDE0290, dotnet_style_primary_constructors | warning | MANDATORY for classes with parameters |
| **Magic String Prevention** | IDE0280 | warning | Use `nameof()` instead of string literals |
| **Readability** | dotnet_style_parentheses_in_relational_binary_operators | warning | Parentheses in boolean expressions |
| **Var Usage** | dotnet_style_var_for_built_in_types | error | Use explicit types for built-in types |
| **Var When Apparent** | dotnet_style_var_when_type_is_apparent | warning | Use `var` when type is obvious |
| **Cleanup** | IDE0005 | warning | Remove unnecessary using directives |
| **Null Propagation** | IDE0031 | suggestion | Use `?.` operator |

### Nullable Reference Types

All projects in the solution have `<Nullable>enable</Nullable>` enabled in their `.csproj` files:

- **Purpose:** Compile-time null safety checking to prevent NullReferenceException
- **Coverage:** All 8 projects (Ui, Ui.Client, Data, Tools, Constants, etc.)
- **Behavior with DI:** Blazor `@inject` directives and constructor-injected services do NOT require null-forgiving operators (`!`) because:
  - Dependency injection container always provides non-null instances
  - No compiler warnings are generated for injected services
  - Runtime guarantees service availability through DI lifecycle management

**Standardized Build Command:**

```bash
dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx
```

**Standardized Clean Command:**

```bash
dotnet clean /nr:false ./src/Askyl.Dsm.WebHosting.slnx
```

---

## Project Architecture

### 1. Askyl.Dsm.WebHosting.Constants

**Purpose:** Centralized constants, defaults, and enums for the entire solution

**Complete Inventory:**

```text

Constants/
├── Application/                            # Application-wide constants (6 files)
│   ├── ApplicationConstants.cs             # App paths, URLs, HTTP client names, session, auth messages
│   ├── DotnetInfoParserConstants.cs        # dotnet --info section headers and framework identifiers
│   ├── InfrastructureConstants.cs          # Directory names (Downloads)
│   ├── LicenseConstants.cs                 # License file management
│   ├── LogConstants.cs                     # Log directory and file paths
│   └── WebSiteConstants.cs                 # Website config, process lifecycle, port validation
├── DSM/                                    # Synology DSM-specific constants (7 files)
│   ├── API/                                # API-related constants
│   │   ├── ApiMethods.cs                   # CRUD operation names (Create, Get, List, etc.)
│   │   ├── ApiNames.cs                     # 7 DSM API identifiers (SYNO.API.Auth, FileStation, etc.)
│   │   ├── ApiVersions.cs                  # Version range constants (Min: 1, Max: 7)
│   │   ├── ReverseProxyConstants.cs        # Proxy error codes and description prefix
│   │   └── SerializationFormats.cs         # Enum: Form, Json
│   ├── FileStation/                        # FileStation-specific constants (1 file)
│   │   └── FileStationDefaults.cs          # Listing patterns, sorting, file types
│   └── System/                             # DSM system defaults (1 file)
│       └── SystemDefaults.cs               # Config paths, external ports (5001 default)
├── JSON/                                   # JSON serialization settings (1 file)
│   └── JsonOptionsCache.cs                 # Static JsonSerializerOptions (camelCase, ignore nulls)
├── Network/                                # Network configuration (2 files)
│   ├── NetworkConstants.cs                 # Cookie headers, localhost, MIME types
│   └── ProtocolTypes.cs                    # Enum: HTTP (0), HTTPS (1)
├── Runtime/                                # .NET runtime definitions (2 files)
│   ├── DotNetFrameworkTypes.cs             # Framework type strings (ASP.NET Core, SDK, Runtime)
│   └── RuntimeConstants.cs                 # Architecture (x64/arm/arm64), OS (linux/osx/windows)
├── Logging/                                # Logging event ID registry (1 file)
│   └── LogEventIds.cs                      # EventId range bases for [LoggerMessage] extensions (documentation only)
├── UI/                                     # User interface constants (2 files)
│   ├── DialogConstants.cs                  # Dialog widths (auto, 0.6, 0.75)
│   └── FileSizeConstants.cs                # Byte calculations (KiB/MiB/GiB), formatting
└── WebApi/                                 # API route definitions (6 files)
    ├── AuthenticationRoutes.cs             # /api/v1/authentication/* (login, logout, status)
    ├── FileManagementRoutes.cs             # /api/v1/files/* (shared-folders, directory-contents)
    ├── FrameworkManagementRoutes.cs        # /api/v1/frameworks/* (install, uninstall)
    ├── LogDownloadRoutes.cs                # /api/v1/logdownload/* (logs)
    ├── RuntimeManagementRoutes.cs          # /api/v1/runtime/* (versions, channels, releases)
    ├── WebsiteHostingRoutes.cs             # /api/v1/websites/* (all, add, update, remove, start, stop)

**Note:** License handling is done client-side via `ILicenseService` (no server controller or route constants needed).
```

**Key Constants by Category:**

| Category | Key Constants | Count |
|----------|---------------|-------|
| **Application** | SettingsFileName, HttpClientName, ApplicationSubPath ("adwh"), Session | ~35 |
| **Websites** | Process timeouts, port range (1024-65535), environment vars, validation messages | ~25 |
| **DSM APIs** | 7 API names, CRUD methods, version ranges, error codes | ~35 + 1 enum |
| **FileStation** | Listing patterns, sorting, pagination (100 limit) | ~15 |
| **Network** | Cookie header ("Cookie"), SSID prefix ("_SSID="), localhost | 6 + 1 enum |
| **Runtime** | Architecture IDs (x64/arm/arm64), OS IDs (linux/osx/windows) | ~15 |
| **UI** | Dialog widths, file size units (KiB/MiB/GiB) | 9 |
| **WebAPI Routes** | 7 controllers × ~3-6 routes each | ~30 |

**Design Principles:**

1. **No Magic Strings:** All literal strings extracted to constants
2. **Type Safety:** Enums for protocol types and serialization formats
3. **Centralized Configuration:** Single source of truth for API routes, DSM identifiers
4. **Static Properties:** JsonOptionsCache provides pre-configured JsonSerializerOptions
5. **Validation Messages:** User-facing error messages centralized for consistency

### 2. Askyl.Dsm.WebHosting.Data

**Purpose:** Core data layer, API definitions, domain services, and result types (13 service contracts)

**Complete Service Contracts Inventory:**

| Interface | Source File | Key Methods | Implemented By |
|-----------|-------------|-------------|----------------|
| **IAuthenticationService** | `Contracts/IAuthenticationService.cs` | LoginAsync(), LogoutAsync(), IsAuthenticatedAsync() | Ui.Services.AuthenticationService, Ui.Client.Services.AuthenticationService |
| **IDotnetVersionService** | `Contracts/IDotnetVersionService.cs` | GetInstalledVersionsAsync(), GetChannelsAsync() | Ui.Services.DotnetVersionService, Ui.Client.Services.DotnetVersionService |
| **IFileSystemService** | `Contracts/IFileSystemService.cs` | GetSharedFoldersAsync(), GetDirectoryContentsAsync() | Ui.Services.FileSystemService, Ui.Client.Services.FileSystemService |
| **IFrameworkManagementService** | `Contracts/IFrameworkManagementService.cs` | InstallFrameworkAsync(), UninstallFrameworkAsync() | Ui.Services.FrameworkManagementService |
| **ILogDownloadService** | `Contracts/ILogDownloadService.cs` | CreateLogZipStreamAsync() | Ui.Services.LogDownloadService |
| **IReverseProxyManagerService** | `Contracts/IReverseProxyManagerService.cs` | CreateAsync(), UpdateAsync(), DeleteAsync() | Ui.Services.ReverseProxyManagerService |
| **IWebSiteHostingService** | `Contracts/IWebSiteHostingService.cs` | GetAllWebsitesAsync(), AddWebsiteAsync() | Ui.Services.WebSiteHostingService, Ui.Client.Services.WebSiteHostingService |

**Note:** `FindByCompositeKeyAsync()` is a private helper method in the implementation, not part of the public interface.
| **IWebSitesConfigurationService** | `Contracts/IWebSitesConfigurationService.cs` | GetAllSitesAsync(), AddSiteAsync(), RemoveSiteAsync() | Ui.Services.WebSitesConfigurationService |
| **IPlatformInfoService** | `Contracts/IPlatformInfoService.cs` | (Properties: ChannelVersion, CurrentArchitecture, CurrentOS) | Tools.Infrastructure.PlatformInfoService |
| **IFileManagerService** | `Contracts/IFileManagerService.cs` | Initialize(), GetDirectory(), DeleteDirectory(), GetFullName() | Tools.Infrastructure.FileManagerService |
| **IArchiveExtractorService** | `Contracts/IArchiveExtractorService.cs` | Decompress(inputFile, exclude) | Tools.Infrastructure.ArchiveExtractorService |
| **IDownloaderService** | `Contracts/IDownloaderService.cs` | DownloadToAsync(), DownloadVersionToAsync(), GetAspNetCoreReleasesAsync() | Tools.Runtime.DownloaderService |
| **IVersionsDetectorService** | `Contracts/IVersionsDetectorService.cs` | GetInstalledVersionsAsync(), IsChannelInstalled(), RefreshCacheAsync() | Tools.Runtime.VersionsDetectorService |

**Structure:**

```text
Data/
├── Contracts/                              # Service interfaces
│   ├── IAuthenticationService.cs           # Authentication facade
│   ├── IDotnetVersionService.cs            # .NET version detection (with RefreshCacheAsync)
│   ├── IFileSystemService.cs               # File system operations
│   ├── IFrameworkManagementService.cs      # Framework installation
│   ├── ILogDownloadService.cs              # Log file retrieval
│   ├── IReverseProxyManagerService.cs      # Proxy configuration
│   ├── IWebSiteHostingService.cs           # Website lifecycle
│   ├── IWebSitesConfigurationService.cs    # Configuration persistence
│   ├── IPlatformInfoService.cs             # Platform detection (Singleton)
│   ├── IFileManagerService.cs              # File management (Scoped, configurable root)
│   ├── IArchiveExtractorService.cs         # Archive extraction (Scoped)
│   ├── IDownloaderService.cs               # .NET downloads with cancellation (Scoped)
│   └── IVersionsDetectorService.cs         # Version detection with smart caching (Singleton)
├── Domain/                                 # Domain models
│   ├── Authentication/                     # Auth-related models
│   │   └── LoginCredentials.cs             # Login credentials
│   ├── FileSystem/                         # File system models
│   │   └── FsEntry.cs                      # File system entry model
│   ├── Licensing/                          # License information
│   │   └── LicenseInfo.cs                  # License data model
│   ├── Runtime/                            # .NET runtime information
│   │   ├── AspNetChannel.cs                # .NET channel info
│   │   ├── AspNetCoreReleaseInfo.cs        # Release version details
│   │   ├── AspNetRelease.cs                # Release metadata
│   │   ├── FrameworkInfo.cs                # Framework metadata
│   │   └── InstallFramework.cs             # Framework installation target
│   └── WebSites/                           # Website management domain
│       ├── ProcessInfo.cs                  # Process runtime snapshot (Id, IsResponding)
│       ├── WebSiteConfiguration.cs         # Main config model
│       ├── WebSiteInstance.cs              # Runtime instance
│       ├── WebSiteRuntimeState.cs          # Immutable record for site state (Running/Stopped/NotResponding)
│       ├── WebSiteInstanceDetails.cs       # Website instance details for UI
│       ├── WebSitesConfiguration.cs        # Persistent configuration store
├── Attributes/                             # Custom attributes
│   └── DsmParameterNameAttribute.cs        # DSM parameter name mapping
├── DsmApi/                                 # DSM API integration
│   ├── Models/                             # Auto-generated response models
│   │   ├── Core/                           # Authentication, system info, ACL
│   │   │   └── Acl/                        # ACL models (CoreAclSet, Rule, Permission, Inherit)
│   │   ├── FileStation/                    # 9 file operation models
│   │   └── ReverseProxy/                   # Proxy configuration models
│   ├── Parameters/                         # Request parameter classes
│   │   ├── Core/                           # Login/logout parameters
│   │   ├── CoreAcl/                        # Access control parameters
│   │   ├── CoreInformations/               # System info queries
│   │   ├── FileStation/                    # 2 file operation parameters
│   │   ├── ReverseProxy/                   # Proxy CRUD operations
│   │   ├── ApiParametersBase.cs            # Base parameter class
│   │   ├── ApiParametersNone.cs            # No-parameters wrapper
│   │   └── IApiParameters.cs               # Parameter interface
│   └── Responses/                          # API response wrappers (8 files)
├── Exceptions/                             # Custom exception types (4 files)
└── Results/                                # Result pattern implementations
    ├── ApiResult.cs                        # Base success/failure result
    ├── ApiResultBool.cs                    # Boolean result wrapper
    ├── ApiResultData<T>.cs                 # Result with data payload
    ├── ApiResultItems<T>.cs                # Result with item collection
    ├── ApiResultValue<T>.cs                # Result with single value
    ├── ApiErrorCode.cs                     # Standardized error codes
    ├── AuthenticationResult.cs             # Auth state with user info
    ├── ChannelsResult.cs                   # .NET channel information
    ├── DirectoryContentsResult.cs          # File system directory listing
    ├── InstallationResult.cs               # Framework installation status
    ├── InstalledVersionsResult.cs          # Installed .NET versions
    ├── ReleasesResult.cs                   # .NET release information
    ├── SharedFoldersResult.cs              # NAS shared folder listing
    ├── WebSiteInstanceResult.cs            # Website-specific operations
    └── WebSiteInstancesResult.cs           # Multiple website results
```

**Key Features:**

- **Result Pattern:** All operations return typed results (eliminates null checks)
- **API Abstraction:** Strong-typed request/response models for DSM APIs (records with `init` setters)
- **Validation:** Data annotations with localized error messages from Constants
- **Service Interfaces:** Clean separation between domain logic and UI implementation

### 3. Askyl.Dsm.WebHosting.Tools

**Purpose:** Utility services, DSM API client, and runtime management tools

**Structure:**

```text
Tools/
├── Extensions/                             # Extension methods
│   ├── ApiResponseExtensions.cs            # Response mapping helpers
│   ├── HttpClientExtensions.cs             # HTTP client helpers
│   └── UriExtensions.cs                    # URI manipulation helpers
├── Infrastructure/                         # Infrastructure utilities
│   ├── ArchiveExtractorService.cs          # gzip + tar extraction (implements IArchiveExtractorService)
│   ├── FileManagerService.cs               # File system initialization (implements IFileManagerService)
│   ├── PlatformInfoService.cs              # Platform detection (implements IPlatformInfoService)
│   ├── ProcessHandle.cs                    # IProcessHandle + SystemProcessHandle (co-located)
│   ├── ProcessRunner.cs                    # IProcessRunner + SystemProcessRunner (co-located)
│   └── ProcessTerminator.cs                # Cross-platform process termination (SIGTERM/CloseMainWindow)
├── Network/                                # Network communication
│   └── DsmApiClient.cs                     # Centralized DSM API client
├── Diagnostics/                            # Diagnostic utilities
│   └── OperationTimer.cs                   # Disposable scope timer (Stopwatch + callback on Dispose)
├── Runtime/                                # .NET runtime management (DI-based)
    ├── DownloaderService.cs                # Binary download utility (implements IDownloaderService)
    └── VersionsDetectorService.cs          # Version detection with smart caching (implements IVersionsDetectorService)
└── Threading/                              # Async coordination utilities
    └── SemaphoreLock.cs                    # Semaphore-based async locking utility
```

**Infrastructure Services Architecture:**

The Tools project contains DI-based infrastructure services for platform detection, file management, archive extraction, and .NET runtime operations.

| Service | Interface | Lifetime | Key Features | Dependencies | Source File |
|---------|-----------|----------|--------------|--------------|-------------|
| **PlatformInfoService** | `IPlatformInfoService` | Singleton | Platform detection, config loading | ILogger | `Tools/Infrastructure/PlatformInfoService.cs` |
| **FileManagerService** | `IFileManagerService` | Scoped | Directory management, configurable root path | ILogger, string rootPath | `Tools/Infrastructure/FileManagerService.cs` |
| **ArchiveExtractorService** | `IArchiveExtractorService` | Scoped | tar.gz extraction | IFileManagerService, ILogger | `Tools/Infrastructure/ArchiveExtractorService.cs` |
| **DownloaderService** | `IDownloaderService` | Scoped | .NET runtime downloads with cancellation | IPlatformInfoService, IFileManagerService | `Tools/Runtime/DownloaderService.cs` |
| **VersionsDetectorService** | `IVersionsDetectorService` | Singleton | Smart caching for dotnet --info | ILogger, ISemaphoreOwner | `Tools/Runtime/VersionsDetectorService.cs` |

**Key Design Decisions:**

1. **Singleton Services (Stateful):** Platform info loaded once at startup; VersionsDetector caches expensive process output
2. **Scoped Services (Request-bound):** FileManager configured per-request via factory lambda; ArchiveExtractor and Downloader depend on Scoped FileManager
3. **Smart Caching Strategy:** VersionsDetectorService uses lazy initialization with explicit cache refresh after install/uninstall operations
4. **CancellationToken Support:** All DownloaderService public methods accept optional CancellationToken for cooperative cancellation flow from UI to infrastructure layer

**DsmApiClient Implementation:**

See `Tools/Network/DsmApiClient.cs` for full implementation.

**Key Features:**

- Singleton pattern (registered in DI container)
- Session management with SID validation and restoration
- Automatic serialization based on `IApiParameters.SerializationFormat`
- Strategy pattern for Form vs JSON serialization
- Compile-time generic constraint `where R : IApiResponse` on `ExecuteAsync<R>` — enables compile-time access to `Success`/`Error` properties (no reflection)
- Structured logging with `[LoggerMessage]` extensions:
  - HTTP request timing (method, URL, status code, duration in milliseconds)
  - Authentication failure logging with error reason from response
  - API error logging for `Success: false` responses (error code + reason)
- HttpClient factory integration for proper lifecycle management
- All infrastructure services testable via interface abstractions

**`IApiResponse` Interface:**

Defined in `Data/DsmApi/Responses/ApiResponseBase.cs`. All DSM API response types implement `IApiResponse` via `ApiResponseBase<T>`.

This enables compile-time access to `Success` and `Error` properties — replacing reflection with type-safe error handling.

**Connection Flow:** See `DsmApiClient.cs` lines 85-120

### 4. Askyl.Dsm.WebHosting.Ui

**Purpose:** Main Blazor hybrid application (Server + WebAssembly rendering)

**Structure:**

```text
Ui/
├── Authorization/                          # Custom authorization
│   └── AuthorizeSessionAttribute.cs        # Custom session-based authorization attribute
├── Controllers/                            # ASP.NET Core API controllers
│   ├── AuthenticationController.cs         # Login/logout/status endpoints
│   ├── FileManagementController.cs         # File system operations
│   ├── FrameworkManagementController.cs    # Framework installation
│   ├── LogDownloadController.cs            # Log file retrieval
│   ├── RuntimeManagementController.cs      # .NET version detection
│   └── WebsiteHostingController.cs         # Website CRUD + lifecycle
├── Properties/                             # Assembly info, launch settings
├── Services/                               # UI business logic services
│   ├── AuthenticationService.cs            # Auth façade over DsmApiClient
│   ├── DotnetVersionService.cs             # .NET version detection
│   ├── FileSystemService.cs                # File operations wrapper
│   ├── FrameworkManagementService.cs       # Framework installation
│   ├── LogDownloadService.cs               # Log file retrieval
│   ├── ReverseProxyManagerService.cs       # Proxy CRUD operations
│   ├── SiteLifecycleManager.cs             # Per-site process management (start/stop, graceful shutdown, force kill)
│   ├── WebSiteHostingService.cs            # Website orchestration (delegates process lifecycle to SiteLifecycleManager)
│   └── WebSitesConfigurationService.cs     # Configuration persistence
├── wwwroot/                                # Static assets (CSS, JS, images)
├── appsettings.json                        # Production configuration
├── appsettings.Development.json            # Development overrides
└── Program.cs                              # Application entry point
```

**Program.cs Configuration:**

See `Ui/Program.cs` for full implementation. Key registration patterns:

**Key Configuration Points:**

- Session middleware configured with 30-minute timeout (see `Program.cs` lines 25-33)
- FileManagerService uses factory pattern: `sp => new FileManagerService(sp.GetRequiredService<ILogger<FileManagerService>>(), ApplicationConstants.RuntimesRootPath)`
- VersionsDetectorService registered as Singleton for effective caching across requests
- All infrastructure services follow dependency hierarchy (Scoped can depend on Singleton, not vice versa)

**Middleware Pipeline:** See `Program.cs` lines 85-95

1. UsePathBase("/adwh") - Sub-path support
2. UseSession() - Session before antiforgery
3. UseRouting() + MapControllers() - API endpoints
4. UseAntiforgery() - CSRF protection
5. MapRazorComponents with InteractiveWebAssembly render mode

**Key Features:** See `Ui/Program.cs` comments for detailed explanations

- Hybrid rendering: Server-side auth + client-side interactivity (InteractiveWebAssembly)
- Session-based authentication with DSM SID persistence in ASP.NET Core session
- Background service for website lifecycle management (starts/stops on host lifecycle)
- FluentUI components for consistent UI/UX across all pages
- Structured logging with Serilog (configuration-based setup)
- Antiforgery protection for Blazor and API endpoints
- Sub-path support via `UsePathBase("/adwh")` for reverse proxy deployment
- DI-based infrastructure services with optimized lifetimes (Singleton/Scoped hierarchy)

### 5. Askyl.Dsm.WebHosting.Ui.Client

**Purpose:** Blazor WebAssembly client library (shared components and HTTP service proxies)

**Complete Component Inventory:**

```text
Ui.Client/
├── Components/                             # Reusable Blazor components
│   ├── Controls/                           # Custom UI controls (4 components)
│   │   ├── AutoDataGrid.razor              # Generic data grid with sorting, reload button, row click/double-click
│   │   ├── LoadingOverlay.razor            # Full-screen overlay for IWorkingState components
│   │   ├── RealTimeNumberField.razor       # Numeric input with real-time binding and validation
│   │   └── RealTimeTextField.razor         # Text/password input with real-time binding
│   ├── Dialogs/                            # FluentUI dialog wrappers (5 dialogs)
│   │   ├── AspNetReleasesDialog.razor      # Channel selection, version grid, install/uninstall actions
│   │   │   └── Services: IDotnetVersionService, IFrameworkManagementService
│   │   ├── DotnetVersionsDialog.razor      # Display installed .NET frameworks with icons
│   │   │   └── Services: IDotnetVersionService.GetInstalledVersionsAsync()
│   │   ├── FileSelectionDialog.razor       # Dual-pane file browser (tree + grid) with lazy loading
│   │   │   └── Services: IFileSystemService, ITreeContentService, IJSRuntime (selectChildItem interop)
│   │   ├── LicensesDialog.razor            # Tabbed license viewer (parallel HTTP fetches)
│   │   │   └── Services: ILicenseService.GetLicensesAsync()
│   │   └── WebSiteConfigurationDialog.razor # Add/edit website form with file picker
│   │       └── Data Model: WebSiteInstance, Nested FileSelectionDialog for path selection
│   ├── Layout/                             # Layout components
│   │   └── MainLayout.razor                # Main app shell with FluentMainLayout, global providers
│   │       └── Providers: FluentToastProvider, FluentDialogProvider, FluentTooltipProvider
│   ├── Pages/                              # Blazor pages (3 pages)
│   │   ├── Home.razor                      # Dashboard with website grid, toolbar actions (add/edit/delete/start/stop)
│   │   │   └── Services: IWebSiteHostingService, IAuthenticationService, ILogDownloadService
│   │   ├── Login.razor                     # Authentication form with platform check (Linux/macOS warning)
│   │   │   └── Services: IAuthenticationService.LoginAsync(), DataAnnotationsValidator
│   │   └── NotFound.razor                  # 404 handler
│   └── Patterns/                           # UI patterns
│       └── WorkingState/                   # IWorkingState interface and CreateWorkingState extension
├── Extensions/                             # Client-side extension methods
│   └── FsEntryExtensions.cs                # File system entry extension methods
├── Interfaces/                             # C# interfaces for JS interop
├── Services/                               # HTTP client wrappers (7 services)
│   ├── AuthenticationService.cs            # Singleton - POST /api/authentication/login, logout, status
│   ├── DotnetVersionService.cs             # GET /api/runtime-management/{versions,channels,releases}
│   ├── FileSystemService.cs                # GET /api/file-management/{shared-folders,directory-contents}
│   ├── FrameworkManagementService.cs       # POST /api/framework-management/{install,uninstall}
│   ├── LicenseService.cs                   # Parallel HTTP fetches from server licenses/ path
│   ├── TreeContentService.cs               # Convert FsEntry to TreeViewItem with lazy loading callbacks
│   └── WebSiteHostingService.cs            # GET/POST/DELETE /api/website-hosting/{all,add,update,remove,start,stop}
├── wwwroot/                                # Client-side static assets
│   └── appsettings.json                    # Client Serilog config (BrowserConsole sink)
├── _Imports.razor                          # Global using directives (System.Net.Http, Microsoft.FluentUI, Icons namespaces)
├── Program.cs                              # WASM entry point (service registration, HttpClient configuration)
└── Routes.razor                            # Router with AppAssembly route discovery, MainLayout default
```

**Key Features:**

1. **Component Library for UI Consistency:**
   - AutoDataGrid<T>: Generic data grid with sorting, reload button, row click/double-click emulation (400ms timer workaround)
   - RealTimeTextField/NumberField: Immediate validation feedback on @oninput and @onchange events
   - LoadingOverlay: Full-screen overlay bound to IWorkingState pattern

2. **HTTP Client Wrappers for Type-Safe API Calls:**
   - All services implement domain contracts from `Askyl.Dsm.WebHosting.Data.Contracts`
   - Use extension methods: PostJsonOrDefaultAsync(), GetJsonOrDefaultAsync() with fallback factories
   - BaseAddress configured to server root (no /adwh path base - reverse proxy handles routing in production)

3. **State Management Patterns:**
   - **IWorkingState Interface:** Components implement IsWorking, Message, NotifyStateChanged() properties
   - **CreateWorkingState Extension:** Disposable pattern for automatic working state management
   - **Dialog State Management:** FluentDialogProvider provides IDialogService, dialogs return DialogResult<T>

4. **FluentUI Integration:**
   - Global providers in MainLayout: Toast, Dialog, Tooltip, MessageBar, Menu
   - Icons imported as static aliases: `IconsRegular16`, `IconsRegular20`, `IconsRegular24`
   - FluentDesignTheme with System mode, custom CSS imports

5. **InteractiveWebAssembly Render Mode:**
   - Seamless server-client transition for interactive components
   - Client-side interactivity without constant server roundtrips
   - Server-side authentication via session cookies (no client-side secrets)

6. **JavaScript Interop Usage:**
   - Single usage in FileSelectionDialog: `JSRuntime.InvokeVoidAsync("selectChildItem", filePath, parentPath)`
   - Purpose: Navigate tree view to child directory after double-clicking folder in grid
   - JavaScript function defined in wwwroot/js/tree-navigation.js (external file)

**Service Registration (Program.cs):**

```csharp
// Singleton - Authentication state managed server-side via session cookies
builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();

// Scoped - HTTP client wrappers for REST API calls
builder.Services.AddScoped<IDotnetVersionService, DotnetVersionService>();
builder.Services.AddScoped<IFrameworkManagementService, FrameworkManagementService>();
builder.Services.AddScoped<IWebSiteHostingService, WebSiteHostingService>();
builder.Services.AddScoped<ILicenseService, LicenseService>();
builder.Services.AddScoped<IFileSystemService, FileSystemService>();
builder.Services.AddScoped<ITreeContentService, TreeContentService>();

// HttpClient configuration
builder.Services.AddHttpClient(ApplicationConstants.HttpClientName, client =>
{
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
});
```

### 6. Askyl.Dsm.WebHosting.Benchmarks

**Purpose:** Performance benchmarking with BenchmarkDotNet

**Key Features:**

- **MemoryDiagnoser** for GC analysis and memory allocation tracking
- **Comparison tests** for string concatenation strategies
- **Real-world scenarios** (URL building, parameter encoding)
- **BenchmarkDotNet 0.15.8** for accurate performance measurements

### 7. Askyl.Dsm.WebHosting.Logging

**Purpose:** Logging extensions with source-generated logger methods

**Key Features:**

- **Source-generated log methods** for compile-time message validation
- **Extension method pattern** for clean logger API
- **Structured logging** support with named parameters
- **Zero-allocation logging** for performance-critical paths
- **Namespace-level category interfaces** — empty marker interfaces (e.g., `ILogAuthenticationService`) for `ILogger<T>` categorization, keeping Logging as a leaf node with zero project references
- **Specialized `ILogger<T>`** — each service injects `ILogger<ILogXxx>` for automatic log categorization by service name

**EventId Management:**

All `[LoggerMessage]` attributes use inline `int` literals (per Microsoft convention).
EventId ranges are documented in `Constants/Logging/LogEventIds.cs`.
Each service owns a dedicated 100K range at 1M spacing to prevent cross-service collisions:

| Range | Service | Extension File |
|-------|---------|----------------|
| `1000001–1000008` | AuthenticationService | `AuthenticationLoggingExtensions.cs` |
| `1100001–1100012` | FileSystemService | `FileSystemServiceLoggingExtensions.cs` |
| `1200001–1200006` | FileManagerService | `FileManagerServiceLoggingExtensions.cs` |
| `1300001–1300007` | LogDownloadService | `LogDownloadServiceLoggingExtensions.cs` |
| `1400001–1400011` | FrameworkManagementService | `FrameworkManagementLoggingExtensions.cs` |
| `1500001–1500009` | DotnetVersionService | `DotnetVersionServiceLoggingExtensions.cs` |
| `1600001–1600022` | SiteLifecycleManager | `ProcessLoggingExtensions.cs` |
| `1700001–1700016` | ReverseProxyManagerService | `ReverseProxyLoggingExtensions.cs` |
| `1800001–1800044` | WebSiteHostingService | `WebsiteLoggingExtensions.cs` |
| `1900001–1900018` | WebSitesConfigurationService | `ConfigurationLoggingExtensions.cs` |
| `2000001–2000012` | DsmApiClient | `DsmApiLoggingExtensions.cs` |
| `2100001–2100008` | ArchiveExtractorService | `ArchiveExtractorLoggingExtensions.cs` |
| `2200001–2200004` | VersionsDetectorService | `VersionsDetectorLoggingExtensions.cs` |
| `2300001–2300002` | PlatformInfoService | `PlatformInfoLoggingExtensions.cs` |
| `2400001–2400005` | DownloaderService | `DownloaderLoggingExtensions.cs` |
| `2500001` | SystemProcessRunner | `ProcessRunnerLoggingExtensions.cs` |
| `2600001–2600005` | SystemProcessHandle | `ProcessHandleLoggingExtensions.cs` |
| `2700001` | LicenseService (client) | `ClientLoggingExtensions.cs` |

**Total:** 215 `[LoggerMessage]` methods across 19 extension files, zero CA2254 warnings.

**Serilog Configuration:**

- Output template: `{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [EventId:{EventId}] {Message:lj}{NewLine}{Exception}`
- Graceful flush: `Log.CloseAndFlush()` registered via `ApplicationStopping` lifetime hook
- Activity correlation: `WithActivity` enricher adds `ActivityId`, `ActivityTraceId`, `ActivitySpanId` to log context

---

## Design Patterns & Principles

### 1. Dependency Injection (DI)

**Service Registration:** See `Ui/Program.cs` lines 45-78 for full implementation.

**Patterns Used:**

- **Singleton:** DsmApiClient, platform info, versions detector (with caching), configuration services, background services
- **Scoped:** File manager (with factory lambda for root path), archive extractor, downloader, UI services - one per request
- **Background Service:** WebSiteHostingService implements IHostedService for lifecycle management

**Service Lifetime Hierarchy:**

```text
Singleton (Application-wide)
├── DsmApiClient
├── PlatformInfoService (platform detection, config loading)
├── VersionsDetectorService (smart caching for dotnet --info)
└── WebSiteHostingService (background service - orchestrator)
    └── SiteLifecycleManager (per-instance process management)

Scoped (Per HTTP request)
├── FileManagerService (configured via factory with root path)
│   └── ArchiveExtractorService (depends on FileManagerService)
│   └── DownloaderService (depends on PlatformInfoService + FileManagerService)
│       └── DotnetVersionService (depends on VersionsDetectorService + DownloaderService)
│       └── FrameworkManagementService (depends on all above services)
├── AuthenticationService
└── LogDownloadService
```

**Key Design Principles:**

1. **Singleton for Stateful Services:** Services that maintain state (caching, configuration) are Singletons
2. **Scoped for Request-bound Operations:** Services that perform per-request operations are Scoped
3. **Factory Lambda for Configuration:** FileManagerService uses factory pattern to inject logger + configure root path
4. **Dependency Hierarchy Respected:** Scoped services can depend on Singletons, but not vice versa
5. **All Infrastructure Services Testable:** No static classes - everything injectable via interfaces
6. **Process Lifecycle Delegation:** WebSiteHostingService orchestrates instances; SiteLifecycleManager handles per-site process start/stop with graceful shutdown and force kill fallback

### 2. Result Pattern

**Benefits:**

- Eliminates exception-based control flow
- Strongly-typed success/failure states
- No null reference exceptions
- Cleaner UI error handling

### 3. Repository/Service Facade Pattern

**Structure:**

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

**Key Interfaces:**

- `IWebSiteHostingService` - Website lifecycle management
- `IAuthenticationService` - DSM authentication facade
- `IReverseProxyManagerService` - Reverse proxy CRUD operations
- `IFileSystemService` - File system operations via FileStation API

### 4. Background Service Pattern

**Architecture:** Two-tier process management with clear separation of concerns.

```text
WebSiteHostingService (BackgroundService, Singleton)
├── Orchestrates website instances via ConcurrentDictionary<Guid, WebSiteInstance>
├── Loads configurations from JSON on startup
├── Manages instance lifecycle (add/update/remove)
└── Delegates per-site process management to SiteLifecycleManager

SiteLifecycleManager (Per-instance, Thread-safe)
├── Starts/stops .NET web application processes via IProcessRunner abstraction (unit-testable)
├── IProcessHandle? replaces direct Process? reference — delegates to SystemProcessHandle
├── Configures environment variables (ASPNETCORE_URLS, ASPNETCORE_ENVIRONMENT, custom vars)
├── Graceful shutdown with ProcessTerminator.SendGracefulShutdownSignal() (SIGTERM on Unix, CloseMainWindow on Windows)
├── Async WaitForExitAsync with linked cancellation token + configurable timeout
├── Force kill fallback if process doesn't exit gracefully
└── Thread-safe operations via Channel-based command queue (eliminates TOCTOU races)
```

**Key Features:**

- **Two-tier architecture** — WebSiteHostingService orchestrates; SiteLifecycleManager handles per-site process lifecycle
- **Singleton lifetime** — WebSiteHostingService runs as one instance per application
- **Startup initialization** — Loads configurations from persistent storage
- **Cross-platform graceful shutdown** — ProcessTerminator sends SIGTERM on Unix (via P/Invoke `libc.kill`) or CloseMainWindow on Windows; ASP.NET Core child processes drain in ~1-3 seconds
- **Async process wait** — WaitForExitAsync with linked cancellation token replaces blocking WaitForExit(timeoutMs)
- **Force kill fallback** — If process doesn't exit within timeout, Process.Kill() is called as last resort
- **Thread-safe operations** — `ConcurrentDictionary` for instance management; Channel-based command serialization in SiteLifecycleManager (eliminates TOCTOU races)
- **Idempotent stop** — Calling `StopAsync()` when already stopped returns success without error
- **Safe disposal** — `DisposeCommand` queues after all pending commands; `Dispose()` blocks until loop drains

### 6. Strategy Pattern (Serialization)

**DsmApiClient Serialization:**

```csharp
public async Task<R?> ExecuteAsync<R>(IApiParameters parameters)
{
    return parameters.SerializationFormat switch
    {
        SerializationFormat.Form => await ExecuteFormAsync<R>(parameters),
        SerializationFormat.Json => await ExecuteJsonAsync<R>(parameters),
        _ => throw new NotSupportedException($"Unsupported format: {parameters.SerializationFormat}")
    };
}
```

**Benefits:**

- Adapts to different DSM API requirements
- Clean separation of serialization logic
- Easy to extend with new formats

### 7. Disposable Scope Pattern (OperationTimer)

**`OperationTimer`** — value-type (`struct`) disposable timer in `Tools/Diagnostics/OperationTimer.cs`.

Starts a `Stopwatch` on construction and invokes a callback with elapsed milliseconds on disposal. Enables scope-based duration logging without manual start/stop boilerplate.

```csharp
// Single-line usage — timer starts on construction, callback fires on Dispose
using var timer = new OperationTimer(elapsed => logger.FrameworkInstalledDuration(elapsed, version));

// ... method body ...

// When method returns (success or exception), timer.Dispose() invokes the callback
```

**Key Features:**

- **Value type** — zero heap allocation; not `readonly` struct (requires mutable `_disposed` flag)
- **Dispose idempotency** — callback fires exactly once regardless of how many times `Dispose()` is called
- **Exception-safe** — `using var` ensures callback fires on both success paths and exception paths
- **Elapsed property** — exposes `ElapsedMilliseconds` for inline access without disposing

**Usage Across Services:**

| Service | Methods with OperationTimer |
|-----------|-----------|
| ReverseProxyManagerService | Create, Update, Delete |
| FrameworkManagementService | Install, Uninstall |
| WebSiteHostingService | Add, Update, Start, Stop, Remove |
| SiteLifecycleManager | ProcessStartCommand, ProcessStopCommand |
| DownloaderService | DownloadReleaseToAsync |
| DotnetVersionService | RefreshCacheAsync |
| WebSitesConfigurationService | AddSite, UpdateSite, RemoveSite |

**Note:** DsmApiClient retains inline `Stopwatch` (single HTTP call, duration passed directly to `ApiRequest` method).

**Benefits:**

- Eliminates repetitive `Stopwatch.StartNew()` / `Stop()` / `logger.Xxx(elapsed)` boilerplate
- Single-line declaration makes intent clear (measure this method's duration)
- Exception-safe — duration logged even when method throws
- Combines with `SemaphoreLock` for locked + timed scopes

---

## Technical Stack

### Frameworks & Libraries

| Component | Technology | Purpose |
|-----------|------------|---------|
| **Runtime** | .NET 10 | Application framework |
| **UI Framework** | Blazor Hybrid (Interactive WebAssembly) | Server + client rendering |
| **UI Components** | FluentUI Blazor | Modern UI component library |
| **Logging** | Serilog | Structured logging |
| **HTTP Client** | Microsoft.Extensions.Http | HttpClient factory |
| **Benchmarking** | BenchmarkDotNet | Performance testing |
| **.NET Releases** | Microsoft.Deployment.DotNet.Releases | Version detection |
| **WASM Server** | Microsoft.AspNetCore.Components.WebAssembly.Server | Blazor WASM hosting |
| **Analyzer Rules** | Roslynator.Analyzers + Formatting.Analyzers | Code style enforcement |

> **Note:** Package versions are declared inline in each `.csproj` file and updated regularly. Consult the project files for current versions.

### Development Tools

- **IDE:** Visual Studio 2022 / VS Code with C# Dev Kit
- **Build Tool:** .NET SDK 10.0
- **Package Manager:** NuGet
- **Version Control:** Git

---

## Data Models & API Integration

### Core Domain Models

- **WebSiteConfiguration** — main config model (name, path, port, SSL, environment variables)
- **WebSiteInstance** — runtime instance wrapping configuration + process lifecycle
- **ProcessInfo** — immutable process snapshot (Id, IsResponding)

**Key Design Decisions:**

- **Snapshot model** — captures `Id` and `IsResponding` at construction, avoiding `InvalidOperationException` if the process exits during JSON serialization
- **No live `Process` reference** — eliminates staleness risk and cross-platform issues with `Process.Responding` (always `false` for headless processes on Windows)

### DSM API Integration

#### Authentication Flow

```text
1. Client → LoginCredentials { Username, Password, [LotP] }
2. DsmApiClient.ReadSettings() → Load /etc/synoinfo.conf
3. DsmApiClient.HandShakeAsync() → SYNO.API.Info query
4. DsmApiClient.AuthenticateAsync() → auth.login API call
5. Response: SID stored in cookie header (ssid=...)
6. Session persisted in ASP.NET Core session
```

#### FileStation Operations

**Supported APIs:**

- `util.list` - List directory contents
- `util.upload` - Upload files
- `util.download` - Download files
- `util.delete` - Delete files/directories
- `util.mkdir` - Create directories
- `file.move` - Move/rename files
- `file.copy` - Copy files
- `core.acl.set` - Set ACL permissions (critical for web hosting)

### Key Operation: Setting HTTP Group Permissions

Uses `SYNO.Core.ACL` API to grant `http` group read/execute permissions on website deployment directories.
Called automatically after framework installation to ensure the reverse proxy can access deployed files.
See `DsmApiClient.cs` for full ACL implementation.

## UI Architecture

### Rendering Strategy

#### Hybrid Mode: Interactive WebAssembly

```csharp
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();
```

**Why Hybrid?**

1. **Server-side authentication** - Secure session management
2. **Client-side interactivity** - Responsive UI without server roundtrips
3. **Cold start performance** - Initial load is server-rendered
4. **Security** - Sensitive operations remain server-side

### Component Hierarchy

```text
App.razor (Root)
└── MainLayout.razor
    └── Page Content
        ├── Home.razor (Dashboard with website grid)
        ├── Login.razor (Authentication)
        └── NotFound.razor (404 handler)

Dialogs (Overlay)
├── WebSiteConfigurationDialog.razor
├── FileSelectionDialog.razor
├── DotnetVersionsDialog.razor
├── AspNetReleasesDialog.razor
└── LicensesDialog.razor
```

### Custom Components

#### AutoDataGrid<T>

Generic data grid with sorting capabilities:

- Type-safe column definitions
- Client-side sorting
- FluentUI DataGrid integration
- Loading states

#### RealTimeTextField / RealTimeNumberField

Input components with immediate validation feedback:

- Integrated with FluentUI TextField/NumberField
- Real-time validation display
- Error message binding

### State Management

**Server-Side:**

- ASP.NET Core Session for authentication (DSM SID)
- `WebSiteHostingService` singleton for website instances
- `WebSitesConfigurationService` for persistent configuration

**Client-Side:**

- HTTP client wrappers for API calls
- Local component state for UI interactions
- Dialog state management via FluentUI

---

## Security Considerations

### Authentication & Session Management

1. **Server-Side Session Storage**
   - DSM SID stored in server session (not client storage)
   - HttpOnly cookies prevent XSS attacks
   - SameSite=Strict prevents CSRF

2. **Antiforgery Protection**
   - Enabled for all Blazor components and API endpoints
   - Token validation on state-changing operations

3. **HTTPS Enforcement**
   - `UseHttpsRedirection()` in middleware pipeline
   - Default protocol for reverse proxy is HTTPS
   - HSTS enabled by default for websites

### API Security

1. **No Client-Side Secrets**
   - All DSM API calls go through server controllers
   - Credentials never exposed to browser

2. **Input Validation**
   - Data annotations on all models

- Server-side validation in services

1. **Error Handling**
   - Generic error messages (no stack traces to client)
   - Structured logging for debugging

### File System Security

1. **HTTP Group Permissions**
   - Critical: Set before deploying applications
   - Ensures nginx can serve files

2. **Path Validation**
   - Validate all file paths against allowed directories
   - Prevent path traversal attacks

### Custom Authorization

**AuthorizeSessionAttribute:**

Location: `Ui/Authorization/AuthorizeSessionAttribute.cs`

Purpose: Session-based authorization for API controllers

Usage:

- Applied to FrameworkManagementController
- Applied to RuntimeManagementController
- Validates active DSM session before allowing access

---

## Performance Optimization

### Benchmarking Results

**String Building (URL Construction):**

| Method | Allocated Memory | GC Cuts |
|--------|------------------|---------|
| Interpolated String | Higher | More frequent |
| StringBuilder | Lower | Less frequent |

**Recommendation:** Use `StringBuilder` for complex URL/parameter building with multiple concatenations.

### Caching Strategy

**Current Implementation:**

- **ApiInformations Cache:** DSM API metadata cached after handshake
- **Instance Cache:** In-memory `ConcurrentDictionary` for website instances
- **Configuration Cache:** JSON file read on startup, in-memory during runtime

**Potential Improvements:**

- Implement distributed caching for multi-instance deployments
- Add HTTP response caching for static assets
- Consider Redis for session storage in production

### Async/Await Pattern

All I/O operations use async/await:

```csharp
// DSM API calls
public async Task<bool> ConnectAsync(LoginCredentials model)

// File operations
public async Task<ApiResult> SetHttpGroupPermissionsAsync(string path, bool recursive)

// Process management
public async Task<ApiResult> StartSiteAsync(WebSiteInstance instance)
```

**Benefits:**

- Non-blocking I/O
- Better scalability under load
- Responsive UI during long operations

---

## Build & Deployment

### Build Configuration

**Standardized Commands:**

```bash
# Build
dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx

# Clean
dotnet clean /nr:false ./src/Askyl.Dsm.WebHosting.slnx

# Publish (for deployment)
dotnet publish ./src/Askyl.Dsm.WebHosting.Ui/Askyl.Dsm.WebHosting.Ui.csproj -c Release -o ./publish
```

**Build Properties:**

- Centralized versioning via `Directory.Build.props`
- Debug symbols disabled in Release builds
- No XML documentation generation (to reduce build time)

### Deployment Targets

1. **Synology DSM 7.2+**
   - x64 architecture (tested)
   - armv7/armv8 (built but untested)
   - Package format: SPK

2. **Deployment Path:** `/usr/local/Askyl.Dsm.WebHosting/`

3. **Configuration Files:**
   - `websites.json` - Persistent website configurations
   - `appsettings.json` - Application settings
   - `/etc/synoinfo.conf` - DSM system config (read-only)

### SPK Packaging

**Process:**

1. Build for target architecture (x64/armv7/armv8)
2. Package files into SPK format using Synology's packaging tools
3. Sign package with developer certificate
4. Upload to Synology Package Center or manual installation

---

## Recommendations

### Immediate Priorities

1. **Unit Test Implementation — Partially Complete**
   - ✅ 187 tests across 9 phases (Data validation, domain, Result types, threading, extensions, I/O, parsing, platform)
   - ⏳ Deferred: `DsmApiClient` (no interface), `DownloaderService` (external library), `WebSiteHostingService` (complex orchestration)
   - See `docs/ai/test-plan-2026-05-04.md` for results and coverage gaps

2. **Certificate Management**
   - Add UI for SSL certificate selection per website
   - Integrate with DSM's certificate API
   - Support Let's Encrypt automation

3. **Enhanced Logging**
   - Add correlation IDs for request tracing
   - Implement log aggregation (e.g., ELK stack)
   - Route application stdout/stderr to downloadable logs

### Medium-Term Improvements

1. **Multi-Language Support**
   - Implement resource files (.resx) for UI strings
   - Add language selection in settings
   - Support RTL layouts

2. **Health Checks**
   - Add `/health` endpoint for monitoring
   - Check website responsiveness
   - Monitor DSM API connectivity

3. **Configuration Migration**
   - Version `websites.json` schema
   - Implement migration tool for schema evolution
   - Add backup/restore functionality

### Long-Term Vision

1. **Database Integration**
   - Migrate from JSON to SQLite/PostgreSQL
   - Enable complex queries and reporting
   - Support multi-user scenarios

2. **Advanced Features**
   - Deploy from compressed files (ZIP/TAR)
   - Application templates marketplace
   - Automated backups and restores

3. **Architecture Evolution**
   - Consider CQRS pattern for scalability
   - Implement event sourcing for audit trail
   - Evaluate full WebAssembly migration (.NET 10+)

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

**Authentication:**

- `auth.login` - User authentication
- `auth.logout` - Session termination
- `auth.multifactor.login` - OTP authentication

**FileStation:**

- `util.list` - Directory listing
- `file.download` - File download
- `core.acl.set` - ACL permission setting

**ReverseProxy:**

- `list` - List all proxies
- `add` - Create proxy rule
- `set` - Update proxy rule
- `delete` - Remove proxy rule

### C. Version History

| Version | Date | Changes |
|---------|------|---------|
| 0.5.7 | May 11, 2026 | Dead code sweep: removed `ApiGenericResponse`, `PaginationDefaults`, `LicenseRoutes`, `DirectoryFilesResult`, `DsmToolsExtensions`; removed NuGet packages `Microsoft.AspNetCore.Mvc.Versioning` and `Microsoft.FluentUI.AspNetCore.Components.Emoji`; preserved `EmptyResponse` as standalone type (used by `DsmApiClient`); cleaned stale references in Constants tree diagram and Tools Extensions listing |
| 0.5.4 | May 1, 2026 | Replaced custom `CloneGenerator` source generator with C# records (`init` setters) — 41 classes converted, `GenerateCloneAttribute`/`IGenericCloneable<T>` removed, `ApiParametersBase<T>` simplified (no cloning needed with immutability); SiteLifecycleManager hardening: lifecycle manager recreation on config update (stale config fix), removed vestigial `.Clone()` calls (Interactive WASM has no shared memory), removed `ProcessTimeoutSeconds` (inlined 10s constant), added `IOException` to `StopAsync` exception filter, `ProcessInfo` converted to snapshot record, parallel startup in `StartEligibleSitesAsync`, `CancellationToken` forwarding in `StopAllSitesAsync` and `GetRuntimeStateAsync`, removed dead null check and misleading `CancellationToken` from `StartAsync`; **post-review fixes**: `RemoveInstanceAsync` restructured to remove persistent config before in-memory state (prevents orphaned configs on failure), `StopAllSitesAsync` wraps `Dispose()` in `finally` (prevents `SemaphoreSlim` leak on exception), `StartAsync` disposes stale `_process` handle before restart (prevents handle leak on crash-restart cycles); **SiteLifecycleManager concurrency rewrite**: replaced `SemaphoreSlim` + `ISemaphoreOwner` with `Channel<LifecycleCommand>` + single consumer loop — eliminates TOCTOU races, no `ObjectDisposedException` boilerplate, safe disposal via queued `DisposeCommand`, `ConfigurationRequiresRestart` uses order-independent dictionary comparison |
| 0.5.4 | April 29, 2026 | SIGTERM process termination fix: added `ProcessTerminator` utility (cross-platform SIGTERM via P/Invoke), replaced blocking `WaitForExit` with async `WaitForExitAsync`, reduced timeouts (HttpClient 90→15s, Process 60→10s) — eliminates DSM reverse proxy 504 errors |
| 0.5.4 | April 25, 2026 | Synchronized with codebase: added `SiteLifecycleManager` two-tier process architecture (graceful shutdown, force kill fallback), documented `DirectoryFilesResult`, `WebSiteRuntimeState`, `DotnetInfoParserConstants`; removed version column from Technical Stack table; cleaned up stale empty directory references |
| 0.5.4 | April 5, 2026 | Architecture documentation synchronized with codebase; corrected service lifetimes, added SemaphoreLock and AuthorizeSessionAttribute documentation |
| 0.5.3 | March 2026 | Architecture documentation update, version bump |
| 0.5.2 | Earlier | Initial architecture documentation |
| ... | ... | Previous versions |
