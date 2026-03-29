# ASkyl.Dsm.WebHosting - Technical Architecture Document

**Version:** 0.5.3
**Target Framework:** .NET 10 (net10.0)
**Last Updated:** March 29, 2026

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
- Source-generated clone methods for data models

The solution follows modern .NET 10 best practices, utilizing Blazor Hybrid architecture (Interactive WebAssembly), FluentUI components, and a clean layered architecture pattern.

**Key Architectural Decisions:**

- **Hybrid Rendering Mode:** Server-side authentication with WebAssembly interactive components
- **Result Pattern:** Strongly-typed success/failure results instead of exceptions for control flow
- **Source Generators:** Custom Roslyn generator for clone method implementation
- **Centralized Constants:** All magic strings/numbers extracted to dedicated Constants project
- **Background Service:** Website hosting service runs as singleton hosted service for lifecycle management

**Current Status (v0.5.3):**

- ✅ Blazor Server + Interactive WebAssembly hybrid rendering
- ✅ DSM API integration (Authentication, FileStation, ReverseProxy)
- ✅ Website lifecycle management with process control
- ✅ JSON-based configuration persistence
- ✅ **Infrastructure services refactored to DI-based architecture** (PlatformInfoService, FileManagerService, ArchiveExtractorService, DownloaderService, VersionsDetectorService)
- ✅ **Smart caching strategy** for expensive operations (VersionsDetectorService with lazy initialization)
- ✅ **Full CancellationToken support** across all async operations
- ✅ **All static classes converted** to injectable services for testability
- ⏳ TODO: Certificate management for reverse proxy
- ⏳ TODO: Multi-language support
- ⏳ TODO: Unit test implementation

---

## Solution Overview

### Solution Structure

```
Askyl.Dsm.WebHosting.slnx (Version 0.5.3)
├── Askyl.Dsm.WebHosting.Benchmarks         # Performance benchmarks (BenchmarkDotNet)
├── Askyl.Dsm.WebHosting.Constants          # Centralized constants & enums
├── Askyl.Dsm.WebHosting.Data               # Core data layer, API definitions, services
├── Askyl.Dsm.WebHosting.DotnetInstaller    # .NET runtime installer utility
├── Askyl.Dsm.WebHosting.Logging            # Logging extensions (source-generated log methods)
├── Askyl.Dsm.WebHosting.SourceGenerators   # Custom source generators (CloneGenerator)
├── Askyl.Dsm.WebHosting.Tools              # Utility tools & DSM API client
├── Askyl.Dsm.WebHosting.Ui                 # Main Blazor Server-WASM hybrid UI
└── Askyl.Dsm.WebHosting.Ui.Client          # Blazor WebAssembly client library
```

### Key Characteristics

- **Multi-project solution** with clear separation of concerns
- **Shared constants** across all projects for maintainability
- **Source generators** for reducing boilerplate code (clone methods, logging)
- **Hybrid rendering mode** (InteractiveServer + InteractiveWebAssembly)
- **Background services** for long-running operations
- **Centralized versioning** via Directory.Build.props

### Build Configuration

All projects share common build settings from `Directory.Build.props`:

```xml
<Version>0.5.3</Version>
<AssemblyVersion>0.5.3.0</AssemblyVersion>
<FileVersion>0.5.3.0</FileVersion>
<InformationalVersion>0.5.3</InformationalVersion>
<PackageVersion>0.5.3</PackageVersion>

<!-- Debug settings -->
<DebugType Condition="'$(Configuration)' == 'Release'">None</DebugType>
<DebugSymbols Condition="'$(Configuration)' == 'Release'">false</DebugSymbols>
<GenerateDocumentationFile>false</GenerateDocumentationFile>
```

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

**Structure:**

```
Constants/
├── Application/                            # Application-wide constants
│   ├── ApplicationConstants.cs             # App paths, URLs, HTTP client names
│   └── ValidationMessages.cs               # Validation error messages
├── DSM/                                    # Synology DSM-specific constants
│   ├── API/                                # API-related constants
│   │   ├── ApiMethods.cs                   # API method names (authenticateLogin, etc.)
│   │   ├── ApiNames.cs                     # DSM API identifiers (SYNO.API.Auth, etc.)
│   │   ├── ApiVersions.cs                  # API version constants
│   │   ├── ReverseProxyConstants.cs        # Reverse proxy settings
│   │   └── SerializationFormats.cs         # Form/JSON serialization enums
│   ├── FileStation/                        # FileStation-specific constants
│   └── System/                             # DSM system defaults (ports, config paths)
├── JSON/                                   # JSON serialization settings
├── Network/                                # Network configuration
│   └── NetworkConstants.cs                 # Cookie headers, port defaults
├── Runtime/                                # .NET runtime definitions
│   ├── DotNetFrameworkTypes.cs             # Runtime type enums
│   └── RuntimeConstants.cs                 # Channels, version patterns
├── UI/                                     # User interface constants
│   ├── DialogConstants.cs                  # UI dialog configurations
│   ├── FileSizeConstants.cs                # File size formatting
│   └── Protocol.cs                         # ProtocolType enum (HTTP/HTTPS)
└── WebApi/                                 # API route definitions
    ├── AuthenticationRoutes.cs             # /api/authentication/*
    ├── FileManagementRoutes.cs             # /api/filemanagement/*
    ├── FrameworkManagementRoutes.cs        # /api/frameworkmanagement/*
    ├── LicenseRoutes.cs                    # /api/license/*
    ├── LogDownloadRoutes.cs                # /api/logdownload/*
    ├── RuntimeManagementRoutes.cs          # /api/runtime/*
    └── WebsiteHostingRoutes.cs             # /api/websites/*
```

**Key Features:**

- All magic strings replaced with compile-time constants
- Static classes for type safety
- Comprehensive validation error messages
- API route definitions centralized for easy maintenance
- ProtocolType enum for HTTP/HTTPS abstraction

### 2. Askyl.Dsm.WebHosting.SourceGenerators

**Purpose:** Custom Roslyn source generator for automatic clone method implementation

**Implementation:**

```csharp
// Usage in data models
[GenerateClone]
public partial class WebSiteConfiguration : IGenericCloneable<WebSiteConfiguration>
{
    // Properties...
    // Clone() method auto-generated
}
```

**Generated Code:**

- `Clone()` method implementation for deep copying
- Implements `IGenericCloneable<T>` interface automatically
- Reduces boilerplate code by 60-70%

**Technical Details:**

- Target: .NET Standard 2.0 (compatible with all projects)
- Uses Microsoft.CodeAnalysis.CSharp 5.0.0
- Generates files in `obj/Generated` directory
- Attribute-based activation via `[GenerateClone]`
- Supports nested cloneable types (recursive cloning)

**Generator Flow:**

1. **Post-Initialization:** Generates `GenerateCloneAttribute.g.cs` for the assembly
2. **Syntax Provider:** Identifies classes with `[GenerateClone]` attribute
3. **Semantic Analysis:** Validates attribute and extracts property declarations
4. **Code Generation:** Creates partial class implementation with Clone() method

### 3. Askyl.Dsm.WebHosting.Data

**Purpose:** Core data layer, API definitions, domain services, and result types

**Structure:**

```
Data/
├── Attributes/                             # Custom attributes
│   └── GenerateCloneAttribute.cs           # Source generator trigger (backup)
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
│   │   └── [Version models]                # Runtime version data
│   └── WebSites/                           # Website management domain
│       ├── ProcessInfo.cs                  # Process runtime information
│       ├── WebSiteConfiguration.cs         # Main config model ([GenerateClone])
│       ├── WebSiteInstance.cs              # Runtime instance ([GenerateClone])
│       └── WebSitesConfiguration.cs        # Persistent configuration store
├── DsmApi/                                 # DSM API integration
│   ├── Models/                             # Auto-generated response models
│   │   ├── Core/                           # Authentication, system info
│   │   ├── FileStation/                    # 25+ file operation definitions
│   │   └── ReverseProxy/                   # Proxy configuration models
│   ├── Parameters/                         # Request parameter classes
│   │   ├── Core/                           # Login/logout parameters
│   │   ├── CoreAcl/                        # Access control parameters
│   │   ├── CoreInformations/               # System info queries
│   │   ├── FileStation/                    # 15+ file operation parameters
│   │   ├── ReverseProxy/                   # Proxy CRUD operations
│   │   ├── ApiParametersBase.cs            # Base parameter class
│   │   ├── ApiParametersNone.cs            # No-parameters wrapper
│   │   └── IApiParameters.cs               # Parameter interface
│   └── Responses/                          # API response wrappers
├── Exceptions/                             # Custom exception types
├── Patterns/                               # Design patterns
│   └── IGenericCloneable<T>.cs             # Clone pattern interface
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
    ├── DirectoryFilesResult.cs             # Directory file operations
    ├── InstallationResult.cs               # Framework installation status
    ├── InstalledVersionsResult.cs          # Installed .NET versions
    ├── ReleasesResult.cs                   # .NET release information
    ├── SharedFoldersResult.cs              # NAS shared folder listing
    ├── WebSiteInstanceResult.cs            # Website-specific operations
    └── WebSiteInstancesResult.cs           # Multiple website results
```

**Key Features:**

- **Result Pattern:** All operations return typed results (eliminates null checks)
- **API Abstraction:** Strong-typed request/response models for DSM APIs
- **Clone Generation:** Source-generated deep copy methods via `[GenerateClone]`
- **Validation:** Data annotations with localized error messages from Constants
- **Service Interfaces:** Clean separation between domain logic and UI implementation

### 4. Askyl.Dsm.WebHosting.Tools

**Purpose:** Utility services, DSM API client, and runtime management tools

**Structure:**

```
Tools/
├── Extensions/                             # Extension methods
│   └── UriExtensions.cs                    # URI manipulation helpers
├── Infrastructure/                         # Infrastructure utilities (DI-based)
│   ├── ArchiveExtractorService.cs          # gzip + tar extraction (implements IArchiveExtractorService)
│   └── FileManagerService.cs               # File system initialization (implements IFileManagerService)
│   └── PlatformInfoService.cs              # Platform detection (implements IPlatformInfoService)
├── Network/                                # Network communication
│   └── DsmApiClient.cs                     # Centralized DSM API client
└── Runtime/                                # .NET runtime management (DI-based)
    ├── DownloaderService.cs                # Binary download utility (implements IDownloaderService)
    └── VersionsDetectorService.cs          # Version detection with smart caching (implements IVersionsDetectorService)
```

**Infrastructure Services Architecture:**

The Tools project contains DI-based infrastructure services for platform detection, file management, archive extraction, and .NET runtime operations.

| Service | Interface | Lifetime | Key Features | Dependencies | Source File |
|---------|-----------|----------|--------------|--------------|-------------|
| **PlatformInfoService** | `IPlatformInfoService` | Singleton | Platform detection, config loading | ILogger | `Tools/Infrastructure/PlatformInfoService.cs` |
| **FileManagerService** | `IFileManagerService` | Scoped | Directory management, configurable root path | ILogger, string rootPath | `Tools/Infrastructure/FileManagerService.cs` |
| **ArchiveExtractorService** | `IArchiveExtractorService` | Scoped | tar.gz extraction | IFileManagerService | `Tools/Infrastructure/ArchiveExtractorService.cs` |
| **DownloaderService** | `IDownloaderService` | Scoped | .NET runtime downloads with cancellation | IPlatformInfoService, IFileManagerService | `Tools/Runtime/DownloaderService.cs` |
| **VersionsDetectorService** | `IVersionsDetectorService` | Singleton | Smart caching for dotnet --info | None (stateful singleton) | `Tools/Runtime/VersionsDetectorService.cs` |

**Key Design Decisions:**

1. **Singleton Services (Stateful):** Platform info loaded once at startup; VersionsDetector caches expensive process output
2. **Scoped Services (Request-bound):** FileManager configured per-request via factory lambda; ArchiveExtractor and Downloader depend on Scoped FileManager
3. **Smart Caching Strategy:** VersionsDetectorService uses lazy initialization with explicit cache refresh (`RefreshCacheAsync()` called after install/uninstall operations) - see `VersionsDetectorService.cs` lines 28-50
4. **CancellationToken Support:** All DownloaderService public methods accept optional CancellationToken for cooperative cancellation flow from UI to infrastructure layer

**DsmApiClient Implementation:**

See `Tools/Network/DsmApiClient.cs` for full implementation.

**Key Features:**
- Singleton pattern (registered in DI container)
- Session management with SID validation and restoration
- Automatic serialization based on `IApiParameters.SerializationFormat`
- Strategy pattern for Form vs JSON serialization
- Error handling with structured logging via Serilog
- HttpClient factory integration for proper lifecycle management
- All infrastructure services testable via interface abstractions

**Connection Flow:** See `DsmApiClient.cs` lines 85-120

### 5. Askyl.Dsm.WebHosting.Ui

**Purpose:** Main Blazor hybrid application (Server + WebAssembly rendering)

**Structure:**

```
Ui/
├── Authorization/                          # Custom authorization
│   └── AuthorizeSessionAttribute.cs        # Session-based auth attribute
├── Components/                             # Razor components
│   ├── App.razor                           # Root component with WASM render mode
│   └── _Imports.razor                      # Global using directives
├── Controllers/                            # ASP.NET Core API controllers
│   ├── AuthenticationController.cs         # Login/logout/status endpoints
│   ├── FileManagementController.cs         # File system operations
│   ├── FrameworkManagementController.cs    # Framework installation
│   ├── HelloWorldController.cs             # Test endpoint
│   ├── LogDownloadController.cs            # Log file retrieval
│   ├── RuntimeManagementController.cs      # .NET version detection
│   └── WebsiteHostingController.cs         # Website CRUD + lifecycle
├── Models/                                 # UI-specific view models
├── Properties/                             # Assembly info, launch settings
├── Services/                               # UI business logic services
│   ├── AuthenticationService.cs            # Auth façade over DsmApiClient
│   ├── DotnetVersionService.cs             # .NET version detection
│   ├── FileSystemService.cs                # File operations wrapper
│   ├── FrameworkManagementService.cs       # Framework installation
│   ├── LogDownloadService.cs               # Log file retrieval
│   ├── ReverseProxyManagerService.cs       # Proxy CRUD operations
│   ├── WebSiteHostingService.cs            # Website lifecycle (background service)
│   └── WebSitesConfigurationService.cs     # Configuration persistence
├── wwwroot/                                # Static assets (CSS, JS, images)
├── appsettings.json                        # Production configuration
├── appsettings.Development.json            # Development overrides
└── Program.cs                              # Application entry point
```

**Program.cs Configuration:**

See `Ui/Program.cs` for full implementation. Key registration patterns:

**Service Registration Summary:**

| Lifetime | Services | Notes |
|----------|----------|-------|
| **Singleton** | DsmApiClient, IPlatformInfoService, IVersionsDetectorService, WebSiteHostingService | Stateful services, caching, background service |
| **Scoped (Factory)** | IFileManagerService | Factory lambda injects ILogger + configures root path |
| **Scoped** | IArchiveExtractorService, IDownloaderService, IAuthenticationService, IDotnetVersionService, IFrameworkManagementService, ILogDownloadService | Request-bound services |

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

### 6. Askyl.Dsm.WebHosting.Ui.Client

**Purpose:** Blazor WebAssembly client library (shared components and services)

**Structure:**

```
Ui.Client/
├── Components/                             # Reusable Blazor components
│   ├── Controls/                           # Custom UI controls
│   │   ├── AutoDataGrid.razor              # Generic data grid with sorting
│   │   ├── LoadingOverlay.razor            # Working state indicator
│   │   ├── RealTimeNumberField.razor       # Input with validation feedback
│   │   └── RealTimeTextField.razor         # Text input with validation
│   ├── Dialogs/                            # FluentUI dialog wrappers
│   │   ├── AspNetReleasesDialog.razor      # Online ASP.NET release info
│   │   ├── DotnetVersionsDialog.razor      # View available .NET versions
│   │   ├── FileSelectionDialog.razor       # Browse DSM file system
│   │   ├── LicensesDialog.razor            # Third-party license display
│   │   └── WebSiteConfigurationDialog.razor # Add/edit website form
│   ├── Layout/                             # Layout components
│   │   ├── MainLayout.razor                # Main app shell with navigation
│   │   ├── NavigationMenu.razor            # FluentUI nav menu
│   │   ├── HeaderBar.razor                 # Top bar with user info
│   │   └── Footer.razor                    # Status indicators
│   ├── Pages/                              # Blazor pages
│   │   ├── Home.razor                      # Dashboard/home page (website grid)
│   │   ├── Login.razor                     # Authentication page
│   │   └── NotFound.razor                  # 404 handler
│   └── Patterns/                           # UI patterns and templates
├── Extensions/                             # Client-side extension methods
├── Interfaces/                             # C# interfaces for JS interop
├── Services/                               # HTTP clients & state management
│   └── [Client-side service proxies]       # API client wrappers
├── wwwroot/                                # Client-side static assets
│   └── appsettings.json                    # Client Serilog config
├── _Imports.razor                          # Global using directives
├── Program.cs                              # WASM entry point
└── Routes.razor                            # Route definitions
```

**Key Features:**

- **Component library** for UI consistency across pages
- **HTTP client wrappers** for API calls (type-safe endpoints)
- **State management** patterns for client-side data
- **FluentUI integration** with icons, themes, and responsive design
- **InteractiveWebAssembly render mode** for seamless server-client transition

### 7. Askyl.Dsm.WebHosting.DotnetInstaller

**Purpose:** Standalone console utility for .NET runtime installation on Synology NAS

**Implementation:** See `DotnetInstaller/Program.cs` - manually instantiates infrastructure services without full DI container:
- Creates ILogger instances via LoggerFactory with console provider
- Instantiates PlatformInfoService, FileManagerService (with root path = "")
- Initializes FileManager to create default directories
- Creates DownloaderService and ArchiveExtractorService with dependencies
- Calls `downloader.DownloadToAsync(true, CancellationToken.None)` then `archiveExtractor.Decompress(fileName)`

**Key Features:**
- Standalone executable (no UI dependencies)
- Manual DI instantiation for console application scenario
- Automatic download from Microsoft .NET release repositories
- Architecture detection via PlatformInfoService for correct binary selection
- Extraction utilities for gzip + tar.gz archives via ArchiveExtractorService
- File system initialization with configurable root path
- CancellationToken support for cooperative cancellation

### 8. Askyl.Dsm.WebHosting.Benchmarks

**Purpose:** Performance benchmarking with BenchmarkDotNet

**Benchmarks:**

```csharp
[MemoryDiagnoser]
public class StringBuilderBenchmark
{
    [Benchmark] public void UrlInterpolatedString()      // Baseline: interpolated strings
    [Benchmark] public void UrlBuilder()                 // StringBuilder approach
    [Benchmark] public void ParametersInterpolatedString()
    [Benchmark] public void ParametersBuilder()          // Recommended for complex URLs
}

[MemoryDiagnoser]
public class UriBuilderBenchmark
{
    // URI building performance tests
}
```

**Key Features:**

- **MemoryDiagnoser** for GC analysis and memory allocation tracking
- **Comparison tests** for string concatenation strategies
- **Real-world scenarios** (URL building, parameter encoding)
- **BenchmarkDotNet 0.15.8** for accurate performance measurements

### 9. Askyl.Dsm.WebHosting.Logging

**Purpose:** Logging extensions with source-generated logger methods

**Implementation:**

```csharp
namespace Askyl.Dsm.WebHosting.Logging.HelloWorld;

public static partial class HelloWorldExtensions
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Dice roll: {Die1} and {Die2}, sum: {Sum}")]
    public static partial void LogDiceRoll(this ILogger logger, int die1, int die2, int sum);
}
```

**Key Features:**

- **Source-generated log methods** for compile-time message validation
- **Extension method pattern** for clean logger API
- **Structured logging** support with named parameters
- **Zero-allocation logging** for performance-critical paths

---

## Design Patterns & Principles

### 1. Dependency Injection (DI)

**Service Registration:** See `Ui/Program.cs` lines 45-78 for full implementation.

**Patterns Used:**

- **Singleton:** DsmApiClient, platform info, versions detector (with caching), configuration services, background services
- **Scoped:** File manager (with factory lambda for root path), archive extractor, downloader, UI services - one per request
- **Background Service:** WebSiteHostingService implements IHostedService for lifecycle management

**Service Lifetime Hierarchy:**

```
Singleton (Application-wide)
├── DsmApiClient
├── PlatformInfoService (platform detection, config loading)
├── VersionsDetectorService (smart caching for dotnet --info)
└── WebSiteHostingService (background service)

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

### 2. Result Pattern

**Implementation:**

```csharp
// Base result types
public class ApiResult { /* Success, Message */ }
public class ApiResultData<T> : ApiResult { /* Data */ }
public class ApiResultItems<T> : ApiResult { /* Items collection */ }

// Usage example
public async Task<WebSiteInstanceResult> AddWebsiteAsync(WebSiteConfiguration configuration)
{
    if (!permissionResult.Success)
    {
        return WebSiteInstanceResult.CreateFailure($"Failed: {permissionResult.Message}");
    }
    
    return WebSiteInstanceResult.CreateSuccess(instance);
}
```

**Benefits:**

- Eliminates exception-based control flow
- Strongly-typed success/failure states
- No null reference exceptions
- Cleaner UI error handling

### 3. Repository/Service Facade Pattern

**Structure:**

```
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

### 4. Source Generator Pattern

**CloneGenerator Implementation:**

```csharp
// Input: [GenerateClone] attribute on partial class
[GenerateClone]
public partial class WebSiteConfiguration : IGenericCloneable<WebSiteConfiguration>
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    // ... properties
}

// Output: Auto-generated Clone() method
public partial class WebSiteConfiguration : IGenericCloneable<WebSiteConfiguration>
{
    public WebSiteConfiguration Clone() => new()
    {
        Id = this.Id,
        Name = this.Name,
        // ... property copying
    };
}
```

**Benefits:**

- Reduces boilerplate code by 60-70%
- Compile-time type safety
- Automatic interface implementation
- Supports nested cloneable types

### 5. Background Service Pattern

**WebSiteHostingService Implementation:**

```csharp
public class WebSiteHostingService(
    ILogger<WebSiteHostingService> logger,
    IWebSitesConfigurationService configService,
    IFileSystemService fileSystemService,
    IReverseProxyManagerService reverseProxyManager) 
    : BackgroundService, IWebSiteHostingService
{
    private readonly ConcurrentDictionary<Guid, WebSiteInstance> _instances = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Load configurations from JSON on startup
        var configs = await configService.GetAllAsync();
        
        // Reinitialize instances (processes not restarted)
        foreach (var config in configs)
        {
            await AddInstanceAsync(config);
        }
        
        // Monitor and manage lifecycle until stopping
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }
}
```

**Key Features:**

- **Singleton lifetime** - One instance per application
- **Startup initialization** - Loads configurations from persistent storage
- **Graceful shutdown** - Stops all website processes on host termination
- **Thread-safe operations** - Uses `ConcurrentDictionary` for instance management

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

---

## Technical Stack

### Frameworks & Libraries

| Component | Technology | Version | Purpose |
|-----------|------------|---------|---------|
| **Runtime** | .NET | 10.0 | Application framework |
| **UI Framework** | Blazor Hybrid | 10.0.5 | Interactive WebAssembly + Server |
| **UI Components** | FluentUI | 4.14.0 | Modern UI component library |
| **Logging** | Serilog | 10.0.0 | Structured logging |
| **HTTP Client** | Microsoft.Extensions.Http | 10.0.5 | HttpClient factory |
| **Benchmarking** | BenchmarkDotNet | 0.15.8 | Performance testing |
| **.NET Releases** | Microsoft.Deployment.DotNet.Releases | 1.0.1 | Version detection |
| **Source Generators** | Microsoft.CodeAnalysis.CSharp | 5.0.0 | Clone generator |

### Development Tools

- **IDE:** Visual Studio 2022 / VS Code with C# Dev Kit
- **Build Tool:** .NET SDK 10.0
- **Package Manager:** NuGet
- **Version Control:** Git

---

## Data Models & API Integration

### Core Domain Models

#### WebSiteConfiguration

```csharp
[GenerateClone]
public partial class WebSiteConfiguration : IGenericCloneable<WebSiteConfiguration>
{
    // General
    public Guid Id { get; set; }
    public string Name { get; set; }
    
    // Application Path
    public string ApplicationPath { get; set; }           // User-friendly path
    public string ApplicationRealPath { get; set; }       // Resolved absolute path
    
    // Networking
    public int InternalPort { get; set; }                 // 1024-65535
    public string HostName { get; set; }                  // e.g., "myapp.local"
    public int PublicPort { get; set; }                   // 443 default
    public ProtocolType Protocol { get; set; }            // HTTPS default
    
    // Runtime
    public string Environment { get; set; }               // "Production" default
    public Dictionary<string, string> AdditionalEnvironmentVariables { get; set; }
    
    // Behavior
    public bool IsEnabled { get; set; }                   // true
    public bool AutoStart { get; set; }                   // true
    public bool EnableHSTS { get; set; }                  // true
}
```

#### WebSiteInstance

```csharp
[GenerateClone]
public partial class WebSiteInstance : IGenericCloneable<WebSiteInstance>
{
    public Guid Id => Configuration.Id;
    public WebSiteConfiguration Configuration { get; set; }
    
    // Runtime state (not serialized)
    [JsonIgnore]
    public ProcessInfo? Process { get; set; }
    
    // Serialized state
    public bool IsRunning { get; set; }
    
    // Computed property
    public string State => Process?.IsResponding == true ? "Running" : 
                           Process == null ? (IsRunning ? "Running" : "Stopped") : "Not Responding";
}
```

### DSM API Integration

#### Authentication Flow

```
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

**Key Operation: Setting HTTP Group Permissions**

```csharp
public async Task<ApiResult> SetHttpGroupPermissionsAsync(string path, bool recursive)
{
    var parameters = new CoreAclSetParameters
    {
        Path = path,
        GroupId = 100,  // http group
        Recursive = recursive
    };
    
    return await _dsmApiClient.ExecuteAsync<ApiResult>(parameters);
}
```

#### ReverseProxy Management

**Composite Key Strategy:**

Instead of storing UUIDs (which can desynchronize), use configuration-based lookup:

```csharp
public async Task<ReverseProxyInfo?> FindByCompositeKeyAsync(WebSiteConfiguration config)
{
    var allProxies = await GetAllAsync();
    
    return allProxies.FirstOrDefault(proxy =>
        proxy.Backend.Port == config.InternalPort &&
        proxy.Frontend.Fqdn == config.HostName &&
        proxy.Frontend.Port == config.PublicPort &&
        proxy.Frontend.Protocol == config.Protocol.ToString().ToLower()
    );
}
```

**Benefits:**

- Always reflects actual DSM state
- No synchronization issues
- Idempotent create operations

---

## UI Architecture

### Rendering Strategy

**Hybrid Mode: Interactive WebAssembly**

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

```
App.razor (Root)
└── MainLayout.razor
    ├── HeaderBar.razor (User info, logout)
    ├── NavigationMenu.razor (Navigation)
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

## Infrastructure Services Architecture

### Overview

The solution uses DI-based infrastructure services for platform detection, file management, archive extraction, .NET runtime downloads, and version detection. All services follow clean interface contracts with optimized lifetimes for performance and testability.

### Infrastructure Services Summary

| Service | Interface | Lifetime | Key Features | Dependencies | Source File |
|---------|-----------|----------|--------------|--------------|-------------|
| **PlatformInfoService** | `IPlatformInfoService` | Singleton | Platform detection, configuration loading | ILogger | `Tools/Infrastructure/PlatformInfoService.cs` |
| **FileManagerService** | `IFileManagerService` | Scoped | Directory management, configurable root path | ILogger, string rootPath | `Tools/Infrastructure/FileManagerService.cs` |
| **ArchiveExtractorService** | `IArchiveExtractorService` | Scoped | tar.gz archive extraction | IFileManagerService | `Tools/Infrastructure/ArchiveExtractorService.cs` |
| **DownloaderService** | `IDownloaderService` | Scoped | .NET runtime downloads with cancellation support | IPlatformInfoService, IFileManagerService | `Tools/Runtime/DownloaderService.cs` |
| **VersionsDetectorService** | `IVersionsDetectorService` | Singleton | Version detection with smart caching | None (stateful singleton) | `Tools/Runtime/VersionsDetectorService.cs` |

### Service Lifetime Strategy

**Singleton Services (Stateful):**
- PlatformInfoService: Platform information loaded once at application startup
- VersionsDetectorService: Maintains cache of expensive `dotnet --info` output across requests for optimal performance

**Scoped Services (Request-bound):**
- FileManagerService: Configured per-request with root path via factory lambda pattern (see `Ui/Program.cs` line 52)
- ArchiveExtractorService: Depends on Scoped FileManagerService for directory operations
- DownloaderService: Supports per-request cancellation tokens, depends on Scoped FileManagerService

### Smart Caching Implementation

VersionsDetectorService implements lazy initialization with explicit cache refresh. See `Tools/Runtime/VersionsDetectorService.cs`:
- **Lines 28-40:** GetInstalledVersionsAsync() - returns cached data after first call
- **Lines 42-55:** RefreshCacheAsync() - re-executes dotnet --info process to update cache

**Benefits:**
- Fast subsequent calls after initial cache population (no process spawning)
- Explicit cache control via `RefreshCacheAsync()` called after install/uninstall operations (see `Ui/Services/FrameworkManagementService.cs` lines 35, 67)
- Thread-safe singleton with single writer during initialization, multiple readers afterward

### CancellationToken Support

DownloaderService supports cooperative cancellation throughout all async operations. See `Tools/Runtime/DownloaderService.cs`:
- All public methods accept optional `CancellationToken cancellationToken = default` parameter
- Cooperative cancellation checks via `cancellationToken.ThrowIfCancellationRequested()` before expensive external API calls
- End-to-end cancellation flow: UI → Controller → Service layer → DownloaderService

### Factory Pattern for Configuration

FileManagerService uses factory lambda to inject logger and configure root path. See `Ui/Program.cs` line 52:

**Benefits:**
- Dependency injection of ILogger for structured logging
- Configuration of root path at registration time (ApplicationConstants.RuntimesRootPath)
- Different instances can manage different directory trees if needed

### Testability

All infrastructure services are fully testable via interface abstractions. Example mock setup pattern:

```csharp
var mockVersionsDetector = new Mock<IVersionsDetectorService>();
mockVersionsDetector.Setup(v => v.GetInstalledVersionsAsync())
    .ReturnsAsync(Task.FromResult(new List<FrameworkInfo> { /* test data */ }));
```

See interface definitions in `Data/Contracts/` folder for full contract specifications.

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
   - Package format: SPK (via Docker-based multi-arch builds)

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

1. **Unit Test Implementation**
   - Start with `WebSiteHostingService` (core business logic)
   - Mock `DsmApiClient` for integration tests
   - Target 80%+ code coverage on critical paths

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
| 0.5.2 | March 2026 | Architecture documentation update, version bump |
| 0.5.1 | Earlier | Initial architecture documentation |
| ... | ... | Previous versions |

---

**Document Maintained By:** AI Assistant (Qwen Code)  
**Last Review Date:** March 26, 2026  
**Next Review Date:** TBD (after major feature implementation)
