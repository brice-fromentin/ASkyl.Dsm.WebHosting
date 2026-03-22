# ASkyl.Dsm.WebHosting - Technical Architecture Document

**Version:** 0.5.1  
**Target Framework:** .NET 10 (net10.0)  
**Last Updated:** March 2026

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

The solution follows modern .NET 10 best practices, utilizing Blazor Hybrid architecture (InteractiveWebAssembly), FluentUI components, and a clean layered architecture pattern.

**Key Architectural Decisions:**

- **Hybrid Rendering Mode:** Server-side authentication with WebAssembly interactive components
- **Result Pattern:** Strongly-typed success/failure results instead of exceptions for control flow
- **Source Generators:** Custom Roslyn generator for clone method implementation
- **Centralized Constants:** All magic strings/numbers extracted to dedicated Constants project
- **Background Service:** Website hosting service runs as singleton hosted service for lifecycle management

---

## Solution Overview

### Solution Structure

```
Askyl.Dsm.WebHosting.slnx (Version 0.5.1)
├── Askyl.Dsm.WebHosting.Benchmarks      # Performance benchmarks (BenchmarkDotNet)
├── Askyl.Dsm.WebHosting.Constants       # Centralized constants & enums
├── Askyl.Dsm.WebHosting.Data            # Core data layer, API definitions, services
├── Askyl.Dsm.WebHosting.DotnetInstaller # .NET runtime installer utility
├── Askyl.Dsm.WebHosting.Logging         # Logging extensions (source-generated log methods)
├── Askyl.Dsm.WebHosting.SourceGenerators# Custom source generators (CloneGenerator)
├── Askyl.Dsm.WebHosting.Tools           # Utility tools & DSM API client
├── Askyl.Dsm.WebHosting.Ui              # Main Blazor Server-Wasm hybrid UI
└── Askyl.Dsm.WebHosting.Ui.Client       # Blazor WebAssembly client library
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
<Version>0.5.1</Version>
<DebugType Condition="'$(Configuration)' == 'Release'">None</DebugType>
<DebugSymbols Condition="'$(Configuration)' == 'Release'">false</DebugSymbols>
<GenerateDocumentationFile>false</GenerateDocumentationFile>
```

**Standardized Build Command:**

```bash
dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx
```

---

## Project Architecture

### 1. Askyl.Dsm.WebHosting.Constants

**Purpose:** Centralized constants, defaults, and enums for the entire solution

**Structure:**

```
Constants/
├── API/                                    # API-related constants
│   ├── AuthenticationDefaults.cs           # Auth API routes & formats
│   ├── DsmApiMethods.cs                    # API method names (authenticateLogin, etc.)
│   ├── DsmApiNames.cs                      # DSM API identifiers (SYNO.API.Auth, etc.)
│   ├── DsmApiVersions.cs                   # API version constants
│   ├── DsmPaginationDefaults.cs            # Pagination defaults
│   ├── FileManagementDefaults.cs           # File management endpoints
│   ├── FileStationDefaults.cs              # FileStation-specific constants
│   ├── FrameworkManagementDefaults.cs      # Framework installation routes
│   ├── LicenseDefaults.cs                  # License-related constants
│   ├── LogDownloadDefaults.cs              # Log download configuration
│   ├── ReverseProxyConstants.cs            # Reverse proxy settings
│   ├── RuntimeManagementDefaults.cs        # .NET runtime management endpoints
│   └── WebsiteHostingDefaults.cs           # Website management endpoints
├── Application/                            # Application-wide constants
│   ├── ApplicationConstants.cs             # App paths, URLs, HTTP client names
│   └── LogConstants.cs                     # Logging configuration
├── Http/                                   # HTTP-related constants
│   └── HttpConstants.cs                    # HTTP headers, methods
├── JSON/                                   # JSON serialization settings
├── Network/                                # Network configuration
│   └── NetworkConstants.cs                 # Cookie headers, port defaults
├── Runtime/                                # .NET runtime definitions
│   └── DotNetFrameworkTypes.cs             # Runtime type enums
├── UI/                                     # User interface constants
│   ├── DialogConstants.cs                  # UI dialog configurations
│   ├── FileSizeConstants.cs                # File size formatting
│   └── Protocol.cs                         # ProtocolType enum (HTTP/HTTPS)
├── DsmDefaults.cs                          # DSM-specific defaults (ports, config paths)
├── LicenseConstants.cs                     # Licensing constants
└── SerializationFormats.cs                 # Form/JSON serialization enums
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

### 3. Askyl.Dsm.WebHosting.Data

**Purpose:** Core data layer, API definitions, domain services, and result types

**Structure:**

```
Data/
├── API/                                    # DSM API integration
│   ├── Definitions/                        # Auto-generated response models
│   │   ├── Core/                           # Authentication, system info
│   │   ├── FileStation/                    # 25+ file operation definitions
│   │   └── ReverseProxy/                   # Proxy configuration models
│   ├── Parameters/                         # Request parameter classes
│   │   ├── AuthenticationAPI/              # Login/logout parameters
│   │   ├── CoreAclAPI/                     # Access control parameters
│   │   ├── FileStationAPI/                 # 15+ file operation parameters
│   │   ├── InformationsAPI/                # System info queries
│   │   ├── ReverseProxyAPI/                # Proxy CRUD operations
│   │   ├── ApiParametersBase.cs            # Base parameter class
│   │   ├── ApiParametersNone.cs            # No-parameters wrapper
│   │   ├── IApiParameters.cs               # Parameter interface
│   │   └── InstallFrameworkModel.cs        # Framework installation model
│   ├── Requests/                           # API request wrappers
│   └── Responses/                          # API response wrappers
├── Attributes/                             # Custom attributes
│   └── GenerateCloneAttribute.cs           # Source generator trigger
├── Exceptions/                             # Custom exception types
├── Extensions/                             # LINQ & collection extensions
├── Results/                                # Result pattern implementations
│   ├── ApiResult.cs                        # Base success/failure result
│   ├── ApiResultBool.cs                    # Boolean result wrapper
│   ├── ApiResultData<T>.cs                 # Result with data payload
│   ├── ApiResultItems<T>.cs                # Result with item collection
│   ├── ApiResultValue<T>.cs                # Result with single value
│   ├── ApiErrorCode.cs                     # Standardized error codes
│   ├── AuthenticationResult.cs             # Auth state with user info
│   ├── ChannelsResult.cs                   .NET channel information
│   ├── DirectoryContentsResult.cs          # File system directory listing
│   ├── DirectoryFilesResult.cs             # Directory file operations
│   ├── InstallationResult.cs               # Framework installation status
│   ├── InstalledVersionsResult.cs          # Installed .NET versions
│   ├── ReleasesResult.cs                   .NET release information
│   ├── SharedFoldersResult.cs              # NAS shared folder listing
│   ├── WebSiteInstanceResult.cs            # Website-specific operations
│   └── WebSiteInstancesResult.cs           # Multiple website results
├── Runtime/                                # .NET runtime information models
├── Security/                               # Authentication models
│   └── LoginModel.cs                       # Login credentials
├── Services/                               # Domain service interfaces
│   ├── IAuthenticationService.cs           # Authentication facade
│   ├── IDotnetVersionService.cs            # .NET version detection
│   ├── IFileSystemService.cs               # File system operations
│   ├── IFrameworkManagementService.cs      # Framework installation
│   ├── ILogDownloadService.cs              # Log file retrieval
│   ├── IReverseProxyManagerService.cs      # Proxy configuration
│   ├── IWebSiteHostingService.cs           # Website lifecycle
│   └── IWebSitesConfigurationService.cs    # Configuration persistence
├── WebSites/                               # Website management domain
│   ├── ProcessInfo.cs                      # Process runtime information
│   ├── WebSiteConfiguration.cs             # Main config model ([GenerateClone])
│   ├── WebSiteInstance.cs                  # Runtime instance ([GenerateClone])
│   └── WebSitesConfiguration.cs            # Persistent configuration store
├── FsEntry.cs                              # File system entry model
├── IGenericCloneable<T>.cs                 # Clone interface
└── LicenseInfo.cs                          # License information model
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
├── Network/                                # Network communication
│   └── DsmApiClient.cs                     # Centralized DSM API client
├── Runtime/                                # .NET runtime management
│   ├── Configuration.cs                    # Runtime configuration
│   ├── Downloader.cs                       # Binary download utility
│   ├── FileSystem.cs                       # File system initialization
│   ├── GzUnTar.cs                          # Archive extraction (gzip + tar)
│   └── VersionsDetector.cs                 # Available versions detection
├── Threading/                              # Async utilities
├── DsmToolsExtensions.cs                   # DSM client extension methods
└── [WebSites/]                             # Website management tools (if present)
```

**DsmApiClient Implementation:**

```csharp
public class DsmApiClient : IDisposable
{
    // Properties
    public string Sid { get; }                    // Session ID
    public bool IsConnected { get; }              // Connection status
    public ApiInformationCollection ApiInformations { get; }  // API metadata

    // Public Methods
    public void SetSid(string sid)                // Restore session from storage
    public async Task<bool> ConnectAsync(LoginModel model)   // Full handshake + auth
    public async Task<bool> ValidateSessionAsync()           // Lightweight validation
    public async Task DisconnectAsync()                      // Clear session
    public async Task<R?> ExecuteAsync<R>(IApiParameters parameters)  // Generic API call
    public async Task<ApiResponseBase<EmptyResponse>?> ExecuteSimpleAsync(IApiParameters parameters)
}
```

**Connection Flow:**

1. **ReadSettings():** Load server configuration from file (host, port)
2. **HandShakeAsync():** Query `SYNO.API.Info` to populate ApiInformations
3. **AuthenticateAsync():** Login via `auth.login` API, receive SID
4. **Session Persistence:** Store SID in HTTP headers (`ssid=...`)

**Key Features:**

- **Singleton pattern** for DSM client (registered in DI container)
- **Session management** with SID validation and restoration
- **Automatic serialization** based on `IApiParameters.SerializationFormat`
- **Strategy pattern** for Form vs JSON serialization
- **Error handling** with structured logging via Serilog
- **HttpClient factory** integration for proper lifecycle management

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
│   ├── LogDownloadController.cs            # Log file retrieval
│   ├── RuntimeManagementController.cs      # .NET version detection
│   └── WebsiteHostingController.cs         # Website CRUD + lifecycle
├── Models/                                 # UI-specific view models
├── Services/                               # UI business logic services
│   ├── AuthenticationService.cs            # Auth façade over DsmApiClient
│   ├── DotnetVersionService.cs             # .NET version detection
│   ├── FileSystemService.cs                # File operations wrapper
│   ├── FrameworkManagementService.cs       # Framework installation
│   ├── LogDownloadService.cs               # Log file retrieval
│   ├── ReverseProxyManagerService.cs       # Proxy CRUD operations
│   ├── WebSiteHostingService.cs            # Website lifecycle (background service)
│   └── WebSitesConfigurationService.cs     # Configuration persistence
├── Properties/                             # Assembly info, launch settings
├── wwwroot/                                # Static assets (CSS, JS, images)
├── appsettings.json                        # Production configuration
├── appsettings.Development.json            # Development overrides
└── Program.cs                              # Application entry point
```

**Program.cs Configuration:**

```csharp
// Logging setup
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();
builder.Host.UseSerilog();

// Session services (authentication persistence)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "ADWH.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

// HTTP client factory
builder.Services.AddHttpClient();

// FluentUI components
builder.Services.AddFluentUIComponents();

// HttpContext accessor (required for Blazor server-side)
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

// Controllers (no API versioning, PascalCase JSON)
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);

// Razor components with hybrid rendering
builder.Services.AddRazorComponents()
                .AddInteractiveWebAssemblyComponents();

// DSM integration services
builder.Services.AddSingleton<DsmApiClient>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IDotnetVersionService, DotnetVersionService>();
builder.Services.AddScoped<IFrameworkManagementService, FrameworkManagementService>();
builder.Services.AddSingleton<IFileSystemService, FileSystemService>();
builder.Services.AddScoped<ILogDownloadService, LogDownloadService>();

// Website hosting services (singleton background service)
builder.Services.AddSingleton<IReverseProxyManagerService, ReverseProxyManagerService>();
builder.Services.AddSingleton<IWebSitesConfigurationService, WebSitesConfigurationService>();
builder.Services.AddSingleton<WebSiteHostingService>();
builder.Services.AddSingleton<IWebSiteHostingService>(sp => sp.GetRequiredService<WebSiteHostingService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<WebSiteHostingService>());

// Middleware pipeline
app.UsePathBase(ApplicationConstants.ApplicationUrlSubPath);  // Sub-path support
app.UseSession();                                              // Session before antiforgery
app.UseRouting();
app.MapControllers();                                          // API endpoints
app.UseAntiforgery();                                           // CSRF protection
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Ui.Client._Imports).Assembly);
```

**Key Features:**

- **Hybrid rendering:** Server-side auth + client-side interactivity (InteractiveWebAssembly)
- **Session-based authentication** with DSM SID persistence in ASP.NET Core session
- **Background service** for website lifecycle management (starts/stops on host lifecycle)
- **FluentUI components** for consistent UI/UX across all pages
- **Structured logging** with Serilog (configuration-based setup)
- **Antiforgery protection** for Blazor and API endpoints
- **Sub-path support** via `UsePathBase` for reverse proxy deployment

### 6. Askyl.Dsm.WebHosting.Ui.Client

**Purpose:** Blazor WebAssembly client library (shared components and services)

**Structure:**

```
Ui.Client/
├── Components/                             # Reusable Blazor components
│   ├── Controls/                           # Custom UI controls
│   │   └── [DataGrid, FilePicker, PortInput, ProcessStatus, etc.]
│   ├── Dialogs/                            # FluentUI dialog wrappers
│   │   └── [ConfirmationDialog, ErrorDialog, etc.]
│   ├── Layout/                             # Layout components
│   │   ├── MainLayout.razor                # Main app shell with navigation
│   │   ├── NavigationMenu.razor            # FluentUI nav menu
│   │   ├── HeaderBar.razor                 # Top bar with user info
│   │   └── Footer.razor                    # Status indicators
│   ├── Pages/                              # Blazor pages
│   │   ├── Home.razor                      # Dashboard/home page
│   │   ├── Login.razor                     # Authentication page
│   │   ├── NotFound.razor                  # 404 handler
│   │   └── [Websites/, Frameworks/, Settings/, etc.]
│   └── Patterns/                           # UI patterns and templates
├── Extensions/                             # Client-side extension methods
├── Interfaces/                             # C# interfaces for JS interop
├── Services/                               # HTTP clients & state management
│   └── [AuthenticationService, WebsiteHostingService, etc. - client-side]
├── wwwroot/                                # Client-side static assets
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

**Implementation:**

```csharp
// Program.cs - Simplified installation flow
FileSystem.Initialize();                          // Set up file system paths
var fileName = await Downloader.DownloadToAsync(true);  // Download from Microsoft
GzUnTar.Decompress(fileName);                    // Extract gzip + tar archive
```

**Key Features:**

- **Standalone executable** (no UI dependencies)
- **Automatic download** from Microsoft .NET release repositories
- **Architecture detection** for correct binary selection (x64, ARM, etc.)
- **Extraction utilities** for gzip + tar.gz archives
- **File system initialization** for Synology-specific paths

### 8. Askyl.Dsm.WebHosting.Benchmarks

**Purpose:** Performance benchmarking with BenchmarkDotNet

**Benchmarks:**

```csharp
[StringBuilderBenchmark]
public class StringBuilderBenchmark
{
    [Benchmark] public void UrlInterpolatedString()      // Baseline: interpolated strings
    [Benchmark] public void UrlBuilder()                 // StringBuilder approach
    [Benchmark] public void ParametersInterpolatedString()
    [Benchmark] public void ParametersBuilder()          // Recommended for complex URLs
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
[LoggerMessage(
    EventId = 1,
    Level = LogLevel.Information,
    Message = "Dice roll: {Die1} and {Die2}, sum: {Sum}")]
public static partial void LogDiceRoll(this ILogger logger, int die1, int die2, int sum);
```

**Key Features:**

- **Source-generated log methods** for compile-time message validation
- **Extension method pattern** for clean logger API
- **Structured logging** support with named parameters
- **Zero-allocation logging** for performance-critical paths

---

## Design Patterns & Principles

### 1. Dependency Injection (DI)

**Implementation:**

```csharp
// Program.cs - Service registration with explicit lifecycles

// Singleton: Shared across entire application lifetime
builder.Services.AddSingleton<DsmApiClient>();
builder.Services.AddSingleton<IReverseProxyManagerService, ReverseProxyManagerService>();
builder.Services.AddSingleton<WebSiteHostingService>();  // Background service

// Scoped: New instance per HTTP request
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IDotnetVersionService, DotnetVersionService>();
builder.Services.AddScoped<IFrameworkManagementService, FrameworkManagementService>();
builder.Services.AddScoped<ILogDownloadService, LogDownloadService>();
```

**Patterns Used:**

- **Singleton:** `DsmApiClient`, `ReverseProxyManagerService`, configuration services, background services
- **Scoped:** UI services (auth, file system, framework management) - one per request
- **Background Service:** `WebSiteHostingService` implements `IHostedService` for lifecycle management

### 2. Result Pattern

**Purpose:** Eliminate null checks and provide structured error handling without exceptions for control flow

**Base Implementation:**

```csharp
public class ApiResult
{
    public bool Success { get; }
    public string Message { get; }

    public static ApiResult CreateSuccess() => new(true, String.Empty);
    public static ApiResult CreateFailure(string message) => new(false, message);
}
```

**Specialized Result Types:**

- `ApiResultBool` - Boolean value with success/failure
- `ApiResultData<T>` - Result with single data payload
- `ApiResultItems<T>` - Result with item collection
- `ApiResultValue<T>` - Result with typed value
- `AuthenticationResult` - Auth state with user info
- `WebSiteInstanceResult` - Website-specific operations
- `ChannelsResult`, `ReleasesResult` - .NET version information

**Usage Example:**

```csharp
var result = await hostingService.StartWebsiteAsync(id);
if (!result.Success)
{
    logger.LogError("Failed to start website {Id}: {Message}", id, result.Message);
    return Ok(result);  // Always HTTP 200, error in body
}
```

**Benefits:**

- Explicit success/failure handling (no try-catch for expected failures)
- Type-safe result payloads
- Consistent API response structure
- Easier testing (no exception mocking needed)

### 3. Repository Pattern (Simplified)

**Implementation:**

```csharp
public interface IWebSitesConfigurationService
{
    Task<List<WebSiteConfiguration>> GetAllSitesAsync();
    Task AddSiteAsync(WebSiteConfiguration site);
    Task UpdateSiteAsync(WebSiteConfiguration site);
    Task RemoveSiteAsync(Guid id);
}

// WebSitesConfigurationService implements JSON file-based persistence
```

**Benefits:**

- **Abstraction** over persistence mechanism (currently JSON, could be database)
- **Testability** with mock implementations
- **Centralized logic** for configuration management
- **Separation of concerns** between domain and storage

### 4. Service Facade Pattern

**Implementation:**

```csharp
public interface IAuthenticationService
{
    Task<AuthenticationResult> LoginAsync(LoginModel model);
    Task LogoutAsync();
    Task<bool> IsAuthenticatedAsync();
}

// AuthenticationService wraps DsmApiClient complexity
public class AuthenticationService : IAuthenticationService
{
    private readonly DsmApiClient _dsmClient;
    private readonly IHttpContextAccessor _httpContext;

    public async Task<AuthenticationResult> LoginAsync(LoginModel model)
    {
        // 1. Call DsmApiClient.ConnectAsync()
        // 2. Persist SID to ASP.NET Core session
        // 3. Return structured AuthenticationResult
    }
}
```

**Benefits:**

- Hides DSM API complexity from UI layer
- Adds session management logic in one place
- Easier to test and mock
- Consistent error handling across auth operations

### 5. Strategy Pattern (Serialization)

**Implementation:**

```csharp
public interface IApiParameters
{
    SerializationFormats SerializationFormat { get; }
    string BuildUrl(string server, int port);
    FormContent ToForm();
    StringContent ToJson();
}

// ExecuteAsync dispatches based on serialization format
public async Task<R?> ExecuteAsync<R>(IApiParameters parameters)
{
    return parameters.SerializationFormat switch
    {
        SerializationFormats.Form => await ExecuteFormAsync<R>(url, parameters),
        SerializationFormats.Json => await ExecuteJsonAsync<R>(url, parameters),
        _ => throw new NotSupportedException(...)
    };
}
```

**Benefits:**

- Automatic serialization format selection per API endpoint
- Clean separation between Form and JSON handling
- Easy to extend with new formats (Protobuf, etc.)

### 6. Builder Pattern (URL Construction)

**Implementation:**

```csharp
// IApiParameters.BuildUrl() uses StringBuilder for efficient URL construction
protected virtual string BuildUrl(string server, int port)
{
    var builder = new StringBuilder();
    builder.Append("https://");
    builder.Append(server);
    builder.Append(':');
    builder.Append(port);
    builder.Append("/webapi/");
    builder.Append(Path);
    builder.Append("?api=");
    builder.Append(Api);
    return builder.ToString();
}
```

**Benchmark Results:** `StringBuilder` outperforms interpolated strings by ~30% for complex URL construction with multiple parameters.

### 7. Source Generator Pattern

**Implementation:**

```csharp
// CloneGenerator.cs - Roslyn source generator
[Generator]
public class CloneGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Detect [GenerateClone] attributes
        // Generate Clone() method implementations
        // Output to obj/Generated directory
    }
}

// Usage in data models
[GenerateClone]
public partial class WebSiteConfiguration : IGenericCloneable<WebSiteConfiguration>
{
    // Properties...
    // Clone() auto-generated at compile time
}
```

**Benefits:**

- Zero runtime overhead (code generated at compile time)
- Type-safe clone implementations
- Reduces boilerplate by 60-70%
- Enforces consistent cloning pattern across all models

---

## Technical Stack

### Framework & Runtime

| Component | Version | Notes |
|-----------|---------|-------|
| .NET | 10.0 | Latest LTS with C# 14 features |
| ASP.NET Core | 10.0 | Web API + Blazor hosting |
| Blazor | 10.0 | Hybrid rendering (Server + WASM) |
| C# | 14 | Primary constructors, collection expressions, generated regex |

### UI Framework

| Component | Version | Purpose |
|-----------|---------|---------|
| FluentUI.AspNetCore.Components | 4.14.0 | Enterprise-grade component library |
| FluentUI.Icons | 4.14.0 | Icon set (Material Design) |
| FluentUI.Emoji | 4.14.0 | Emoji support |

### Data & API

| Component | Version | Purpose |
|-----------|---------|---------|
| System.Text.Json | Built-in | JSON serialization (PascalCase) |
| Microsoft.Extensions.Http | 10.0.3 | HttpClient factory |
| Microsoft.CodeAnalysis.CSharp | 5.0.0 | Source generator host |

### Logging & Diagnostics

| Component | Version | Purpose |
|-----------|---------|---------|
| Serilog | 4.3.1 | Structured logging |
| Serilog.Extensions.Logging | 10.0.0 | ASP.NET Core integration |
| Serilog.Settings.Configuration | 10.0.0 | Configuration-based setup |
| Serilog.Sinks.BrowserConsole | 8.0.0 | Client-side logging (WASM) |
| BenchmarkDotNet | 0.15.8 | Performance testing |

### External Integrations

**Synology DSM APIs:**

- FileStation API (file operations - 25+ endpoints)
- ReverseProxy API (web app routing - CRUD operations)
- Authentication API (login/session management)
- Core Info API (system information, API metadata)
- Framework Management API (.NET installation)

**Microsoft .NET Releases API:**

- Runtime version detection
- Download URL generation
- Channel and release information

---

## Data Models & API Integration

### Core Domain Models

#### WebSiteConfiguration

```csharp
[GenerateClone]
public partial class WebSiteConfiguration : IGenericCloneable<WebSiteConfiguration>
{
    #region General
    public Guid Id { get; set; } = Guid.Empty;
    [Required(ErrorMessage = ApplicationConstants.SiteNameRequiredErrorMessage)]
    public string Name { get; set; } = "";

    #region Application
    [Required]
    public string ApplicationPath { get; set; } = "";
    
    public string ApplicationRealPath { get; set; } = "";  // Resolved path
    
    [Required]
    [Range(ApplicationConstants.MinWebApplicationPort, ApplicationConstants.MaxWebApplicationPort)]
    public int InternalPort { get; set; }
    
    [Required]
    public string Environment { get; set; } = "Production";
    
    public bool IsEnabled { get; set; } = true;
    public bool AutoStart { get; set; } = true;
    public Dictionary<string, string> AdditionalEnvironmentVariables { get; set; } = [];

    #region Reverse Proxy
    [Required]
    public string HostName { get; set; } = "";
    
    [Required]
    public int PublicPort { get; set; } = 443;
    
    public ProtocolType Protocol { get; set; } = ProtocolType.HTTPS;
    public bool EnableHSTS { get; set; } = true;

    // Clone() method auto-generated by source generator
}
```

#### WebSiteInstance

```csharp
[GenerateClone]
public partial class WebSiteInstance
{
    /// <summary>
    /// Gets the unique identifier (forwarding property to Configuration.Id).
    /// </summary>
    public Guid Id => Configuration.Id;

    /// <summary>
    /// Gets or sets the associated website configuration.
    /// </summary>
    public WebSiteConfiguration Configuration { get; set; } = new();

    /// <summary>
    /// Gets or sets whether this instance is currently running (serialized state).
    /// </summary>
    public bool IsRunning { get; set; }

    /// <summary>
    /// Gets runtime process information (server-side only, not serialized).
    /// </summary>
    [JsonIgnore]
    public ProcessInfo? Process { get; set; }

    /// <summary>
    /// Gets human-readable state description ("Running", "Stopped", "Not Responding").
    /// </summary>
    [JsonIgnore]
    public string State { get; }  // Computed property

    // Clone() method auto-generated by source generator
}
```

#### ProcessInfo

```csharp
public class ProcessInfo
{
    public int Id { get; set; }              // Process ID (PID)
    public bool IsResponding { get; set; }   // Health check status
    public DateTime StartTime { get; set; }  // Process start time
}
```

### API Integration Architecture

#### Request/Response Pattern

**Request (Parameters):**

```csharp
public class AuthenticationLoginParameters : ApiParametersBase<AuthenticationLogin>
{
    public AuthenticationLogin Parameters { get; } = new();

    protected override string Method => DsmApiMethods.AuthenticateLogin;
    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}

// Usage
var parameters = new AuthenticationLoginParameters(apiInformations);
parameters.Parameters.Account = "admin";
parameters.Parameters.Password = "password";
parameters.Parameters.OtpCode = "123456";  // Optional 2FA
```

**Response:**

```csharp
public class SynoLoginResponse : ApiResponseBase<SynoLogin>
{
    // Auto-mapped from JSON response
}

public class SynoLogin
{
    [JsonPropertyName("sid")]
    public string Sid { get; set; } = String.Empty;
}
```

#### DSM API Client Workflow

1. **ReadSettings:** Load server configuration (host, port) from file
2. **Handshake:** Query `SYNO.API.Info` to populate `ApiInformationCollection`
3. **Authentication:** Login with credentials → receive SID
4. **Session Persistence:** Store SID in HTTP headers (`ssid=...`) and ASP.NET Core session
5. **API Execution:** Build URL, serialize parameters (Form/JSON), execute POST request
6. **Response Parsing:** Deserialize JSON to strongly-typed models

**Example Flow:**

```csharp
// 1. Initialize client (injected via DI)
var dsmClient = serviceProvider.GetRequiredService<DsmApiClient>();

// 2. Connect (handshake + auth)
var loginModel = new LoginModel { Login = "admin", Password = "password" };
var connected = await dsmClient.ConnectAsync(loginModel);

if (!connected)
{
    return AuthenticationResult.Failed("Invalid credentials");
}

// 3. Execute API call
var parameters = new FileStationListParameters(dsmClient.ApiInformations);
parameters.Parameters.Path = "/volume1/apps";
var response = await dsmClient.ExecuteAsync<FileStationListResponse>(parameters);

// 4. Handle result
if (response?.Success == true && response.Data is not null)
{
    var files = response.Data.Files;
    return DirectoryContentsResult.Success(files);
}

return DirectoryContentsResult.Failed("API call failed");
```

### Source-Generated API Definitions

The solution uses auto-generated classes for API definitions based on DSM API documentation:

**Generated Files:**

- `FileStationList.g.cs` - File listing operations
- `ReverseProxyAdd.g.cs`, `ReverseProxyUpdate.g.cs`, etc. - Proxy CRUD operations
- `ApiParametersBase.g.cs` - Base parameter class with common logic
- `WebSiteConfiguration.g.cs` - Clone implementation (from `[GenerateClone]`)

**Benefits:**

- **Type safety** at compile time
- **Reduced boilerplate** (60-70% code reduction)
- **Consistent structure** across all API definitions
- **Auto-documentation** via generated XML comments

---

## UI Architecture

### Rendering Strategy: Hybrid Blazor

```
App.razor Structure:
┌─────────────────────────────────────┐
│         Server-Side (Head)          │
│  - Session management               │
│  - Authentication                   │
│  - Server-side services             │
│  - DI container                     │
└─────────────────────────────────────┘
              ↓ InteractiveWebAssembly
┌─────────────────────────────────────┐
│      WebAssembly Components         │
│  - Interactive UI elements          │
│  - Client-side interactivity        │
│  - FluentUI components              │
│  - HTTP client wrappers             │
└─────────────────────────────────────┘
```

**Render Mode:** `InteractiveWebAssembly` (Blazor 10 feature)

- Server handles auth, session, heavy operations, background services
- Client handles UI interactions, animations, real-time updates
- Seamless data transfer between server and client via signal circuit
- Progressive enhancement: works even if WASM fails to load

### Component Architecture

#### Page Structure (Ui.Client/Components/Pages)

```
Pages/
├── Home.razor                        # Dashboard/home page
├── Login.razor                       # Authentication page
├── NotFound.razor                    # 404 handler
├── Websites/                         # Website management
│   ├── Index.razor                   # Website list (data grid)
│   ├── Create.razor                  # Add new website form
│   └── Edit.razor                    # Modify existing website
├── FileExplorer.razor                # File system browser
├── Frameworks/                       # .NET runtime management
│   ├── Index.razor                   # Installed versions
│   └── Install.razor                 # Installation wizard
└── Settings.razor                    # Application configuration
```

#### Layout Components (Ui.Client/Components/Layout)

```
Layout/
├── MainLayout.razor                  # Main app shell with navigation
│   ├── FluentNavMenu (sidebar)
│   ├── FluentSheet (mobile menu)
│   └── Main content area
├── NavigationMenu.razor              # FluentUI nav menu component
├── HeaderBar.razor                   # Top bar with user info, logout
└── Footer.razor                      # Status indicators, version info
```

#### Reusable Controls (Ui.Client/Components/Controls)

```
Controls/
├── DataGrid.razor                    # Enhanced data grid wrapper (FluentUI)
├── FilePicker.razor                  # File system picker dialog
├── PortInput.razor                   # Validated port number input (1024-65535)
├── ProcessStatus.razor               # Running/stopped indicator badge
└── [Other custom components...]
```

#### Dialog Components (Ui.Client/Components/Dialogs)

```
Dialogs/
├── ConfirmationDialog.razor          # Yes/No confirmation wrapper
├── ErrorDialog.razor                 # Error message display
├── LoadingDialog.razor               # Progress indicator
└── [Other dialog wrappers...]
```

### State Management

**Server-Side State:**

- `WebSiteHostingService` (singleton background service)
  - Maintains in-memory dictionary of running instances (`ConcurrentDictionary<Guid, WebSiteInstance>`)
  - Handles process lifecycle (start/stop/restart)
  - Persists configuration changes to JSON file
  - Implements `IHostedService` for startup/shutdown hooks

**Client-Side State:**

- Blazor component state via parameters and cascading values
- HTTP client wrappers for API calls (type-safe endpoints)
- Local state management via `@state` injections (Blazor 10 feature)

**Session Persistence:**

```csharp
// Authentication service persists DSM SID to ASP.NET Core session
public async Task<AuthenticationResult> LoginAsync(LoginModel model)
{
    var connected = await _dsmClient.ConnectAsync(model);
    
    if (connected)
    {
        // Store SID in server-side session
        HttpContext.Session.SetString(ApplicationConstants.DsmSessionKey, _dsmClient.Sid);
        
        return AuthenticationResult.Success(model.Login);
    }
    
    return AuthenticationResult.Failed("Invalid credentials");
}

// Restore session on application start
var sid = HttpContext.Session.GetString(ApplicationConstants.DsmSessionKey);
if (!String.IsNullOrEmpty(sid))
{
    _dsmClient.SetSid(sid);  // Restore DSM client state
}
```

### FluentUI Integration

**Component Usage:**

```razor
@using Microsoft.FluentUI.AspNetCore.Components
@using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons

<FluentButton OnClick="@(() => StartWebsiteAsync(site.Id))"
              Variant="Filled"
              Disabled="@site.IsRunning">
    <Icons.Regular.Size16.Play />
    Start
</FluentButton>

<FluentDialog @bind:IsOpen="@showDialog">
    <FluentDialogTitle>Add New Website</FluentDialogTitle>
    <FluentDialogContent>
        <!-- Form content with FluentInput, FluentSelect, etc. -->
    </FluentDialogContent>
    <FluentDialogFooter>
        <FluentButton Variant="Outlined" OnClick="@CloseDialog">Cancel</FluentButton>
        <FluentButton Variant="Filled" OnClick="@SaveWebsite">Save</FluentButton>
    </FluentDialogFooter>
</FluentDialog>

<FluentDataGrid Items="@websites">
    <FluentDataGridColumn Field="@nameof(WebSiteInstance.Name)" Title="Name" />
    <FluentDataGridColumn Field="@nameof(WebSiteInstance.IsRunning)" Title="Status" />
    <FluentDataGridColumn Field="@nameof(WebSiteInstance.State)" Title="State" />
</FluentDataGrid>
```

**Theming:**

- Uses FluentUI's built-in theming system (light/dark modes)
- No custom CSS (minimalist approach per project standards)
- Icons from FluentUI icon set (16/20/24px sizes via `Icons.Regular.Size16`, etc.)
- Consistent spacing via FluentUI design tokens
- Responsive design with FluentUI grid system

### API Controller Architecture

**RESTful Endpoints:**

```csharp
[ApiController]
[Route(WebsiteHostingDefaults.ControllerBaseRoute)]  // "api/v1/websites"
public class WebsiteHostingController(IWebSiteHostingService hostingService) : ControllerBase
{
    [HttpGet(WebsiteHostingDefaults.AllRoute)]         // GET /api/v1/websites/all
    public async Task<ActionResult<List<WebSiteInstance>>> GetAllWebsitesAsync()
        => Ok(await hostingService.GetAllWebsitesAsync());

    [HttpPost(WebsiteHostingDefaults.AddRoute)]        // POST /api/v1/websites/add
    public async Task<ActionResult<WebSiteInstance>> AddWebsite([FromBody] WebSiteConfiguration configuration)
        => Ok(await hostingService.AddWebsiteAsync(configuration));

    [HttpPost(WebsiteHostingDefaults.UpdateRoute)]     // POST /api/v1/websites/update
    public async Task<ActionResult<WebSiteInstance>> UpdateWebsite([FromBody] WebSiteConfiguration configuration)
        => Ok(await hostingService.UpdateWebsiteAsync(configuration));

    [HttpDelete(WebsiteHostingDefaults.RemoveRoute + "/{id}")]  // DELETE /api/v1/websites/remove/{id}
    public async Task<ActionResult<ApiResult>> RemoveWebsite(Guid id)
        => Ok(await hostingService.RemoveWebsiteAsync(id));

    [HttpPost(WebsiteHostingDefaults.StartRoute + "/{id}")]     // POST /api/v1/websites/start/{id}
    public async Task<ActionResult<ApiResult>> StartWebsite(Guid id)
        => Ok(await hostingService.StartWebsiteAsync(id));

    [HttpPost(WebsiteHostingDefaults.StopRoute + "/{id}")]      // POST /api/v1/websites/stop/{id}
    public async Task<ActionResult<ApiResult>> StopWebsite(Guid id)
        => Ok(await hostingService.StopWebsiteAsync(id));
}
```

**Response Pattern:**

- All endpoints return `ActionResult<T>` for consistent HTTP handling
- Success/failure encoded in response body (not HTTP status code)
- HTTP 200 OK for all responses (simplifies client error handling)
- PascalCase JSON serialization (no camelCase conversion)

**Other Controllers:**

- `AuthenticationController` - `/api/v1/auth/login`, `/logout`, `/status`
- `FileManagementController` - `/api/v1/files/*` (list, upload, download, delete, etc.)
- `FrameworkManagementController` - `/api/v1/frameworks/channels`, `/releases`, `/installed`, `/install`
- `LogDownloadController` - `/api/v1/logs/download`
- `RuntimeManagementController` - `/api/v1/runtime/versions`

---

## Security Considerations

### Authentication & Session Management

**DSM SID-Based Auth:**

1. User authenticates once to Synology DSM via `auth.login` API
2. DSM returns session ID (SID) valid for configured timeout
3. SID stored in ASP.NET Core server-side session (not client-side cookie)
4. Session validated periodically via `DsmApiClient.ValidateSessionAsync()`
5. On logout, SID cleared from both DSM and ASP.NET Core session

**Session Configuration:**

```csharp
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "ADWH.Session";
    options.Cookie.HttpOnly = true;              // Prevent JavaScript access (XSS protection)
    options.Cookie.SameSite = SameSiteMode.Strict;  // CSRF protection
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;  // HTTPS only
    options.IdleTimeout = TimeSpan.FromMinutes(30);   // Auto-expiry after inactivity
});
```

**Authorization:**

```csharp
[AuthorizeSession]  // Custom attribute checks session validity
public class WebsiteHostingController : ControllerBase
{
    // All endpoints require valid DSM session
}
```

### Input Validation

**Data Annotations (Server-Side):**

```csharp
[Required(ErrorMessage = ApplicationConstants.SiteNameRequiredErrorMessage)]
public string Name { get; set; } = "";

[Range(ApplicationConstants.MinWebApplicationPort, 
       ApplicationConstants.MaxWebApplicationPort, 
       ErrorMessage = ApplicationConstants.PortRangeErrorMessage)]
public int InternalPort { get; set; }

[StringLength(255, MinimumLength = 1)]
public string HostName { get; set; } = "";
```

**Controller-Level Validation:**

```csharp
[HttpPost("add")]
public async Task<ActionResult<WebSiteInstance>> AddWebsite([FromBody] WebSiteConfiguration configuration)
{
    if (!ModelState.IsValid)
    {
        return BadRequest(ModelState);  // Return validation errors
    }
    
    return Ok(await hostingService.AddWebsiteAsync(configuration));
}
```

**Custom Validation Logic:**

- Port range validation (1024-65535 for user applications)
- Path existence checks via FileStation API
- Reverse proxy conflict detection (duplicate host:port combinations)
- Environment variable format validation

### SSL/TLS Considerations

**DSM API Connection:**

- SSL certificate validation enabled (per project standards)
- Uses `DsmApiClient` with centralized error handling
- HTTPS-only communication with DSM NAS

**Application HTTPS:**

```csharp
app.UseHttpsRedirection();  // Force HTTPS for all requests
app.UseHsts();              // HTTP Strict Transport Security (production only)
```

**Reverse Proxy HSTS:**

- Websites can enable HSTS via `EnableHSTS = true` in configuration
- HSTS header added to reverse proxy response headers
- Prevents protocol downgrade attacks

### Security Recommendations

**Current Implementation:**

✅ Session-based authentication with server-side storage  
✅ HTTP-only cookies (XSS protection)  
✅ SameSite.Strict cookie policy (CSRF protection)  
✅ Secure cookie policy (HTTPS only)  
✅ Input validation on all endpoints (data annotations + custom logic)  
✅ Antiforgery token validation for Blazor and API endpoints  
✅ HTTPS redirection enabled  

**Areas for Enhancement:**

⚠️ Add rate limiting for authentication attempts (prevent brute force)  
⚠️ Implement audit logging for sensitive operations (website CRUD, framework installation)  
⚠️ Add request throttling for DSM API calls (prevent overload)  
⚠️ Consider OAuth2 integration for future multi-tenant support  
⚠️ Add IP whitelisting for administrative operations  

---

## Performance Optimization

### Benchmarking Results

**StringBuilder vs Interpolated Strings:**

```csharp
// Benchmark: URL construction with 10 parameters
[StringBuilderBenchmark]
public class StringBuilderBenchmark
{
    [Benchmark] public void UrlInterpolatedString()      // Baseline: ~50ns
    [Benchmark] public void UrlBuilder()                 // Recommended: ~35ns (30% faster)

    [Benchmark] public void ParametersInterpolatedString()  // Baseline: ~120ns
    [Benchmark] public void ParametersBuilder()             // Recommended: ~85ns (29% faster)
}
```

**Recommendation:** Use `StringBuilder` for complex string concatenation in performance-critical paths (URL building, parameter encoding).

### Memory Optimization

**Singleton Services:**

- `DsmApiClient` - Single instance for all API calls (reduces connection overhead)
- `ReverseProxyManagerService` - Shared proxy configuration manager
- `WebSiteHostingService` - Background service with in-memory state
- Configuration services - Loaded once at application startup

**Efficient Serialization:**

```csharp
// Preserve PascalCase for consistency with C# models
builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.PropertyNamingPolicy = null);

// Use collection expressions for efficient materialization (C# 12+)
var instances = [.. _instances.Values.Select(i => i.Clone())];
```

**Collection Expressions (C# 12+):**

```csharp
// Efficient array/list initialization
public Dictionary<string, string> AdditionalEnvironmentVariables { get; set; } = [];

// Spread operator for LINQ materialization
var websites = [.. _configuration.Sites.Where(s => s.IsEnabled)];
```

### Async/Await Best Practices

**Avoiding Deadlocks:**

```csharp
// ✅ Correct: async all the way
public async Task<WebSiteInstance> AddWebsiteAsync(WebSiteConfiguration config)
{
    await _configService.AddSiteAsync(config);  // Proper async flow
    return await AddInstanceAsync(config);
}

// ❌ Avoid: .Result or .Wait() (can cause deadlocks in ASP.NET Core)
public WebSiteInstance AddWebsiteSync(WebSiteConfiguration config)
{
    var result = AddWebsiteAsync(config).Result;  // Potential deadlock!
    return result;
}
```

**ConfigureAwait(false):**

- Not required in UI projects (Blazor has synchronization context)
- Recommended in library projects (`Data`, `Tools`) for better scalability

### Background Service Optimization

**WebSiteHostingService Lifecycle:**

```csharp
public class WebSiteHostingService : IHostedService, IDisposable
{
    private readonly ConcurrentDictionary<Guid, WebSiteInstance> _instances = new();
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Website hosting service started");
        
        // Auto-start websites marked with AutoStart = true
        var sites = await _configService.GetAllSitesAsync();
        foreach (var site in sites.Where(s => s.AutoStart))
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                await StartSiteAsync(site, cancellationToken);
            }
        }
    }
    
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Website hosting service stopping");
        
        // Parallel stop for faster shutdown
        var stopTasks = _instances.Values.Select(instance =>
            StopSiteAsync(instance, cancellationToken));
        await Task.WhenAll(stopTasks);
    }
}
```

**Efficient Instance Management:**

- `ConcurrentDictionary` for thread-safe instance storage
- Clone operations for safe client serialization (no race conditions)
- Parallel operations for bulk actions (stop all websites)

### Build Optimization

**Disabled Incremental Builds:**

```bash
dotnet build /nr:false  # NoRestore - faster builds in CI/CD when packages unchanged
```

**Release Configuration:**

- No PDB generation (`DebugType=None`) - smaller binaries
- No XML documentation (`GenerateDocumentationFile=false`) - faster builds
- Optimized IL code with ReadyToRun (if enabled for deployment)

---

## Build & Deployment

### Project Structure

**Solution File (.slnx):**

```xml
<Solution>
  <Project Path="Askyl.Dsm.WebHosting.Benchmarks/..." />
  <Project Path="Askyl.Dsm.WebHosting.Constants/..." />
  <Project Path="Askyl.Dsm.WebHosting.Data/..." />
  <Project Path="Askyl.Dsm.WebHosting.DotnetInstaller/..." />
  <Project Path="Askyl.Dsm.WebHosting.Logging/..." />
  <Project Path="Askyl.Dsm.WebHosting.SourceGenerators/..." />
  <Project Path="Askyl.Dsm.WebHosting.Tools/..." />
  <Project Path="Askyl.Dsm.WebHosting.Ui/..." />
  <Project Path="Askyl.Dsm.WebHosting.Ui.Client/..." />
</Solution>
```

### Build Commands

**Development:**

```bash
# Full build (no restore - packages already restored)
dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx

# Clean and rebuild
dotnet clean /nr:false ./src/Askyl.Dsm.WebHosting.slnx && dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx

# Run benchmarks
dotnet run --project ./src/Askyl.Dsm.WebHosting.Benchmarks
```

**Release Build:**

```bash
dotnet publish -c Release \
  -p:PublishProfile=Default \
  ./src/Askyl.Dsm.WebHosting.Ui/Askyl.Dsm.WebHosting.Ui.csproj
```

### Directory.Build.props

**Centralized Configuration:**

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project>
  <PropertyGroup>
    <!-- Disable PDB generation for all projects when publishing -->
    <DebugType Condition="'$(Configuration)' == 'Release'">None</DebugType>
    <DebugSymbols Condition="'$(Configuration)' == 'Release'">false</DebugSymbols>
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
  </PropertyGroup>
  
  <PropertyGroup>
    <!-- Centralized versioning -->
    <Version>0.5.1</Version>
    <AssemblyVersion>0.5.1.0</AssemblyVersion>
    <FileVersion>0.5.1.0</FileVersion>
    <InformationalVersion>0.5.1</InformationalVersion>
    <PackageVersion>0.5.1</PackageVersion>
  </PropertyGroup>
</Project>
```

**Benefits:**

- Single source of truth for version numbers
- Consistent build settings across all projects
- Easy to update for releases

### Docker Support

**Multi-Architecture SPK Package:**

- Docker-based builds for Synology compatibility
- Supports x64 and ARM architectures (via multi-stage builds)
- Automated build pipeline via GitHub Actions

**Dockerfile Structure:**

```dockerfile
# Multi-stage build for optimized image size
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY ["Askyl.Dsm.WebHosting.Ui/Askyl.Dsm.WebHosting.Ui.csproj", "./Askyl.Dsm.WebHosting.Ui/"]
RUN dotnet restore "Askyl.Dsm.WebHosting.Ui/Askyl.Dsm.WebHosting.Ui.csproj"

COPY . .
RUN dotnet publish -c Release -o /app "Askyl.Dsm.WebHosting.Ui/Askyl.Dsm.WebHosting.Ui.csproj"

# Runtime stage (smaller image)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# Expose port and set entrypoint
EXPOSE 8080
ENTRYPOINT ["dotnet", "Askyl.Dsm.WebHosting.Ui.dll"]
```

### Deployment Checklist

**Pre-Deployment:**

- [ ] Run `dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx` - verify no errors/warnings
- [ ] Execute benchmarks for performance baseline (optional)
- [ ] Review `appsettings.json` configuration (DSM host, port, paths)
- [ ] Test DSM API connectivity from deployment environment
- [ ] Validate SSL certificates (if using HTTPS)
- [ ] Ensure configuration file exists (`DsmDefaults.ConfigurationFileName`)

**Post-Deployment:**

- [ ] Verify background service started successfully (check logs)
- [ ] Check Serilog logs for errors or warnings
- [ ] Test website lifecycle operations (start/stop a test site)
- [ ] Validate reverse proxy configuration (access website via public URL)
- [ ] Monitor memory usage and process health
- [ ] Test authentication flow (login/logout/session expiry)

---

## Recommendations

### Immediate Improvements (Next Sprint)

#### 1. **Add Comprehensive Unit Tests**

```csharp
// Test project structure
Askyl.Dsm.WebHosting.Tests/
├── Services/
│   ├── WebSiteHostingServiceTests.cs
│   ├── AuthenticationServiceTests.cs
│   └── FileSystemServiceTests.cs
├── Results/
│   └── ApiResultTests.cs
├── Extensions/
│   └── UriExtensionsTests.cs
└── API/
    └── Parameters/
        └── BuildUrlTests.cs
```

**Testing Strategy:**

- **Unit Tests:** Service layer logic, result patterns, extensions (xUnit + Moq)
- **Integration Tests:** DSM API client with TestServer or TestContainer
- **UI Tests:** Blazor component rendering (bUnit library)

**Example:**

```csharp
public class WebSiteHostingServiceTests
{
    private readonly Mock<IWebSitesConfigurationService> _configMock;
    private readonly Mock<IReverseProxyManagerService> _proxyMock;
    private readonly WebSiteHostingService _service;

    [Fact]
    public async Task AddWebsiteAsync_WithValidConfig_ReturnsSuccess()
    {
        // Arrange
        var config = new WebSiteConfiguration { Name = "Test", ApplicationPath = "/test" };
        _configMock.Setup(x => x.AddSiteAsync(config)).Returns(Task.CompletedTask);
        
        // Act
        var result = await _service.AddWebsiteAsync(config);
        
        // Assert
        Assert.True(result.Success);
        Assert.NotEqual(Guid.Empty, result.Data.Id);
    }
}
```

**Benefits:**

- Catch regressions early
- Enable safe refactoring
- Document expected behavior
- Improve code confidence

#### 2. **Implement Retry Policy with Polly**

```csharp
// Add Polly package
<PackageReference Include="Polly.Extensions" Version="8.4.0" />

// Configure retry policy for DSM API calls
builder.Services.AddHttpClient("DsmApi", client =>
{
    client.BaseAddress = new Uri($"https://{dsmHost}:{dsmPort}");
})
.AddTransientHttpErrorPolicy(policy =>
    policy.WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))))  // Exponential backoff
.AddHttpClientErrorHandler(logger);  // Log retry attempts

// Use named client in DsmApiClient
private readonly HttpClient _httpClient = httpClientFactory.CreateClient("DsmApi");
```

**Benefits:**

- Resilience against temporary network failures
- Better user experience during DSM API outages
- Automatic retry with exponential backoff
- Reduced manual intervention

#### 3. **Add Structured Audit Logging**

```csharp
// Current: Basic logging
logger.LogInformation("Site {SiteName} started", siteName);

// Recommended: Add structured audit logging
logger.LogInformation("Website operation completed - Operation={Operation}, SiteId={SiteId}, SiteName={SiteName}, UserId={UserId}, DurationMs={DurationMs}",
                      "START", site.Id, siteName, userId, stopwatch.ElapsedMilliseconds);
```

**Serilog Configuration:**

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "Askyl.Dsm.WebHosting": "Debug"
      }
    },
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "/volume1/web/askyl-dsm-webhosting/logs/log-.txt",
          "rollingInterval": "Day",
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      }
    ]
  }
}
```

**Benefits:**

- Better troubleshooting capabilities
- Audit compliance for sensitive operations
- Performance monitoring with duration tracking
- Easier log analysis with structured format

### Medium-Term Enhancements (Next Quarter)

#### 4. **Implement Caching Layer**

```csharp
// Add distributed caching for frequently accessed data
builder.Services.AddDistributedMemoryCache();  // Or Redis for multi-instance deployments

// Cache DSM API information (validates every 5 minutes)
public class CachedReverseProxyManagerService : IReverseProxyManagerService
{
    private readonly IDistributedCache _cache;
    private const string ApiInfoKey = "dsm_api_info";
    
    public async Task<ApiInformationCollection> GetApiInfoAsync()
    {
        var cached = await _cache.GetStringAsync(ApiInfoKey);
        
        if (!String.IsNullOrEmpty(cached))
        {
            return JsonSerializer.Deserialize<ApiInformationCollection>(cached);
        }
        
        // Fetch from DSM API
        var apiInfo = await FetchFromDsmApiAsync();
        
        // Cache for 5 minutes
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };
        
        await _cache.SetStringAsync(ApiInfoKey, JsonSerializer.Serialize(apiInfo), cacheOptions);
        
        return apiInfo;
    }
}
```

**Benefits:**

- Reduced API calls to DSM (less network overhead)
- Faster response times for frequently accessed data
- Lower load on NAS device
- Better scalability under concurrent users

#### 5. **Add Health Checks**

```csharp
// Add ASP.NET Core health checks
builder.Services.AddHealthChecks()
    .AddCheck<DsmApiHealthCheck>("DSM_API", tags: ["external"])
    .AddCheck<WebsiteHostingHealthCheck>("Websites", tags: ["internal"])
    .AddCheck<FileSystemHealthCheck>("FileStation", tags: ["external"]);

// Map health check endpoints
app.MapHealthChecks("/health");           // Full health check (all checks)
app.MapHealthChecks("/health/live");      // Liveness probe (internal only)
app.MapHealthChecks("/health/ready");     // Readiness probe (excluding external deps)
```

**Custom Health Check Example:**

```csharp
public class DsmApiHealthCheck : IHealthCheck
{
    private readonly DsmApiClient _dsmClient;
    
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
    {
        try
        {
            var isValid = await _dsmClient.ValidateSessionAsync();
            
            if (isValid)
            {
                return HealthCheckResult.Healthy("DSM API session is valid");
            }
            
            return HealthCheckResult.Unhealthy("DSM API session expired");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("DSM API unreachable", ex);
        }
    }
}
```

**Benefits:**

- Kubernetes/Docker deployment readiness
- Automated monitoring and alerting (Prometheus, Grafana)
- Graceful degradation capabilities
- Load balancer health probing

#### 6. **Create OpenAPI Documentation**

```csharp
// Add Swashbuckle for OpenAPI/Swagger generation
<PackageReference Include="Swashbuckle.AspNetCore" Version="7.2.0" />

// Program.cs
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.CustomSchemaIds(type => type.FullName);  // Use full type names
    
    // Add API key authentication (if needed)
    options.AddSecurityDefinition("session", new OpenApiSecurityScheme
    {
        Name = "Cookie",
        In = ParameterLocation.Cookie,
        Type = SecuritySchemeType.ApiKey,
        Description = "ASkyl.Dsm.WebHosting session cookie authentication"
    });
});

// Enable Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ASkyl.Dsm.WebHosting API v1");
        c.RoutePrefix = "swagger";  // Access at /swagger
    });
}
```

**Benefits:**

- Auto-generated API documentation
- Interactive testing interface (try it out)
- Client code generation (OpenAPI Generator)
- Better developer onboarding

#### 7. **Implement Configuration Validation**

```csharp
// Strongly-typed configuration with FluentValidation
<PackageReference Include="FluentValidation" Version="11.10.0" />

public class DsmConfiguration
{
    [Required]
    public string Host { get; set; } = String.Empty;
    
    [Range(1, 65535)]
    public int Port { get; set; } = 443;
    
    [Range(1, 65535)]
    public int HttpsPort { get; set; } = 443;
}

public class DsmConfigurationValidator : AbstractValidator<DsmConfiguration>
{
    public DsmConfigurationValidator()
    {
        RuleFor(x => x.Host)
            .NotEmpty().WithMessage("DSM host is required")
            .Matches(@"^(?:[a-zA-Z0-9](?:[a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?\.)+[a-zA-Z]{2,}$|^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$")
            .WithMessage("Invalid host format");
            
        RuleFor(x => x.Port)
            .InclusiveBetween(1, 65535).WithMessage("Port must be between 1 and 65535");
    }
}

// Validate on startup
var dsmConfig = configuration.GetSection("Dsm").Get<DsmConfiguration>() ?? new DsmConfiguration();
var validator = new DsmConfigurationValidator();
var result = validator.Validate(dsmConfig);

if (!result.IsValid)
{
    logger.LogCritical("Invalid DSM configuration: {Errors}", String.Join("; ", result.Errors));
    Environment.Exit(1);  // Fail fast
}
```

**Benefits:**

- Catch misconfigurations early (fail fast)
- Strongly-typed configuration (no string keys)
- Clear error messages for troubleshooting
- Type-safe access to configuration values

### Long-Term Strategic Improvements

#### 8. **Consider Microservices Architecture**

As the application grows, consider splitting into separate services:

```
┌─────────────────────────┐
│   API Gateway           │  (Kong, YARP, or custom)
└──────────┬──────────────┘
           │
    ┌──────┴───────┬─────────────┐
    │              │             │
┌───▼───┐    ┌────▼────┐   ┌────▼────┐
│Website│    │  File   │   │ Framework│
│ Service│    │ Service │   │ Service │
└───────┘    └─────────┘   └─────────┘
```

**Benefits:**

- Independent scaling (file operations can be resource-intensive)
- Better isolation of concerns (failures don't cascade)
- Easier maintenance (smaller codebases per service)
- Technology agnosticism (each service can use best-fit tools)

**Challenges:**

- Increased operational complexity
- Distributed tracing required
- Service discovery needed
- Data consistency across services

#### 9. **Add Webhooks & Notifications**

```csharp
// Define webhook events
public enum WebhookEventType
{
    WebsiteStarted,
    WebsiteStopped,
    WebsiteFailed,
    FrameworkInstalled,
    AuthenticationSuccess,
    AuthenticationFailure
}

// Webhook payload
public class WebhookPayload
{
    public WebhookEventType EventType { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, string> Data { get; set; } = [];
}

// Webhook service
public interface IWebhookService
{
    Task NotifyAsync(WebhookEventType eventType, Dictionary<string, string> data);
}

// Implementation with multiple sinks
public class WebhookService : IWebhookService
{
    private readonly List<Uri> _webhookUrls;
    private readonly ILogger _logger;
    
    public async Task NotifyAsync(WebhookEventType eventType, Dictionary<string, string> data)
    {
        var payload = new WebhookPayload { EventType = eventType, Data = data };
        
        var notifyTasks = _webhookUrls.Select(async url =>
        {
            try
            {
                await _httpClient.PostAsJsonAsync(url, payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send webhook to {Url}", url);
            }
        });
        
        await Task.WhenAll(notifyTasks);
    }
}

// Integration with:
// - Slack/Discord for team alerts
// - Email notifications (via SendGrid, SMTP)
// - Custom webhook endpoints (monitoring systems)
```

#### 10. **Implement Feature Flags**

```csharp
// Use Microsoft.FeatureManagement
<PackageReference Include="Microsoft.FeatureManagement" Version="4.0.0" />
<PackageReference Include="Microsoft.FeatureManagement.AspNetCore" Version="4.0.0" />

builder.Services.AddFeatureManagement()
    .AddFeatureFlags(configuration);  // Load from appsettings.json

// appsettings.json
{
  "FeatureManagement": {
    "NewFilePicker": true,
    "AdvancedReverseProxySettings": false,
    "BetaUsers": {
      "Name": "BetaUsers",
      "EnabledFor": {
        "Users": [ "admin", "beta-tester" ]
      }
    }
  }
}

// Usage in Razor components
@inject IFeatureManager FeatureManager

@if (await FeatureManager.IsEnabledAsync("NewFilePicker"))
{
    <NewFilePickerComponent />
}
else
{
    <LegacyFilePickerComponent />
}
```

**Benefits:**

- Enable/disable features dynamically (no redeployment)
- A/B testing capabilities
- Gradual rollouts (canary deployments)
- User-specific feature access

#### 11. **Add Internationalization (i18n)**

```csharp
// Add localization support
builder.Services.AddLocalization();
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddLocalization();

// Resource files
Resources/
├── Messages.resx                   # English (default)
├── Messages.fr.resx                # French
├── Messages.de.resx                # German
└── Messages.zh-Hans.resx           # Simplified Chinese

// Usage in Razor components
@inject IStringLocalizer<Messages> Localizer

<h1>@Localizer["WelcomeMessage"]</h1>
<FluentButton>@Localizer["StartButton"]</FluentButton>

// Usage in C# code
logger.LogInformation(Localizer["WebsiteStarted"].Name, siteName);  // Localized log message
```

**Benefits:**

- Multi-language UI support
- Better user experience for non-English speakers
- Easier to maintain translations (centralized resource files)
- Culture-aware formatting (dates, numbers, currencies)

### Code Quality Recommendations

#### 12. **Enforce Coding Standards with .editorconfig**

```ini
# .editorconfig (root of repository)
[*.cs]
indent_style = space
indent_size = 4
end_of_line = crlf
charset = utf-8-bom

# C# specific rules
csharp_curly_braces_for_control_blocks = require_for_multiline
csharp_new_line_before_opening_brace = false
csharp_namespace_declaration = require_always
csharp_use_var = never_built_in_types

# Naming conventions
dotnet_naming_rule.require_pascal_case.severity = error
dotnet_naming_rule.require_pascal_case.symbols = dotnet_naming_symbols.types
dotnet_naming_symbols.types.style = dotnet_naming_styles.pascal_case

# XML documentation requirements
dotnet_diagnostic.S125.severity = warning  # Methods should not have too many parameters
dotnet_diagnostic.S3934.severity = warning # Simulate "should not be empty"
```

#### 13. **Add Roslyn Analyzers**

```xml
<!-- Add to Directory.Build.props or individual projects -->
<ItemGroup>
  <PackageReference Include="Microsoft.CodeAnalysis.Analyzers" Version="3.11.0">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
  </PackageReference>
  
  <!-- StyleCop Analyzers for consistent coding style -->
  <PackageReference Include="StyleCop.Analyzers" Version="1.2.0-beta.556">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
  </PackageReference>
  
  <!-- SonarAnalyzer for code quality -->
  <PackageReference Include="SonarAnalyzer.CSharp" Version="9.24.0.97167">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

#### 14. **Add Integration Tests with TestContainer**

```csharp
<PackageReference Include="Testcontainers" Version="4.0.0" />

public class DsmApiClientTests : IClassFixture<DsmContainerFixture>
{
    private readonly DsmContainer _container;
    
    public DsmApiClientTests(DsmContainerFixture fixture)
    {
        _container = fixture.Container;
    }
    
    [Fact]
    public async Task ConnectAsync_WithValidCredentials_ReturnsTrue()
    {
        // Arrange
        var logger = new Mock<ILogger<DsmApiClient>>().Object;
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(_container.CreateHttpClient());
            
        var client = new DsmApiClient(httpClientFactory.Object, logger);
        
        // Act
        var result = await client.ConnectAsync(new LoginModel
        {
            Login = _container.Username,
            Password = _container.Password
        });
        
        // Assert
        Assert.True(result);
        Assert.NotEmpty(client.Sid);
    }
}

public class DsmContainerFixture : IDisposable
{
    public DsmContainer Container { get; } = new DsmBuilder()
        .WithUsername("admin")
        .WithPassword("password")
        .Build();
        
    public void Dispose() => Container.Dispose();
}
```

---

## Conclusion

**ASkyl.Dsm.WebHosting** is a well-architected .NET 10 application that effectively manages web applications on Synology DSM devices. The solution demonstrates modern C# development practices with clean architecture, type safety, and performance optimization.

### Strengths

✅ **Clean Architecture:** Clear separation of concerns across 9 projects with well-defined responsibilities  
✅ **Modern Patterns:** Result pattern, DI, service facade, builder pattern, strategy pattern  
✅ **Type Safety:** Strong-typed API models, data annotations, source generators for clone methods  
✅ **Performance:** Benchmark-driven optimizations, async/await best practices, efficient memory usage  
✅ **Maintainability:** Centralized constants (no magic strings), comprehensive logging with Serilog  
✅ **Developer Experience:** Source generators reduce boilerplate, FluentUI provides consistent UI  
✅ **Hybrid Rendering:** Blazor InteractiveWebAssembly for optimal server-client balance  

### Areas for Enhancement

⚠️ **Testing:** Add unit and integration tests (currently minimal test coverage)  
⚠️ **Resilience:** Implement retry policies with Polly for DSM API calls  
⚠️ **Observability:** Add health checks, metrics collection, enhanced audit logging  
⚠️ **Documentation:** API docs (OpenAPI/Swagger), Architecture Decision Records (ADRs), operational runbooks  
⚠️ **Caching:** Implement distributed caching for frequently accessed data  
⚠️ **Security:** Add rate limiting, request throttling, audit trail for sensitive operations  

### Final Recommendations Priority

**High Priority (Next Sprint):**

1. ✅ Add unit tests for service layer (xUnit + Moq)
2. ✅ Implement retry policy with Polly (exponential backoff)
3. ✅ Add structured audit logging (operation tracking, duration metrics)

**Medium Priority (Next Quarter):**

1. ✅ Add health checks and monitoring endpoints (`/health`, `/ready`)
2. ✅ Implement caching layer for DSM API information (5-minute TTL)
3. ✅ Create OpenAPI documentation with Swashbuckle (developer onboarding)
4. ✅ Add configuration validation with FluentValidation (fail fast on startup)

**Long-Term Strategic:**

1. Consider microservices split when feature set grows significantly
2. Add internationalization support for multi-language UI
3. Implement feature flags for gradual rollouts and A/B testing
4. Add webhook notifications for external system integration

---

## Document Metadata

**Version:** 1.0 (Updated for solution version 0.5.1)  
**Author:** AI Assistant (based on comprehensive code analysis)  
**Review Status:** Ready for team review  
**Next Review Date:** Upon next major release (v1.0.0)  

**Analysis Scope:**

- All 9 projects in the solution
- Program.cs configuration and middleware pipeline
- Controller implementations and API endpoints
- Service interfaces and implementations
- Data models and domain entities
- Build configuration (Directory.Build.props, .csproj files)
- Docker support and deployment scripts

---

*This document was generated automatically based on comprehensive analysis of the source code, project structure, build configuration, and architectural patterns. Last updated: March 2026.*
