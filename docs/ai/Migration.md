# Migration Document – From Uiz‑Old to Ui / Ui.Client

**Location**: `docs/ia/Migration.md`
**Version**: 2.10 (Updated March 7, 2026)
**Purpose**: Capture the strategic goals, architectural changes, and concrete steps required to evolve the legacy **Uiz‑Old** project into the modern **Ui** (server‑side) and **Ui.Client** (WebAssembly) codebases.

---

## 0. Current State Verification

Run before any migration step to confirm actual project structure.

| Project | Expected | Actual | Status |
|---------|----------|--------|--------|
| `Askyl.Dsm.WebHosting.Ui` | `.NET 10`, server-side host | ✅ Verified | ✅ |
| `Askyl.Dsm.WebHosting.Ui.Client` | `.NET 10`, WebAssembly | ✅ Fully migrated | ✅ |
| `Askyl.Dsm.WebHosting.Data` | Neutral models | ✅ Verified | ✅ |
| `Askyl.Dsm.WebHosting.Constants` | API/Application constants | ✅ Extensive coverage (25 files) | ✅ |
| Services in `Ui/Services/` | Server-side implementations | ✅ Complete | ✅ |
| Controllers in `Ui/Controllers/` | MVC REST endpoints (6 controllers) | ✅ All implemented | ✅ |
| **License Service** | **Client-side parallel loading** | **✅ Completed** | **✅** |
| `Askyl.Dsm.WebHosting.Uiz-Old` | Legacy monolithic project | ✅ Removed (March 22, 2026) | ✅ Complete |

### Additional Projects

| Project | Purpose | Status |
|---------|---------|--------|
| `Askyl.Dsm.WebHosting.Tools` | Utility functions, extensions, and helper classes | ✅ Present |
| `Askyl.Dsm.WebHosting.Logging` | Serilog configuration and sinks for structured logging | ✅ Present |
| `Askyl.Dsm.WebHosting.DotnetInstaller` | Standalone .NET runtime installer tool | ✅ Present |
| `Askyl.Dsm.WebHosting.SourceGenerators` | Custom source generators for compile-time code generation | ✅ Present |
| `Askyl.Dsm.WebHosting.Benchmarks` | Performance benchmarking suite | ✅ Present |

**Build Time**: 2.85 seconds (excellent performance)

**Build Status**:

```bash
dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx
  La génération a réussi.
     0 Avertissement(s)
     0 Erreur(s)
Temps écoulé 00:00:02.85
```

**HttpClient Extension Methods**: All Ui.Client service proxies now use enhanced extension methods with typed default value support:

- `GetJsonAsyncWithDefault<T>()` - GET requests with null handling
- `PostJsonAsyncWithDefault<TRequest, T>()` - POST requests with null handling
- `DeleteJsonAsyncWithDefault<T>()` - DELETE requests with null handling

All methods use .NET 10 extension method syntax and factory functions for proper error handling.

**Uri Extensions**: New `.NET 14` extension methods using `extension()` syntax:

- `ToLowerInvariant()` on bool values
- `WithQuery()` for building URIs with query parameters from tuples

**Action**: This section should be checked before each migration phase.

### Status Update (March 7, 2026)

The Home.razor component in Ui.Client is **fully migrated** with complete website hosting operations:

- ✅ `LoadInstancesAsync()` fully implemented
- ✅ `AddWebSite()`, `EditWebSite()`, `DeleteWebSite()` integrated with services
- ✅ `StartSelectedInstance()`, `StopSelectedInstance()` fully implemented
- ✅ `DownloadLogs()` uses constants-based routes

**Note**: The ADD/EDIT configuration UI dialog (`WebSiteConfigurationDialog`) is **fully implemented**. Only minor visual polish remains.

### Completed: Derived Result Types for Consistency (March 2026)

**Status**: ✅ **COMPLETED** - All derived result types successfully implemented and integrated.

**Implemented Changes**:

- ✅ Created `ApiResultBool` - Specific type for boolean API operations (preserves Success, Message, Data)
- ✅ Created `InstalledVersionsResult` - For `List<FrameworkInfo>` queries
- ✅ Created `ChannelsResult` - For `List<AspNetChannel>` queries
- ✅ Created `ReleasesResult` - For `List<AspNetRelease>` queries
- ✅ Created `SharedFoldersResult` - For `List<FsEntry>` queries (replaced FileSystemItem)
- ✅ Created `DirectoryContentsResult` - For `List<FsEntry>` queries (replaced IQueryable<FileStationFile>)
- ✅ Created `DirectoryFilesResult` - For `List<FsEntry>` queries (replaced FileStationFile)

**Additional Enhancements**:

- ✅ Refactored `ApiResult` and `ApiResultData<T>` from records to classes with property encapsulation
- ✅ Added HttpClient extension methods with typed default value support:
  - `GetJsonAsyncWithDefault<T>()` - GET requests with null handling using factory functions
  - `PostJsonAsyncWithDefault<TRequest, T>()` - POST requests with null handling using factory functions
  - `DeleteJsonAsyncWithDefault<T>()` - DELETE requests with null handling using factory functions

**Benefits Achieved**:

- ✅ **Consistency**: All `ApiResultData<T>` types now have explicit, documented names
- ✅ **Type Safety**: Compiler-enforced type checking with clear semantics
- ✅ **Maintainability**: Pattern established for future API result types
- ✅ **IntelliSense**: Better autocomplete and documentation in IDE
- ✅ **Error Handling**: Message preserved in all operations (no silent failures)

**Impact Summary**:

- 7 new derived types created in `Askyl.Dsm.WebHosting.Data/Results/`
- 8 service methods migrated in `DotnetVersionService` and `FileSystemService`
- 24 files modified across Ui, Ui.Client, and Data projects
- Net change: +260 lines, -175 lines (85 net increase for improved type safety)

**Note**: `AuthenticationResult` and `InstallationResult` already exist as specialized types and remain unchanged.

### Completed: File System Abstraction Refactoring (March 7, 2026)

**Status**: ✅ **COMPLETED** - Modernized file system types with better type safety and server-side filtering.

**Implemented Changes**:

- ✅ Created `FsEntry` record replacing `FileSystemItem` with explicit `IsDirectory` boolean property
- ✅ Created `AclPermissions` record replacing `SetHttpGroupPermissionsRequest` for ACL clarity
- ✅ Updated all result types to use `FsEntry` instead of `FileSystemItem`/`FileStationFile`:
  - `SharedFoldersResult` → `List<FsEntry>`
  - `DirectoryContentsResult` → `List<FsEntry>` (was `IQueryable<FileStationFile>`)
  - `DirectoryFilesResult` → `List<FsEntry>`
- ✅ Enhanced `GetDirectoryContentsAsync(string path, bool directoryOnly = false)` with server-side filtering
- ✅ Created `UriExtensions.cs` with .NET 14 extension methods using `extension()` syntax

**Additional Enhancement: Result Type Refactoring (March 7, 2026)**

- ✅ Renamed `ApiResultData<T>` → `ApiResultItems<TItem>` for clearer intent
- ✅ Changed `.Data` property to `.Items` across all result types
- ✅ Enforced `List<TItem>` pattern at compile-time (no more generic `T`)
- ✅ Updated 7 result classes and all usages in UI components

**Benefits Achieved**:

- ✅ **Type Safety**: Explicit `IsDirectory` property instead of inferring from path suffix
- ✅ **Performance**: Server-side directory filtering eliminates client-side processing
- ✅ **Clarity**: `AclPermissions` is more semantically correct than `SetHttpGroupPermissionsRequest`
- ✅ **Modern C#**: Uses .NET 14 `extension()` syntax for cleaner extension methods
- ✅ **Consistency**: Class and property names align (`ApiResultItems` → `.Items`)
- ✅ **Discoverability**: IntelliSense shows `.Items` which is more intuitive than `.Data`

**Impact Summary**:

- 3 new files created (`FsEntry.cs`, `AclPermissions.cs`, `UriExtensions.cs`)
- 2 legacy files deleted (`FileSystemItem.cs`, `SetHttpGroupPermissionsRequest.cs`)
- 1 base class renamed (`ApiResultData.cs` → `ApiResultItems.cs`)
- 6 result types updated to use `FsEntry` and `.Items` property
- Net change: +3 files, -2 files (net +1 file)

**Note**: All existing functionality preserved; this is a pure refactoring for improved type safety and performance.

### Build Verification (March 7, 2026)

```bash
dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx
  La génération a réussi.
     0 Avertissement(s)
     0 Erreur(s)
Temps écoulé 00:00:02.31
```

**Current State**: All ApiResult pattern phases completed, HttpClient extension methods enhanced with typed default value support. Migration at 98% completion - ready for integration tests and UI polish phase.

### Recent Changes (February 28, 2026)

**ApiResult Pattern Architecture**: Complete refactoring of result types to unified pattern:

- ✅ Created `ApiResult` (non-generic success/failure)
- ✅ Created `ApiResultData<T>` (generic data-carrying results)
- ✅ Updated `AuthenticationResult` and `InstallationResult` to use consistent patterns
- ❌ Removed legacy `OperationResult`, `InstallFrameworkModel.cs`, `OperationResult.cs` from Results folder
- ✅ All 6 controllers now return HTTP 200 with result objects (no error status codes)
- ✅ Service layer returns `ApiResult<T>` for all operations
- ✅ Ui.Client proxies consistently use `ApiResult<T>` pattern

**HttpClient Extension Methods Enhancement**: Added typed default value support:

- ✅ Created `GetJsonAsyncWithDefault<T>()`, `PostJsonAsyncWithDefault<TRequest, T>()`, `DeleteJsonAsyncWithDefault<T>()` extension methods
- ✅ Methods accept either direct default values or factory functions for null handling
- ✅ Uses .NET 10 extension method syntax with `extension(HttpClient client)` block
- ✅ All Ui.Client service proxies migrated to use new extension methods

**Net Impact**: -206 lines (515 insertions, 721 deletions across 33 files)

---

## 0b. Migration Status (FINAL)

### Completed Features (98%)

| Feature | Status | Notes |
|---------|--------|-------|
| Project structure & DI setup | ✅ Complete | Both projects target .NET 10 |
| Authentication flow | ✅ Complete | Session-based auth with DsmApiClient |
| License feature | ✅ Complete | **Client-side parallel loading from wwwroot/licenses/** |
| Runtime management | ✅ Complete | Server-side implementation + WASM proxy |
| File system operations | ✅ Complete | Server-side façade |
| AspNetReleasesDialog | ✅ Complete | WASM migration + REST integration + auth protection |
| LicensesDialog | ✅ Complete | Optimized static file loading pattern |
| Log Download | ✅ Complete | Constants-based routes, session-based auth, no ITemporaryTokenService needed |
| Website Hosting CRUD API | ✅ Complete | IWebSiteHostingService façade + controller + REST endpoints |
| AutoDataGrid | ✅ Complete | Grid with Name, Path, Port, State columns + row selection |
| WebSiteConfigurationDialog | ✅ Complete | Full Add/Edit dialog with FileSelection integration |
| Home.razor Operations | ✅ Complete | All methods calling services correctly |

### ⚠️ Remaining Work (2%)

| Feature | Status | Notes |
|---------|--------|-------|
| **Integration Tests** | 🔄 In Progress | End-to-end testing of all features |
| **UI Polish** | 📋 Planned | Minor visual improvements for dialogs |

### Estimated Time to Completion

| Phase | Days | Notes |
|-------|------|-------|
| Integration tests & validation | 2-3 | End-to-end testing of all features |
| UI polish for dialogs | <1 | Minor visual improvements only |
| **Total** | **2-3 days remaining** | Assuming 1 developer (FINAL - no code stubs needed) |

> **Note on Log Download**: ✅ **COMPLETED**. With session-based authentication, log download uses direct navigation to `/api/v1/logdownload/logs`. No `ITemporaryTokenService` is required.

> **Note on ITTemporaryTokenService**: This service is now **deprecated** in the new architecture. It was only used for temporary token generation in Uiz-Old and is no longer needed since log download uses session-based authentication.

---

## 0c. Original Migration Time (Historical Reference)

| Phase | Days | Notes |
|-------|------|-------|
| Project structure & DI setup | 1 | ✅ Completed |
| Service façades & controllers | 3-5 | ✅ Completed |
| Razor component migration | 2-3 | ✅ Completed |
| HttpClient & API integration | 2 | ✅ Completed |
| Configuration porting | 1 | ✅ Completed |
| Testing & validation | 2-3 | 🔄 In Progress |
| **Total** | **9-14 days** | ✅ Completed (revised to 2-3 days remaining) |

---

## 0d. Current State Summary (Revised - February 27, 2026)

**Migration Progress**: ~98% complete

### Key Achievements

- ✅ Full split architecture with Ui (server) and Ui.Client (WASM)
- ✅ Service façades for authentication, runtime management, file system, log download, website hosting
- ✅ REST API controllers for server-side functionality (**6 controllers**): Authentication, FileManagement, FrameworkManagement, RuntimeManagement, WebsiteHosting, LogDownload
- ✅ **Constants-based routing**: All controllers use dedicated defaults classes (AuthenticationDefaults, FileManagementDefaults, WebsiteHostingDefaults, etc.) - 15 API constant files total
- ✅ **Optimized license loading**: Parallel HTTP requests from `wwwroot/licenses/` with browser caching
- ✅ Session-based authentication flow with custom `[AuthorizeSession]` attribute
- ✅ AspNetReleasesDialog fully migrated with REST integration and security
- ✅ LicensesDialog fully migrated with optimized static file loading
- ✅ **Log Download**: Constants-based routes, session-based auth (ITemporaryTokenService is obsolete)
- ✅ **Website Hosting CRUD API**: Full REST endpoints implemented and integrated in Home.razor
- ✅ **AutoDataGrid**: Grid displays all websites with columns: Name, Path, Internal Port, State
- ✅ **WebSiteConfigurationDialog**: Full Add/Edit dialog with FileSelection integration
- ✅ **HttpClient Configuration**: Named client `"UiClient"` registered in Ui.Client Program.cs
- ✅ **Constants Coverage**: 25 constant files across API, Application, Runtime, UI, Network namespaces

### Deprecated Services

- ⚠️ **ITemporaryTokenService** - No longer required for standard log downloads. Kept in Uiz-Old only if special token-based features are needed.

### ⚠️ Remaining Work (UI Polish Only)

- ⏳ Integration tests and validation (2-3 days)
- ⏳ Minor UI polish for dialogs (<1 day)

> **Note on Home.razor**: The component is **fully migrated** with all methods calling services correctly. The ADD/EDIT configuration dialog (`WebSiteConfigurationDialog`) is also fully implemented.

> **Note on Log Download Completion**: The log download feature is now fully migrated with session-based authentication. The client navigates directly to `/api/v1/logdownload/logs` and the session cookie is automatically sent for validation. No `ITemporaryTokenService` was required.

---

## 1. Key Architectural Changes

| Area | What Changed |
|------|--------------|
| **Project Structure** | The monolithic `Uiz‑Old` solution is split into three distinct projects: <br>• **Ui** – Server‑side host (`Askyl.Dsm.WebHosting.Ui`). <br>• **Ui.Client** – Client‑side WebAssembly app (`Askyl.Dsm.WebHosting.Ui.Client`). <br>• **Data** – Neutral `Askyl.Dsm.WebHosting.Data` project holding all shared domain models and constants. |
| **Razor Files Relocation** | All `.razor` components from `Uiz-Old/Components/**` are now under `Ui.Client/Components/**`. Namespaces updated to `Askyl.Dsm.WebHosting.Ui.Client`. |
| **Service Exposure** | Services formerly accessed directly (`DsmApiClient`) are now wrapped by façade services (`IAuthenticationService`, `IRuntimeManagementService`, etc.) and exposed via dedicated MVC controllers in **Ui**. |
| **WebAPI Controllers** | New controllers (`AuthenticationController`, `RuntimeManagementController`, …) expose the façade methods as REST endpoints (`api/v{version}/[controller]`). |
| **Dependency Injection** | Services are registered with precise lifetimes (singleton/scoped) and injected into UI components via constructor injection. |

---

## 1b. Namespace Standardization

**Decision**: Use `Askyl.Dsm.WebHosting.Ui.Client` (with dot before "Client").

**Why**:

- Consistent with C# naming conventions
- Matches actual implementation in all source files
- Reduces confusion between directory path and namespace

**Apply to**:

- All `@namespace` declarations
- `_Imports.razor` imports
- Program.cs registrations
- Migration documentation

---

## 2. Target Architecture (Ui / Ui.Client)

| Component | Location | Role |
|-----------|----------|------|
| **Ui** | `src/Askyl.Dsm.WebHosting.Ui/` | Server‑side host (Blazor Server). Contains core services, controllers, and shared models. Exposes APIs used by the client and internal runtime‑management services. |
| **Ui.Client** | `src/Askyl.Dsm.WebHosting.Ui.Client/` | Client‑side WebAssembly application. Provides *all* UI rendering (no server‑side UI). Consumes the same service contracts exposed by **Ui**. |

> **Key point:** The *UX* will now be rendered exclusively in **Ui.Client** (WASM). All interactive components, layouts, and pages must reside in this project.

---

## 2b. What Changed in the Razor Layer

- **All `.razor` files moved**: From `src/Askyl.Dsm.WebHosting.Uiz-Old/Components/**` to `src/Askyl.Dsm.WebHosting.Ui.Client/Components/**`.
- **Namespace updated** to `Askyl.Dsm.WebHosting.Ui.Client`.
- **No direct `@inject DsmApiClient`** anymore; UI now injects façade services (`IAuthenticationService`, `IRuntimeManagementService`) or calls controller endpoints via HTTP.
- **Code‑behind migration**: `.razor.cs` files become partial classes inheriting from `ComponentBase`.

---

## 3. Migration Goals

1. **Upgrade to .NET 10** – Align both projects with the latest LTS SDK. ✅ Complete
2. **Split the monolith** – Move shared services, runtime‑management, and configuration into **Ui**; keep pure presentation logic in **Ui.Client**. ✅ Complete
3. **Make UI exclusive to WASM** – All user‑facing components, layouts, and pages reside in `Ui.Client`. Server-side Razor components are retained only for back‑end functionality. ✅ Complete
4. **Preserve existing APIs** – Service contracts (`IFrameworkManagementService`, `IFileSystemService`, etc.) remain unchanged to avoid breaking changes for callers. ✅ Complete
5. **Modernize tooling & practices** – Adopt .NET 10 project format, top‑level statements where possible, and remove legacy patterns (e.g., explicit `Startup.cs`). ✅ Complete

---

## 3b. Service Layer Changes

| Service | New Location | Responsibility |
|---------|--------------|----------------|
| `IAuthenticationService` | **Ui.Services** + **Ui.Client.Interfaces** | Handles authentication flows and session validation. Server implementation + client proxy. |
| `IRuntimeManagementService` | **Ui.Services** + **Ui.Client.Services** | Manages runtime installations, version checks, and refresh operations. Server implementation + client proxy. |
| `IFileSystemService`, `ILicenseService`, etc. | **Ui** (server) / **Ui.Client** (client) | Varies by service. Some are server-only (FileSystem), others are client-only (License). See Section 7 for details. |

> **Note:** The façade services provide a clean, testable surface that isolates DSM‑API specifics from the UI layer.

---

## 4. WebAPI Controllers – REST Endpoints

| Controller | Primary Responsibility | Route Prefix | Status |
|-----------|------------------------|--------------|--------|
| `AuthenticationController` | Handles login, logout, and session status. | `api/v1/authentication` | ✅ Complete |
| `RuntimeManagementController` | Manages runtime installation, uninstallation, and version queries. | `api/v1/runtime` | ✅ Complete |
| `FileManagementController` | Exposes file‑system operations (shared‑folder enumeration, permission changes). | `api/v1/filemanagement` | ✅ Complete |
| `FrameworkManagementController` | Manages ASP.NET framework installation/uninstallation. | `api/v1/framework` | ✅ Complete |
| `LogDownloadController` | Serves log archives via session-protected endpoints. | `api/v1/logdownload` | ✅ Complete |
| **WebsiteHostingController** | **Manages website lifecycle (CRUD + start/stop operations).** | **api/v1/websites** | **✅ Complete** |

All controllers use version‑aware routing (`api/v{version}/[controller]`) to support future API versioning. All routes are defined in constants classes under `Askyl.Dsm.WebHosting.Constants.API`.

---

## 4b. Client-Server Communication

### HttpClient Registration in Ui.Client

In `Program.cs` for **Ui.Client**:

```csharp
builder.Services.AddHttpClient(ApplicationConstants.HttpClientName, client =>
{
    // In WASM, baseAddress is the server URL where Ui app is served
    // API controllers are hosted at the domain root (without /adwh path base)
    // Reverse proxy handles /adwh/api/... -> /api/... routing in production
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
});

// HttpClientName = "UiClient" (defined in ApplicationConstants.cs)

// Register façade services (not DsmApiClient directly!)
builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();
```

**Note**: The documentation previously referenced `"ADWH.Api"` as the client name. The actual implementation uses `"UiClient"` as defined in `ApplicationConstants.HttpClientName`.

In components:

```razor
@inject IHttpClientFactory HttpClientFactory

// ...
var client = HttpClientFactory.CreateClient(ApplicationConstants.HttpClientName);
var response = await client.GetFromJsonAsync<...>(WebsiteHostingDefaults.AllFullRoute);
```

---

## 4c. Shared Data Project (`Askyl.Dsm.WebHosting.Data`)

The repository also contains a **separate data‑focused project**:

```
src/Askyl.Dsm.WebHosting.Data/
```

*What it holds*:

- **Shared data contracts** (interfaces, records, enums, DTOs) that are used across both **Ui** and **Ui.Client**.
- **Common attributes**, validation attributes (`[DsmParameterName]`), and exception types.
- **Generic utilities** such as `IGenericCloneable` and base response definitions.

*Why it matters for the migration*:

- The data layer is **agnostic of hosting**; it can be referenced by both **Ui** and **Ui.Client** without introducing any runtime dependencies.
- By moving shared models into this dedicated project, we keep **Ui** and **Ui.Client** focused on their respective concerns (hosting vs presentation).
- All domain models reside in the `Askyl.Dsm.WebHosting.Data` project; they will be referenced by both Ui and Ui.Client after migration. No changes are required to their definitions—only an explicit addition of a project reference from each of **Ui** and **Ui.Client**.

> **Additional Shared Projects**:
>
> - **Askyl.Dsm.WebHosting.Constants**: Contains all API routes, application settings, runtime constants, UI sizing constants, and network configuration. 25 files total across API, Application, Runtime, UI, and Network namespaces.
> - **Askyl.Dsm.WebHosting.Tools**: Utility functions, extension methods, and helper classes used across the solution.
> - **Askyl.Dsm.WebHosting.Logging**: Serilog configuration and sinks for structured logging.

> **Implication:** When we later discuss *Models* in detail, these models reside in the shared `Data` project and are consumed by UI components in **Ui.Client** and by service return values in **Ui**. The model definitions remain unchanged; they will simply be referenced by the new projects after migration.

---

## 4d. Authentication Flow Architecture

### Why Direct DsmApiClient Injection is Problematic

| Issue | Impact |
|-------|--------|
| **Tight coupling** | UI components depend on low-level HTTP client details |
| **No separation of concerns** | Authentication logic mixed with UI rendering |
| **Hard to test** | Cannot unit test login flow without browser context |
| **Session state duplication** | Each component manages its own auth state |

### Three-Layer Pattern

```
┌─────────────────┐
│   Ui.Client     │  ← UI Layer (WASM)
│   Login.razor   │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│   Ui.Server     │  ← Façade Layer
│ AuthService     │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│   DsmApiClient  │  ← HTTP Client (Tools layer)
└─────────────────┘
```

### Registration Sequence

1. **Ui.Server**: Register `DsmApiClient` (singleton) and `AuthenticationService` (scoped)
2. **Ui.Client**: Register `IAuthenticationService` as a proxy that calls REST endpoints
3. **UI Components**: Inject `IAuthenticationService` - never `DsmApiClient`

### Authentication State Management

| Layer | State Location |
|-------|----------------|
| **DsmApiClient** | In-memory `_sid` cookie |
| **AuthenticationService** | Encapsulated via `IsConnected` property |
| **Ui.Client** | Call `/api/v1/authentication/status` to check |

### Migration Steps for Login.razor

| Step | Action | File(s) |
|------|--------|---------|
| 1 | Create `IAuthenticationService` façade | `Ui/Services/IAuthenticationService.cs` / `Ui.Client/Interfaces/IAuthenticationService.cs` |
| 2 | Implement `AuthenticationService` in `Ui` | `Ui/Services/AuthenticationService.cs` |
| 3 | Register in DI container | `Ui/Program.cs`, `Ui.Client/Program.cs` |
| 4 | Create REST endpoints | `Ui/Controllers/AuthenticationController.cs` |
| 5 | Migrate Login.razor to `Ui.Client` | `Ui.Client/Components/Pages/Login.razor` |
| 6 | Update DI injection | Replace `DsmApiClient`, add `IAuthenticationService` |

---

## 5. Migration Impact

- **Separation of Concerns**: UI logic now lives purely in `Ui.Client`; host‑specific services are encapsulated within `Ui`.
- **Testability**: Façade services can be unit‑tested independently of the UI.
- **Future‑Proofing**: Version‑aware routing and façade abstraction protect against upcoming DSM‑API changes.
- **Maintainability**: Clear separation between *host* responsibilities (Server) and *presentation* responsibilities (WASM).

---

## 5b. Concrete Changes Required

| Area | What to Do | Status |
|------|------------|--------|
| **Project files** | Update both `.csproj` files to target `.NET 10`. Add `<OutputType>Exe</OutputType>` for **Ui** and `<OutputType>Wasm</OutputType>` for **Ui.Client**. Ensure the `Data` project references the appropriate SDK and is referenced by both **Ui** and **Ui.Client**. | ✅ Complete |
| **Program.cs** | - **Ui**: Keep host configuration, Serilog setup, DI registrations, and service lifetimes. <br>- **Ui.Client**: Add Blazor WASM bootstrapping (`builder.Services.AddHttpClient()`, `app.RootComponent.Register<App>()`). | ✅ Complete |
| **Shared services** | Move any *pure* business logic that does not depend on server‑specific APIs into a new **Core** folder (or keep in **Ui** if they still need hosting context). | ✅ Complete |
| **Configuration** | Centralize `appsettings.json` usage; consider adding a shared `AppOptions` class for values like `ChannelVersion`. | ✅ Complete |
| **Routing & UI** | - Replace server‑side routing (`Routes.razor`) with **Ui.Client** route hierarchy (`Routes.razor`, `Pages/`, `Layout/`). <br>- Ensure all UI components are marked `[Parameter]` and use `EventCallback` for interactivity. | ✅ Complete |
| **Dependency Injection** | Verify lifetimes: <br>• Scoped services used only in **Ui** stay there.<br>• Services required by **Ui.Client** must be registered as `Singleton` or `Scoped` accordingly (`builder.Services.AddScoped<IFrameworkManagementService, FrameworkManagementService>();`). | ✅ Complete |
| **HttpClient** | Register named client (`"UiClient"`) with base URL pointing to `Ui` server in `Ui.Client`. | ✅ Complete |

---

## 6b. HttpClient Configuration – Final Verification

The `Program.cs` in `Ui.Client` must use:

```csharp
// ✅ CORRECT
builder.Services.AddHttpClient(ApplicationConstants.HttpClientName, client =>
{
    // In WASM, baseAddress is the server URL where Ui app is served
    // API controllers are hosted at the domain root (without /adwh path base)
    // Reverse proxy handles /adwh/api/... -> /api/... routing in production
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
});

// HttpClientName value: "UiClient" (defined in ApplicationConstants.cs)

// Register façade services (not DsmApiClient directly!)
builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();
```

**Why This Matters:**

- `Ui.Client` is a WebAssembly that runs in the browser
- All API calls must go to the **server** (`Ui`) where `DsmApiClient` has access to DSM APIs
- The named client (`"UiClient"`) points to the same server URL that serves the WASM app

> **Note:** In development mode, base address might be `http://localhost:8080/`; in production it's your configured domain. Both cases point to where `Ui` is served.

---

## 6c. Path Base Handling (Production URLs)

### Architecture

The application uses a split routing strategy:

| Component | Route Prefix | Middleware Order |
|-----------|--------------|------------------|
| **API Controllers** | `/api/v{version}/...` | Mapped BEFORE `UsePathBase()` |
| **Razor Components** | `/adwh/...` | Mapped AFTER `UsePathBase("/adwh")` |

### Why This Matters

- API controllers are registered at `/api/...` (no path base prefix)
- Razor components are served under `/adwh/...` (with path base)
- In production, the reverse proxy routes:
  - `https://nas.askyl.ovh:713/adwh/api/v1/...` → to `/api/v1/...` on the app
  - `https://nas.askyl.ovh:713/adwh/` → to the Blazor app

### Program.cs Order Matters

```csharp
// 1. Apply path base BEFORE Razor components, AFTER API controllers
app.UsePathBase(ApplicationConstants.ApplicationUrlSubPath);

// 2. Map API controllers FIRST (they're at /api/...)
app.MapControllers();

// 3. Add antiforgery middleware before Razor components
app.UseAntiforgery();

// 4. Map Razor components under /adwh/
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode();
```

### Constants

```csharp
// ApplicationConstants.cs
public const string ApplicationUrlSubPath = "/adwh";
public const string HttpClientName = "UiClient";
public const string LoginPagePath = "login";
```

---

## 7. Service Placement – Where Each Service Lives

| Service | Server Location | Client Location | Reason it Belongs There | Consumption Pattern |
|---------|-----------------|-----------------|------------------------|---------------------|
| **FrameworkManagementService** / `IDotnetVersionService` | **Ui.Services** | Ui.Client.Services (proxy) | Deals with installing, querying and uninstalling .NET runtimes; requires a host process that can access the file system and SDK information. | REST API via HttpClient "UiClient" |
| **IFileSystemService** / `FileSystemService` | **Ui.Services** | Ui.Client.Services (proxy) | Wraps DSM FileStation API calls; now supports server-side directory filtering via `directoryOnly` parameter. Returns `FsEntry` data objects with explicit `IsDirectory` property instead of inferring from path suffix. | REST API via HttpClient "UiClient" |
| **ILicenseService** / `LicenseService` | N/A | **Ui.Client.Services** | **Static license files served directly from `wwwroot/licenses/`; parallel HTTP loading for performance. No server round-trip required.** | Direct static file access (no REST API) |
| **ILogDownloadService** / `LogDownloadService` | **Ui.Services** | N/A (client navigates to endpoint) | Generates the in‑memory ZIP stream that contains package logs, debug logs, etc., and supplies it via session-protected endpoint. | Client navigates to `/api/v1/logdownload/logs` directly |
| **WebSiteHostingService** | **Ui.Services** + HostedService | Ui.Client.Services (proxy) | Orchestrates the lifecycle of hosted web‑site processes (`Process.Start`, `Process.CloseMainWindow`). It only makes sense in a server process that can spawn and manage OS processes. | REST API via HttpClient "UiClient" |
| **ITreeContentService** / `TreeContentService` | N/A | **Ui.Client.Services** | Provides directory tree expansion functionality for file selection dialogs. | Direct service call (no HTTP) |

> **Note on ITemporaryTokenService**: This service is **NOT required for standard log downloads** which use session-based authentication. It should only be used when you need expiring download links, one-time use tokens, or revocable access control.

### Client-Side Service Proxies (`Ui.Client.Services/`)

All client-side services follow the same pattern:

```csharp
public class WebSiteHostingService(IHttpClientFactory httpClientFactory) : IWebSiteHostingService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(ApplicationConstants.HttpClientName);

    public async Task<List<WebSiteInstance>> GetAllWebsitesAsync()
    {
        var websites = await _httpClient.GetJsonAsync<List<WebSiteInstance>>(WebsiteHostingDefaults.AllFullRoute);
        return websites ?? [];
    }
    
    // ... other methods calling REST endpoints
}
```

---

### 7d. License Service Optimization

**Decision**: License files are served directly from `wwwroot/licenses/` via parallel HTTP requests in **Ui.Client**, bypassing REST API round-trips.

**Why This Approach?**

| Factor | Analysis |
|--------|----------|
| **Content Type** | Static `.txt` files containing license agreements (Application.txt, FluentUI Blazor.txt, NET.txt, Serilog.txt) |
| **Change Frequency** | Immutable after deployment; only updated during application updates |
| **Security Requirements** | None – licenses are public documentation, no sensitive data |
| **Access Pattern** | High frequency (loaded every time LicensesDialog opens) |
| **File Size** | <100KB each (enforced by `LicenseConstants.MaxLicenseSizeBytes`) |

**Implementation Pattern:**

```csharp
// Ui.Client/Interfaces/ILicenseService.cs
namespace Askyl.Dsm.WebHosting.Ui.Client.Interfaces;

public interface ILicenseService
{
    Task<IReadOnlyList<LicenseInfo>> GetLicensesAsync();
}

// Ui.Client/Services/LicenseService.cs
public class LicenseService(IHttpClientFactory httpClientFactory) : ILicenseService
{
    private IReadOnlyList<LicenseInfo>? _licenses;

    public async Task<IReadOnlyList<LicenseInfo>> GetLicensesAsync()
    {
        if (_licenses is not null)
        {
            return _licenses; // Cached after first load
        }

        // Parallel loading: 4 files loaded simultaneously
        var tasks = LicenseDefaults.LicenseFileNames.Select(
            async fileName => await LoadLicenseAsync(fileName));
        var results = await Task.WhenAll(tasks);

        _licenses = results.Where(result => result is not null)
                          .Cast<LicenseInfo>()
                          .ToList()
                          .AsReadOnly();
        return _licenses;
    }

    private async Task<string> FetchLicenseContentAsync(string fileName)
    {
        // Direct static file access via relative URL
        var url = $"licenses/{fileName}";

        using var httpClient = httpClientFactory.CreateClient(ApplicationConstants.HttpClientName);
        httpClient.Timeout = TimeSpan.FromSeconds(10);

        return await httpClient.GetStringAsync(url);
    }
}
```

**Performance Benefits:**

| Metric | Server REST Approach | Direct wwwroot (Current) |
|--------|---------------------|--------------------------|
| HTTP Requests per dialog open | 1 (to `/api/v1/licenses/all`) | 4 (parallel, cached) |
| Latency (cached) | ~50-100ms | ~5-20ms |
| Server CPU | Required for JSON serialization | None |
| Server I/O | File read + memory stream | Browser caching handles it |

---

## 8. Project Dependencies Matrix

```
┌─────────────────────────────────────────────────────────┐
│                    Askyl.Dsm.WebHosting                 │
│                         .slnx                           │
└──────────────┬──────────────────────┬───────────────────┘
               │                      │
    ┌──────────▼──────────┐  ┌────────▼──────────┐
    │   Ui (Server)       │  │ Ui.Client (WASM)  │
    ├─────────────────────┤  ├───────────────────┤
    │ • Controllers       │  │ • Components      │
    │ • Services          │  │ • Services        │
    │ • BackgroundTasks   │  │ • Interfaces      │
    └──┬──────────────┬───┘  └──┬──────────┬─────┘
       │              │         │          │
       ▼              ▼         ▼          ▼
┌────────────┐ ┌────────────┐ ┌─────────┐ ┌─────────┐
│ Constants  │ │   Data     │ │ Logging │ | Tools   │
└────────────┘ └────────────┘ └─────────┘ └─────────┘

Optional/Dev:
┌────────────┐ ┌──────────────────┐ ┌──────────────────┐
│ Benchmarks │ │ SourceGenerators │ │ DotnetInstaller  │
└────────────┘ └──────────────────┘ └──────────────────┘

Legacy (Reference Only):
┌──────────────────┐
│ Uiz-Old          │
│ (Deprecated)     │
└──────────────────┘
```

---

## 9. Migration Checklist

### Phase 1: Foundation (Complete ✅)

- [x] Create `Askyl.Dsm.WebHosting.Ui` project (.NET 10, Blazor Server)
- [x] Create `Askyl.Dsm.WebHosting.Ui.Client` project (.NET 10, Blazor WASM)
- [x] Create `Askyl.Dsm.WebHosting.Data` shared models project
- [x] Migrate all constants to `Askyl.Dsm.WebHosting.Constants`
- [x] Set up DI registration in both `Program.cs` files
- [x] Configure HttpClient named client `"ADWH.Api"`

### Phase 2: Service Layer (Complete ✅)

- [x] Implement `AuthenticationService` + REST controller
- [x] Implement `DotnetVersionService` + REST controller
- [x] Implement `FrameworkManagementService` + REST controller
- [x] Implement `FileSystemService` + REST controller
- [x] Implement `LogDownloadService` + REST endpoint
- [x] Implement `WebSiteHostingService` (BackgroundService) + REST controller
- [x] Create client-side service proxies in `Ui.Client.Services/`

### Phase 3: UI Migration (Complete ✅)

- [x] Migrate all `.razor` components to `Ui.Client/Components/`
- [x] Update namespaces to `Askyl.Dsm.WebHosting.Ui.Client`
- [x] Replace `DsmApiClient` injections with façade services
- [x] Implement `Home.razor` with AutoDataGrid binding
- [x] Implement dialog components (Licenses, AspNetReleases, DotnetVersions)
- [x] Implement `WebSiteConfigurationDialog` with FileSelection

### Phase 4: Integration & Testing (In Progress 🔄)

- [ ] End-to-end testing of authentication flow
- [ ] Test website CRUD operations
- [ ] Test start/stop lifecycle management
- [ ] Verify log download functionality
- [ ] Validate license loading performance
- [ ] UI polish for dialog components

---

## 10. Known Issues & Technical Debt

### ⚠️ Critical (Fixed)

| Issue | Resolution | Status |
|-------|------------|--------|
| Wrong HttpClient name (`"John"` vs `"ADWH.Api"`) | Updated to `ApplicationConstants.HttpClientName` | ✅ Fixed |
| Incorrect namespace pattern (`UiClient` vs `Ui.Client`) | Corrected in all source files | ✅ Fixed |

### 🟢 Completed Refactoring (March 7, 2026)

| Issue | Resolution | Status |
|-------|------------|--------|
| FileSystemItem lacked IsDirectory property | Created FsEntry with explicit IsDirectory boolean | ✅ Fixed |
| SetHttpGroupPermissionsRequest naming unclear | Renamed to AclPermissions for ACL clarity | ✅ Fixed |
| Client-side directory filtering inefficient | Moved to server-side via directoryOnly parameter | ✅ Fixed |

### 📋 Minor (Documentation)

| Issue | Resolution | Priority |
|-------|------------|----------|
| Missing WebsiteHostingController documentation | Add to Section 4 table | High |
| Outdated migration status estimates | Updated to reflect 98% completion | Completed |
| Service consumption patterns unclear | Added Section 7 with detailed matrix | Completed |

---

## Appendix A: Constants Reference

### API Route Defaults (`Askyl.Dsm.WebHosting.Constants.API/`)

| File | Purpose | Example Usage |
|------|---------|---------------|
| `AuthenticationDefaults.cs` | Auth controller routes | `AuthenticationDefaults.LoginFullRoute` |
| `WebsiteHostingDefaults.cs` | Website CRUD + lifecycle | `WebsiteHostingDefaults.AllFullRoute`, `StartFullRoute`, etc. |
| `FileManagementDefaults.cs` | File system operations | `FileManagementDefaults.SharedFoldersFullRoute` |
| `RuntimeManagementDefaults.cs` | .NET runtime management | `RuntimeManagementDefaults.AvailableFullRoute` |
| `FrameworkManagementDefaults.cs` | ASP.NET framework management | `FrameworkManagementDefaults.InstalledFullRoute` |
| `LogDownloadDefaults.cs` | Log download endpoint | `LogDownloadDefaults.LogsFullRoute` |

### Application Constants (`Askyl.Dsm.WebHosting.Constants.Application/`)

```csharp
public static class ApplicationConstants
{
    public const string ApplicationUrlSubPath = "/adwh";
    public const string HttpClientName = "ADWH.Api";
    public const string LoginPagePath = "/Login";
    public const string DotnetExecutable = "dotnet";
    public const string AspNetCoreUrlsEnvironmentVariable = "ASPNETCORE_URLS";
    public const string AspNetCoreEnvironmentVariable = "ASPNETCORE_ENVIRONMENT";
}
```

---

## Appendix B: File Structure Summary

### Ui.Server (`Askyl.Dsm.WebHosting.Ui/`)

```
Ui/
├── Controllers/
│   ├── AuthenticationController.cs
│   ├── RuntimeManagementController.cs
│   ├── FileManagementController.cs
│   ├── FrameworkManagementController.cs
│   ├── LogDownloadController.cs
│   └── WebsiteHostingController.cs
├── Services/
│   ├── AuthenticationService.cs
│   ├── DotnetVersionService.cs
│   ├── FrameworkManagementService.cs
│   ├── FileSystemService.cs
│   ├── LogDownloadService.cs
│   └── WebSiteHostingService.cs (BackgroundService)
├── Components/
│   ├── App.razor
│   └── _Imports.razor
├── Program.cs
└── appsettings.json
```

### Ui.Client (`Askyl.Dsm.WebHosting.Ui.Client/`)

```
Ui.Client/
├── Components/
│   ├── Pages/
│   │   ├── Home.razor
│   │   ├── Login.razor
│   │   └── NotFound.razor
│   ├── Dialogs/
│   │   ├── LicensesDialog.razor
│   │   ├── AspNetReleasesDialog.razor
│   │   ├── DotnetVersionsDialog.razor
│   │   ├── WebSiteConfigurationDialog.razor
│   │   └── FileSelectionDialog.razor
│   ├── Layout/
│   └── Controls/
├── Services/
│   ├── WebSiteHostingService.cs (proxy)
│   ├── AuthenticationService.cs (proxy)
│   ├── DotnetVersionService.cs (proxy)
│   ├── FrameworkManagementService.cs (proxy)
│   ├── FileSystemService.cs (proxy)
│   └── LicenseService.cs (client-only, static files)
├── Interfaces/
│   ├── IWebSiteHostingService.cs
│   ├── ILicenseService.cs
│   └── ITreeContentService.cs
├── Program.cs
├── Routes.razor
└── wwwroot/licenses/
    ├── Application.txt
    ├── FluentUI Blazor.txt
    ├── NET.txt
    └── Serilog.txt
```

---

## Appendix C: Build Verification Commands

### Full Solution Build

```bash
dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx
```

**Expected Output:**

```
Identification des projets à restaurer...
  Tous les projets sont à jour pour la restauration.
  Askyl.Dsm.WebHosting.Constants -> ...
  Askyl.Dsm.WebHosting.Data -> ...
  Askyl.Dsm.WebHosting.Ui.Client -> ...
  Askyl.Dsm.WebHosting.Ui -> ...

La génération a réussi.
    0 Avertissement(s)
    0 Erreur(s)
```

### Clean Build (Fresh Compilation)

```bash
dotnet clean ./src/Askyl.Dsm.WebHosting.slnx && dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx
```

---

## 8. Controller Result Pattern Migration

### Current State Analysis (February 27, 2026)

All 6 API controllers in `Askyl.Dsm.WebHosting.Ui` have been analyzed for result pattern consistency:

| Controller | Total Methods | Uses Result Pattern | Deviations | Consistency Score |
|------------|---------------|---------------------|------------|-------------------|
| `AuthenticationController` | 3 | 2/3 (67%) | 1 minor | ⚠️ Good |
| `FileManagementController` | 3 | 1/3 (33%) | 1 major | ❌ Poor |
| `FrameworkManagementController` | 2 | 2/2 (100%) | 0 | ✅ Excellent |
| `RuntimeManagementController` | 5 | 1/5 (20%) | 4 major | ❌ Poor |
| `LogDownloadController` | 1 | N/A (file download) | 1 minor | ⚠️ Acceptable |
| `WebsiteHostingController` | 6 | 3/6 (50%) | 3 major | ⚠️ Moderate |

**Total**: 20 methods, 9 use result patterns (45%), 11 deviations (55%)

### Result Patterns Available

| Pattern | Location | Purpose |
|---------|----------|---------|
| `OperationResult` | `Data.Results` | Generic success/failure with nullable error message |
| `InstallationResult` | `Data.Results` | Installation-specific with detailed Message property |
| `AuthenticationResult` | `Data.Security` | Authentication-specific result (login/logout) |

### Deviations Summary

| Severity | Count | Affected Controllers | Impact |
|----------|-------|---------------------|--------|
| 🔴 Major | 8 | FileManagement (1), RuntimeManagement (4), WebsiteHosting (3) | Breaking API consistency, unclear error differentiation |
| 🟡 Minor | 2 | Authentication (1), LogDownload (1) | Inconsistent HTTP status codes for errors |

### Specific Deviations by Controller

#### 🔴 High Priority - Major Deviations

**RuntimeManagementController** (4 deviations):

- `GetInstalledVersionsAsync()` → Returns `List<FrameworkInfo>` instead of `OperationResult<List<FrameworkInfo>>`
- `IsChannelInstalled()` → Returns `bool` instead of `OperationResult<bool>` (optional)
- `IsVersionInstalled()` → Returns `bool` instead of `OperationResult<bool>` (optional)
- `GetChannelsAsync()` → Returns `List<AspNetChannel>` instead of `OperationResult<List<AspNetChannel>>`
- `GetReleasesWithStatusAsync()` → Returns `List<AspNetRelease>` instead of `OperationResult<List<AspNetRelease>>`

**WebsiteHostingController** (3 deviations):

- `GetAllWebsitesAsync()` → Returns `List<WebSiteInstance>` instead of `OperationResult<List<WebSiteInstance>>`
- `AddWebsite()` → Returns `WebSiteInstance` directly, errors use `StatusCode(500)` instead of result pattern
- `UpdateWebsite()` → Same inconsistency as AddWebsite

**FileManagementController** (1 deviation):

- `GetSharedFoldersAsync()` → Returns `List<FileSystemItem>` instead of wrapping in result pattern

#### 🟡 Medium Priority - Minor Deviations

**LogDownloadController**:

- Error cases return `Ok(OperationResult.CreateFailure(...))` instead of proper HTTP status codes (`NotFound()`, `StatusCode(503)`)

**AuthenticationController.GetStatusAsync()**:

- Returns `AuthenticationStatus` enum directly (acceptable for simple queries, but inconsistent with other auth methods)

### Migration Plan

#### Phase 1: WebsiteHostingController (High Priority - CRUD Operations)

**Estimated Time**: 2 hours

| Step | Action | Methods to Update |
|------|--------|-------------------|
| 1.1 | Convert GET to return `OperationResult<T>` | `GetAllWebsitesAsync()` |
| 1.2 | Convert POST Add/Update to use result pattern | `AddWebsite()`, `UpdateWebsite()` |
| 1.3 | Standardize error handling (all use `Ok(OperationResult.CreateFailure(...))`) | All methods |

**Expected Changes**:

- Return type: `Task<ActionResult<List<WebSiteInstance>>>` → `Task<ActionResult<OperationResult<List<WebSiteInstance>>>>`
- Success responses remain wrapped in `Ok(result)`
- Error handling becomes consistent across all CRUD operations

#### Phase 2: RuntimeManagementController (High Priority - Query Operations)

**Estimated Time**: 2 hours

| Step | Action | Methods to Update |
|------|--------|-------------------|
| 2.1 | Convert all GET methods to return `OperationResult<T>` | `GetInstalledVersionsAsync()`, `GetChannelsAsync()`, `GetReleasesWithStatusAsync()` |
| 2.2 | Consider wrapping boolean queries in result pattern (optional) | `IsChannelInstalled()`, `IsVersionInstalled()` |
| 2.3 | Remove all `StatusCode(500, ...)` calls and use result pattern instead | All methods |

**Expected Changes**:

- Return type: `Task<ActionResult<List<FrameworkInfo>>>` → `Task<ActionResult<OperationResult<List<FrameworkInfo>>>>`
- Boolean queries remain as-is or optionally wrapped in `OperationResult<bool>`
- Consistent error differentiation between "no data found" vs "service error"

#### Phase 3: FileManagementController (High Priority - File Operations)

**Estimated Time**: 1 hour

| Step | Action | Methods to Update |
|------|--------|-------------------|
| 3.1 | Convert GET methods to return `OperationResult<T>` | `GetSharedFoldersAsync()` |
| 3.2 | Ensure consistency with WebsiteHostingController pattern | All methods |

**Expected Changes**:

- Return type: `Task<ActionResult<List<FileSystemItem>>>` → `Task<ActionResult<OperationResult<List<FileSystemItem>>>>`
- Error responses use consistent result pattern format

#### Phase 4: LogDownloadController (Medium Priority - File Download)

**Estimated Time**: 30 minutes

| Step | Action | Methods to Update |
|------|--------|-------------------|
| 4.1 | Use proper HTTP status codes for error cases | `DownloadLogs()` |
| 4.2 | Success remains as `FileCallbackResult` (correct) | - |

**Expected Changes**:

- Directory not found → Return `NotFound("Logs directory not found")` instead of `Ok(OperationResult.CreateFailure(...))`
- General errors → Return `StatusCode(503, ...)` with result pattern in body

#### Phase 5: AuthenticationController (Low Priority - Minor Inconsistency)

**Estimated Time**: 30 minutes

| Step | Action | Methods to Update |
|------|--------|-------------------|
| 5.1 | Optional: Wrap status query in result pattern | `GetStatusAsync()` |

**Expected Changes**:

- Return type: `Task<ActionResult<AuthenticationStatus>>` → `Task<ActionResult<OperationResult<AuthenticationStatus>>>` (optional)

### Migration Checklist

| Phase | Controller/Task | Status | Methods Updated | Estimated Hours | Actual Hours | Completed Date |
|-------|-----------------|--------|-----------------|-----------------|--------------|----------------|
| **Phase 0** | **Create ApiResult Foundation** | ⏳ Pending | N/A (foundation) | 0.5 | - | - |
| Phase 1 | WebsiteHostingController | ⏳ Pending | 0/6 | 2.0 | - | - |
| Phase 2 | RuntimeManagementController | ⏳ Pending | 0/5 | 2.0 | - | - |
| Phase 3 | FileManagementController | ⏳ Pending | 0/3 | 1.0 | - | - |
| Phase 4 | LogDownloadController | ⏳ Pending | 0/1 | 0.5 | - | - |
| Phase 5 | AuthenticationController | ⏳ Pending | 0/1 (optional) | 0.5 | - | - |
| **FrameworkManagementController** | ✅ Complete | 2/2 | 0.0 | 0.0 | N/A (already consistent, will be updated in Phase 0) |

**Total Estimated Time**: 6.5 hours (1 day of focused work, including Phase 0 foundation creation)

### Benefits of Migration

1. **API Consistency**: All controllers follow the same pattern for success/error responses
2. **Better Error Differentiation**: Clients can distinguish between "no data found" and "service error"
3. **Type Safety**: Result patterns provide structured error information
4. **Testability**: Easier to test error scenarios with consistent return types
5. **Client Experience**: Predictable API behavior across all endpoints

### Rollback Plan

Since the product is not yet released, **API versioning is NOT required**. Breaking changes can be applied directly without maintaining backward compatibility.

**Recommended Approach**: Implement a unified `ApiResult` base pattern that all result types will follow, eliminating the need for rollback mechanisms.

---

## 10. Service Layer Result Pattern Architecture (NEW - February 28, 2026)

### Architectural Decision: Services Return ApiResult<T>, Controllers Are Thin Wrappers

**Decision**: All service layer methods will return `ApiResult<T>` types instead of throwing exceptions for business logic errors.

**Rationale**:

- ✅ **Client code simplicity**: No try/catch blocks needed for expected failures
- ✅ **HTTP 200 constraint**: Always returns HTTP 200, client checks `result.Success` property
- ✅ **Clear intent**: Success vs failure are first-class concepts in service contracts
- ✅ **Testability**: Easy to test both success and failure paths without mocking exceptions

### Architecture Overview (4-Layer Pattern)

```
┌─────────────────────────────────────────────────────────────┐
│  Layer 4: UI Components (.razor files in Ui.Client)         │
│  - Simple if-check pattern: if (!result.Success) return;   │
│  - No try/catch for expected business errors               │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│  Layer 3: Client Service Proxies (Ui.Client.Services/)      │
│  - Translate HTTP 200 responses to ApiResult<T>            │
│  - No exception handling for expected failures             │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│  Layer 2: API Controllers (Ui.Controllers/)                 │
│  - Thin wrappers: just pass through results                │
│  - Always return Ok(result) - HTTP 200                     │
│  - No business logic, no error handling                    │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│  Layer 1: Business Services (Ui.Services/)                  │
│  - Return ApiResult<T> for ALL operations                  │
│  - Handle errors internally, never throw business exceptions│
│  - Express business logic clearly with success/failure     │
└─────────────────────────────────────────────────────────────┘
```

### Layer 1: Business Services (Return ApiResult<T>)

**Interface Contract**:

```csharp
public interface IWebSiteHostingService
{
    Task<OperationResultData<List<WebSiteInstance>>> GetAllWebsitesAsync();
    Task<ApiResult> AddWebsiteAsync(WebSiteConfiguration config);
    Task<ApiResult> UpdateWebsiteAsync(WebSiteConfiguration config);
    Task<ApiResult> RemoveWebsiteAsync(Guid id);
    Task<ApiResult> StartWebsiteAsync(Guid id);
    Task<ApiResult> StopWebsiteAsync(Guid id);
}

public interface IDotnetVersionService
{
    Task<OperationResultData<List<FrameworkInfo>>> GetInstalledVersionsAsync();
    Task<OperationResult<bool>> IsChannelInstalledAsync(string channel, string frameworkType = DotNetFrameworkTypes.AspNetCore);
    Task<ApiResult> InstallFrameworkAsync(InstallFrameworkModel model);
    Task<ApiResult> UninstallFrameworkAsync(string version);
}

public interface IFileSystemService
{
    Task<OperationResultData<List<FileSystemItem>>> GetSharedFoldersAsync();
    Task<OperationResultData<IQueryable<FileStationFile>>> GetDirectoryContentsAsync(string path);
    Task<ApiResult> SetHttpGroupPermissionsAsync(SetHttpGroupPermissionsRequest request);
}
```

**Implementation Pattern**:

```csharp
public class WebSiteHostingService : IWebSiteHostingService
{
    private readonly ConcurrentDictionary<Guid, WebSiteInstance> _instances = new();
    private readonly ILogger<WebSiteHostingService> _logger;

    public async Task<OperationResultData<List<WebSiteInstance>>> GetAllWebsitesAsync()
    {
        try 
        {
            var instances = await LoadInstancesAsync();
            return OperationResultData<List<WebSiteInstance>>.CreateSuccess(instances);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load websites");
            return OperationResultData<List<WebSiteInstance>>.CreateFailure(
                $"Failed to load: {ex.Message}");
        }
    }

    public async Task<ApiResult> AddWebsiteAsync(WebSiteConfiguration config)
    {
        if (string.IsNullOrWhiteSpace(config.Name))
            return ApiResult.CreateFailure("Website name is required");
        
        if (string.IsNullOrWhiteSpace(config.Path))
            return ApiResult.CreateFailure("Website path is required");

        try 
        {
            var instance = await StartNewInstanceAsync(config);
            _instances[instance.Id] = instance;
            await _configService.SaveSiteAsync(config);
            
            return ApiResult.CreateSuccess($"Website '{config.Name}' added successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding website: {Name}", config.Name);
            return ApiResult.CreateFailure($"Failed to add: {ex.Message}");
        }
    }

    public async Task<ApiResult> RemoveWebsiteAsync(Guid id)
    {
        if (!_instances.ContainsKey(id))
            return ApiResult.CreateFailure($"Website with ID {id} not found");

        try 
        {
            var instance = _instances[id];
            await StopProcessAsync(instance);
            _instances.Remove(id, out _);
            await _configService.SaveSitesAsync(_instances.Values.Select(i => i.Configuration));
            
            return ApiResult.CreateSuccess($"Website with ID {id} removed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing website: {Id}", id);
            return ApiResult.CreateFailure($"Failed to remove: {ex.Message}");
        }
    }

    // ... other methods follow same pattern
}
```

**Key Principles**:

1. ❌ **Never throw exceptions for business logic errors** (not found, validation failures, etc.)
2. ✅ **Always return `ApiResult` or `OperationResultData<T>`**
3. ✅ **Catch all exceptions internally and wrap in failure result**
4. ✅ **Use descriptive error messages that UI can display to users**

---

### Layer 2: API Controllers (Thin Wrappers)

**Pattern**: Controllers simply pass through service results with HTTP 200

```csharp
[ApiController]
[Route(WebsiteHostingDefaults.ControllerBaseRoute)]
[AuthorizeSession]
public class WebsiteHostingController(IWebSiteHostingService hostingService, 
    ILogger<WebsiteHostingController> logger) : ControllerBase
{
    [HttpGet(WebsiteHostingDefaults.AllRoute)]
    public async Task<ActionResult<OperationResultData<List<WebSiteInstance>>>> GetAllWebsitesAsync()
        => Ok(await hostingService.GetAllWebsitesAsync()); // Always HTTP 200

    [HttpPost(WebsiteHostingDefaults.AddRoute)]
    public async Task<ActionResult<ApiResult>> AddWebsiteAsync([FromBody] WebSiteConfiguration config)
        => Ok(await hostingService.AddWebsiteAsync(config)); // Always HTTP 200

    [HttpPut(WebsiteHostingDefaults.UpdateRoute)]
    public async Task<ActionResult<ApiResult>> UpdateWebsiteAsync([FromBody] WebSiteConfiguration config)
        => Ok(await hostingService.UpdateWebsiteAsync(config)); // Always HTTP 200

    [HttpDelete(WebsiteHostingDefaults.RemoveRoute + "/{id}")]
    public async Task<ActionResult<ApiResult>> RemoveWebsiteAsync(Guid id)
        => Ok(await hostingService.RemoveWebsiteAsync(id)); // Always HTTP 200

    [HttpPost(WebsiteHostingDefaults.StartRoute + "/{id}")]
    public async Task<ActionResult<ApiResult>> StartWebsiteAsync(Guid id)
        => Ok(await hostingService.StartWebsiteAsync(id)); // Always HTTP 200

    [HttpPost(WebsiteHostingDefaults.StopRoute + "/{id}")]
    public async Task<ActionResult<ApiResult>> StopWebsiteAsync(Guid id)
        => Ok(await hostingService.StopWebsiteAsync(id)); // Always HTTP 200
}
```

**Controller Responsibilities**:

- ✅ Deserialize request bodies
- ✅ Call service methods
- ✅ Return `Ok(result)` - always HTTP 200
- ❌ **NO business logic** (handled by services)
- ❌ **NO error handling** (services return failure results)
- ❌ **NO try/catch blocks**

---

### Layer 3: Client Service Proxies (Translate to ApiResult<T>)

**Pattern**: HTTP calls always succeed (HTTP 200), but result body indicates success/failure

```csharp
public interface IWebSiteHostingService
{
    Task<OperationResultData<List<WebSiteInstance>>> GetAllWebsitesAsync();
    Task<ApiResult> AddWebsiteAsync(WebSiteConfiguration config);
    Task<ApiResult> UpdateWebsiteAsync(WebSiteConfiguration config);
    Task<ApiResult> RemoveWebsiteAsync(Guid id);
    Task<ApiResult> StartWebsiteAsync(Guid id);
    Task<ApiResult> StopWebsiteAsync(Guid id);
}

public class WebSiteHostingService : IWebSiteHostingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebSiteHostingService> _logger;

    public WebSiteHostingService(IHttpClientFactory httpClientFactory, 
        ILogger<WebSiteHostingService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("UiClient");
        _logger = logger;
    }

    public async Task<OperationResultData<List<WebSiteInstance>>> GetAllWebsitesAsync()
    {
        try 
        {
            var result = await _httpClient.GetFromJsonAsync<OperationResultData<List<WebSiteInstance>>>(
                WebsiteHostingDefaults.AllFullRoute);
            
            return result ?? OperationResultData<List<WebSiteInstance>>.CreateFailure(
                "Failed to load websites from server");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HTTP error loading websites");
            // Network errors still throw exceptions - these are not business errors
            throw; 
        }
    }

    public async Task<ApiResult> AddWebsiteAsync(WebSiteConfiguration config)
    {
        try 
        {
            var json = JsonSerializer.Serialize(config, JsonOptionsCache.WriteIndented);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(WebsiteHostingDefaults.AddFullRoute, content);
            // Response is always HTTP 200
            
            var result = await response.Content.ReadFromJsonAsync<ApiResult>();
            return result ?? ApiResult.CreateFailure("Failed to add website");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HTTP error adding website");
            throw; // Network errors still throw
        }
    }

    public async Task<ApiResult> RemoveWebsiteAsync(Guid id)
    {
        try 
        {
            var response = await _httpClient.DeleteAsync(
                $"{WebsiteHostingDefaults.RemoveRoute}/{id}");
            // Response is always HTTP 200
            
            var result = await response.Content.ReadFromJsonAsync<ApiResult>();
            return result ?? ApiResult.CreateFailure("Failed to remove website");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HTTP error removing website: {Id}", id);
            throw; // Network errors still throw
        }
    }

    // ... other methods follow same pattern
}
```

**Client Proxy Responsibilities**:

- ✅ Translate HTTP 200 responses to `ApiResult<T>` by reading response body
- ✅ Handle serialization/deserialization
- ❌ **NO try/catch for expected business errors** (result.Success handles these)
- ⚠️ **Still throw on network errors** (connection failures, timeouts) - these are not business logic

---

### Layer 4: UI Components (Simple if-check Pattern)

**Pattern**: Check `result.Success` before accessing data, no try/catch for business errors

```razor
@inject IWebSiteHostingService WebsiteService
@inject ILicensesDialogService LicensesService

<PageTitle>Home - ADWH</PageTitle>

<h2>.NET Web Hosting Manager</h2>

@if (_isLoading)
{
    <p>Loading websites...</p>
}
else if (_loadError is not null)
{
    <Alert severity="error">Failed to load websites: {_loadError}</Alert>
}
else if (_websites.Count == 0)
{
    <Alert severity="info">No websites configured. Click "Add Website" to create one.</Alert>
}
else
{
    <AutoDataGrid @ref="_dataGrid" Items="@_websites" SelectionMode="SelectionMode.Single"
                  SelectedItem="@_selectedWebsite" RowSelected="@HandleRowSelected"
                  EmptyMessage="No websites configured">
        <DataGridTemplateColumn Header="Name">
            @context.Name
        </DataGridTemplateColumn>
        <DataGridTemplateColumn Header="Path">
            @context.Configuration.Path
        </DataGridTemplateColumn>
        <DataGridTemplateColumn Header="Port">
            @context.Configuration.InternalPort
        </DataGridTemplateColumn>
        <DataGridTemplateColumn Header="State">
            <Chip severity="@GetStateSeverity(context.State)" 
                  label="@context.State.ToString()" />
        </DataGridTemplateColumn>
    </AutoDataGrid>

    <Toolbar>
        <Button icon={Icons.Filled.Add} label="Add Website" 
                onClick="@HandleAddClick" variant="contained" />
        <Button icon={Icons.Outlined.Edit} label="Edit Website" 
                onClick="@HandleEditClick" disabled="@(_selectedWebsite == null)" />
        <Button icon={Icons.Outlined.Delete} label="Delete Website" 
                onClick="@HandleDeleteClick" disabled="@(_selectedWebsite == null)" />
        <Button icon={Icons.Outlined.PlayArrow} label="Start" 
                onClick="@HandleStartClick" disabled="@(_selectedWebsite?.State != WebSiteState.Stopped)" />
        <Button icon={Icons.Outlined.Stop} label="Stop" 
                onClick="@HandleStopClick" disabled="@(_selectedWebsite?.State != WebSiteState.Running)" />
    </Toolbar>

    <WebSiteConfigurationDialog @ref="_addEditDialog" 
                                OnSave="@HandleSaveWebsite" />
</else>

@code {
    private List<WebSiteInstance> _websites = [];
    private WebSiteInstance? _selectedWebsite;
    private bool _isLoading = true;
    private string? _loadError;
    private AutoDataGrid<WebSiteInstance>? _dataGrid;
    private WebSiteConfigurationDialog? _addEditDialog;

    protected override async Task OnInitializedAsync()
    {
        await LoadWebsites();
    }

    private async Task LoadWebsites()
    {
        _isLoading = true;
        StateHasChanged();

        // SIMPLE: Just check result.Success, no try/catch needed!
        var result = await WebsiteService.GetAllWebsitesAsync();
        
        if (!result.Success)
        {
            _loadError = result.ErrorMessage ?? "Unknown error";
            _isLoading = false;
            StateHasChanged();
            return; // Early return - simple and clear!
        }

        _websites = result.Data; // Typed access guaranteed after Success check
        _isLoading = false;
        StateHasChanged();
    }

    private async Task HandleAddClick()
    {
        await _addEditDialog?.ShowAsync(null);
    }

    private async Task HandleEditClick()
    {
        if (_selectedWebsite == null) return;
        
        await _addEditDialog?.ShowAsync(_selectedWebsite.Configuration);
    }

    private async Task HandleDeleteClick()
    {
        if (_selectedWebsite == null) return;

        // SIMPLE: Check result.Success, no try/catch for business errors!
        var result = await WebsiteService.RemoveWebsiteAsync(_selectedWebsite.Id);
        
        if (!result.Success)
        {
            // Show error message to user - no exception handling needed!
            StateHasChanged();
            return;
        }

        // Success - reload list
        await LoadWebsites();
    }

    private async Task HandleSaveWebsite(WebSiteConfiguration config)
    {
        ApiResult result;
        
        if (config.Id == Guid.Empty)
        {
            result = await WebsiteService.AddWebsiteAsync(config);
        }
        else
        {
            result = await WebsiteService.UpdateWebsiteAsync(config);
        }

        // SIMPLE: Just check Success, no try/catch!
        if (!result.Success)
        {
            // Show error: result.ErrorMessage
            StateHasChanged();
            return;
        }

        await LoadWebsites();
    }

    private async Task HandleStartClick()
    {
        if (_selectedWebsite == null) return;

        var result = await WebsiteService.StartWebsiteAsync(_selectedWebsite.Id);
        
        if (!result.Success)
        {
            // Show error message
            StateHasChanged();
            return;
        }

        await LoadWebsites();
    }

    private async Task HandleStopClick()
    {
        if (_selectedWebsite == null) return;

        var result = await WebsiteService.StopWebsiteAsync(_selectedWebsite.Id);
        
        if (!result.Success)
        {
            // Show error message
            StateHasChanged();
            return;
        }

        await LoadWebsites();
    }

    private static Severity GetStateSeverity(WebSiteState state) => state switch
    {
        WebSiteState.Running => Severity.Success,
        WebSiteState.Stopped => Severity.Warning,
        WebSiteState.Error => Severity.Danger,
        _ => Severity.Info
    };

    private async Task HandleRowSelected(DataGridRowClickEventArgs<WebSiteInstance> args)
    {
        _selectedWebsite = args.Data;
        StateHasChanged();
    }
}
```

**UI Component Principles**:

- ✅ **Simple if-check pattern**: `if (!result.Success) return;`
- ✅ **No try/catch for business errors** (handled by result pattern)
- ✅ **Access `.Data` safely after checking `.Success`**
- ⚠️ **Still need try/catch for network errors** (HTTP failures, timeouts)

---

## 10b. Migration Strategy with Service Layer Changes

### Updated Phase 0: Create ApiResult Foundation (75 minutes - No Change)

Same as before - create base types and update specialized result types.

### NEW Phase 0.5: Update Service Interfaces and Implementations (~3 hours)

| Task | Services to Update | Time Estimate |
|------|-------------------|---------------|
| Convert all service methods to return `ApiResult<T>` | IWebSiteHostingService, IDotnetVersionService, IFileSystemService, IAuthenticationService | 2 hours |
| Implement error handling in service methods (catch/return failure) | All service implementations | 1 hour |

### Phase 1: Simplify Controllers (~1 hour)

| Task | Controllers to Update | Time Estimate |
|------|----------------------|---------------|
| Remove try/catch blocks from controllers | All 6 controllers | 30 min |
| Update return types to match service results | All controllers | 30 min |

### Phase 2: Update Client Proxies (~1 hour)

| Task | Services to Update | Time Estimate |
|------|-------------------|---------------|
| Translate HTTP responses to `ApiResult<T>` | All client-side service proxies | 1 hour |

### Total Updated Timeline (Including Ui.Client)

| Phase | Task | Time Estimate |
|-------|------|---------------|
| **Phase 0** | Create ApiResult foundation | 75 min |
| **Phase 0.5** | Update Service layer (Ui.Services/) | 3 hours |
| **Phase 1** | Simplify Controllers (Ui.Controllers/) | 1 hour |
| **Phase 2** | Update Client Proxies (Ui.Client.Services/) - NEW | 1.5 hours |
| **Phase 3** | Update UI Components (Ui.Client/Components/) - NEW | 1 hour |
| **Total** | | **~8 hours (1 working day)** |

---

## 10e. Benefits of Service Layer ApiResult Pattern

### For Client Code Simplicity ✅

```razor
// ❌ WITHOUT service result pattern - requires try/catch everywhere
private async Task DeleteWebsiteAsync(WebSiteInstance website)
{
    try 
    {
        await _httpClient.DeleteAsync($"/api/websites/{website.Id}");
        await LoadWebsites(); // Success path
    }
    catch (HttpOperationException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
    {
        // Handle expected error - but client needs to know this is normal!
        ShowError($"Website not found: {ex.Message}");
    }
    catch (Exception ex)
    {
        // Unknown error
        ShowError($"Failed to delete: {ex.Message}");
    }
}

// ✅ WITH service result pattern - simple if-check!
private async Task DeleteWebsiteAsync(WebSiteInstance website)
{
    var result = await _websiteService.RemoveWebsiteAsync(website.Id);
    
    if (!result.Success)
    {
        ShowError(result.ErrorMessage ?? "Failed to delete");
        return; // Early return - simple and clear!
    }

    await LoadWebsites(); // Success path only, no exception handling needed!
}
```

### For Service Layer Clarity ✅

```csharp
// ❌ BEFORE - Mixed patterns (exceptions vs null)
public async Task<WebSiteInstance?> GetWebsiteByIdAsync(Guid id)
{
    var instance = await _configService.GetSiteByIdAsync(id);
    
    if (instance == null)
        throw new KeyNotFoundException($"Website {id} not found"); // Exception for business error?
    
    return instance;
}

// ✅ AFTER - Clear success/failure contract
public async Task<OperationResultData<WebSiteInstance>> GetWebsiteByIdAsync(Guid id)
{
    var instance = await _configService.GetSiteByIdAsync(id);
    
    if (instance == null)
        return OperationResultData<WebSiteInstance>.CreateFailure(
            $"Website {id} not found"); // Explicit failure, no exception
    
    return OperationResultData.CreateSuccess(instance);
}
```

### For Controller Simplicity ✅

```csharp
// ❌ BEFORE - Controllers have try/catch and error handling logic
[HttpDelete("api/websites/{id}")]
public async Task<ActionResult> RemoveWebsite(Guid id)
{
    try 
    {
        await _service.RemoveAsync(id);
        return Ok(); // Success
    }
    catch (KeyNotFoundException ex)
    {
        logger.LogWarning(ex, "Website not found: {Id}", id);
        return NotFound(new { error = ex.Message }); // HTTP 404 - breaks HTTP 200 constraint!
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error removing website");
        return StatusCode(500, new { error = "Failed to remove" }); // HTTP 500 - breaks constraint!
    }
}

// ✅ AFTER - Controllers are thin wrappers, always HTTP 200
[HttpDelete("api/websites/{id}")]
public async Task<ActionResult<ApiResult>> RemoveWebsite(Guid id)
{
    return Ok(await _service.RemoveAsync(id)); // Always HTTP 200!
}
```

---

## 10d. HTTP 200 Constraint - Client Expectations

### Why This Works Well with Result Pattern

Since controllers always return HTTP 200, the client **MUST** check `result.Success` property:

```csharp
// CLIENT CODE PATTERN (ENFORCED):
var result = await service.DoSomethingAsync();

// ALWAYS CHECK SUCCESS FIRST!
if (!result.Success) 
{
    // Handle error - display result.ErrorMessage to user
    ShowError(result.ErrorMessage);
    return; // Exit early
}

// Only access .Data after checking Success
var data = result.Data; // Safe - guaranteed non-null if Success is true
```

### Documentation Requirement

Add this pattern to client-side service interface documentation:

```csharp
/// <summary>
/// All methods always return HTTP 200 from server.
/// Client code MUST check result.Success before accessing result.Data.
/// </summary>
public interface IWebSiteHostingService
{
    /// <summary>
    /// Returns OperationResultData with Success=false if operation failed.
    /// Always returns HTTP 200 - never throws HttpOperationException for business errors.
    /// </summary>
    Task<OperationResultData<List<WebSiteInstance>>> GetAllWebsitesAsync();
    
    // ... other methods follow same pattern
}
```

---

## Summary: Service Layer Result Pattern Decision

**Decision**: ✅ **USE ApiResult<T> IN SERVICES**

This architecture perfectly matches your requirement of always returning HTTP 200 from controllers, while keeping client code simple through the result pattern.

**Key Benefits**:

1. ✅ Client code is simple (no try/catch for business errors)
2. ✅ Controllers remain thin wrappers (just pass-through results)
3. ✅ Services express business logic clearly (success/failure as first-class concepts)
4. ✅ Perfect fit for HTTP 200 constraint
5. ✅ Easier testing (mock result objects, not exceptions)

**Implementation Cost**: ~6.5 hours total (Phase 0 + Phase 0.5 + Phase 1 + Phase 2)

---

## 11. Ui.Client Service Layer Impact Analysis (NEW - February 28, 2026)

### Current State Analysis

Your Ui.Client has **7 service proxy files** with **inconsistent patterns**:

| Service | Current Return Types | Consistency Issues |
|---------|---------------------|-------------------|
| `WebSiteHostingService` | Mixed: `List<T>`, `T`, `OperationResult` | ❌ Inconsistent - some return raw types, some use OperationResult |
| `DotnetVersionService` | All `List<T>` or `bool` | ❌ No result pattern at all |
| `FileSystemService` | Mixed: `List<T>`, `IQueryable<T>`, `OperationResult` | ❌ Inconsistent |
| `FrameworkManagementService` | `InstallationResult` only | ✅ Consistent but old pattern |
| `AuthenticationService` | `AuthenticationResult`, `bool`, `AuthenticationStatus` | ⚠️ Partial - Login uses result, others don't |

### Impact by Service (Detailed)

#### 1. WebSiteHostingService.cs 🔴 HIGH IMPACT

**Current Code Issues:**

```csharp
// ❌ INCONSISTENT PATTERNS
public async Task<List<WebSiteInstance>> GetAllWebsitesAsync()
{
    var websites = await _httpClient.GetJsonAsync<List<WebSiteInstance>>(...);
    return websites ?? []; // Returns empty list on error - client can't distinguish!
}

public async Task<WebSiteInstance> AddWebsiteAsync(...)
{
    var result = await _httpClient.PostJsonAsync<..., WebSiteInstance>(...);
    return result ?? new WebSiteInstance(configuration); // Silent failure!
}

public async Task<OperationResult> RemoveWebsiteAsync(Guid id)
{
    var result = await _httpClient.DeleteJsonAsync<OperationResult>(...);
    return result ?? OperationResult.CreateFailure("Unknown error"); // ✅ Good pattern
}
```

**Required Changes:**

```csharp
// ✅ CONSISTENT WITH APIRESULT PATTERN
public async Task<OperationResultData<List<WebSiteInstance>>> GetAllWebsitesAsync()
{
    var result = await _httpClient.GetJsonAsync<OperationResultData<List<WebSiteInstance>>>(...);
    return result ?? OperationResultData<List<WebSiteInstance>>.CreateFailure(
        "Failed to load websites from server"); // Clear failure
}

public async Task<ApiResult> AddWebsiteAsync(WebSiteConfiguration configuration)
{
    var json = JsonSerializer.Serialize(configuration);
    var content = new StringContent(json, Encoding.UTF8, "application/json");
    
    var response = await _httpClient.PostAsync(..., content);
    // HTTP 200 always - check body for success/failure
    var result = await response.Content.ReadFromJsonAsync<ApiResult>();
    return result ?? ApiResult.CreateFailure("Failed to add website");
}

public async Task<ApiResult> RemoveWebsiteAsync(Guid id)
{
    var response = await _httpClient.DeleteAsync(...);
    var result = await response.Content.ReadFromJsonAsync<ApiResult>();
    return result ?? ApiResult.CreateFailure("Failed to remove website");
}
```

**Impact on UI Components:**

- **Before**: Client must check if list is empty (can't tell if error or no data)
- **After**: Check `result.Success` - clear distinction between success and failure

---

#### 2. DotnetVersionService.cs 🔴 HIGH IMPACT

**Current Code Issues:**

```csharp
// ❌ NO RESULT PATTERN AT ALL
public async Task<List<FrameworkInfo>> GetInstalledVersionsAsync()
{
    var versions = await _httpClient.GetJsonAsync<List<FrameworkInfo>>(...);
    return versions ?? []; // Silent failure - client has no way to know!
}

public async Task<bool> IsChannelInstalledAsync(...)
{
    var result = await _httpClient.GetJsonAsync<bool>(...);
    return result == true; // What if HTTP request failed? Still returns false!
}
```

**Required Changes:**

```csharp
// ✅ USE APIRESULT PATTERN
public async Task<OperationResultData<List<FrameworkInfo>>> GetInstalledVersionsAsync()
{
    var result = await _httpClient.GetJsonAsync<OperationResultData<List<FrameworkInfo>>>(...);
    return result ?? OperationResultData<List<FrameworkInfo>>.CreateFailure(
        "Failed to load .NET versions from server");
}

public async Task<OperationResult<bool>> IsChannelInstalledAsync(...)
{
    var result = await _httpClient.GetJsonAsync<OperationResultData<bool>>(...);
    
    if (!result.Success)
        return OperationResult.CreateFailure(result.ErrorMessage ?? "Unknown error");
    
    return OperationResult.CreateSuccess(result.Data); // Explicit success path
}
```

**Impact on UI Components:**

- **Before**: Silent failures - user sees empty list, no error message
- **After**: Clear error messages when server is unavailable or returns errors

---

#### 3. FileSystemService.cs 🟡 MEDIUM IMPACT

**Current Code Issues:**

```csharp
// ❌ MIXED PATTERNS
public async Task<List<FileSystemItem>> GetSharedFoldersAsync(Func<string, Task> errorHandler)
{
    var sharedFolders = await _httpClient.GetJsonAsync<List<FileSystemItem>>(...);
    return sharedFolders ?? []; // Silent failure!
}

public async Task<IQueryable<FileStationFile>> GetDirectoryContentsAsync(...)
{
    var files = await _httpClient.GetJsonAsync<List<FileStationFile>>(...);
    return files?.AsQueryable() ?? Enumerable.Empty<FileStationFile>().AsQueryable();
}

public async Task<OperationResult> SetHttpGroupPermissionsAsync(...)
{
    var result = await _httpClient.PostJsonAsync<..., OperationResult>(...);
    return result ?? OperationResult.CreateFailure("Unknown error"); // ✅ Good
}
```

**Required Changes:**

```csharp
// ✅ CONSISTENT WITH APIRESULT PATTERN
public async Task<OperationResultData<List<FileSystemItem>>> GetSharedFoldersAsync()
{
    var result = await _httpClient.GetJsonAsync<OperationResultData<List<FileSystemItem>>>(...);
    return result ?? OperationResultData<List<FileSystemItem>>.CreateFailure(
        "Failed to load shared folders");
}

public async Task<OperationResultData<IQueryable<FileStationFile>>> GetDirectoryContentsAsync(...)
{
    var result = await _httpClient.GetJsonAsync<OperationResultData<List<FileStationFile>>>(...);
    
    if (!result.Success)
        return OperationResult.CreateFailure<IQueryable<FileStationFile>>(
            result.ErrorMessage ?? "Unknown error");
    
    return OperationResultData.CreateSuccess(result.Data.AsQueryable());
}

public async Task<ApiResult> SetHttpGroupPermissionsAsync(...)
{
    var response = await _httpClient.PostAsJsonAsync(..., model);
    var result = await response.Content.ReadFromJsonAsync<ApiResult>();
    return result ?? ApiResult.CreateFailure("Failed to set permissions");
}
```

**Note:** Remove the `errorHandler` parameter - errors now come through `result.ErrorMessage`

---

#### 4. FrameworkManagementService.cs 🟢 LOW IMPACT

**Current Code:**

```csharp
// ✅ ALMOST CONSISTENT (uses InstallationResult)
public async Task<InstallationResult> InstallFrameworkAsync(...)
{
    var result = await _httpClient.PostJsonAsync<..., InstallationResult>(...);
    return result ?? InstallationResult.CreateFailure("Unknown error"); // Good pattern!
}
```

**Required Changes:**

```csharp
// MINIMAL CHANGE - Just update to extend ApiResultBase<string>
public async Task<InstallationResult> InstallFrameworkAsync(string version, string channel)
{
    var model = new InstallFrameworkModel(version, channel);
    var result = await _httpClient.PostJsonAsync<..., InstallationResult>(...);
    return result ?? InstallationResult.CreateFailure("Unknown error"); // Still works!
}
```

**Impact:** Minimal - already using good pattern, just needs to extend base class in Phase 0

---

#### 5. AuthenticationService.cs 🟡 MEDIUM IMPACT

**Current Code Issues:**

```csharp
// ✅ Login uses result pattern (good!)
public async Task<AuthenticationResult> LoginAsync(...)
{
    return await _httpClient.PostJsonAsync<..., AuthenticationResult>(...)
        ?? new AuthenticationResult(false, "Unknown error"); // Good!
}

// ❌ Others don't use results
public async Task<bool> LogoutAsync()
{
    var response = await _httpClient.PostAsync(...);
    return response.IsSuccessStatusCode; // Only checks HTTP status, not result body
}

public async Task<AuthenticationStatus> GetStatusAsync()
{
    var result = await _httpClient.GetJsonAsync<AuthenticationStatus>(...);
    return result == default ? AuthenticationStatus.NotAuthenticated : result;
}
```

**Required Changes:**

```csharp
// ✅ CONSISTENT WITH APIRESULT PATTERN
public async Task<bool> LogoutAsync()
{
    var response = await _httpClient.PostAsync(...);
    
    // HTTP 200 always - check body for actual success/failure
    var result = await response.Content.ReadFromJsonAsync<ApiResult>();
    return result?.Success == true;
}

public async Task<OperationResultData<AuthenticationStatus>> GetStatusAsync()
{
    var result = await _httpClient.GetJsonAsync<OperationResultData<AuthenticationStatus>>(...);
    
    if (!result.Success)
        return OperationResult.CreateFailure<AuthenticationStatus>(
            "Failed to check authentication status");
    
    return OperationResultData.CreateSuccess(result.Data);
}
```

---

### HttpClientExtensions.cs ⚠️ NEEDS ADDITIONS

**Required Additions:**

```csharp
// Need method to read result from response (for when HTTP 200 always)
public static class HttpClientExtensions
{
    // Existing methods...
    
    // NEW: Read ApiResult from HttpResponseMessage
    public static async Task<ApiResult?> ReadApiResultAsync(
        this HttpContent content, 
        CancellationToken cancellationToken = default)
    {
        return await content.ReadFromJsonAsync<ApiResult>(cancellationToken);
    }

    // NEW: Read OperationResultData<T> from HttpResponseMessage  
    public static async Task<OperationResultData<T>?> ReadResultDataAsync<T>(
        this HttpContent content, 
        CancellationToken cancellationToken = default)
    {
        return await content.ReadFromJsonAsync<OperationResultData<T>>(cancellationToken);
    }
}
```

---

### Summary of Required Changes for Ui.Client

| Service | Files to Update | Methods to Change | Time Estimate |
|---------|-----------------|-------------------|---------------|
| `WebSiteHostingService` | 1 interface, 1 class | All 6 methods | 30 min |
| `DotnetVersionService` | 1 class (no interface in Ui.Client) | All 5 methods | 20 min |
| `FileSystemService` | 1 class (check if interface exists) | All 3 methods | 15 min |
| `FrameworkManagementService` | 1 class (check if interface exists) | 2 methods | 10 min |
| `AuthenticationService` | 1 interface, 1 class | 2 methods (Logout, GetStatus) | 15 min |
| `HttpClientExtensions` | 1 file | Add 2 new extension methods | 10 min |
| **Total** | ~8 files | ~19 methods | **~1.5 hours** |

---

### Benefits for Ui.Client

#### Before (Current State)

```razor
// ❌ Can't distinguish errors from empty data
var websites = await websiteService.GetAllWebsitesAsync();
if (websites.Count == 0)
{
    // Is this an error? Or just no websites configured?
    // Client has NO WAY to know!
}

// ❌ Silent failures
var result = await versionService.GetInstalledVersionsAsync();
// If HTTP request failed, returns empty list - user sees nothing wrong
```

#### After (With ApiResult<T>)

```razor
// ✅ Clear success/failure distinction
var result = await websiteService.GetAllWebsitesAsync();

if (!result.Success)
{
    // ERROR: Server unavailable or operation failed
    ShowError(result.ErrorMessage ?? "Failed to load websites");
    return;
}

// SUCCESS: Safe to access data
var websites = result.Data;
if (websites.Count == 0)
{
    // No websites configured - this is EXPECTED behavior, not an error
    ShowInfo("No websites configured. Add one to get started.");
}
```

---

### Migration Priority for Ui.Client

1. **Phase 2: Update Client Proxies** (from migration document)
   - Start with DotnetVersionService and WebSiteHostingService (highest impact)
   - Then FrameworkManagementService, FileSystemService, AuthenticationService
   - Add HttpClientExtensions helper methods

2. **Update UI Components** to use new result pattern
   - Home.razor - all website operations
   - AspNetReleasesDialog - version queries
   - Any other components using these services

3. **Total Time**: ~1.5 hours for service proxies + ~1 hour for UI updates = **~2.5 hours**

---

### Key Insight

Your current Ui.Client services have **mixed patterns** - some use OperationResult, others return raw types. This creates inconsistency where:

- Some failures are visible (OperationResult)
- Some failures are silent (raw types with `?? []`)

The ApiResult pattern makes **all errors explicit and visible**, which is perfect for your HTTP 200 constraint since the client MUST check `result.Success` to know if operation succeeded.

---

## 9. ApiResult Base Pattern Design (Updated February 28, 2026)

### Current Result Types Analysis

| Type | Location | Generic Support | Status | Decision |
|------|----------|-----------------|--------|----------|
| `OperationResult` | `Data.Results` | ❌ No | Non-generic only | ❌ **REPLACE** with `ApiResult` base |
| `InstallationResult` | `Data.Results` | ❌ No | Custom properties | ✅ **KEEP** - needs domain properties |
| `AuthenticationResult` | `Data.Security` | ❌ No | Authentication-specific | ⚠️ **KEEP** - extends with Token property |

### Final Result Type Architecture (Updated)

After analysis, the result pattern architecture is refined to minimize redundancy while maintaining clarity:

#### Base Layer (New - Phase 0 Task)

```csharp
namespace Askyl.Dsm.WebHosting.Data.Results;

/// <summary>
/// Base interface for all API result types.
/// Provides common contract for success/failure checking.
/// </summary>
public interface IApiResult
{
    bool Success { get; }
    string? ErrorMessage { get; }
}

/// <summary>
/// Non-generic base class for operations that don't return data.
/// </summary>
public abstract record ApiResult : IApiResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a successful result without data.
    /// </summary>
    public static ApiResult CreateSuccess(string? message = null)
        => new SuccessResult(message);

    /// <summary>
    /// Creates a failure result.
    /// </summary>
    public static ApiResult CreateFailure(string errorMessage)
        => new FailureResult(errorMessage);

    private record SuccessResult(string? Message) : ApiResult
    {
        public override bool Success => true;
        public override string? ErrorMessage => Message;
    }

    private record FailureResult(string ErrorMsg) : ApiResult
    {
        public override bool Success => false;
        public override string? ErrorMessage => ErrorMsg;
    }
}

/// <summary>
/// Generic base class for API results that carry data.
/// </summary>
/// <typeparam name="T">The type of data carried by this result.</typeparam>
public abstract record ApiResultBase<T> : IApiResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public T? Data { get; init; }

    /// <summary>
    /// Creates a successful result with data.
    /// </summary>
    public static ApiResultBase<T> CreateSuccess(T data, string? message = null)
        => new SuccessResult(data, message);

    /// <summary>
    /// Creates a failure result without data.
    /// </summary>
    public static ApiResultBase<T> CreateFailure(string errorMessage)
        => new FailureResult(errorMessage);

    private record SuccessResult(T Data, string? Message) : ApiResultBase<T>
    {
        public override bool Success => true;
        public override string? ErrorMessage => Message;
    }

    private record FailureResult(string ErrorMsg) : ApiResultBase<T>
    {
        public override bool Success => false;
        public override string? ErrorMessage => ErrorMsg;
    }
}
```

#### Specialized Result Types (Extending Base Pattern)

##### 1. OperationResultData<T> - Generic Data-Carrying Results

```csharp
// Askyl.Dsm.WebHosting.Data.Results/OperationResultData.cs

/// <summary>
/// Generic result type for operations that return data.
/// Extends ApiResultBase<T> with semantic clarity.
/// </summary>
/// <typeparam name="T">The type of data returned.</typeparam>
public record OperationResultData<T> : ApiResultBase<T>;
```

**Usage**: Replace `OperationResult<List<T>>` with clearer `OperationResultData<List<T>>`.

##### 2. AuthenticationResult - Auth-Specific Results

```csharp
// Askyl.Dsm.WebHosting.Data.Security/AuthenticationResult.cs

/// <summary>
/// Result type for authentication operations (login/logout).
/// Carries session token as data when successful.
/// </summary>
public record AuthenticationResult : ApiResultBase<string?>
{
    /// <summary>
    /// Session token if authentication was successful.
    /// </summary>
    public string? Token => Data;

    /// <summary>
    /// Creates a successful authentication result with token.
    /// </summary>
    public static new AuthenticationResult CreateSuccess(string token, string? message = null)
        => new() { Success = true, ErrorMessage = message, Data = token };

    /// <summary>
    /// Creates a failed authentication result.
    /// </summary>
    public static new AuthenticationResult CreateFailure(string errorMessage)
        => new() { Success = false, ErrorMessage = errorMessage, Data = null };
}
```

**Why Keep**: Needs explicit `Token` property for semantic clarity and custom factory methods.

##### 3. InstallationResult - Domain-Specific Results

```csharp
// Askyl.Dsm.WebHosting.Data.Results/InstallationResult.cs

/// <summary>
/// Result type for framework/runtime installation operations.
/// Includes domain-specific properties beyond base pattern.
/// </summary>
public record InstallationResult : ApiResultBase<string> // Data = version string
{
    /// <summary>
    /// Unique installation identifier (if available).
    /// </summary>
    public string? InstallationId { get; init; }

    /// <summary>
    /// Installed version (same as Data for consistency).
    /// </summary>
    public string? Version => Data;

    /// <summary>
    /// Timestamp when installation was completed.
    /// </summary>
    public DateTime? InstalledAt { get; init; }

    /// <summary>
    /// Creates a successful installation result.
    /// </summary>
    public static new InstallationResult CreateSuccess(
        string version, 
        string message = "Installation completed successfully.")
        => new() 
        { 
            Success = true, 
            ErrorMessage = null, 
            Data = version,
            Version = version,
            InstalledAt = DateTime.UtcNow
        };

    /// <summary>
    /// Creates a failed installation result.
    /// </summary>
    public static new InstallationResult CreateFailure(string message)
        => new() { Success = false, ErrorMessage = message, Data = null };
}
```

**Why Keep**: Needs domain-specific properties (`InstallationId`, `InstalledAt`) beyond base pattern.

##### 4. OperationResult - REMOVED (Replaced by ApiResult)

```csharp
// ❌ DEPRECATED: Remove this file in Phase 0 migration
// Use ApiResult directly instead for non-generic operations
```

**Replacement**: All usages of `OperationResult` should be replaced with `ApiResult`.

### Result Type Decision Matrix

| Original Type | Final Status | Reason | Replacement |
|--------------|--------------|--------|-------------|
| `OperationResult` | ❌ **REMOVED** | Fully covered by base class | Use `ApiResult` directly |
| `AuthenticationResult` | ✅ **KEPT** | Needs Token property + custom factories | Extends `ApiResultBase<string?>` |
| `InstallationResult` | ✅ **KEPT** | Needs domain properties (Id, Timestamp) | Extends `ApiResultBase<string>` |
| `OperationResultData<T>` | ✅ **NEW** | Semantic clarity for generic results | Extends `ApiResultBase<T>` |

### Migration Benefits

1. **Reduced Duplication**: From 4 independent types to 2 base classes + 3 specialized wrappers
2. **Consistent Behavior**: All result types share same success/failure logic in base class
3. **Type Safety**: `IApiResult` interface enables polymorphic handling
4. **Semantic Clarity**: Specialized types clearly communicate intent (Auth vs Install vs Generic)
5. **Extensibility**: Easy to add new specialized results without duplicating core logic

### Updated Migration Checklist

| Phase | Task | Status | Files to Create/Update | Time Estimate |
|-------|------|--------|------------------------|---------------|
| **Phase 0.1** | Create `IApiResult` interface | ⏳ Pending | `Data.Results/IApiResult.cs` | 5 min |
| **Phase 0.2** | Create `ApiResult` base class | ⏳ Pending | `Data.Results/ApiResult.cs` | 15 min |
| **Phase 0.3** | Create `ApiResultBase<T>` generic base | ⏳ Pending | `Data.Results/ApiResultBase.cs` | 15 min |
| **Phase 0.4** | Update `AuthenticationResult` | ⏳ Pending | `Data.Security/AuthenticationResult.cs` | 10 min |
| **Phase 0.5** | Update `InstallationResult` | ⏳ Pending | `Data.Results/InstallationResult.cs` | 15 min |
| **Phase 0.6** | Create `OperationResultData<T>` | ⏳ Pending | `Data.Results/OperationResultData.cs` | 5 min |
| **Phase 0.7** | Remove `OperationResult` (deprecated) | ⏳ Pending | Delete file, update all usages | 10 min |
| **Total Phase 0** | | | | **75 minutes** |

### Updated Controller Migration Phases (After Phase 0)

#### Phase 1: WebsiteHostingController

- Use `OperationResultData<List<WebSiteInstance>>` for GET methods
- Use `ApiResult` for DELETE/POST operations without data return
- Fix HTTP status codes for errors

#### Phase 2: RuntimeManagementController  

- Use `OperationResultData<List<T>>` for all query methods
- Consider wrapping boolean queries in `OperationResultData<bool>`

#### Phase 3: FileManagementController

- Use `OperationResultData<List<T>>` for GET methods
- Maintain consistency with other controllers

#### Phase 4: LogDownloadController

- Keep success as `FileCallbackResult`
- Fix errors to use proper HTTP status codes (`NotFound()`, `StatusCode(503)`)

#### Phase 5: AuthenticationController

- Use updated `AuthenticationResult` from Phase 0
- Wrap `GetStatusAsync()` in result pattern (optional)

### Migration Strategy with ApiResult Base (Updated)

#### Phase 0: Create ApiResult Foundation (75 minutes - Updated Estimate)

**Step 1**: Create base types in `Askyl.Dsm.WebHosting.Data.Results`:

- `IApiResult.cs` - Interface with Success, ErrorMessage properties
- `ApiResult.cs` - Non-generic abstract record for operations without data return
- `ApiResultBase<T>.cs` - Generic abstract record for operations returning data

**Step 2**: Update existing result types:

- Convert `AuthenticationResult` to extend `ApiResultBase<string?>` with explicit Token property
- Convert `InstallationResult` to extend `ApiResultBase<string>` with InstallationId, InstalledAt properties
- Create new `OperationResultData<T>` record extending `ApiResultBase<T>` for semantic clarity

**Step 3**: Deprecate `OperationResult`:

- Mark as obsolete and update all usages to use `ApiResult` instead
- Remove file after controller migration is complete

**Files to Create/Update**:

```
✅ New Files:
  - Data.Results/IApiResult.cs
  - Data.Results/ApiResult.cs  
  - Data.Results/ApiResultBase<T>.cs
  - Data.Results/OperationResultData.cs

🔄 Update Files:
  - Data.Security/AuthenticationResult.cs (extend ApiResultBase<string?>)
  - Data.Results/InstallationResult.cs (extend ApiResultBase<string>)
  - Delete Data.Results/OperationResult.cs (deprecated)
```

**Expected Changes**: ~5 new files, 2 existing files updated, 1 file deleted.

#### Phase 1: WebsiteHostingController (Updated - After Phase 0 Complete)**Estimated Time**: 2 hours

| Step | Action | Methods to Update | Return Type Changes |
|------|--------|-------------------|---------------------|
| 1.1 | Convert GET to use `OperationResultData<T>` | `GetAllWebsitesAsync()` | `ActionResult<List<WebSiteInstance>>` → `ActionResult<OperationResultData<List<WebSiteInstance>>>` |
| 1.2 | Convert POST/DELETE to use `ApiResult` | `AddWebsite()`, `UpdateWebsite()`, `RemoveWebsite()`, `StartWebsite()`, `StopWebsite()` | Use `ApiResult` for non-data operations |
| 1.3 | Fix HTTP status codes for errors | All methods | Errors → `BadRequest(result)` or `StatusCode(500, result)` instead of always `Ok()` |

### Benefits of ApiResult Base Pattern (Updated)

1. **Reduced Duplication**: From 4 independent types to 2 base classes + 3 specialized wrappers
2. **Consistent Behavior**: All result types share same success/failure logic in base class
3. **Type Safety**: `IApiResult` interface enables polymorphic handling across all results
4. **Semantic Clarity**: Specialized types clearly communicate intent (Auth vs Install vs Generic)
5. **Extensibility**: Easy to add new specialized results without duplicating core logic
6. **Client Simplicity**: Client code can check `result.Success` and access `result.Data` uniformly

### Example Usage in Controllers (Updated)

```csharp
// GET method returning data - use OperationResultData<T>
[HttpGet(WebsiteHostingDefaults.AllRoute)]
public async Task<ActionResult<OperationResultData<List<WebSiteInstance>>>> GetAllWebsitesAsync()
{
    try
    {
        var instances = await hostingService.GetInstances();
        return Ok(OperationResultData<List<WebSiteInstance>>.CreateSuccess(instances));
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error retrieving websites");
        return BadRequest(OperationResultData<List<WebSiteInstance>>.CreateFailure($"Failed to retrieve websites: {ex.Message}"));
    }
}

// DELETE method without data return - use ApiResult
[HttpDelete(WebsiteHostingDefaults.RemoveRoute + "/{id}")]
public async Task<ActionResult<ApiResult>> RemoveWebsite(Guid id)
{
    try
    {
        await hostingService.RemoveInstanceAsync(id);
        return Ok(ApiResult.CreateSuccess($"Website with ID {id} removed successfully"));
    }
    catch (KeyNotFoundException)
    {
        logger.LogWarning("Website not found: {Id}", id);
        return NotFound(ApiResult.CreateFailure($"Website with ID {id} not found"));
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error removing website");
        return StatusCode(500, ApiResult.CreateFailure($"Failed to remove website: {ex.Message}"));
    }
}

// Usage in client-side service proxies - simplified
public async Task<OperationResultData<List<WebSiteInstance>>> GetAllWebsitesAsync()
{
    var response = await _httpClient.GetFromJsonAsync<OperationResultData<List<WebSiteInstance>>>(WebsiteHostingDefaults.AllFullRoute);
    return response ?? OperationResultData<List<WebSiteInstance>>.CreateFailure("Failed to load websites");
}

// Client code becomes simpler and more consistent
var result = await websiteService.GetAllWebsitesAsync();
if (result.Success)
{
    var websites = result.Data; // Typed data access, no casting needed!
    foreach (var site in websites) { /* process */ }
}
else
{
    logger.LogWarning("Failed to load websites: {Error}", result.ErrorMessage);
}
```

### Rollback Plan (Updated)

Since the product is not yet released, **API versioning is NOT required**. Breaking changes can be applied directly without maintaining backward compatibility.

**Recommended Approach**: Implement a unified `ApiResult` base pattern that all result types will follow, eliminating the need for rollback mechanisms.

If API stability becomes critical in future releases:

1. Keep both old and new result types during transition period
2. Add `[Obsolete]` attribute to deprecated types
3. Create v2 controllers with new result patterns
4. Maintain backward compatibility until deprecation deadline passes

---

## Recommendations and Next Steps (Updated)

### 🔴 Immediate Actions Required (Priority Order)

1. **Phase 0: Create ApiResult Foundation** - **75 minutes**
   - Create `IApiResult`, `ApiResult`, `ApiResultBase<T>`, `OperationResultData<T>`
   - Update `AuthenticationResult` and `InstallationResult` to extend base classes
   - Deprecate and remove `OperationResult`

2. **Remove Legacy Uiz-Old Project**
   - The monolithic `Uiz-Old` project is still present but no longer needed
   - All components have been migrated to `Ui.Client`
   - **Action**: Delete `src/Askyl.Dsm.WebHosting.Uiz-Old` after final verification

3. **Remove Deprecated ITemporaryTokenService**
   - Service is only kept in Uiz-Old for potential special token-based features
   - Not used anywhere in the new architecture (Ui or Ui.Client)
   - **Action**: Delete `src/Askyl.Dsm.WebHosting.Uiz-Old/Services/TemporaryTokenService.cs`

4. **Migrate Controllers to Unified Result Pattern** - **6 hours total**
   - Phase 1: WebsiteHostingController (2 hours)
   - Phase 2: RuntimeManagementController (2 hours)
   - Phase 3: FileManagementController (1 hour)
   - Phase 4: LogDownloadController (30 minutes)
   - Phase 5: AuthenticationController (30 minutes)

5. **Add Integration Tests**
   - Create end-to-end tests for:
     - Authentication flow (login/logout/session validation)
     - Website CRUD operations (add, edit, delete, start, stop)
     - Runtime management (install, uninstall, version check)
     - Log download functionality
     - File system operations

### 🟡 Medium Priority

1. **Update Session Cookie Configuration**
   - Consider adding constant for session cookie name (`"ADWH.Session"` in Ui/Program.cs)
   - Document session timeout configuration (30 minutes idle timeout)

2. **Document Additional Projects**
   - Add architecture diagrams showing `Tools`, `Logging`, `DotnetInstaller` projects
   - Document their usage patterns and dependencies

3. **Add Performance Benchmarks**
   - Leverage existing `Benchmarks` project for performance testing
   - Establish baseline metrics for build time (2.38s), API response times, WASM load times

### 🟢 Nice to Have

1. **Update Build Time Metrics**
   - Current build time: 2.38 seconds (excellent)
   - Document as performance baseline in CI/CD pipeline

2. **Enhance Constants Coverage**
   - Consider adding constants for:
     - Session cookie name
     - Hosted service names
     - Cache expiration times

---

## Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | October 2025 | Original | Initial migration documentation |
| 2.0 | February 27, 2026 | Updated | Corrected HttpClient name, namespace pattern, added WebsiteHostingController docs, updated completion status to 98% |
| **2.1** | **February 27, 2026** | **Comprehensive Update** | **Added full solution analysis: corrected HttpClientName to "UiClient", documented all additional projects (Tools, Logging, DotnetInstaller, SourceGenerators, Benchmarks), added ITreeContentService to service placement table, updated constants coverage documentation, added build verification section with actual build time (2.38s), marked Uiz-Old for removal, added Recommendations section** |
| **2.2** | **February 27, 2026** | **Controller Pattern Analysis** | **Added Section 8: Controller Result Pattern Migration - analyzed all 6 controllers (45% consistency), documented 11 deviations across 5 controllers, created phased migration plan with estimated timeline (6 hours total)** |
| **2.3** | **February 27, 2026** | **ApiResult Base Pattern Design** | **Added Section 9: Unified ApiResult architecture with IApiResult interface, ApiResultBase<T> generic base class, and ApiResult non-generic base class; removed API versioning requirement (product not released); added Phase 0 for creating foundation (30 minutes) before controller migrations** |
| **2.4** | **February 28, 2026** | **Refined Result Pattern Architecture** | **Updated Section 9: Refined result type decisions - OperationResult to be REMOVED (replaced by ApiResult base), AuthenticationResult and InstallationResult KEPT as specialized wrappers extending base classes; created new OperationResultData<T> for semantic clarity; updated Phase 0 estimate from 30 min to 75 min with detailed task breakdown; added decision matrix showing which types to keep vs remove** |
| **2.5** | **February 28, 2026** | **Service Layer Result Pattern Architecture** | **Added Section 10: New 4-layer architecture where services return ApiResult<T>, controllers are thin wrappers always returning HTTP 200; added detailed examples for all layers (services, controllers, client proxies, UI components); updated migration timeline to include Phase 0.5 (service layer updates - 3 hours); total time ~6.5 hours (1 working day)** |
| **2.6** | **February 28, 2026** | **Ui.Client Service Layer Impact Analysis** | **Added Section 11: Detailed analysis of all Ui.Client service proxies with current state issues, required changes for each service (WebSiteHostingService, DotnetVersionService, FileSystemService, FrameworkManagementService, AuthenticationService), HttpClientExtensions additions needed; updated timeline to include Phase 2 (Ui.Client proxies - 1.5 hours) and Phase 3 (UI components - 1 hour); total time ~8 hours** |
| **2.7** | **February 28, 2026** | **ApiResult Pattern Implementation Complete** | **Implemented full ApiResult architecture: Created ApiResult.cs and ApiResultItems.cs (renamed from ApiResultData), removed legacy OperationResult types, updated all 6 controllers to return HTTP 200 with result objects, refactored Ui.Services and Ui.Client.Services to use unified pattern. Net change: -206 lines across 33 files. All planned phases (0, 0.5, 1, 2) completed.** |
| **2.8** | **March 1, 2026** | **HttpClient Extension Methods Enhancement** | **Added typed default value support to HttpClientExtensions: Created GetJsonAsyncWithDefault<T>(), PostJsonAsyncWithDefault<TRequest, T>(), DeleteJsonAsyncWithDefault<T>() using .NET 10 extension method syntax. All Ui.Client service proxies migrated to use new methods with factory functions for null handling.** |
| **2.9** | **March 1, 2026** | **Derived Result Types Planning** | **Added upcoming migration phase for consistency: Plan to create 7 derived result types (ApiResultBool, InstalledVersionsResult, ChannelsResult, ReleasesResult, SharedFoldersResult, DirectoryContentsResult, DirectoryFilesResult) to replace generic ApiResultItems<TItem> patterns. Estimated time: 25-30 minutes.** |
| **2.10** | **March 7, 2026** | **File System Abstraction & Result Type Refactoring** | **Replaced FileSystemItem with FsEntry (added IsDirectory property), renamed SetHttpGroupPermissionsRequest to AclPermissions for ACL clarity, updated all result types to use FsEntry, added directoryOnly parameter to GetDirectoryContentsAsync() for server-side filtering, created UriExtensions.cs with .NET 14 extension() syntax. Additionally refactored ApiResultData<T> → ApiResultItems<TItem>, renamed .Data property to .Items across all result types, enforced List<TItem> pattern at compile-time. Net change: +3 new files (FsEntry.cs, AclPermissions.cs, UriExtensions.cs), -2 deleted files (FileSystemItem.cs, SetHttpGroupPermissionsRequest.cs), 1 base class renamed.** |

---

## Summary: Result Pattern Migration Plan (Updated March 7, 2026)

### ✅ COMPLETED - ApiResult Pattern Implementation

**All phases implemented successfully**:

- ✅ **Phase 0**: Created `ApiResult` and `ApiResultItems<TItem>` foundation types
- ✅ **Phase 0.5**: Updated all Ui.Services to return `ApiResult<T>`
- ✅ **Phase 1**: Simplified all 6 controllers to thin wrappers returning HTTP 200
- ✅ **Phase 2**: Updated all Ui.Client.Service proxies to use `ApiResult<T>`

### 🔄 IN PROGRESS - HttpClient Extension Methods Enhancement

**Completed**:

- ✅ Created typed default value extension methods using .NET 10 syntax
- ✅ Migrated all Ui.Client service proxies to use new methods
- ✅ All null handling uses factory functions for proper error preservation

### ✅ COMPLETED - Result Type Refactoring (March 7, 2026)

**Completed**:

- ✅ Renamed `ApiResultData<T>` → `ApiResultItems<TItem>` for clearer intent
- ✅ Changed `.Data` property to `.Items` across all result types
- ✅ Enforced `List<TItem>` pattern at compile-time (no more generic `T`)
- ✅ Updated 7 result classes and all usages in UI components

### 📋 PLANNED - Derived Result Types Migration (March 2026)

**Goal**: Replace generic `ApiResultItems<TItem>` with specific derived types for consistency.

**Planned Changes**:

- Create `ApiResultBool` - For boolean API operations
- Create `InstalledVersionsResult` - For framework version queries
- Create `ChannelsResult` - For ASP.NET channel queries
- Create `ReleasesResult` - For release status queries
- Create `SharedFoldersResult` - For file system folder queries
- Create `DirectoryContentsResult` - For directory contents queries
- Create `DirectoryFilesResult` - For file list queries

**Impact**: 7 new types, 8 service methods to migrate, ~25-30 minutes.
│ UI Components            │ if (result.Success)      │ ✅
└─────────────────────────────────────────────────────┘

```

**Unified Result Types (Current)**:
- `ApiResult` - Non-generic success/failure (no data)
- `ApiResultData<T>` - Generic with typed Data property
- `AuthenticationResult` - Auth-specific (extends ApiResult)
- `InstallationResult` - Domain properties (extends ApiResult)

**Unified Result Types (After Derived Migration)**:
- All above + 7 new derived types for consistency:
  - `ApiResultBool` - Boolean operations
  - `InstalledVersionsResult` - FrameworkInfo lists
  - `ChannelsResult` - AspNetChannel lists
  - `ReleasesResult` - AspNetRelease lists
  - `SharedFoldersResult` - FileSystemItem lists
  - `DirectoryContentsResult` - FileStationFile queryables
  - `DirectoryFilesResult` - FileStationFile lists

**Removed Legacy Types**:
- ❌ `OperationResult` - Replaced by `ApiResult`
- ❌ `InstallFrameworkModel.cs` (Results folder) - Moved to API/Parameters
- ❌ `AuthenticationStatus.cs` - No longer needed

### Key Benefits Achieved

- ✅ **Client code simplicity**: Simple `if (result.Success)` pattern, no try/catch for business errors
- ✅ **Controller simplicity**: Always return `Ok(result)`, no error handling logic
- ✅ **Service clarity**: Success/failure as first-class concepts in contracts
- ✅ **Ui.Client consistency**: All services use same result pattern - no silent failures
- ✅ **HTTP 200 everywhere**: Perfect fit for session-based auth constraint across entire stack
- ✅ **Reduced duplication**: From 4 independent types to 2 base classes + 3 wrappers

### Actual Implementation Time

**Total**: ~8 hours (1 working day) - **On schedule!**

| Phase | Estimated | Actual | Status |
|-------|-----------|--------|--------|
| Phase 0: Foundation | 75 min | ✅ Complete | ✅ |
| Phase 0.5: Ui.Services | 3 hours | ✅ Complete | ✅ |
| Phase 1: Controllers | 1 hour | ✅ Complete | ✅ |
| Phase 2: Ui.Client.Proxies | 1.5 hours | ✅ Complete | ✅ |
| Phase 3: UI Components | 1 hour | N/A* | - |

*Phase 3 not needed - UI components already use simple result checks

---

## Remaining Work (Unchanged)

### ⚠️ Pending Tasks

| Task | Status | Notes |
|------|--------|-------|
| **File System Abstraction Refactoring** | ✅ Complete | FsEntry, AclPermissions, UriExtensions created; all result types updated |
| **Result Type Refactoring** | ✅ Complete | ApiResultData → ApiResultItems renamed, .Data → .Items property changed across all result types |
| **Derived Result Types Migration** | ✅ Complete | All 7 derived types (ApiResultBool, InstalledVersionsResult, ChannelsResult, ReleasesResult, SharedFoldersResult, DirectoryContentsResult, DirectoryFilesResult) implemented |
| **Remove Uiz-Old Project** | 📋 Planned | 26 C# files to delete after final verification |
| **Integration Tests** | 🔄 In Progress | End-to-end testing of all features |
| **UI Polish** | 📋 Planned | Minor visual improvements for dialogs |

### Estimated Time to Completion

| Phase | Days | Notes |
|-------|------|-------|
| Remove Uiz-Old project | <0.5 | Cleanup after verification |
| Integration tests & validation | 2-3 | End-to-end testing of all features |
| UI polish for dialogs | <1 | Minor visual improvements only |
| **Total** | **2-3 days remaining** | Assuming 1 developer (FINAL - no code stubs needed) |

---

## Migration Progress Summary

**Overall Completion**: **98%** ✅

**Code Quality Metrics**:
- Build time: 2.77 seconds (excellent)
- Build errors: 0
- Build warnings: 0
- Code reduction: -206 lines in latest refactoring
- Test coverage: In progress

**Architecture Status**:
- ✅ Split architecture (Ui + Ui.Client + Data) fully implemented
- ✅ Service façade pattern complete across all layers
- ✅ REST API controllers using consistent result patterns
- ✅ Session-based authentication with [AuthorizeSession] attribute
- ✅ Constants-based routing (25 constant files)
- ✅ HttpClient configuration standardized ("UiClient" named client)

**Next Milestone**: Integration test completion and Uiz-Old project removal

---

**End of Document**
