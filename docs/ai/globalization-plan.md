# Globalization (Multi-Language) Plan

## Status: Planning

---

## 1. Goals

- Support multiple languages in the UI (Blazor Interactive WebAssembly)
- Default language: **English (en-US)**
- Target languages: **French (fr-FR)**
- Culture-aware formatting (dates, numbers, currencies)
- Runtime language switching **without full page reload**
- Persist user's language preference in `localStorage`
- **Centralize all user-facing strings** in a dedicated globalization assembly
- **Eliminate user-facing `const string`** from `ApplicationConstants` and `ValidationConstants`

---

## 2. Architecture Decisions

### 2.1 Approach: Dedicated Globalization Assembly + .NET Built-in Localization

A new assembly `Askyl.Dsm.WebHosting.Globalization` will own all user-facing strings and localization infrastructure.

**Rationale:**
- **Separation of concerns**: UI projects (`Ui`, `Ui.Client`) don't manage localization — they consume it
- **Shared resource files**: Server and WASM use the same `.resx` files → consistent messages across HTTP API boundary
- **Zero project references**: `Globalization` depends only on `Microsoft.Extensions.Localization` (NuGet)
- **No third-party libraries**: .NET 10's native `loadAllSatelliteResources` eliminates the need for `Blazor.WebAssembly.DynamicCulture`
- **Compile-time safety**: Dummy class + `IStringLocalizer<T>` pattern (no `.Designer.cs` files — they cause namespace collisions with `ResourcesPath`)

### 2.2 Culture Management Strategy

| Concern | Decision | Rationale |
|---|---|---|
| **Culture propagation** | `ICultureManager` service (scoped, shared between server & WASM) | Blazor Interactive WebAssembly requires culture set on both sides |
| **Persistence** | Browser `localStorage` (client) + `Accept-Language` header (server fallback) | Survives page reloads; no server-side storage needed |
| **Runtime switching** | `ICultureManager.SetCultureAsync(string)` → event-based re-render | No page reload; components subscribe to culture change events |
| **Supported cultures** | Explicit list in `LocalizationOptions` at startup | Prevents invalid culture requests |
| **Satellite assemblies** | `loadAllSatelliteResources: true` in `index.html` | .NET 10 native feature — all cultures loaded at startup |

### 2.3 Resource File Organization

```
Askyl.Dsm.WebHosting.Globalization/
├── Resources/
│   ├── SharedResource.resx            # Default (en-US) — fallback
│   ├── SharedResource.fr-FR.resx      # French translations
│   └── SharedResource.cs              # Dummy class (NO Designer.cs)
├── LocalizationKeys.cs                # Key constants (not values!)
└── GlobalizationServiceCollectionExtensions.cs  # DI registration
```

**Naming Convention:**
- Keys use `PascalCase`, grouped by domain prefix:
  - `Login_PageTitle`, `Login_DialogTitle`, `Login_LoginLabel`
  - `Home_PageTitle`, `Home_AddButton`, `Home_EditButton`
  - `Common_OK`, `Common_Cancel`, `Common_Close`, `Common_Save`, `Common_Delete`
  - `Error_FailedToLoadWebsites`, `Error_FailedToStartWebsite`
  - `Validation_PathRequired`, `Validation_PathTraversalDetected`

### 2.4 Dependency Graph (After)

```
┌──────────────────────────────────────────────────────────┐
│              Askyl.Dsm.WebHosting.Globalization           │
│  (NuGet: Microsoft.Extensions.Localization only)         │
│                                                          │
│  Resources/                                              │
│  ├── SharedResource.resx          (en-US default)        │
│  ├── SharedResource.fr-FR.resx    (French)               │
│  └── SharedResource.cs            (dummy class)          │
│                                                          │
│  LocalizationKeys.cs                                    │
│  GlobalizationServiceCollectionExtensions.cs            │
└──────────┬───────────────────────────────────────┬───────┘
           │                                       │
    ┌──────▼──────┐                       ┌────────▼────────┐
    │  Ui (Server) │                       │  Ui.Client (WASM)│
    │              │                       │                 │
    │ Services/    │                       │ Components/     │
    │ (use         │                       │ (use            │
    │  IString     │                       │  IString        │
    │  Localizer)  │                       │  Localizer)     │
    └──────────────┘                       └─────────────────┘
           │                                       │
           └──────────┬────────────────────────────┘
                      │
           ┌──────────▼──────────┐
           │  Constants (Shared)  │
           │  (technical only:    │
           │   paths, timeouts,   │
           │   session keys,      │
           │   route names)       │
           └─────────────────────┘
```

**Key principle**: `Globalization` has **zero project references**. Both `Ui` and `Ui.Client` reference it independently. No circular dependencies possible.

### 2.5 Message Flow (Before vs After)

**Before (hardcoded, unlocalizable):**
```
Server (Ui)                          HTTP API                      WASM (Ui.Client)
─────────────                       ──────────                    ──────────────────
SiteLifecycleManager                ApiResult                     Home.razor
  .CreateFailure(                     .Message =                  ToastService.ShowError(
    "Site configuration                "Site configuration          result.Message ??
     is being updated")               is being updated")            "Failed to start website")
```

**After (localized end-to-end):**
```
Server (Ui)                          HTTP API                      WASM (Ui.Client)
─────────────                       ──────────                    ──────────────────
SiteLifecycleManager                ApiResult                     Home.razor
  _localizer[                          .Message =                  ToastService.ShowError(
    Error.SiteConfig                   "La configuration du         result.Message ??
    Updating] →                         site est en cours           _localizer[
    "La configuration                   de mise à jour"]             Error.FailedToStart]
     du site est en
     cours de mise à jour"
```

### 2.6 What Stays in Constants vs What Moves to Globalization

| Keep in `Constants` | Move to `Globalization` |
|---|---|
| `HttpClientName = "UiClient"` | `"Authentication failed"` |
| `HttpClientTimeoutSeconds = 15` | `"Authentication successful"` |
| `ApplicationUrlSubPath = "/adwh"` | `"The operation failed. Check the logs for details."` |
| `SessionTimeoutMinutes = 30` | `"Failed to load websites"` |
| `DsmSessionKey = "DsmSid"` | `"Site configuration is being updated"` |
| `DoubleClickTimeoutMilliseconds = 400` | `"Path is required"` |
| `RuntimesRootPath = "../runtimes"` | `"Invalid path: traversal sequences are not allowed"` |
| Route names (`/adwh/api/...`) | `"Version is required"` |
| Dialog widths (`"60%"`, `"75%"`) | All UI labels, titles, button texts |

---

## 3. Implementation Phases

### Phase 1: Globalization Assembly Foundation

- [ ] Create `Askyl.Dsm.WebHosting.Globalization` project (class library, `net10.0`)
- [ ] Add NuGet: `Microsoft.Extensions.Localization`
- [ ] Create `Resources/SharedResource.resx` (en-US default) with ALL user-facing strings
- [ ] Create `Resources/SharedResource.fr-FR.resx` (French translations — empty initially)
- [ ] Create `Resources/SharedResource.cs` (dummy class, no Designer)
- [ ] Create `LocalizationKeys.cs` (key constants organized by domain)
- [ ] Create `GlobalizationServiceCollectionExtensions.cs` (DI registration)
- [ ] Add project references: `Ui → Globalization`, `Ui.Client → Globalization`
- [ ] Configure `.csproj`: `<BlazorWebAssemblyLoadAllGlobalizationData>true</BlazorWebAssemblyLoadAllGlobalizationData>`

### Phase 2: Migrate Strings from Constants → Resources

- [ ] Move user-facing strings from `ApplicationConstants` → `SharedResource.resx`
  - `PlatformNotSupportedErrorMessage`
  - `AuthenticationFailedErrorMessage`
  - `AuthenticationSuccessfulMessage`
  - `OperationFailedErrorMessage`
  - `RateLimitExceededErrorMessage`
  - `FailedToLoadDirectoryContentsErrorMessage`
  - `LoadingSharedFoldersMessage`
  - `LoadingDirectoryContentsMessage`
- [ ] Move user-facing strings from `ValidationConstants` → `SharedResource.resx`
  - `PathRequired`
  - `PathTraversalDetected`
  - `InvalidVersionFormat`
  - `OperationFailed`
  - `EnvVarKeyTooLong`
  - `EnvVarValueTooLong`
- [ ] Keep technical constants in `Constants` (paths, timeouts, session keys, routes)
- [ ] Update server services to use `IStringLocalizer` instead of `ApplicationConstants`

### Phase 3: Migrate Server-Side Service Messages

- [ ] `SiteLifecycleManager.cs` (Server): Replace hardcoded `CreateFailure(...)` → `_localizer[...]`
  - "Site configuration is being updated"
  - "Failed to queue start command"
  - "Failed to queue stop command"
  - "Site '{name}' is already running"
  - "Application binary not found: {path}"
  - "Incompatible framework"
- [ ] `WebSiteHostingService.cs` (Server): Replace hardcoded `CreateFailure(...)` → `_localizer[...]`
  - "Instance not found"
  - "No application path configured"
  - "Site with ID '{id}' not found"
- [ ] `FrameworkManagementService.cs` (Server): Replace hardcoded `CreateFailure(...)` → `_localizer[...]`
  - "Version is required"
- [ ] `FileSystemService.cs` (Server): Replace hardcoded `CreateFailure(...)` → `_localizer[...]`
  - "Failed to set ACL permissions for {path}..."
- [ ] `AuthenticationService.cs` (Server): Replace hardcoded `CreateFailure(...)` → `_localizer[...]`

### Phase 4: Migrate WASM Client Service Messages

- [ ] `WebSiteHostingService.cs` (WASM): Replace fallback `CreateFailure(...)` → `_localizer[...]`
  - "Failed to load websites", "Failed to add website", etc.
- [ ] `FileSystemService.cs` (WASM): Replace fallback `CreateFailure(...)` → `_localizer[...]`
- [ ] `DotnetVersionService.cs` (WASM): Replace fallback `CreateFailure(...)` → `_localizer[...]`
- [ ] `FrameworkManagementService.cs` (WASM): Replace fallback `CreateFailure(...)` → `_localizer[...]`
- [ ] `AuthenticationService.cs` (WASM): Replace fallback `CreateFailure(...)` → `_localizer[...]`

### Phase 5: UI Components Localization (`.razor` files)

- [ ] Localize `Pages/Login.razor` (labels, messages, titles, page title)
- [ ] Localize `Pages/Home.razor` (toolbar buttons, grid columns, toast fallbacks, page title)
- [ ] Localize `Pages/NotFound.razor` (page title, content)
- [ ] Localize `Dialogs/WebSiteConfigurationDialog.razor` (all labels, buttons, section headers)
- [ ] Localize `Dialogs/AspNetReleasesDialog.razor` (all labels, buttons, column titles)
- [ ] Localize `Dialogs/FileSelectionDialog.razor` (loading messages, error fallbacks)
- [ ] Localize `Dialogs/DotnetVersionsDialog.razor` (if exists)
- [ ] Localize `Dialogs/LicensesDialog.razor` (if exists)
- [ ] Localize `Controls/AutoDataGrid.razor` ("Loading items...", "No items found.", "Items : {count}")
- [ ] Localize `Controls/LoadingOverlay.razor` (if has user-facing strings)
- [ ] Localize `Controls/RealTimeNumberField.razor` (if has user-facing strings)
- [ ] Localize `Controls/RealTimeTextField.razor` (if has user-facing strings)

### Phase 6: Culture Manager & Runtime Switching

- [ ] Create `ICultureManager` interface in `Globalization` assembly
- [ ] Implement `CultureManager` for WASM (localStorage persistence, JS interop)
- [ ] Implement `CultureManager` for Server (Accept-Language header, session)
- [ ] Register `ICultureManager` in both `Program.cs` files
- [ ] Configure `loadAllSatelliteResources: true` in `wwwroot/index.html`
- [ ] Set culture at app startup based on: localStorage → browser Accept-Language → default

### Phase 7: Language Selector UI

- [ ] Add language selector dropdown to `MainLayout.razor` header
- [ ] Wire up `ICultureManager.SetCultureAsync()` on selection change
- [ ] Persist selection to `localStorage`
- [ ] Visual feedback (current language highlighted)
- [ ] Trigger component re-render on culture change (event-based)

### Phase 8: Culture-Aware Formatting

- [ ] Ensure dates render per culture (FluentUI date components)
- [ ] Ensure numbers render per culture
- [ ] Verify pluralization rules work per culture

### Phase 9: Testing & Validation

- [ ] Test language switching at runtime (no reload)
- [ ] Test persistence across page reloads
- [ ] Test fallback to English for missing translations
- [ ] Test browser culture detection on first visit
- [ ] Test server-side messages are localized in API responses
- [ ] Build passes with no warnings
- [ ] Format passes (`dotnet format`)

---

## 4. Complete String Inventory

> **Source**: Full audit of all user-facing strings across `Ui`, `Ui.Client`, and `Constants` projects.

### 4.1 Pages

#### `Pages/Login.razor`

| String | Key | Context |
|---|---|---|
| `ADWH - Login` | `Login_PageTitle` | `<PageTitle>` |
| `Authentication (DSM account)` | `Login_DialogTitle` | Dialog header |
| `Login:` | `Login_LoginLabel` | Username field label |
| `Password:` | `Login_PasswordLabel` | Password field label |
| `OTP:` | `Login_OTPLabel` | OTP field label |
| `OK` | `Common_OK` | Submit button |
| `Authenticating...` | `Login_Authenticating` | Loading overlay |

#### `Pages/Home.razor`

| String | Key | Context |
|---|---|---|
| `ADWH - Home` | `Home_PageTitle` | `<PageTitle>` |
| `Add` | `Home_AddButton` | Toolbar button |
| `Edit` | `Home_EditButton` | Toolbar button |
| `Delete` | `Common_Delete` | Toolbar button |
| `.NET Version` | `Home_DotnetVersionButton` | Toolbar button |
| `ASP.NET Online` | `Home_AspNetOnlineButton` | Toolbar button |
| `Licenses` | `Home_LicensesButton` | Toolbar button |
| `Download Logs` | `Home_DownloadLogsButton` | Toolbar button |
| `Logout` | `Home_LogoutButton` | Toolbar button |
| `Name` | `Home_GridColumnName` | Grid column title |
| `Path` | `Home_GridColumnPath` | Grid column title |
| `Framework` | `Home_GridColumnFramework` | Grid column title |
| `Internal Port` | `Home_GridColumnInternalPort` | Grid column title |
| `State` | `Home_GridColumnState` | Grid column title |
| `Failed to delete website` | `Error_FailedToDeleteWebsite` | Toast fallback |
| `Failed to start website` | `Error_FailedToStartWebsite` | Toast fallback |
| `Failed to stop website` | `Error_FailedToStopWebsite` | Toast fallback |
| `Failed to logout` | `Error_FailedToLogout` | Toast fallback |

#### `Pages/NotFound.razor`

| String | Key | Context |
|---|---|---|
| `Not found` | `NotFound_PageTitle` | `<PageTitle>` |
| `Ouiiinnnn` | `NotFound_Content` | Page content (placeholder) |

### 4.2 Dialogs

#### `Dialogs/WebSiteConfigurationDialog.razor`

| String | Key | Context |
|---|---|---|
| `Edit Website` | `WebsiteConfig_EditTitle` | Dialog header (edit mode) |
| `Add Website` | `WebsiteConfig_AddTitle` | Dialog header (add mode) |
| `Name` | `WebsiteConfig_NameLabel` | Field label |
| `Application Settings` | `WebsiteConfig_AppSettingsSection` | Section header |
| `Application Path` | `WebsiteConfig_AppPathLabel` | Field label |
| `Environment` | `WebsiteConfig_EnvironmentLabel` | Field label |
| `Internal Port` | `WebsiteConfig_InternalPortLabel` | Field label |
| `Shutdown Timeout (seconds)` | `WebsiteConfig_ShutdownTimeoutLabel` | Field label |
| `Enabled` | `WebsiteConfig_EnabledLabel` | Checkbox label |
| `Auto Start` | `WebsiteConfig_AutoStartLabel` | Checkbox label |
| `Reverse Proxy` | `WebsiteConfig_ReverseProxySection` | Section header |
| `Hostname` | `WebsiteConfig_HostnameLabel` | Field label |
| `Protocol` | `WebsiteConfig_ProtocolLabel` | Field label |
| `Public Port` | `WebsiteConfig_PublicPortLabel` | Field label |
| `Enable HSTS` | `WebsiteConfig_EnableHSTSLable` | Checkbox label |
| `Cancel` | `Common_Cancel` | Button |
| `Update` | `WebsiteConfig_UpdateButton` | Submit button (edit mode) |
| `Create` | `WebsiteConfig_CreateButton` | Submit button (add mode) |
| `Application path is required...` | `Error_ApplicationPathRequired` | Dialog error |

#### `Dialogs/AspNetReleasesDialog.razor`

| String | Key | Context |
|---|---|---|
| `ASP.NET Online` | `AspNetReleases_DialogTitle` | Dialog header |
| `Close` | `Common_Close` | Button |
| `Channel` | `AspNetReleases_ChannelLabel` | Label |
| `Refresh` | `Common_Refresh` | Button title |
| `Select a version` | `AspNetReleases_SelectVersion` | Action button text |
| `Install {version}` | `AspNetReleases_InstallVersion` | Action button text (template) |
| `Uninstall {version}` | `AspNetReleases_UninstallVersion` | Action button text (template) |
| `Version` | `AspNetReleases_GridColumnVersion` | Grid column title |
| `Installed` | `AspNetReleases_GridColumnInstalled` | Grid column title |
| `Security` | `AspNetReleases_GridColumnSecurity` | Grid column title |
| `Release` | `AspNetReleases_GridColumnRelease` | Grid column title |
| `Failed to load channels` | `Error_FailedToLoadChannels` | Toast fallback |
| `Failed to load releases` | `Error_FailedToLoadReleases` | Toast fallback |
| `Installation completed successfully` | `Success_InstallationCompleted` | Toast success |
| `Installation failed` | `Error_InstallationFailed` | Toast error |
| `Uninstallation completed successfully` | `Success_UninstallationCompleted` | Toast success |
| `Uninstallation failed` | `Error_UninstallationFailed` | Toast error |

#### `Dialogs/FileSelectionDialog.razor`

| String | Key | Context |
|---|---|---|
| `Loading shared folders...` | `Loading_SharedFolders` | Working state message |
| `Loading directory contents...` | `Loading_DirectoryContents` | Working state message |
| `Failed to load shared folders` | `Error_FailedToLoadSharedFolders` | Dialog error fallback |
| `Failed to load directory contents` | `Error_FailedToLoadDirectoryContents` | Dialog error fallback |

### 4.3 Controls

#### `Controls/AutoDataGrid.razor`

| String | Key | Context |
|---|---|---|
| `Loading items...` | `AutoDataGrid_Loading` | Loading content |
| `No items found.` | `AutoDataGrid_Empty` | Empty content |
| `Items : {count}` | `AutoDataGrid_ItemsCount` | Toolbar label (template) |

### 4.4 Server-Side Service Messages (bubble up via API → `.Message`)

#### `Services/SiteLifecycleManager.cs` (Server)

| String | Key | Context |
|---|---|---|
| `Site configuration is being updated` | `Error_SiteConfigUpdating` | Start/stop blocked |
| `Failed to queue start command` | `Error_FailedToQueueStart` | Queue failure |
| `Failed to queue stop command` | `Error_FailedToQueueStop` | Queue failure |
| `Site '{name}' is already running` | `Error_SiteAlreadyRunning` | Start when running |
| `Application binary not found: {path}` | `Error_ApplicationBinaryNotFound` | Start failure |
| `Incompatible framework` | `Error_IncompatibleFramework` | Runtime mismatch |

#### `Services/WebSiteHostingService.cs` (Server)

| String | Key | Context |
|---|---|---|
| `Instance not found` | `Error_InstanceNotFound` | Remove failure |
| `No application path configured` | `Error_NoApplicationPath` | Start failure |
| `Site with ID '{id}' not found` | `Error_SiteNotFound` | Update/remove failure |

#### `Services/FrameworkManagementService.cs` (Server)

| String | Key | Context |
|---|---|---|
| `Version is required` | `Validation_VersionRequired` | Install/uninstall validation |

#### `Services/FileSystemService.cs` (Server)

| String | Key | Context |
|---|---|---|
| `Failed to set ACL permissions for {path}...` | `Error_FailedToSetACL` | Permission failure |

### 4.5 WASM Client Service Messages (fallback when API call fails)

#### `Services/WebSiteHostingService.cs` (WASM)

| String | Key | Context |
|---|---|---|
| `Failed to load websites` | `Error_FailedToLoadWebsites` | HTTP fallback |
| `Failed to add website` | `Error_FailedToAddWebsite` | HTTP fallback |
| `Failed to update website` | `Error_FailedToUpdateWebsite` | HTTP fallback |
| `Failed to remove website` | `Error_FailedToRemoveWebsite` | HTTP fallback |
| `Failed to start website` | `Error_FailedToStartWebsite` | HTTP fallback |
| `Failed to stop website` | `Error_FailedToStopWebsite` | HTTP fallback |

#### `Services/FileSystemService.cs` (WASM)

| String | Key | Context |
|---|---|---|
| `Failed to load shared folders` | `Error_FailedToLoadSharedFolders` | HTTP fallback |
| `Failed to load directory contents for path: {path}` | `Error_FailedToLoadDirectoryContentsWithPath` | HTTP fallback |

#### `Services/DotnetVersionService.cs` (WASM)

| String | Key | Context |
|---|---|---|
| `Failed to load installed versions` | `Error_FailedToLoadInstalledVersions` | HTTP fallback |
| `Failed to check if channel '{channel}' is installed` | `Error_FailedToCheckChannelInstalled` | HTTP fallback |
| `Failed to load available channels` | `Error_FailedToLoadChannels` | HTTP fallback |
| `Failed to load releases for channel '{channel}'` | `Error_FailedToLoadReleasesForChannel` | HTTP fallback |

#### `Services/FrameworkManagementService.cs` (WASM)

| String | Key | Context |
|---|---|---|
| `Failed to install framework` | `Error_FailedToInstallFramework` | HTTP fallback |

#### `Services/AuthenticationService.cs` (WASM)

| String | Key | Context |
|---|---|---|
| `Failed to login` | `Error_FailedToLogin` | HTTP fallback |
| `Unknown error` | `Error_Unknown` | HTTP fallback |
| `Failed to check authentication status` | `Error_FailedToCheckAuthStatus` | HTTP fallback |

### 4.6 Strings Currently in `ApplicationConstants` (to migrate)

| Current Constant | New Key | Context |
|---|---|---|
| `PlatformNotSupportedErrorMessage` | `Error_PlatformNotSupported` | Dialog on login |
| `AuthenticationFailedErrorMessage` | `Error_AuthenticationFailed` | Toast on login failure |
| `AuthenticationSuccessfulMessage` | `Success_AuthenticationSuccessful` | Toast on login success |
| `OperationFailedErrorMessage` | `Error_OperationFailed` | Generic server fallback |
| `RateLimitExceededErrorMessage` | `Error_RateLimitExceeded` | Toast on rate limit |
| `FailedToLoadDirectoryContentsErrorMessage` | `Error_FailedToLoadDirectoryContents` | Dialog error |
| `LoadingSharedFoldersMessage` | `Loading_SharedFolders` | Working state |
| `LoadingDirectoryContentsMessage` | `Loading_DirectoryContents` | Working state |

### 4.7 Strings Currently in `ValidationConstants` (to migrate)

| Current Constant | New Key | Context |
|---|---|---|
| `PathRequired` | `Validation_PathRequired` | Server validation |
| `PathTraversalDetected` | `Validation_PathTraversalDetected` | Server validation |
| `InvalidVersionFormat` | `Validation_InvalidVersionFormat` | Server validation |
| `OperationFailed` | `Error_OperationFailed` | Server fallback (duplicate of ApplicationConstants) |
| `EnvVarKeyTooLong` | `Validation_EnvVarKeyTooLong` | Server validation (template) |
| `EnvVarValueTooLong` | `Validation_EnvVarValueTooLong` | Server validation (template) |

### 4.8 Common/Shared Strings

| String | Key | Context |
|---|---|---|
| `OK` | `Common_OK` | Buttons |
| `Cancel` | `Common_Cancel` | Buttons |
| `Close` | `Common_Close` | Dialog close |
| `Save` | `Common_Save` | Buttons |
| `Delete` | `Common_Delete` | Buttons |
| `Refresh` | `Common_Refresh` | Buttons |
| `Loading...` | `Common_Loading` | Loading states |

---

## 5. Technical Details

### 5.1 Globalization Assembly Structure

```
Askyl.Dsm.WebHosting.Globalization/
├── Askyl.Dsm.WebHosting.Globalization.csproj
│   ├── <TargetFramework>net10.0</TargetFramework>
│   ├── <PackageReference Microsoft.Extensions.Localization />
│   └── <Nullable>enable</Nullable>
├── Resources/
│   ├── SharedResource.resx              # en-US (default, fallback)
│   ├── SharedResource.fr-FR.resx        # French
│   └── SharedResource.cs                # Dummy class (NO Designer.cs)
├── LocalizationKeys.cs                  # Key constants by domain
└── GlobalizationServiceCollectionExtensions.cs  # DI registration
```

### 5.2 Dummy Class Pattern (No Designer.cs)

Per Microsoft docs, when using `ResourcesPath`, Designer files cause namespace collisions. We use a dummy class:

```csharp
// Resources/SharedResource.cs
namespace Askyl.Dsm.WebHosting.Globalization.Resources;

/// <summary>
/// Dummy class for IStringLocalizer&lt;T&gt; type parameter.
/// Do not add members — translations are accessed via LocalizationKeys.
/// </summary>
public static class SharedResource { }
```

### 5.3 LocalizationKeys.cs

```csharp
namespace Askyl.Dsm.WebHosting.Globalization;

public static class LocalizationKeys
{
    public static class Common
    {
        public const string OK = nameof(OK);
        public const string Cancel = nameof(Cancel);
        public const string Close = nameof(Close);
        // ...
    }

    public static class Login
    {
        public const string PageTitle = nameof(PageTitle);
        public const string DialogTitle = nameof(DialogTitle);
        // ...
    }

    public static class Error
    {
        public const string FailedToLoadWebsites = nameof(FailedToLoadWebsites);
        public const string OperationFailed = nameof(OperationFailed);
        // ...
    }

    public static class Validation
    {
        public const string PathRequired = nameof(PathRequired);
        public const string PathTraversalDetected = nameof(PathTraversalDetected);
        // ...
    }
}
```

### 5.4 Server-Side Configuration (Ui/Program.cs)

```csharp
// Register globalization services
builder.Services.AddGlobalization();

var supportedCultures = new[]
{
    new CultureInfo("en-US"),
    new CultureInfo("fr-FR")
};

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("en-US");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

// ... later in pipeline ...
app.UseRequestLocalization();
```

### 5.5 Client-Side Configuration (Ui.Client/Program.cs)

```csharp
// Register globalization services
builder.Services.AddGlobalization();

// Load culture from localStorage at startup
builder.Services.AddScoped(sp =>
{
    var cultureManager = sp.GetRequiredService<ICultureManager>();
    _ = cultureManager.InitializeAsync(); // fire-and-forget
    return cultureManager;
});
```

### 5.6 WASM Satellite Assembly Loading (wwwroot/index.html)

```html
<!-- .NET 10: load ALL satellite assemblies at startup -->
<script src="_framework/blazor.webassembly.js" autostart="false"></script>
<script>
    Blazor.start({
        loadAllGlobalizationData: true,
        loadAllSatelliteResources: true
    });
</script>
```

### 5.7 ICultureManager Interface

```csharp
public interface ICultureManager
{
    CultureInfo CurrentUICulture { get; }
    CultureInfo CurrentCulture { get; }
    IReadOnlyList<CultureInfo> SupportedCultures { get; }
    Task InitializeAsync();
    Task SetCultureAsync(string cultureName);
    event EventHandler<CultureInfo> CultureChanged;
}
```

### 5.8 Resource Access Patterns

**In `.razor` files:**
```csharp
@inject IStringLocalizer<SharedResource> T

<FluentButton>@T[LocalizationKeys.Common.OK]</FluentButton>
<FluentLabel>@T[LocalizationKeys.Login.LoginLabel]</FluentLabel>
```

**In C# services (server-side, for API response messages):**
```csharp
private readonly IStringLocalizer<SharedResource> _localizer;

public MyService(IStringLocalizer<SharedResource> localizer)
{
    _localizer = localizer;
}

public ApiResult DoSomething()
{
    return ApiResult.CreateFailure(_localizer[LocalizationKeys.Error.OperationFailed]);
}
```

**In C# services (WASM, for fallback messages):**
```csharp
private readonly IStringLocalizer<SharedResource> _localizer;

public async Task<ApiResult> CallApiAsync()
{
    return await _httpClient.GetJsonOrDefaultAsync<ApiResult>(
        url,
        () => ApiResult.CreateFailure(_localizer[LocalizationKeys.Error.FailedToLoadWebsites]));
}
```

---

## 6. Risks & Considerations

| Risk | Mitigation |
|---|---|
| WASM culture data size | .NET 10 `loadAllSatelliteResources` loads all at startup; ICU data for en-US + fr-FR is ~200KB gzipped |
| Runtime culture switch in Interactive WASM | Event-based re-render via `CultureChanged` event; no page reload |
| Missing translations | English `.resx` is always the fallback; missing keys fall back to key name |
| FluentUI internal strings | Most labels are passed as `Text`/`Label` parameters (we control these); paginator "Page X of Y" remains in English (acceptable) |
| Server messages in API responses | Server uses `IStringLocalizer` → localized strings travel in `.Message` → WASM displays them directly |
| Template strings with placeholders | Use `_localizer[key, arg1, arg2]` overload for strings like `"Site '{0}' is already running"` |

---

## 7. Decisions Log

| Date | Decision | Rationale |
|---|---|---|
| 2026-05-28 | Dedicated `Globalization` assembly | Separation of concerns; shared resources across server + WASM; zero project references |
| 2026-05-28 | Single `SharedResource.resx` | Simpler management; ~100 keys total is manageable in one file |
| 2026-05-28 | `LocalizationKeys.cs` (not Designer.cs) | No namespace collisions; explicit key constants; works with `ResourcesPath` |
| 2026-05-28 | Custom `ICultureManager` (no third-party library) | .NET 10 native `loadAllSatelliteResources` makes `Blazor.WebAssembly.DynamicCulture` unnecessary |
| 2026-05-28 | No page reload on language switch | Event-based re-render; better UX |
| 2026-05-28 | localStorage for persistence | No server dependency; survives reloads; standard browser API |
| 2026-05-28 | Migrate user-facing strings from `Constants` | `const string` cannot be localized; `ApplicationConstants` and `ValidationConstants` should only hold technical values |

---

## 8. Next Steps

1. Begin Phase 1: Create `Askyl.Dsm.WebHosting.Globalization` assembly
2. Populate `SharedResource.resx` with all strings from Section 4
3. Create `SharedResource.fr-FR.resx` with French translations
4. Migrate strings from `ApplicationConstants` and `ValidationConstants`
5. Update server and WASM services to use `IStringLocalizer`
6. Localize all `.razor` components
7. Implement `ICultureManager` and language selector UI
