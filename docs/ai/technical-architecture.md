# ASkyl.Dsm.WebHosting - Technical Architecture Document

**Target Framework:** .NET 10 (net10.0)
**Last Updated:** June 21, 2026 (Over-engineering reduction completed: Benchmarks removed, API constants merged,
PHP converters consolidated, UriExtensions inlined, LicenseConstants inlined, two interfaces dropped,
DsmParameterNameAttribute replaced with virtual property, LocalizedText+ResourceManagerCache removed,
ILocalizer returns string directly, 13 new tests added for LogDownload/License/TreeContent services)

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
- ⏳ **Multi-language support** (June 2026 — Phases 1-9 complete, Phase 10 remaining):
  - ✅ Globalization assembly with `SharedResource.resx` + satellite assemblies (fr-FR)
  - ✅ Server-side culture discovery via satellite assembly directory scanning
  - ✅ Culture injection to WASM via `Blazor.start()` environment variable
  - ✅ `ICultureManager` (in `Data.Contracts`) — resolves culture once at login (DSM-controlled, no runtime switching)
  - ✅ `AcceptLanguageHandler` — propagates culture to server via HTTP headers
  - ✅ `RequestLocalization` middleware — server reads `Accept-Language` from WASM
  - ✅ FluentValidation migration (Phase 7) — shared validators in Globalization assembly
  - ✅ Culture-aware formatting (Phase 9) — date/time format preferences flow from DSM UserSettings to WASM
  - ⏳ End-to-end testing & validation (Phase 10)
- ✅ Unit test implementation (10 phases complete — May 2026)
- ✅ **IProcessRunner abstraction** for SiteLifecycleManager — co-located interface + implementation (ProcessRunner.cs, ProcessHandle.cs), enables full unit testing of process lifecycle
- ✅ **LoggerMessage migration** — 168 source-generated `[LoggerMessage]` extension methods across 19 source files; zero CA2254 warnings
- ✅ **DSM API logging** — request timing, authentication failures, and API errors logged via `[LoggerMessage]` extensions; compile-time `IApiResponse` constraint replaces reflection
- ✅ **Serilog configuration** — output template with `{EventId}`, `Log.CloseAndFlush()` on graceful shutdown, `WithActivity` enricher for correlation tracking
- ✅ **OperationTimer** — value-type disposable timer for scope-based duration logging across all services; replaced manual `Stopwatch` boilerplate with single-line `using var` pattern
- ✅ **Runtime detection** (May 22, 2026) — `AssemblyRuntimeDetector` parses `*.runtimeconfig.json` to detect
  required .NET version; blocks site start if incompatible; framework column on Home grid;
  `RequiredFramework` on instance only (not persisted)
- ✅ **ProcessLoggingExtensions** renumbered with sub-range spacing (1600xxx–1604xxx) to allow inserting log messages per region
- ✅ **SiteEntry pair class** — `WebSiteHostingService` uses `ConcurrentDictionary<Guid, SiteEntry>` pairing instance + lifecycle manager; eliminates parallel dictionary synchronization
- ✅ **Session validation** (May 25, 2026):
  - ✅ Async authorization filter validates against DSM server (`SYNO.Core.User.get`)
  - ✅ 1-minute TTL cache matches DSM minimum session timeout
  - ✅ `DsmUsername` stored alongside `DsmSid` for defense-in-depth
  - ✅ `IsAuthenticatedAsync()` consolidated (replaces `IsSessionValidAsync`)

**Security Score:** ⭐⭐⭐⭐⭐ (5/5) - Production-ready (all 12 security phases complete)

---

## Solution Overview

### Solution Structure

```text
Askyl.Dsm.WebHosting.slnx
├── Askyl.Dsm.WebHosting.Constants          # Centralized constants & enums
├── Askyl.Dsm.WebHosting.Data               # Core data layer, API definitions, services
├── Askyl.Dsm.WebHosting.Globalization      # Localization resources, validators, culture management, C# 14 scoped extensions
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
<!-- Centralized versioning (see src/Directory.Build.props for current value) -->
<Version>0.5.x</Version>
<AssemblyVersion>0.5.x.0</AssemblyVersion>
<FileVersion>0.5.x.0</FileVersion>
<InformationalVersion>0.5.x</InformationalVersion>
<PackageVersion>0.5.x</PackageVersion>

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

<!-- Enable C# 14 preview features (scoped extension keyword) -->
<EnablePreviewFeatures>true</EnablePreviewFeatures>
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
│   ├── ApplicationConstants.cs             # App paths, URLs, HTTP client names, session (DsmSid, DsmUsername, TTL)
│   ├── DotnetInfoParserConstants.cs        # dotnet --info section headers and framework identifiers
│   ├── InfrastructureConstants.cs          # Directory names (Downloads)
│   ├── LogConstants.cs                     # Log directory and file paths
│   ├── SecurityHeaders.cs                  # HTTP security header values (CSP, X-Frame-Options, etc.)
│   ├── ValidationConstants.cs              # Validation message constants (path traversal, version format, env vars)
│   └── WebSiteConstants.cs                 # Website config, process lifecycle, port validation
├── Globalization/                          # Culture and localization constants (1 file)
│   └── GlobalizationConstants.cs           # Default culture/language, text direction (LTR/RTL), env var names, Accept-Language header
├── DSM/                                    # Synology DSM-specific constants (7 files)
│   ├── API/                                # API-related constants
│   │   ├── ApiConstants.cs                 # Merged API names, methods, and version ranges (ImmutableDictionary + const fields)
│   │   ├── DsmConstants.cs                 # Shared DSM error codes (ErrorCodeAuthenticationFailed = -4)
│   │   ├── PhpDotNetFormatTokens.cs        # PHP → .NET format token mappings (ImmutableDictionary, data only)
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
| **Application** | SettingsFileName, HttpClientName, ApplicationSubPath ("adwh"), DsmSid, DsmUsername, SessionValidationTtlMinutes | ~30 |
| **Globalization** | DefaultCulture, DefaultCultureInfo, DefaultLanguageTag, TextDirectionLtr/Rtl, SupportedCultures/SystemCulture env vars, AcceptLanguageHeader | 7 |
| **Websites** | Process timeouts, port range (1024-65535), WellKnownWebPorts [80, 443], environment vars, validation messages | ~26 |
| **DSM APIs** | 8 API names (incl. Core.User), CRUD methods, version ranges (merged ApiConstants), PHP→.NET format tokens, shared error codes | ~37 + 1 enum + 1 ImmutableDictionary |
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

**Purpose:** Core data layer, API definitions, domain services, and result types (12 service contracts)

**Complete Service Contracts Inventory:**

| Interface | Source File | Key Methods | Implemented By |
|-----------|-------------|-------------|----------------|
| **IAuthenticationService** | `Contracts/IAuthenticationService.cs` | LoginAsync(), LogoutAsync(), IsAuthenticatedAsync() (validates against DSM server) | Ui.Services.AuthenticationService, Ui.Client.Services.AuthenticationService |
| **IDotnetVersionService** | `Contracts/IDotnetVersionService.cs` | GetInstalledVersionsAsync(), GetChannelsAsync() | Ui.Services.DotnetVersionService, Ui.Client.Services.DotnetVersionService |
| **IFileSystemService** | `Contracts/IFileSystemService.cs` | GetSharedFoldersAsync(), GetDirectoryContentsAsync() | Ui.Services.FileSystemService, Ui.Client.Services.FileSystemService |
| **IFrameworkManagementService** | `Contracts/IFrameworkManagementService.cs` | InstallFrameworkAsync(), UninstallFrameworkAsync() | Ui.Services.FrameworkManagementService |
| **ILogDownloadService** | `Contracts/ILogDownloadService.cs` | CreateLogZipStreamAsync() | Ui.Services.LogDownloadService |
| **IReverseProxyManagerService** | `Contracts/IReverseProxyManagerService.cs` | CreateAsync(), UpdateAsync(), DeleteAsync() | Ui.Services.ReverseProxyManagerService |
| **IWebSiteHostingService** | `Contracts/IWebSiteHostingService.cs` | GetAllWebsitesAsync(), AddWebsiteAsync() | Ui.Services.WebSiteHostingService, Ui.Client.Services.WebSiteHostingService |
| **IFileManagerService** | `Contracts/IFileManagerService.cs` | Initialize(), GetDirectory(), DeleteDirectory(), GetFullName() | Tools.Infrastructure.FileManagerService |
| **IArchiveExtractorService** | `Contracts/IArchiveExtractorService.cs` | Decompress(inputFile, exclude) | Tools.Infrastructure.ArchiveExtractorService |
| **IDownloaderService** | `Contracts/IDownloaderService.cs` | DownloadToAsync(), DownloadVersionToAsync(), GetAspNetCoreReleasesAsync() | Tools.Runtime.DownloaderService |
| **IVersionsDetectorService** | `Contracts/IVersionsDetectorService.cs` | GetInstalledVersionsAsync(), IsChannelInstalled(), RefreshCacheAsync() | Tools.Runtime.VersionsDetectorService |
| **IAssemblyRuntimeDetector** | `Contracts/IAssemblyRuntimeDetector.cs` | Detect() | Tools.Runtime.AssemblyRuntimeDetector |

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
│   ├── IFileManagerService.cs              # File management (Scoped, configurable root)
│   ├── IArchiveExtractorService.cs         # Archive extraction (Scoped)
│   ├── IDownloaderService.cs               # .NET downloads with cancellation (Scoped)
│   ├── IVersionsDetectorService.cs         # Version detection with smart caching (Singleton)
│   └── IAssemblyRuntimeDetector.cs         # Runtime detection from *.runtimeconfig.json (Singleton)
├── Domain/                                 # Domain models
│   ├── Authentication/                     # Auth-related domain models
│   │   └── LoginCredentials.cs             # Login credentials
│   ├── FileSystem/                         # File system models
│   │   └── FsEntry.cs                      # File system entry model
│   ├── Licensing/                          # License information
│   │   └── LicenseInfo.cs                  # License data model
│   ├── Runtime/                            # .NET runtime information
│   │   ├── AspNetChannel.cs                # .NET channel info
│   │   ├── AspNetCoreReleaseInfo.cs        # Release version details
│   │   ├── AspNetRelease.cs                # Release metadata
│   │   ├── AssemblyRuntimeInfo.cs          # Detected runtime info (channel, compatibility, error message)
│   │   ├── FrameworkInfo.cs                # Framework metadata
│   │   └── InstallFramework.cs             # Framework installation target
│   └── WebSites/                           # Website management domain
│       ├── ProcessInfo.cs                  # Process runtime snapshot (Id, IsResponding)
│       ├── WebSiteConfiguration.cs         # Main config model (settings only — no runtime state)
│       ├── WebSiteInstance.cs              # Runtime instance (owns RequiredFramework — not persisted)
│       ├── WebSiteRuntimeState.cs          # Immutable record for site state (Running/Stopped/NotResponding)
  │       ├── WebSiteInstanceDetails.cs       # Website instance details for UI
│       ├── WebSitesConfiguration.cs        # Persistent configuration store
├── DsmApi/                                 # DSM API integration
│   ├── Models/                             # API models (records with init setters)
│   │   ├── Auth/                           # Authentication models
│   │   │   └── AuthenticateLogin.cs        # Login request payload
│   │   ├── Core/                           # Core API models
│   │   │   ├── Acl/                        # ACL models (CoreAclSet, Rule, Permission, Inherit)
│   │   │   ├── ApiInformation.cs           # API information model
│   │   │   ├── ApiInformationCollection.cs # API collection wrapper
│   │   │   ├── ApiInformationQuery.cs      # Query parameters
│   │   │   └── User/                       # User models
│   │   │       └── CoreUserGetEntry.cs     # User get request payload
│   │   ├── FileStation/                    # 9 file operation models
│   │   └── ReverseProxy/                   # Proxy configuration models
│   ├── Parameters/                         # Request parameter classes
│   │   ├── Auth/                           # Authentication parameters
│   │   │   └── AuthLoginParameters.cs      # Login request (SYNO.API.Auth.login)
│   │   ├── Core/                           # Core API parameters
│   │   │   ├── Acl/                        # ACL parameters
│   │   │   │   └── CoreAclSetParameters.cs # ACL set request (SYNO.Core.Acl.set)
│   │   │   ├── AppPortal/                  # AppPortal parameters
│   │   │   │   └── ReverseProxy/           # Reverse proxy CRUD
│   │   │   │       ├── ReverseProxyCreateParameters.cs
│   │   │   │       ├── ReverseProxyDeleteParameters.cs
│   │   │   │       ├── ReverseProxyListParameters.cs
│   │   │   │       └── ReverseProxyUpdateParameters.cs
│   │   │   └── User/                       # User parameters
│   │   │       └── CoreUserGetParameters.cs # User get request (SYNO.Core.User.get)
│   │   ├── FileStation/                    # 2 file operation parameters
│   │   ├── Info/                           # API info queries
│   │   │   └── InformationsQueryParameters.cs # System info query (SYNO.Core.Info.query)
│   │   ├── ApiParametersBase.cs            # Base parameter class
│   │   ├── ApiParametersNone.cs            # No-parameters wrapper
│   │   └── IApiParameters.cs               # Parameter interface
│   └── Responses/                          # API response wrappers
│       ├── ApiInformationResponse.cs       # API info query response
│       ├── ApiResponseBase.cs              # Generic response base with Error model
│       ├── Auth/                           # Authentication responses
│       │   └── AuthLoginResponse.cs        # Login response (sid)
│       ├── Core/                           # Core API responses
│       │   ├── Acl/                        # ACL responses
│       │   │   └── CoreAclSetResponse.cs   # ACL set response (task_id)
│       │   ├── AppPortal/                  # AppPortal responses
│       │   │   └── ReverseProxy/           # Reverse proxy responses
│       │   │       └── ReverseProxyListResponse.cs # Proxy list response
│       │   └── User/                       # User responses
│       │       └── CoreUserGetResponse.cs  # User get response (users[])
│       └── FileStation/                    # FileStation responses
│           ├── FileStationListResponse.cs  # File list response
│           └── FileStationListShareResponse.cs # Share list response
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
├── Converters/                             # Format/language converters
│   ├── DsmLanguageToCultureConverter.cs    # Conversion logic: DSM 3-letter language code → .NET culture name (returns null for "def", logs on trim)
│   └── PhpFormatToDotNetConverter.cs       # Consolidated converter: PHP date/time tokens → .NET format strings (uses PhpDotNetFormatTokens from Constants)
├── Extensions/                             # Extension methods
│   ├── ApiResponseExtensions.cs            # Response mapping helpers
│   └── HttpClientExtensions.cs             # HTTP client helpers (C# 14 scoped `extension(HttpClient)`)
├── Infrastructure/                         # Infrastructure utilities
│   ├── ArchiveExtractorService.cs          # gzip + tar extraction (implements IArchiveExtractorService)
│   ├── FileManagerService.cs               # File system initialization (implements IFileManagerService)
│   ├── PlatformInfoService.cs              # Platform detection (no interface, direct injection)
│   ├── ProcessHandle.cs                    # IProcessHandle + SystemProcessHandle (co-located)
│   ├── ProcessRunner.cs                    # IProcessRunner + SystemProcessRunner (co-located)
│   └── ProcessTerminator.cs                # Cross-platform process termination (SIGTERM/CloseMainWindow)
├── Network/                                # Network communication
│   └── DsmApiClient.cs                     # Centralized DSM API client
├── Diagnostics/                            # Diagnostic utilities
│   └── OperationTimer.cs                   # Disposable scope timer (Stopwatch + callback on Dispose)
├── Runtime/                                # .NET runtime management (DI-based)
    ├── DownloaderService.cs                # Binary download utility (implements IDownloaderService)
    ├── VersionsDetectorService.cs          # Version detection with smart caching (implements IVersionsDetectorService)
    └── AssemblyRuntimeDetector.cs          # Runtime detection from *.runtimeconfig.json (implements IAssemblyRuntimeDetector)
└── Threading/                              # Async coordination utilities
    └── SemaphoreLock.cs                    # Semaphore-based async locking utility

```

**Infrastructure Services Architecture:**

The Tools project contains DI-based infrastructure services for platform detection, file management, archive extraction, and .NET runtime operations.

| Service | Interface | Lifetime | Key Features | Dependencies | Source File |
|---------|-----------|----------|--------------|--------------|-------------|
| **PlatformInfoService** | _(no interface, dropped)_ | Singleton | Platform detection, config loading | ILogger | `Tools/Infrastructure/PlatformInfoService.cs` |
| **FileManagerService** | `IFileManagerService` | Scoped | Directory management, configurable root path | ILogger, string rootPath | `Tools/Infrastructure/FileManagerService.cs` |
| **ArchiveExtractorService** | `IArchiveExtractorService` | Scoped | tar.gz extraction | IFileManagerService, ILogger | `Tools/Infrastructure/ArchiveExtractorService.cs` |
| **DownloaderService** | `IDownloaderService` | Scoped | .NET runtime downloads with cancellation | PlatformInfoService, IFileManagerService | `Tools/Runtime/DownloaderService.cs` |
| **VersionsDetectorService** | `IVersionsDetectorService` | Singleton | Smart caching for dotnet --info | ILogger, ISemaphoreOwner | `Tools/Runtime/VersionsDetectorService.cs` |
| **SystemProcessRunner** | `IProcessRunner` | Singleton | Spawns OS processes, creates SystemProcessHandle | ILogger, ILoggerFactory | `Tools/Infrastructure/ProcessRunner.cs` |
| **SystemProcessHandle** | `IProcessHandle` | Transient (per-process) | Wraps `Process` for testability, graceful shutdown | `ILogger<ILogSystemProcessHandle>` | `Tools/Infrastructure/ProcessHandle.cs` |

**Process Lifecycle Services:**

The `SystemProcessRunner` requires `ILoggerFactory` to create child loggers for `SystemProcessHandle` instances.

This is because `ILogger<ILogSystemProcessRunner>` and `ILogger<ILogSystemProcessHandle>` are distinct closed generic types — an invalid cast would throw `InvalidCastException` at runtime.

The runner uses `loggerFactory.CreateLogger<ILogSystemProcessHandle>()` to produce correctly-typed loggers for each spawned process.

> **Why `ILoggerFactory`?** — `ILogger<T>` is a closed generic type.
> Casting `ILogger<ILogSystemProcessRunner>` to `ILogger<ILogSystemProcessHandle>` throws `InvalidCastException` at runtime.
> The factory creates the correct logger type.

```csharp
// SystemProcessRunner requires ILoggerFactory to create correctly-typed child loggers
return new SystemProcessHandle(
    loggerFactory.CreateLogger<ILogSystemProcessHandle>(), process);
```

```text
SystemProcessRunner (ILoggerFactory)
    └── Creates SystemProcessHandle per spawned process
            └── Logs process events via ILogger<ILogSystemProcessHandle>
```

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
  - User preferences fetch failure logging (best-effort, Debug level) via `FetchUserPreferencesFailed` (EventId 2000013)
- HttpClient factory integration for proper lifecycle management
- All infrastructure services testable via interface abstractions

**`IApiResponse` Interface:**

Defined in `Data/DsmApi/Responses/ApiResponseBase.cs`. All DSM API response types implement `IApiResponse` via `ApiResponseBase<T>`.

This enables compile-time access to `Success` and `Error` properties — replacing reflection with type-safe error handling.

**Config Parsing:** `ReadSettings()` uses `Split(new[] { '=' }, 2)` to preserve values containing `=` (base64 data, URLs). Keys consumed: `external_host_ip`, `language`, `external_port_dsm_https`.

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
│   ├── SiteLifecycleManager.cs             # Per-site process management (start/stop, graceful shutdown, force kill, framework validation on start)
│   ├── WebSiteHostingService.cs            # Website orchestration (framework detection on init, delegates lifecycle to SiteLifecycleManager, SiteEntry pairs instance + manager)
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
│   │   └── NotFound.razor                  # 404 handler — FluentUI centered page with localized home link
│   └── Patterns/                           # UI patterns
│       └── WorkingState/                   # IWorkingState interface and CreateWorkingState extension
├── Contracts/                              # Client-side service contracts
│   └── INavigationGuard.cs                 # Router navigation guard interface (OnNavigateAsync, OnNavigate)
├── Extensions/                             # Client-side extension methods
│   └── FsEntryExtensions.cs                # File system entry extension methods
├── Interfaces/                             # C# interfaces for JS interop
├── Services/                               # HTTP client wrappers + culture management (10 services)
│   ├── AcceptLanguageHandler.cs            # DelegatingHandler — attaches Accept-Language header from ICultureManager
│   ├── AuthenticationService.cs            # Singleton - POST /api/authentication/login, logout, status
│   ├── AuthenticationNavigationGuard.cs    # Singleton - INavigationGuard impl, Router OnNavigateAsync guard, enforces auth before render (no flash)
│   ├── CultureManager.cs                   # ICultureManager impl — safe static init, resolves culture at login, clones with date/time formats
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
└── Routes.razor                            # Router with OnNavigateAsync auth guard, AppAssembly route discovery, MainLayout default
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

// Singleton - Router navigation guard (enforces auth before render)
builder.Services.AddSingleton<AuthenticationNavigationGuard>();

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

### 6. Askyl.Dsm.WebHosting.Logging

**Purpose:** Logging extensions with source-generated logger methods

**Key Features:**

- **Source-generated log methods** for compile-time message validation
- **Extension method pattern** for clean logger API
- **Structured logging** support with named parameters
- **Zero-allocation logging** for performance-critical paths
- **Namespace-level category interfaces** — empty marker interfaces (e.g., `ILogAuthenticationService`) for `ILogger<T>` categorization, keeping Logging as a leaf node with zero project references
- **Specialized `ILogger<T>`** — each service injects `ILogger<ILogXxx>` for automatic log categorization by service name
- **Server/Client folder separation** — `Server/` contains extensions for server-side services; `Client/` contains extensions for WebAssembly client-side components

**Project Structure:**

```text
Logging/
├── Server/                                 # Server-side logging extensions
│   ├── Authentication/                     # AuthenticationService
│   │   └── AuthenticationLoggingExtensions.cs
│   ├── DsmApi/                             # DsmApiClient
│   │   └── DsmApiLoggingExtensions.cs
│   ├── FileManagement/                     # FileStation-related services
│   │   ├── FileManagerServiceLoggingExtensions.cs
│   │   ├── FileSystemServiceLoggingExtensions.cs
│   │   └── LogDownloadServiceLoggingExtensions.cs
│   ├── Framework/                          # .NET framework services
│   │   ├── DotnetVersionServiceLoggingExtensions.cs
│   │   └── FrameworkManagementLoggingExtensions.cs
│   ├── Infrastructure/                     # Infrastructure services
│   │   ├── ArchiveExtractorLoggingExtensions.cs
│   │   ├── DownloaderLoggingExtensions.cs
│   │   ├── PlatformInfoLoggingExtensions.cs
│   │   └── VersionsDetectorLoggingExtensions.cs
│   ├── ProcessLifecycle/                   # Process management
│   │   ├── ProcessHandleLoggingExtensions.cs
│   │   ├── ProcessLoggingExtensions.cs
│   │   └── ProcessRunnerLoggingExtensions.cs
│   ├── ReverseProxy/                       # Reverse proxy management
│   │   └── ReverseProxyLoggingExtensions.cs
│   └── WebsiteHosting/                     # Website hosting services
│       ├── ConfigurationLoggingExtensions.cs
│       └── WebsiteLoggingExtensions.cs
└── Client/                                 # Client-side (WASM) logging extensions
    └── ClientLoggingExtensions.cs          # Home, dialogs, license service
```

**EventId Management:**

All `[LoggerMessage]` attributes use inline `int` literals (per Microsoft convention).
EventId ranges are documented in `Constants/Logging/LogEventIds.cs`.
Each service owns a dedicated 100K range at 1M spacing to prevent cross-service collisions:

| Range | Service | Extension File | Folder |
|-------|---------|----------------|--------|
| `1000001–1000007` | AuthenticationService | `AuthenticationLoggingExtensions.cs` | `Server/Authentication/` |
| `1100001–1100012` | FileSystemService | `FileSystemServiceLoggingExtensions.cs` | `Server/FileManagement/` |
| `1200001–1200006` | FileManagerService | `FileManagerServiceLoggingExtensions.cs` | `Server/FileManagement/` |
| `1300001–1300007` | LogDownloadService | `LogDownloadServiceLoggingExtensions.cs` | `Server/FileManagement/` |
| `1400001–1400007` | FrameworkManagementService | `FrameworkManagementLoggingExtensions.cs` | `Server/Framework/` |
| `1500001–1500007` | DotnetVersionService | `DotnetVersionServiceLoggingExtensions.cs` | `Server/Framework/` |
| `1600001–1600019` | SiteLifecycleManager | `ProcessLoggingExtensions.cs` | `Server/ProcessLifecycle/` |
| `1700001–1700013` | ReverseProxyManagerService | `ReverseProxyLoggingExtensions.cs` | `Server/ReverseProxy/` |
| `1800001–1800031` | WebSiteHostingService | `WebsiteLoggingExtensions.cs` | `Server/WebsiteHosting/` |
| `1900001–1900012` | WebSitesConfigurationService | `ConfigurationLoggingExtensions.cs` | `Server/WebsiteHosting/` |
| `2000001–2000013` | DsmApiClient | `DsmApiLoggingExtensions.cs` | `Server/DsmApi/` |
| `2100001–2100006` | ArchiveExtractorService | `ArchiveExtractorLoggingExtensions.cs` | `Server/Infrastructure/` |
| `2200001–2200004` | VersionsDetectorService | `VersionsDetectorLoggingExtensions.cs` | `Server/Infrastructure/` |
| `2250001–2250005` | AssemblyRuntimeDetector | `AssemblyRuntimeDetectorLoggingExtensions.cs` | `Server/Infrastructure/` |
| `2300001–2300002` | PlatformInfoService | `PlatformInfoLoggingExtensions.cs` | `Server/Infrastructure/` |
| `2400001–2400004` | DownloaderService | `DownloaderLoggingExtensions.cs` | `Server/Infrastructure/` |
| `2500001` | SystemProcessRunner | `ProcessRunnerLoggingExtensions.cs` | `Server/ProcessLifecycle/` |
| `2600001–2600005` | SystemProcessHandle | `ProcessHandleLoggingExtensions.cs` | `Server/ProcessLifecycle/` |

**Client-Side Logging (WebAssembly):**

Client-side components use `ClientLoggingExtensions.cs` for structured logging in the WebAssembly runtime.

| Range | Service | Category Marker |
|-------|---------|-----------------|
| `7000001` | LicenseService | `ILogLicenseService` |

**Total:** 168 `[LoggerMessage]` methods across 19 source files (18 server + 1 client), zero CA2254 warnings.

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

**Architectural Trade-off — Singleton `DsmApiClient`:**

`DsmApiClient` is registered as Singleton despite holding per-session state (`_sid`, `_httpClient` cookie header, session validation cache). This is intentional because:

1. **Shared `ApiInformations`:** API discovery cache is expensive to re-fetch (handshake call)
2. **`HttpClient` reuse:** Named client with configured `BaseAddress` and timeouts — benefits from connection pooling
3. **`BackgroundService` anchor:** `WebSiteHostingService` (Singleton) depends on services using `DsmApiClient`. `IHostedService` is always Singleton.

**Mitigation:** `SetSid()` updates `_sid` + cookie header. Session validation cache uses a 1-minute TTL. Multi-user scenarios would need a Scoped wrapper.

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
├── Orchestrates website instances via ConcurrentDictionary<Guid, SiteEntry>
├── SiteEntry pairs WebSiteInstance + SiteLifecycleManager (eliminates parallel dictionary sync)
├── Loads configurations from JSON on startup
├── Detects required framework on init (sets RequiredFramework on instance — not persisted)
├── Manages instance lifecycle (add/update/remove)
└── Delegates per-site process management to SiteLifecycleManager

SiteLifecycleManager (Per-instance, Thread-safe)
├── Starts/stops .NET web application processes via IProcessRunner abstraction (unit-testable)
├── Validates framework compatibility on start (blocks if incompatible)
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
- **Thread-safe operations** — `ConcurrentDictionary<Guid, SiteEntry>` for instance management
  (eliminates parallel dictionary sync); Channel-based command serialization in SiteLifecycleManager
  (eliminates TOCTOU races)
- **Idempotent stop** — Calling `StopAsync()` when already stopped returns success without error
- **Safe disposal** — `DisposeCommand` queues after all pending commands; `Dispose()` blocks until loop drains
- **Framework detection** — `IAssemblyRuntimeDetector.Detect()` called on init (sets `RequiredFramework` on instance) and on start (blocks if incompatible)

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
6. Session persisted in ASP.NET Core session (DsmSid + DsmUsername)
```

#### Session Validation

The `IsAuthenticatedAsync()` method performs server-side validation against the DSM to detect expired or revoked sessions:

```text
1. Check local session keys (DsmSid + DsmUsername) exist
2. Check validation cache (1-minute TTL — matches DSM minimum session timeout)
3. If cache expired: call SYNO.Core.User.get with cached username
4. Response: success (user found) or error -4 (invalid/expired SID)
5. Cache result for 1 minute to avoid per-request API overhead
6. Clear session keys and return false if validation fails
```

**API Choice Rationale:**

- `SYNO.API.Auth` only exposes `login` and `logout` — no `querySession` method (confirmed error 103 on DSM 7.2+)
- `SYNO.Core.User.get` is the lightest API that validates session state
- Returns error `-4` (Authentication Failed) for invalid/expired SID
- Accepts any non-auth error as valid (user-specific errors still mean SID is alive)

**Singleton Architectural Trade-off:**

`DsmApiClient` is intentionally Singleton despite holding per-session state (`_sid`, `_sessionValid`, `_lastSessionValidation`):

1. **Shared `ApiInformations`:** API discovery cache is expensive to re-fetch (handshake call)
2. **`HttpClient` reuse:** Named client with configured `BaseAddress` and timeouts — benefits from connection pooling
3. **`BackgroundService` anchor:** `WebSiteHostingService` (Singleton) depends on services using `DsmApiClient`. `IHostedService` is always Singleton.

**Mitigation:** `SetSid()` updates `_sid` and `_httpClient` cookie header. Session validation cache uses a 1-minute TTL. Multi-user scenarios would need a Scoped wrapper.

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
App.razor (Root — server-rendered shell)
├── FluentDesignTheme (System mode — light/dark theme)
└── FluentLayout (full-viewport layout container)
    └── Routes (InteractiveWebAssembly)
        └── MainLayout.razor (FluentMainLayout with Header/Body)
            └── Page Content
                ├── Home.razor (Dashboard with website grid)
                ├── Login.razor (Authentication)
                └── NotFound.razor (404 — FluentUI centered page with localized home link)

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

1. **Router-Level Navigation Guard**
   - `AuthenticationNavigationGuard` intercepts all navigation via `<Router OnNavigateAsync>`
   - Async auth check (`IsAuthenticatedAsync`) runs before any component renders
   - Login page path excluded from guard — all other routes require authentication
   - No cached auth state — re-checks on every navigation to avoid stale state after login
   - Eliminates flash of protected content before redirect to login

2. **Server-Side Session Storage**
   - DSM SID stored in server session (not client storage)
   - Username stored alongside SID for defense-in-depth (`DsmUsername`)
   - HttpOnly cookies prevent XSS attacks
   - SameSite=Strict prevents CSRF

3. **Server-Side Session Validation**
   - `IsAuthenticatedAsync()` validates both session keys and calls `SYNO.Core.User.get`
   - 1-minute TTL cache matches DSM minimum session timeout
   - Detects expired or revoked sessions via DSM server (error `-4`)
   - Clears session keys and redirects to login on validation failure

4. **Antiforgery & CSRF Protection**
   - Enabled for all Blazor components and API endpoints
   - SameSite=Strict documented on all 5 API controllers as primary CSRF defense
   - Token validation on state-changing operations

5. **HTTPS & HSTS Enforcement**
   - `UseHttpsRedirection()` in middleware pipeline
   - `UseHsts()` enabled for non-development environments (30-day max-age)
   - Default protocol for reverse proxy is HTTPS

### API Security

1. **Authorization Coverage**
   - `[AuthorizeSession]` applied to all 5 API controllers (WebsiteHosting, FileManagement, FrameworkManagement, RuntimeManagement, LogDownload)
   - `AuthenticationController` intentionally public for login/logout/status
   - Validates active DSM session (both session keys + server-side validation) before allowing access

2. **Input Validation**
   - Data annotations on all models
   - Server-side validation in services:
     - Path traversal prevention (`IsPathValid()` rejects `..` and encoded variants)
     - Version format validation (`IsValidVersionFormat()` prevents directory escape)
     - Environment variable limits (256 chars key, 4096 chars value)

3. **Rate Limiting**
   - Login endpoint throttled: 5 attempts per minute per IP
   - Prevents brute-force attacks against DSM credentials

4. **Error Handling & Information Disclosure**
   - Generic error messages (`OperationFailedErrorMessage`) returned to clients
   - Full exception details retained server-side via `[LoggerMessage]` extensions
   - Structured logging for debugging without leaking internal paths

5. **No Client-Side Secrets**
   - All DSM API calls go through server controllers
   - Credentials never exposed to browser

### File System Security

1. **HTTP Group Permissions**
   - Critical: Set before deploying applications
   - Ensures nginx can serve files

2. **Path Validation**
   - Validate all file paths against allowed directories
   - Prevent path traversal attacks via `IsPathValid()` helper

### Dependency & CI Security

1. **Automated Vulnerability Scanning**
   - Dependabot configured for weekly NuGet and GitHub Actions checks
   - CI pipeline includes `dotnet list package --vulnerable` step
   - Flags known vulnerabilities in pull requests

2. **Log Content Audit**
   - 180+ `[LoggerMessage]` methods audited across 19 files
   - Zero PII, secrets, or credentials logged
   - Structured logging with Serilog ensures safe diagnostic output

---

## Globalization & Localization

### Architecture Overview

Culture is **DSM-controlled** — resolved once at login from user/system preferences, then locked for the session. There is no runtime language switching in the UI.

The Globalization assembly serves two purposes:

1. **Localization resources** — `SharedResource.resx` with satellite assemblies per culture
2. **Shared validators** — FluentValidation validators used by both server and client

### Culture Flow

1. **Server discovers cultures** — `GlobalizationSettings` singleton (in `Ui/Infrastructure/`) scans `Globalization` assembly directory for satellite assembly subdirectories at construction time
2. **Server reads DSM system culture** — `ApplyDsmSystemCulture()` extension extracts `language` from DSM API, converts via `DsmLanguageToCultureConverter`, sets `IGlobalizationSettings.SystemCulture`
3. **Server injects to WASM** — supported cultures as JSON + system culture, injected via `Blazor.start()` using `dotnet.withEnvironmentVariable()` for `ADWH_SUPPORTED_CULTURES` and `ADWH_SYSTEM_CULTURE`
4. **WASM parses cultures** — `CultureManager` static initializer deserializes `ADWH_SUPPORTED_CULTURES` JSON env var (no server-side `GlobalizationSettings` dependency)
5. **CultureManager pre-resolves** — static fields capture `BrowserCulture` (from WASM runtime's auto-set `CurrentUICulture`), `SystemCulture`, and `SupportedCultures` (from env var) as `CultureInfo?`
6. **Early CultureManager resolution** — `Program.cs` forces DI resolution of `ICultureManager` before `host.RunAsync()` — sets `CurrentUICulture` before any page renders
7. **Login resolves culture** — priority: login response `Culture` → system culture → browser culture → default `en-US`
8. **WASM propagates to server** — `AcceptLanguageHandler` attaches `Accept-Language` header to all HTTP requests
9. **Server reads header** — `RequestLocalization` middleware sets thread culture per request

**html lang attribute** — set server-side in `App.razor` via `GetLanguageTag()`:

1. **DSM system culture** — from `IGlobalizationSettings.SystemCulture` (injected singleton)
2. **Accept-Language header** — parsed directly, matched against supported cultures (handles neutral languages like `fr` → `fr-FR`)
3. **Default** — `en`

**Why not `IRequestCultureFeature`?** — ASP.NET Core request localization doesn't match neutral languages (`fr`) to specific cultures (`fr-FR`), so we parse `Accept-Language` directly.

**Logout flow** — `forceLoad: true` triggers full page reload, which resets culture to system/browser resolution.

### Date/Time Format Flow

User-specific date/time format preferences flow from DSM UserSettings through to the WASM culture:

1. **Server fetches UserSettings** — `SYNO.Core.UserSettings.get` (best-effort, post-auth) extracts `Personal.dateFormat` and `Personal.timeFormat` (PHP-style format strings)
2. **Server converts formats** — `PhpFormatToDotNetConverter` converts PHP date/time tokens to .NET format strings using `PhpDotNetFormatTokens` from Constants (e.g., `"Y/m/d"` → `"yyyy/MM/dd"`)
3. **Server passes to WASM** — `AuthenticationResult` carries `DateFormat` and `TimeFormat` properties alongside `Culture`
4. **WASM applies formats** — `CultureManager.InitializeFromLogin()` clones the resolved `CultureInfo` and overrides `DateTimeFormat` patterns:
   - `ShortDatePattern` / `LongDatePattern` ← `DateFormat`
   - `ShortTimePattern` / `LongTimePattern` ← `TimeFormat`
5. **UI uses standard formats** — date columns use `Format="d"` (short date) and `Format="g"` (short date+time), which automatically respect the user's custom patterns

**Defensive coding:** All culture/format operations are wrapped in try-catch:

- `CultureNotFoundException` / `ArgumentException` → fall back to system culture
- `FormatException` → keep culture default patterns, log warning via `InvalidDateFormatIgnored` / `InvalidTimeFormatIgnored`

**Known limitation:** `SystemCulture` does not include date/time format preferences from system config.
The date/time format flow only applies when the user has explicit preferences in `SYNO.Core.UserSettings.get`.
System-level date/time format discovery is a future enhancement.

### Key Components

| Component | Location | Purpose |
|---|---|---|
| `ICultureManager` | `Data.Contracts` | Interface: `InitializeFromLogin(string?, string?, string?)`, `ResetToSystem()`, `CurrentCulture`, `CurrentUICulture` |
| `CultureManager` | `Ui.Client` | WASM implementation — safe static init (`SafeParseSupportedCultures`, `SafeGetBrowserCulture`, `SafeResolveSystemCultureFromEnv`), resolves culture at login, clones with date/time formats, updates `html lang` and `dir` via `GetTextDirection()` extension on every culture change |
| `IGlobalizationSettings` | `Data.Contracts` | Interface: `SupportedCultures`, `SupportedCultureNamesJson`, `SystemCulture` |
| `GlobalizationSettings` | `Ui/Infrastructure/` | Singleton service implementing `IGlobalizationSettings` — discovers cultures from satellite resources at construction, logs via `ILogger<ILogGlobalizationSettings>` |
| `AcceptLanguageHandler` | `Ui.Client` | `DelegatingHandler` that attaches `Accept-Language` header from `ICultureManager` |
| `GlobalizationExtensions` | `Ui/Extensions/` | `ApplyDsmSystemCulture()` — fetches DSM language and sets `SystemCulture`; `UseGlobalizationRequestLocalization()` — configures and adds middleware |
| `GlobalizationConstants` | `Constants/Globalization/` | `DefaultCulture`/`DefaultCultureInfo`, `TextDirectionLtr`/`TextDirectionRtl`, env var names, `AcceptLanguageHeader` |
| `CultureInfoExtensions` | `Globalization/Extensions/` | C# 14 scoped `extension(CultureInfo)` — `GetTextDirection()` returns LTR/RTL from `TextInfo.IsRightToLeft` |
| `GlobalizationServiceCollectionExtensions` | `Globalization/Extensions/` | C# 14 scoped `extension(IServiceCollection)` — `AddGlobalization()` registers `ILocalizer` singleton |
| `LocalizationKeys.cs` | `Globalization` | Strongly-typed keys organized by model (`L.WebSiteConfiguration.*`, `L.LoginCredentials.*`) |
| `ILocalizer` | `Globalization` | Abstraction interface — returns `string` directly via indexer, hides `ResourceManager` from consumer projects |
| `Localizer` | `Globalization` | Implementation of `ILocalizer` wrapping `ResourceManager` (not `IStringLocalizer`) — reads `CurrentUICulture` at call time, so culture changes after login work without re-rendering; missing keys return `[{key}]`; holds static `SharedResource` field |
| `DsmLanguageToCultureConverter` | `Tools/Converters/` | Conversion logic: DSM 3-letter language code → .NET culture name (returns `null` for `"def"`, logs via `Debug.WriteLine` when input is trimmed) |
| `PhpFormatToDotNetConverter` | `Tools/Converters/` | Consolidated converter: PHP date/time tokens → .NET format strings (uses `PhpDotNetFormatTokens` ImmutableDictionary from Constants project) |
| `WebSiteConfigurationValidator` | `Globalization/Validators/` | FluentValidation rules for `WebSiteConfiguration` (8 properties, separate messages for InternalPort/PublicPort) |
| `LoginCredentialsValidator` | `Globalization/Validators/` | FluentValidation rules for `LoginCredentials` (2 properties) |
| `DeferredMessageExtensions` | `Globalization/Validators/` | `WithLocalizedMessage()` extension — defers resource key resolution to validation time via `WithMessage(Func&lt;T, string&gt;)` so culture changes after login are respected |

### Validation Architecture

Validators are defined once in Globalization and consumed by both server and client — eliminating duplicate validation logic:

| Layer | Package | Registration | Behavior |
|-------|---------|--------------|----------|
| **Globalization** | `FluentValidation` + `FluentValidation.DependencyInjectionExtensions` | Contains both validators | Defines all rules with deferred localized messages via `WithLocalizedMessage()` extension — uses `WithMessage(Func<T, string>)` to resolve resource keys at validation time, ensuring culture changes after login are respected |
| **Server (Ui)** | `FluentValidation.AspNetCore` | `AddFluentValidationAutoValidation()` | Auto-populates ModelState; invalid POST returns 400 Bad Request |

**Dependency Graph:**

```text
Globalization → Data (for domain models)
Globalization → FluentValidation, FluentValidation.DependencyInjectionExtensions
Ui (server) → Globalization + FluentValidation.AspNetCore + Data.Contracts (IGlobalizationSettings)
Ui.Client (WASM) → Globalization + Data.Contracts (ICultureManager)
```

**Registration (Program.cs):**

```csharp
// Server (Ui/Program.cs)
builder.Services.AddValidatorsFromAssemblyContaining<SharedResource>();
builder.Services.AddFluentValidationAutoValidation();

// Client (Ui.Client/Program.cs)
builder.Services.AddValidatorsFromAssemblyContaining<SharedResource>();
```

### Culture Resolution Priority

**At construction (login page, post-logout):**

1. **DSM system culture** — from `ADWH_SYSTEM_CULTURE` env var (pre-resolved to `CultureInfo?` as `SystemCulture`)
2. **Browser culture** — from WASM runtime's auto-set `CultureInfo.CurrentUICulture` (pre-resolved to `CultureInfo` as `BrowserCulture`)
3. **Default** — `en-US`

**After login:**

1. **Login response culture** — server resolved user vs system preference (from DSM settings)
2. **DSM system culture** — same as above (pre-resolved)
3. **Browser culture** — same as above (pre-resolved)
4. **Default** — `en-US`

**Matching strategy:** `FindMatchingCulture(CultureInfo)` uses `CultureInfo.Equals` for exact match, then `TwoLetterISOLanguageName` for parent culture fallback. All matching is done at class load time.

### Adding a New Culture

1. Add `SharedResource.<culture>.resx` to `Globalization/Resources/`
2. Build — SDK auto-generates satellite assembly in `<culture>/` subdirectory
3. Server auto-discovers via directory scan → injects to WASM
4. **Zero code changes needed**

### Adding a New Validator

1. Create `XxxValidator.cs` in `Globalization/Validators/`
2. Add keys to `LocalizationKeys.cs` under model-scoped class (e.g., `L.XxxModel.*`)
3. Add strings to both `.resx` files
4. Validator auto-discovered by `AddValidatorsFromAssemblyContaining<SharedResource>()`
5. **Zero registration changes needed**

### Design Decisions

- **DSM-controlled culture**: No runtime switching — culture locked after login, reset to system on logout
- **Synchronous culture methods**: `InitializeFromLogin()` and `ResetToSystem()` are `void` — no I/O involved
- **Pre-resolved static fields**: `BrowserCulture`, `SystemCulture`, and `SupportedCultures` captured at class load — `ResolveSystemCulture()` is allocation-free at runtime
- **Safe static initialization**: Each static field uses a `Safe*` wrapper
  (`SafeParseSupportedCultures`, `SafeGetBrowserCulture`,
  `SafeResolveSystemCultureFromEnv`) that catches `CultureNotFoundException`,
  `ArgumentException`, and `JsonException` — prevents `TypeInitializationException`
  from crashing WASM on malformed env vars
- **`NotSupportedException` on pattern setters**: Date/time pattern overrides
  catch both `FormatException` and `NotSupportedException` — cloned cultures
  are mutable, but defensive coding guards against rare immutable culture variants
- **Pure C# browser detection**: WASM runtime auto-sets `CultureInfo.CurrentUICulture` from Accept-Language header — no JS interop needed
- **`DsmLanguageToCultureConverter` returns `null` for `"def"`**: `"def"` means "use browser default" — not English
- **`GlobalizationSettings` as singleton in Ui/Infrastructure/**: Server-only `IGlobalizationSettings` implementation — avoids WASM file system API crashes; discovery logged via `[LoggerMessage]`
- **`GlobalizationExtensions` in `Ui/Extensions/`**: `ApplyDsmSystemCulture()` fetches DSM language; `UseGlobalizationRequestLocalization()` resolves settings from DI and adds middleware
- **`ILocalizer` abstraction**: Hides `ResourceManager` from consumer projects — indexer returns `string` directly, no wrapper type
- **`ResourceManager` instead of `IStringLocalizer<T>`**: `IStringLocalizer<T>` caches culture at construction in WASM. `ResourceManager` reads `CurrentUICulture` at call time
- **`BlazorWebAssemblyLoadAllGlobalizationData`**: Required for dynamic culture changes at WASM startup — without it, changing culture before first page render throws `AggregateException`
- **Early `CultureManager` resolution**: `Program.cs` forces DI resolution before `host.RunAsync()` — ensures `CurrentUICulture` is set before login page renders
- **Full page reload on logout**: `forceLoad: true` resets WASM circuit, culture re-resolves to system/browser
- **Server-side Accept-Language parsing**: `IRequestCultureFeature` doesn't match neutral languages (`fr`) to specific cultures (`fr-FR`) — `GetLanguageTag()` parses header directly
- **`CultureManager` updates `html lang` and `dir` via `IJSRuntime`**: Sets `document.documentElement.lang` and `dir` (`rtl`/`ltr`) on every culture change — enables RTL support
- **Missing translation bracketed format**: `Localizer` and `DeferredMessageExtensions` return `[{key}]` for missing keys — makes absent translations visible in UI without crashing
- **No client-side `setHtmlLang` JS interop (legacy)**: Replaced by `CultureManager.UpdateHtmlLangAndDir()` via `IJSRuntime` which updates both `lang` and `dir` attributes
- **`AuthenticationResult.Culture` is `{ get; set; }`**: System.Text.Json requires setters for deserialization into properties
- **`IsAuthenticated` marked `[JsonIgnore]`**: Redundant alias for `Success` — pollutes JSON response
- **ASP.NET Core default camelCase JSON**: Removed `PropertyNamingPolicy = null` — camelCase is industry standard and matches WASM client defaults
- **Satellite assembly discovery**: `Directory.GetDirectories()` on assembly location, filtered by project satellite assembly name
- **`dotnet.withEnvironmentVariable()`**: Pure .NET approach, no JS variables or JS interop needed for culture discovery
- **`SupportedCultures` discovered at construction**: `GlobalizationSettings` singleton discovers cultures in its constructor (logged via `[LoggerMessage]`), available through DI before login
- **`MarkupString` in `App.razor`**: Required to avoid HTML-encoding of JSON double quotes inside `<script>` block
- **Shared validators in Globalization**: Single source of truth — server auto-validation uses the same FluentValidation rules defined in the Globalization assembly
- **Model-scoped localization keys**: `L.WebSiteConfiguration.*` and `L.LoginCredentials.*` clarify model ownership
- **No DataAnnotations**: All validation migrated to FluentValidation — DataAnnotations cannot use runtime-localized messages
- **Separate port validation messages**: `InternalPort` (1024-65535) and
  `PublicPort` (80, 443, or 1024-65535) have distinct validation keys
  (`InternalPortRange`, `PublicPortRange`) since `WellKnownWebPorts` allows
  standard web ports below the minimum application port
- **`AuthenticationResult` carries `DateFormat` and `TimeFormat`**: Formats converted on server, passed to WASM alongside culture
- **Culture-aware date formats via `CultureInfo.DateTimeFormat` override**: Clones culture and overrides patterns for user-specific preferences
- **Standard format specifiers `"d"` and `"g"` in UI**: Leverages `CultureInfo.DateTimeFormat` automatically — no custom formatting logic in components
- **Dynamic `html lang` attribute**: Reads from `IRequestCultureFeature` for accessibility and SEO — server-side request culture is the source of truth
- **Defensive try-catch for culture operations**: `CultureNotFoundException`/`ArgumentException` fall back to system culture; `FormatException` keeps culture defaults with warning log
- **Same format applied to short/long patterns**: DSM provides one date and one time format per user; applying to both short/long variants is the pragmatic trade-off
- **`AuthenticationNavigationGuard` enforces auth before render**: Router `OnNavigateAsync` guard eliminates UI flash; no auth caching to avoid stale state; sync `OnNavigate` throws
- **`FluentDesignTheme` + `FluentLayout` in App.razor**: App-level theme (System mode) and full-viewport layout; `FluentMainLayout` in MainLayout.razor provides Header/Body structure with background theming
- **`GlobalizationConstants` centralizes culture defaults**: `DefaultCulture`, `DefaultCultureInfo`, text direction, env var names and `Accept-Language` header moved from `ApplicationConstants` to `Constants/Globalization/`
- **C# 14 scoped extensions in `Extensions/` folders**: `CultureInfoExtensions` and `GlobalizationServiceCollectionExtensions` use `extension(T)` syntax
- **`GetTextDirection()` extension**: Centralizes RTL/LTR logic (`culture.TextInfo.IsRightToLeft`); used by SSR `App.razor` and WASM `CultureManager`

---

## Performance Optimization

### Caching Strategy

**Current Implementation:**

- **ApiInformations Cache:** DSM API metadata cached after handshake
- **Session Validation Cache:** 1-minute TTL for DSM session validation results (avoids per-request API overhead)
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
   - ✅ 400 tests across 10 phases (Data validation, domain, Result types, threading, extensions, I/O, parsing, platform, LogDownloadService, LicenseService, TreeContentService)
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

| Date | Changes |
|------|---------|
| June 20, 2026 | Over-engineering reduction: removed Benchmarks project (-260 lines, -BenchmarkDotNet dep); merged ApiVersions+ApiMethods+ApiNames into ApiConstants; inlined LicenseConstants into LicenseService; consolidated PHP date/time converters into PhpFormatToDotNetConverter with PhpDotNetFormatTokens (ImmutableDictionary) in Constants; inlined UriExtensions (1 consumer); dropped IPlatformInfoService and IWebSitesConfigurationService interfaces (single impl, no tests); added 13 tests for LogDownloadService (3), LicenseService (5), TreeContentService (5); separated DsmLanguageCodes (data) from DsmLanguageToCultureConverter (logic) |
| May 11, 2026 | Dead code sweep: removed `ApiGenericResponse`, `PaginationDefaults`, `LicenseRoutes`, `DirectoryFilesResult`, `DsmToolsExtensions`; removed NuGet packages `Microsoft.AspNetCore.Mvc.Versioning` and `Microsoft.FluentUI.AspNetCore.Components.Emoji`; preserved `EmptyResponse` as standalone type (used by `DsmApiClient`); cleaned stale references in Constants tree diagram and Tools Extensions listing |
| May 1, 2026 | Replaced custom `CloneGenerator` source generator with C# records (`init` setters) — 41 classes converted, `GenerateCloneAttribute`/`IGenericCloneable<T>` removed, `ApiParametersBase<T>` simplified (no cloning needed with immutability); SiteLifecycleManager hardening: lifecycle manager recreation on config update (stale config fix), removed vestigial `.Clone()` calls (Interactive WASM has no shared memory), removed `ProcessTimeoutSeconds` (inlined 10s constant), added `IOException` to `StopAsync` exception filter, `ProcessInfo` converted to snapshot record, parallel startup in `StartEligibleSitesAsync`, `CancellationToken` forwarding in `StopAllSitesAsync` and `GetRuntimeStateAsync`, removed dead null check and misleading `CancellationToken` from `StartAsync`; **post-review fixes**: `RemoveInstanceAsync` restructured to remove persistent config before in-memory state (prevents orphaned configs on failure), `StopAllSitesAsync` wraps `Dispose()` in `finally` (prevents `SemaphoreSlim` leak on exception), `StartAsync` disposes stale `_process` handle before restart (prevents handle leak on crash-restart cycles); **SiteLifecycleManager concurrency rewrite**: replaced `SemaphoreSlim` + `ISemaphoreOwner` with `Channel<LifecycleCommand>` + single consumer loop — eliminates TOCTOU races, no `ObjectDisposedException` boilerplate, safe disposal via queued `DisposeCommand`, `ConfigurationRequiresRestart` uses order-independent dictionary comparison |
| April 29, 2026 | SIGTERM process termination fix: added `ProcessTerminator` utility (cross-platform SIGTERM via P/Invoke), replaced blocking `WaitForExit` with async `WaitForExitAsync`, reduced timeouts (HttpClient 90→15s, Process 60→10s) — eliminates DSM reverse proxy 504 errors |
| April 25, 2026 | Synchronized with codebase: added `SiteLifecycleManager` two-tier process architecture (graceful shutdown, force kill fallback), documented `DirectoryFilesResult`, `WebSiteRuntimeState`, `DotnetInfoParserConstants`; removed version column from Technical Stack table; cleaned up stale empty directory references |
| April 5, 2026 | Architecture documentation synchronized with codebase; corrected service lifetimes, added SemaphoreLock and AuthorizeSessionAttribute documentation |
| March 2026 | Architecture documentation update, version bump |
| Earlier | Initial architecture documentation |
