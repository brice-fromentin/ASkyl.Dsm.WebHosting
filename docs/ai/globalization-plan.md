# Globalization (Multi-Language) Plan

## Status: In Progress (Phase 8 next)

---

## 1. Goals

- Support multiple languages in the UI (Blazor Interactive WebAssembly)
- Default language: **English (en-US)**
- Target languages: **French (fr-FR)**
- Culture-aware formatting (dates, numbers, currencies)
- Runtime language switching **without full page reload**
- **Honor DSM user preferences as the source of truth** (no browser localStorage)
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
| **Persistence** | None — DSM is the single source of truth (system config + per-user API) | No localStorage; language follows DSM preferences |
| **Runtime switching** | `ICultureManager.SetCultureAsync(string)` → event-based re-render | No page reload; components subscribe to culture change events |
| **Supported cultures** | Defined in code (en-US, fr-FR) | Matches available resource files |
| **Satellite assemblies** | `loadAllSatelliteResources: true` in `index.html` | .NET 10 native feature — all cultures loaded at startup |
| **DSM system preferences** | Read from `/etc/synoinfo.conf` at startup (always available, no auth needed) | System language only |
| **DSM user preferences** | Best-effort fetch via `SYNO.Core.UserSettings.get` after login | Per-user language (`Personal.lang`), date/time formats (`Personal.dateFormat`, `Personal.timeFormat`) with `/etc/synoinfo.conf` fallback |

### 2.2.1 DSM User Preferences Integration

**Problem:** The application runs on a Synology NAS where each DSM user has configured language and date/time format preferences. We want to honor these by default rather than forcing English.

**Research findings:**

| DSM API | What it returns | Contains language prefs? |
|---|---|---|
| `SYNO.Core.User.get` | `name`, `uid`, `email`, `groups` | ❌ No |
| `SYNO.Core.User.list` | Same fields + `description`, `expired`, `2fa_status` | ❌ No |
| `SYNO.Core.Desktop.SessionData.getjs` | `SynoToken`, `isAdmin`, `user` | ❌ No |
| `SYNO.Core.System.info` | `time_zone` (e.g. `"America/New_York"`) | ❌ No (system-level only — not used) |
| `SYNO.Core.Region.Language` | `language` (e.g. `"def"`), `maillang` (e.g. `"enu"`) | ❌ No (system-level only — not used) |
| `SYNO.Core.Region.NTP` | `timezone`, `date_format`, `time_format` | ❌ No (system-level only — not used) |
| `SYNO.Core.UserSettings` | All user settings including `Personal.lang`, `Personal.dateFormat`, `Personal.timeFormat` | ✅ **Per-user language + date/time format — used!** (method: `get`, no payload) |

**Verified API responses (from live DSM 7.x instance):**

```json
// SYNO.Core.UserSettings (v1, method: get) — NO payload needed
// Returns ALL user settings (~1400 lines). Only Personal section is relevant:
{
  "data": {
    "Personal": {
      "lang": "ita",
      "dateFormat": "Y/m/d",
      "timeFormat": "h:i a"
    },
    "Desktop": { ... },
    "SYNO.SDS.App.FileStation3.Instance": { ... },
    // ... 100+ other app-specific settings (ignored)
  },
  "success": true
}

// SYNO.Core.Region.Language (v1, method: get) — NOT USED (system-level only)
{
  "data": {
    "language": "def",
    "maillang": "enu"
  },
  "success": true
}

// SYNO.Core.Region.NTP (v3, method: get) — NOT USED (system-level only)
{
  "timezone": "Amsterdam",
  "date_format": "d/m/Y",
  "time_format": "H:i",
  "timestamp": 1780143170,
  "now": "Sat May 30 14:12:50 2026"
}
```

**Key observations:**

- `SYNO.Core.UserSettings.get` (v1) returns **all** user settings (~1400 lines) but only `Personal.lang`, `Personal.dateFormat`, `Personal.timeFormat` are needed
- `SYNO.Core.UserSettings` with method `apply` requires a payload body (error 114) — **method `get` works without payload**
- `SYNO.Core.Region.Language` and `SYNO.Core.Region.NTP` are **system-level** settings (shared by all users) — not used since `UserSettings` provides per-user data
- `Personal.lang` uses 3-letter BCP-47-like codes: `"ita"` = it-IT, `"fra"` = fr-FR, `"enu"` = en-US, etc.
- `Personal.dateFormat` and `Personal.timeFormat` use **PHP-style format strings** (`Y/m/d`, `h:i a`)

**Strategy: Culture resolution (two phases, no localStorage):**

```text
┌──────────────────────────────────────────────────────────────┐
│                    Login Page                                 │
│  (System settings loaded from /etc/synoinfo.conf ✅)          │
│                                                               │
│  1. System language ("/etc/synoinfo.conf") — always available │
│  2. Browser navigator.language                                │
│  3. Default: en-US                                            │
└──────────────────────┬───────────────────────────────────────┘
                       │ user logs in
                       ▼
┌──────────────────────────────────────────────────────────────┐
│              Post-Login (Home + rest of app)                  │
│                                                               │
│  1. UserSettings.Personal.lang (API, best-effort)             │
│     - Already resolves user override vs. system fallback      │
│  2. Browser navigator.language (if "def")                     │
│  3. Default: en-US                                            │
└──────────────────────────────────────────────────────────────┘
```

**Key logic:** `UserSettings.Personal.lang` already resolves user override vs. system fallback server-side:

- `UserSettings.Personal.lang` is set and not `"def"` → use it (resolved language)
- `UserSettings.Personal.lang` is `"def"` or null → return `null` from server, let WASM use browser `navigator.language`

**Key design decisions:**

- **No `localStorage`** — DSM is the single source of truth; no browser persistence needed
- **`/etc/synoinfo.conf` is always available** — read at app startup before any authentication
- **`SYNO.Core.UserSettings.get` is best-effort** — per-user language + date/time format; not required
- **Per-user language IS supported** — `Personal.lang` returns the user's language preference
- **Per-user date/time format IS supported** — `Personal.dateFormat` + `Personal.timeFormat` (PHP-style)
- **No "first visit" ambiguity** — system settings provide an immediate fallback
- **User can change language in DSM Control Panel** → change takes effect on next app visit (no sync needed)

**API Version Compatibility (verified from community libraries):**

| API | DSM 5 | DSM 6 | DSM 7 | Methods | Used? |
|---|---|---|---|---|---|
| `SYNO.Core.UserSettings` | v1 | v1 | v1 | `get` + `set` | ✅ **YES** (method: `get`) |
| `SYNO.Core.Region.Language` | v1 | v1 | v1 | `get`, `set` | ❌ No (system-level only) |
| `SYNO.Core.Region.NTP` | v1 | v2 | **v3** | `get`, `set` | ❌ No (system-level only) |
| `SYNO.Core.PersonalSettings` | ✗ | ✗ | v1 | `get`, `set` | ❌ No (not needed) |

**Why `SYNO.Core.UserSettings.get` instead of `SYNO.Core.Region.Language`:**

- `SYNO.Core.UserSettings` with method `apply` **requires a payload body** — calling without returns error 114
- `SYNO.Core.UserSettings` with method `get` requires **no payload**, returns all user settings including per-user language + date/time format
- `SYNO.Core.Region.Language` is system-level only — no per-user override detection
- **Per-user language IS supported**: `Personal.lang` = user's language preference
- **Per-user date/time format IS supported**: `Personal.dateFormat` + `Personal.timeFormat` = PHP-style format strings

**Additional related APIs discovered:**

- `SYNO.Core.PersonalSettings` (DSM 7 only, v1) — simpler per-user settings, not needed since `UserSettings.get` works
- `SYNO.Core.Region.NTP.DateTimeFormat` — separate API for date/time format (DSM 6+), not needed
- `SYNO.Core.Region.NTP.Server` — separate API for enabling/disabling NAS as NTP server, not needed

**Documentation sources (unofficial — NO official Synology documentation exists for these APIs):**

- **[N4S4/synology-api](https://github.com/N4S4/synology-api)** — Python library with full CoreSystem API docs, unit tests showing request/response patterns
- **[mib1185/py-synologydsm-api](https://github.com/mib1185/py-synologydsm-api)** — Home Assistant library with API version info across DSM 5/6/7
- **[pmilano1/synology-dsm-api](https://github.com/pmilano1/synology-dsm-api)** — Comprehensive API documentation repository
- Synology only publishes the [DSM Login Web API Guide](https://kb.synology.com/en-us/DG/DSM_Login_Web_API_Guide/2) (authentication only) — all Core API knowledge is community reverse-engineered

**Implementation approach:**

1. **Server-side — System preferences (no auth needed):**
   - Extend `ReadSettingsAsync()` in `DsmApiClient` to extract additional keys from the already-parsed `/etc/synoinfo.conf` dictionary:
     - `language` → system language (e.g. `"def"` for browser default, or specific code)
   - Store in new `DsmSystemPreferences` property on `DsmApiClient`
   - **Zero additional I/O** — file is already read and parsed; just extract 1 more key from the existing dictionary

2. **Server-side — User preferences (after auth, best-effort):**
   - After `AuthenticateAsync()` succeeds, call `SYNO.Core.UserSettings.get` (v1, method: `get`) → extracts `Personal.lang`, `Personal.dateFormat`, `Personal.timeFormat`
   - Non-blocking: if the API call fails, system preferences from `/etc/synoinfo.conf` are used as fallback
   - No payload needed; response is large (~1400 lines) but only 3 fields are extracted

3. **Server-side — Code conversion (DSM → .NET):**
   - All DSM code conversions happen **on the server only** — the WASM client never sees raw DSM codes
   - `DsmLanguageToCultureConverter`: `"enu"` → `"en-US"`, `"fra"` → `"fr-FR"`, etc.
   - `PhpDateFormatToDotNetConverter`: `"Y/m/d"` → `"yyyy/MM/dd"`, `"d/m/Y"` → `"dd/MM/yyyy"`, etc.
   - `PhpTimeFormatToDotNetConverter`: `"H:i"` → `"H:mm"`, `"h:i a"` → `"h:mm tt"`, etc.

4. **Login flow — Enriched AuthenticationResult:**
   - `AuthenticationService.LoginAsync()` fetches UserSettings and converts language code to .NET format
   - `AuthenticationResult` is enriched with pre-converted culture string (no separate API endpoint needed):

     ```csharp
     public string? Culture { get; init; }            // "en-US" (from Personal.lang or system fallback)
     ```

   - **Eliminates:** `UserPreferencesController`, `GET /user-preferences` endpoint, extra HTTP round-trip

5. **Client-side (`CultureManager`):** Receives pre-converted data from login response:
   - Priority: `Culture` (from login response) → browser `navigator.language` → `en-US`
   - No DSM code converters needed on WASM side — just `new CultureInfo(string)`

6. **Login page:** Uses system `language` from `DsmSystemPreferences` (available immediately, no API call needed)

**New artifacts needed:**

- `Askyl.Dsm.WebHosting.Data/Domain/System/DsmSystemPreferences.cs` — domain model for system prefs (raw DSM codes)
- `Askyl.Dsm.WebHosting.Data/DsmApi/Parameters/Core/UserSettings/CoreUserSettingsParameters.cs` — API: `SYNO.Core.UserSettings`, method: `get`, version: 1
- `Askyl.Dsm.WebHosting.Data/DsmApi/Responses/Core/UserSettings/CoreUserSettingsResponse.cs` — response model extracting `Personal.lang`, `Personal.dateFormat`, `Personal.timeFormat`
- `Askyl.Dsm.WebHosting.Tools/Converters/DsmLanguageToCultureConverter.cs` — DSM language code (`"enu"`) → .NET culture name (`"en-US"`)
- `Askyl.Dsm.WebHosting.Tools/Converters/PhpDateFormatToDotNetConverter.cs` — PHP `"Y/m/d"` → .NET `"yyyy/MM/dd"`
- `Askyl.Dsm.WebHosting.Tools/Converters/PhpTimeFormatToDotNetConverter.cs` — PHP `"H:i"` → .NET `"H:mm"`
- Add constant to `SystemDefaults.cs`: `KeyLanguage`
- Extend `ReadSettingsAsync()` in `DsmApiClient` to populate `SystemPreferences`
- Update `DsmApiClient.ConnectAsync()` to fetch UserSettings post-auth (best-effort)
- **Modify `AuthenticationResult`** to include `Culture`
- **Modify `AuthenticationService.LoginAsync()`** (server) to convert codes and populate `AuthenticationResult`
- **Add** `CoreUserSettings` to `RequiredApisJoined` in `ApiNames.cs`
- **Add** `CoreUserSettings` constant to `ApiNames.cs`

### 2.3 Resource File Organization

```text
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

```text
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

```text
Server (Ui)                          HTTP API                      WASM (Ui.Client)
─────────────                       ──────────                    ──────────────────
SiteLifecycleManager                ApiResult                     Home.razor
  .CreateFailure(                     .Message =                  ToastService.ShowError(
    "Site configuration                "Site configuration          result.Message ??
     is being updated")               is being updated")            "Failed to start website")
```

**After (localized end-to-end):**

```text
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

### Phase 1: Globalization Assembly Foundation ✅ Done

- [x] Create `Askyl.Dsm.WebHosting.Globalization` project (class library, `net10.0`)
- [x] Add NuGet: `Microsoft.Extensions.Localization`
- [x] Create `Resources/SharedResource.resx` (en-US default) with ALL user-facing strings
- [x] Create `Resources/SharedResource.fr-FR.resx` (French translations — empty initially)
- [x] Create `Resources/SharedResource.cs` (dummy class, no Designer)
- [x] Create `LocalizationKeys.cs` (key constants organized by domain as `L.*`)
- [x] Create `GlobalizationServiceCollectionExtensions.cs` (DI registration)
- [x] Add project references: `Ui → Globalization`, `Ui.Client → Globalization`
- [x] Add project to `.slnx`
- [x] Format → Build → Verify: zero errors, zero warnings

> **Commit:** `4981b61` — 9 files, +1015 lines

### Phase 2: Migrate Strings from Constants → Resources ✅ Done

- [x] Move user-facing strings from `ApplicationConstants` → `SharedResource.resx`
  - `PlatformNotSupportedErrorMessage` → `L.Error.PlatformNotSupported`
  - `AuthenticationFailedErrorMessage` → `L.Error.AuthenticationFailed`
  - `AuthenticationSuccessfulMessage` → `L.Success.AuthenticationSuccessful`
  - `OperationFailedErrorMessage` → `L.Error.OperationFailed`
  - `RateLimitExceededErrorMessage` → `L.Error.RateLimitExceeded`
  - `FailedToLoadDirectoryContentsErrorMessage` → `L.Error.FailedToLoadDirectoryContents`
  - `LoadingSharedFoldersMessage` → `L.Loading.SharedFolders`
  - `LoadingDirectoryContentsMessage` → `L.Loading.DirectoryContents`
- [x] Move user-facing strings from `ValidationConstants` → `SharedResource.resx`
  - `PathRequired` → `L.Validation.PathRequired`
  - `PathTraversalDetected` → `L.Validation.PathTraversalDetected`
  - `InvalidVersionFormat` → `L.Validation.InvalidVersionFormat`
  - `OperationFailed` → `L.Error.OperationFailed`
  - `EnvVarKeyTooLong` → `L.Validation.EnvVarKeyTooLong`
  - `EnvVarValueTooLong` → `L.Validation.EnvVarValueTooLong`
- [x] Keep technical constants in `Constants` (paths, timeouts, session keys, routes)
- [x] Update server services to use `IStringLocalizer` instead of `ApplicationConstants`
- [x] Remove user-facing strings from `ApplicationConstants.cs` and `ValidationConstants.cs`
  - `ValidationConstants` now only contains numeric limits (`EnvVarKeyMaxLength`, `EnvVarValueMaxLength`)

> **Bug fixes discovered:**
>
> - `SharedResource.cs` was `static class` → caused `CS0718` (can't use static as generic type arg). Fixed to `sealed class`.
> - `AddGlobalization()` registered `IStringLocalizer<SharedResource>` explicitly but `AddLocalization()` factory already handles this. Simplified to just `AddLocalization()`.
> - `options.ResourcesPath = "Resources"` caused localizer to look for resources in wrong path. Removed — default behavior discovers by full type name.

### Phase 3: Migrate Server-Side Service Messages ✅ Done

- [x] `SiteLifecycleManager.cs` (Server): Added `IStringLocalizer<SharedResource>` to constructor
  - 6 hardcoded strings → `_localizer[L.Error.*]`
  - 2 `ApplicationConstants.OperationFailedErrorMessage` → `_localizer[L.Error.OperationFailed]`
- [x] `WebSiteHostingService.cs` (Server): Added localizer to constructor
  - 6 `ApplicationConstants.OperationFailedErrorMessage` → `_localizer[L.Error.OperationFailed]`
  - 3 `ValidationConstants.EnvVar*` → `_localizer[L.Validation.*]`
  - 3 hardcoded strings → `_localizer[L.Error.*]`
  - Updated all 3 `SiteLifecycleManager` instantiation sites with localizer
- [x] `FrameworkManagementService.cs` (Server): Added localizer
  - 4 `ApplicationConstants.OperationFailedErrorMessage` → `_localizer[L.Error.OperationFailed]`
  - 1 `ValidationConstants.InvalidVersionFormat` → `_localizer[L.Validation.InvalidVersionFormat]`
  - 2 hardcoded "Version is required" → `_localizer[L.Validation.VersionRequired]`
- [x] `FileSystemService.cs` (Server): Added localizer
  - 2 `ApplicationConstants.OperationFailedErrorMessage` → `_localizer[L.Error.OperationFailed]`
  - 2 `ValidationConstants.PathTraversalDetected` → `_localizer[L.Validation.PathTraversalDetected]`
- [x] `DotnetVersionService.cs` (Server): Added localizer
  - 5 `ApplicationConstants.OperationFailedErrorMessage` → `_localizer[L.Error.OperationFailed]`
- [x] `AuthenticationService.cs` (Server): Added localizer
  - 1 `ApplicationConstants.OperationFailedErrorMessage` → `_localizer[L.Error.OperationFailed]`

### Phase 4: Migrate WASM Client Service Messages ✅ Done

- [x] `WebSiteHostingService.cs` (WASM): Added localizer to constructor — 6 fallback strings → `_localizer[L.Error.*]`
- [x] `FileSystemService.cs` (WASM): Added localizer — 2 fallback strings → `_localizer[L.Error.*]`
- [x] `DotnetVersionService.cs` (WASM): Added localizer — 5 fallback strings → `_localizer[L.Error.*]`
- [x] `FrameworkManagementService.cs` (WASM): Added localizer — 2 fallback strings → `_localizer[L.Error.*]`
- [x] `AuthenticationService.cs` (WASM): Added localizer — `ApplicationConstants.RateLimitExceededErrorMessage` + 2 hardcoded fallbacks
- [x] `TreeContentService.cs` (WASM): Added localizer — 2 `ApplicationConstants.FailedToLoadDirectoryContentsErrorMessage`
- [x] `Login.razor`: Added `@inject IStringLocalizer<SharedResource>` — replaced 3 `ApplicationConstants` references
- [x] `FileSelectionDialog.razor`: Added `@inject IStringLocalizer<SharedResource>` — replaced 3 `ApplicationConstants` references
- [x] Both `Program.cs` files: Added `builder.Services.AddGlobalization()` call + using directive

> **Bug fixes discovered:**
>
> - Home.razor: removed double HTML encoding in ShowSafeErrorToast (Blazor auto-encodes)
> - `SiteLifecycleManagerTests.cs`: Added `Mock<IStringLocalizer<SharedResource>>` — all 21 tests pass.

### Phase 5: UI Components Localization (`.razor` files) ✅ Done

- [x] Localize `Pages/Login.razor` — PageTitle, dialog title, labels (Login/Password/OTP), OK button, authenticating message
- [x] Localize `Pages/Home.razor` — PageTitle, toolbar buttons (Add/Edit/Delete/.NET Version/ASP.NET Online/Licenses/Download Logs/Logout), grid column titles, all toast/loading messages
- [x] Localize `Pages/NotFound.razor` — PageTitle, content text
- [x] Localize Dialogs/WebSiteConfigurationDialog.razor — Dialog title, all labels, section headers, buttons, error messages
- [x] Localize `Dialogs/AspNetReleasesDialog.razor` — Dialog title, channel label, grid columns (Version/Installed/Security/Release), action button text, install/uninstall messages, error messages
- [x] Localize `Dialogs/FileSelectionDialog.razor` — Dialog title, grid columns (Name/Size/Type/Modified), empty state messages, buttons (Cancel/Select File), error messages
- [x] Localize `Dialogs/DotnetVersionsDialog.razor` — Dialog title, searching/loading message, "not found" message, error message
- [x] Localize `Dialogs/LicensesDialog.razor` — Dialog title, loading message, Close button
- [x] Localize `Controls/AutoDataGrid.razor` — Loading content, empty content, items count label
- [x] `Controls/LoadingOverlay.razor` — No user-facing strings (only renders `WorkingStateComponent.Message`)
- [x] `Controls/RealTimeNumberField.razor` — No user-facing strings (label passed via parameter)
- [x] `Controls/RealTimeTextField.razor` — No user-facing strings (label passed via parameter)

> **New keys added:**
>
> - `Common_Size`, `Common_Type`, `Common_Modified`
> - `Loading_Installing`, `Loading_Uninstalling`
> - `Home_LoadingWebsites`
> - `DotnetVersions_DialogTitle`, `DotnetVersions_Searching`, `DotnetVersions_NotFound`, `DotnetVersions_FailedToLoad`
> - `Licenses_DialogTitle`, `Licenses_Loading`
> - `FileSelection_DialogTitle`, `FileSelection_SelectFile`, `FileSelection_NoFilesFound`, `FileSelection_SelectFolder`, `FileSelection_Directory`, `FileSelection_File`
> **Build:** zero errors, zero warnings

### Phase 5: DSM System & User Preferences Integration (Server-Side)

**All DSM code conversion happens on the server only — WASM never sees raw DSM codes.**

**System preferences (from `/etc/synoinfo.conf`, no auth needed):**

- [x] Add constant to `SystemDefaults.cs`: `KeyLanguage = "language"`
- [x] Create `DsmSystemPreferences.cs` domain model in `Data/Domain/System/`:
  - `Language` (string, e.g. `"def"` for browser default, or specific code)
- [x] Extend `ReadSettings()` in `DsmApiClient` to extract `language` from the existing dictionary → populate `SystemPreferences` property
  - **Zero additional I/O** — file is already read; just extract 1 more key

**DSM code converters (server-side, in `Tools/Converters/`):**

- [x] Create `DsmLanguageToCultureConverter.cs` (DSM language code to .NET culture: enu→en-US, fra→fr-FR, etc.)
- [x] Create `PhpDateFormatToDotNetConverter.cs` (PHP `"Y/m/d"` → .NET `"yyyy/MM/dd"`, `"d/m/Y"` → .NET `"dd/MM/yyyy"`, etc.)
- [x] Create `PhpTimeFormatToDotNetConverter.cs` (PHP `"H:i"` → .NET `"H:mm"`, `"h:i a"` → .NET `"h:mm tt"`, etc.)
- [x] ~~Create `SupplangToCultureConverter.cs`~~ — **Removed**: not used (app defines supported cultures in code)
- [x] ~~Create `DsmTimezoneToIanaConverter.cs`~~ — **Removed**: not used (app doesn't consume timezone)

**User preferences (from API, after auth, best-effort):**

- [x] Create `CoreUserSettingsParameters.cs` (API: `SYNO.Core.UserSettings`, method: `get`, version: 1)
- [x] Create `CoreUserSettingsResponse.cs` (response model — extracts `Personal.lang`, `Personal.dateFormat`, `Personal.timeFormat`)
- [x] Add `UserLanguage`, `UserDateFormat`, `UserTimeFormat` properties to `DsmApiClient`
- [x] Update `DsmApiClient.FetchUserLanguageAsync()` to populate all three properties from `Personal`
- [x] Add `CoreUserSettings` to `RequiredApisJoined` in `ApiNames.cs`
- [x] Add `CoreUserSettings` constant to `ApiNames.cs`

**Enrich AuthenticationResult with pre-converted culture data:**

- [x] Modify `AuthenticationResult` to include:
  - `Culture` (string?, e.g. `"en-US"`) — from UserSettings.Personal.lang or system fallback, converted
- [x] Modify `AuthenticationService.LoginAsync()` (server) to:
  - Call `apiClient.FetchUserLanguageAsync()` (best-effort)
  - Convert DSM language code to .NET format using converter
  - Return enriched `AuthenticationResult.CreateAuthenticated(message: ..., culture: ...)`
- [x] Update `AuthenticationService.LoginAsync()` (WASM client proxy) — no changes needed (deserializes new properties automatically)

**No separate API endpoint needed — all data flows through the login response.**

### Phase 6: Culture Manager & Resolution ✅ Done

**WASM receives pre-converted .NET culture strings from the login response — no DSM code converters needed on the client.**

**Key design: culture flows from WASM → server via HTTP headers (no server-side CultureManager needed).**

**Supported cultures are discovered server-side from embedded `.resx` resources and injected to WASM via `Blazor.start()` environment variable — zero hardcoded lists.**

- [x] Create `ICultureManager` interface in `Globalization` assembly:
  - `Task InitializeFromLoginAsync(string? culture)` — called with culture from login response
  - `CultureInfo CurrentCulture` — current active culture
  - `CultureInfo CurrentUICulture` — same as CurrentCulture
  - **No `SetCultureAsync` or `CultureChanged`** — culture is DSM-controlled, not user-switchable
- [x] Implement `CultureManager` for WASM (Ui.Client):
  - `static SupportedCultures` — initialized at class load via `Environment.GetEnvironmentVariable()` (injected by server)
  - Uses `System.Text.Json` to deserialize JSON array from environment variable
  - Resolution: `Culture` (from login response) → browser `navigator.language` → `en-US`
  - No DSM code converters — just `new CultureInfo(string)`
  - Sets `CultureInfo.DefaultThreadCurrentUICulture` and `Thread.CurrentThread.CurrentUICulture`
  - **No runtime culture switching** — culture is locked for the session after login
- [x] Create `AcceptLanguageHandler` (`DelegatingHandler`) — attaches `Accept-Language` header to all HTTP requests from WASM
- [x] Create `GlobalizationExtensions.cs` in Ui project (server-side):
  - `DiscoverSupportedCultures()` — scans assembly directory for culture subdirectories containing project satellite assemblies (`*.resources.dll`)
  - `SupportedCultures` — static auto-property, computed once at class load
  - `SupportedCultureNamesJson` — static auto-property, JSON-serialized once at class load
  - `ConfigureGlobalizationRequestLocalization()` — registers supported cultures with `RequestLocalizationOptions`
  - `UseGlobalizationRequestLocalization()` — adds `UseRequestLocalization` middleware
- [x] Configure `App.razor` to inject cultures via `Blazor.start()`:
  - Uses `dotnet.withEnvironmentVariable()` to pass `ADWH_SUPPORTED_CULTURES` JSON array
  - Uses `MarkupString` to render JSON inline in `<script>` block (single quotes wrap JS strings to avoid HTML escaping of inner double quotes)
- [x] Add constant `ApplicationConstants.SupportedCulturesEnvironmentVariable` for environment variable name
- [x] Register `ICultureManager` in WASM `Program.cs` (scoped)
- [x] Wire HTTP client with `AcceptLanguageHandler` in WASM `Program.cs`
- [x] Configure `loadAllSatelliteResources: true` in `App.razor` (was done in Phase 5)
- [x] Add `culture.js` for browser `navigator.language` detection via JS interop
- [x] Wire `Login.razor` to call `CultureManager.InitializeFromLoginAsync(result.Culture)` after successful login

> **Architecture decisions:**
>
> - **DSM-controlled culture**: Culture is resolved once at login from DSM user/system preferences — no runtime switching
> - **Server-side discovery**: `ResourceManager.GetResourceFiles()` not available in WASM — server discovers via `Assembly.GetManifestResourceNames()` and injects via `Blazor.start()`
> - **`dotnet.withEnvironmentVariable()`**: Pure .NET approach, no JS variable or JS interop needed for culture discovery
> - **Static `SupportedCultures`**: Initialized at class load (DI registration time), available before login
> - **Adding a new culture**: Add a `.resx` file → server auto-discovers → injects to WASM → zero code changes needed
>
> **Bug fixes discovered:**
>
> - `AcceptLanguageHandler` used `Microsoft.Net.Http.Headers` — corrected to `System.Net.Http.Headers.StringWithQualityHeaderValue`
> - Initial `ResourceManager.GetResourceFiles()` approach not viable in WASM — switched to `dotnet.withEnvironmentVariable()` via `Blazor.start()`

### Phase 7: FluentValidation Migration (Localized Validation Messages) ✅ Done

**Problem:** DataAnnotations [Required(ErrorMessage = "...")] requires compile-time constant strings.

**Solution:** Replace DataAnnotations with FluentValidation — industry-standard approach that injects `IStringLocalizer` at runtime for full localization.

**Why Phase 7 (not earlier):** Depends on Phase 6 (Culture Manager) so IStringLocalizer respects user culture.

**Architecture: Single shared validator in Globalization assembly**

The validator lives in `Globalization/Validators/` — shared by both server and client:

- **Server** (`Ui`): Uses `FluentValidation.AspNetCore` for automatic model binding validation (returns 400 with ModelState errors)
- **Client** (`Ui.Client`): Uses **Blazilla** (`<FluentValidator />`) for real-time UI validation with localized messages

**Dependency graph:**

```text
Globalization → Data (for domain models)
Globalization → FluentValidation, FluentValidation.DependencyInjectionExtensions
Ui (server) → Globalization + FluentValidation.AspNetCore
Ui.Client (WASM) → Globalization + Blazilla
```

**Models migrated:**

| Model | Validator | Rules | UI Component |
|-------|-----------|-------|--------------|
| `WebSiteConfiguration` | `WebSiteConfigurationValidator` | Name (required, length), ApplicationPath (required), InternalPort (required, range), Environment (required), ProcessTimeoutSeconds (range), HostName (required), PublicPort (required, range) | `WebSiteConfigurationDialog.razor` |
| `LoginCredentials` | `LoginCredentialsValidator` | Login (required), Password (required) | `Login.razor` |

- [x] Add `Data` project reference + FluentValidation packages to `Globalization.csproj`
- [x] Create `WebSiteConfigurationValidator.cs` in `Globalization/Validators/` (7 rules)
- [x] Create `LoginCredentialsValidator.cs` in `Globalization/Validators/` (2 rules)
- [x] Restructure `LocalizationKeys.cs` — flat `L.Validation.*` → model-scoped `L.WebSiteConfiguration.*` and `L.LoginCredentials.*`
- [x] Update both `.resx` files with new key names + French translations for LoginCredentials
- [x] Register validators: `builder.Services.AddValidatorsFromAssemblyContaining<SharedResource>()` in both `Program.cs` (server + client)
- [x] Server: Add `FluentValidation.AspNetCore` + `AddFluentValidationAutoValidation()` for automatic model binding
- [x] Client: Replace `<DataAnnotationsValidator />` with `<FluentValidator />` (Blazilla) in both dialogs
- [x] Add `@using Blazilla` to `_Imports.razor`
- [x] Remove user-facing validation error messages from `WebSiteConstants.cs` (keep numeric limits: `MinWebApplicationPort`, `MaxWebApplicationPort`, etc.)
- [x] Remove DataAnnotations from `WebSiteConfiguration.cs` and `LoginCredentials.cs` models
- [x] Update `WebSiteConfigurationTests.cs` to use FluentValidation validator (replaced DataAnnotations `Validator.TryValidateObject`)
- [x] Update `LoginCredentialsTests.cs` to use FluentValidation validator (replaced DataAnnotations `Validator.TryValidateObject`)
- [x] **Not migrated:** `RuntimeConstants` error strings in `DownloaderService` (Tools) — internal exceptions caught and converted to `L.Error.OperationFailed`
- [x] **Audit complete:** Full codebase grep confirmed zero remaining DataAnnotations

> **Bug fixes discovered:**
>
> - `FluentValidation.AspNetCore` not WASM-compatible — used **Blazilla** (`<FluentValidator />`) for client, `FluentValidation.AspNetCore` for server auto-validation
> - FluentValidation 12 changed `WithMessage()` API — no more `Func<string>`, uses direct string resolution
> - `LocalizedString` has no implicit `string` conversion — use `.Value` property
> - Tests needed rewrite: DataAnnotations `Validator.TryValidateObject` → FluentValidation `IValidator.Validate()`
> - Server registration: `AddFluentValidationAutoValidation()` on `IServiceCollection` (not on `IMvcBuilder`)
> - `LoginCredentials` had DataAnnotations missed in initial audit — added to migration scope

### Phase 8: Localize Remaining Hardcoded UI Strings ✅ Done

**Problem:** Code-behind logic in `.razor` files still contains hardcoded strings for toasts, confirmations, working states, and error messages.

**Root cause:** Phase 5 localized the markup (template) content but missed code-behind strings in event handlers.

**Strings localized:**

| File | String | Key | Context |
|---|---|---|---|
| `Home.razor` | `Are you sure you want to delete...` | `Home.DeleteConfirmation` | Delete confirmation dialog |
| `Home.razor` | `Deleting website '{0}'...` | `Loading.DeletingWebsite` | Working state message |
| `Home.razor` | `Error deleting website: {0}` | `Home.ErrorDeleting` | Error toast |
| `Home.razor` | `Starting website '{0}'...` | `Loading.StartingWebsite` | Working state message |
| `Home.razor` | `Error starting website: {0}` | `Home.ErrorStarting` | Error toast |
| `Home.razor` | `Stopping website '{0}'...` | `Loading.StoppingWebsite` | Working state message |
| `Home.razor` | `Error stopping website: {0}` | `Home.ErrorStopping` | Error toast |
| `Home.razor` | `Error during logout: {0}` | `Home.ErrorLoggingOut` | Error toast |
| `AspNetReleasesDialog.razor` | `Installation error: {0}` | `AspNetReleases.InstallationError` | Error dialog |
| `AspNetReleasesDialog.razor` | `Click OK if you want to proceed with uninstalling ASP.NET Core {0}` | `AspNetReleases.UninstallConfirmation` | Uninstall confirmation |
| `AspNetReleasesDialog.razor` | `Uninstallation error: {0}` | `AspNetReleases.UninstallationError` | Error dialog |
| `WebSiteConfigurationDialog.razor` | `The website requires .NET {0} which is not installed. Install now?` | `WebsiteConfig.FrameworkNotInstalled` | Install framework prompt |
| `WebSiteConfigurationDialog.razor` | `Error {0} website: {1}` | `WebsiteConfig.ErrorModifying` | Error dialog (updating/creating) |
| `WebSiteConfigurationDialog.razor` | `Updating '{0}'...` | `Loading.UpdatingWebsite` | Working state message |
| `WebSiteConfigurationDialog.razor` | `Creating '{0}'...` | `Loading.CreatingWebsite` | Working state message |
| `DotnetVersionsDialog.razor` | `Error while searching for global .NET version: {0}` | `DotnetVersions.ErrorSearching` | Error message |

> **Commit:** `TBD`

### Phase 8b: ILocalizer Abstraction (Hide IStringLocalizer) ✅ Done

**Problem:** `IStringLocalizer<SharedResource>` was the public localization API. Consumers could inject it directly, bypassing the abstraction layer. This prevented enforcing consistent usage.

**Solution:** Created `ILocalizer` interface with a single indexer that wraps `IStringLocalizer<SharedResource>` — hidden inside the Globalization assembly.

**Artifacts:**

| File | Purpose |
|------|---------|
| `ILocalizer.cs` | Interface with strongly-typed indexer `this[string name, params object[] arguments]` |
| `Localizer.cs` | Wrapper implementation holding `IStringLocalizer<SharedResource>` |
| `GlobalizationServiceCollectionExtensions.cs` | Added `AddScoped<ILocalizer>` registration |

**Usage:**

```csharp
// Inject
@inject ILocalizer localizer

// Simple key
@localizer[L.Home.PageTitle]

// Key with args
localizer[L.Home.DeleteConfirmation, selectedName]
localizer[L.WebsiteConfig.ErrorModifying, action, ex.Message]
```

**Files migrated:** 24 files (6 server services, 6 client services, 9 components, 2 validators, 3 tests)

**Result:** `IStringLocalizer` references reduced from 31 → 6 (all inside Globalization internals). Consumer projects (`Ui`, `Ui.Client`, `Tests`) have zero `Microsoft.Extensions.Localization` imports.

> **Commit:** `TBD`

### Phase 8c: CultureManager Refinement & JSON Serialization Fixes ✅ Done

**Issues discovered during testing:**

| Issue | Root Cause | Fix |
|-------|-----------|-----|
| `result.Culture` was `null` after login | `AuthenticationResult.Culture` was get-only — System.Text.Json can't set it | Changed to `{ get; set; }` |
| `IsAuthenticated` appeared in JSON | Redundant alias for `Success` | Added `[JsonIgnore]` |
| Server used PascalCase JSON | `PropertyNamingPolicy = null` overrode ASP.NET Core default | Removed override — back to camelCase |
| `DsmLanguageToCultureConverter` returned `"en-US"` for `"def"` | `"def"` means "browser default", not English | Returns `null` for `"def"`/unrecognized codes |
| `InitializeFromLoginAsync`/`ResetToSystemAsync` not truly async | No I/O — just in-memory lookups | Changed to `void` synchronous methods |
| Browser language detection used JS interop | Unnecessary — WASM runtime auto-sets `CultureInfo.CurrentUICulture` | Pure C# via `CultureInfo.CurrentUICulture` |
| `culture.js` tried to overwrite `navigator.language` | Read-only built-in property — assignment failed silently | Deleted file, no longer needed |
| `ResolveSystemCulture()` re-looked up env vars on each call | Inefficient — repeated work at runtime | Pre-resolve static fields to `CultureInfo?` |
| `GlobalizationExtensions` in Ui root with static settings | Mixed concerns — settings belong in Globalization assembly | Created `GlobalizationSettings` in Globalization, moved extensions to `Ui/Extensions/` |

**Artifacts:**

| File | Change |
|------|--------|
| `GlobalizationSettings.cs` | New — static settings class (supported cultures + system culture) in Globalization assembly |
| `Ui/Extensions/GlobalizationExtensions.cs` | Moved from Ui root, uses .NET 10 file-scoped `extension` pattern |
| `ICultureManager.cs` | `InitializeFromLogin(string?)` + `ResetToSystem()` (synchronous, no `Task`) |
| `CultureManager.cs` | Pre-resolves `BrowserCulture` and `SystemCulture` as static `CultureInfo?`; `FindMatchingCulture` uses `CultureInfo` + `TwoLetterISOLanguageName` |
| `DsmLanguageToCultureConverter.cs` | Returns `string?` — `null` for `"def"`/unrecognized |
| `AuthenticationResult.cs` | `Culture { get; set; }`, `IsAuthenticated` marked `[JsonIgnore]` |
| `ApplicationConstants.cs` | Added `SystemCultureEnvironmentVariable`, removed `JsInteropNavigatorLanguage` |
| `App.razor` | Injects system culture via `Blazor.start()`, removed `culture.js` reference |
| `Program.cs` (Ui) | Wires system culture from DsmApiClient after build |
| `Home.razor` | Calls `CultureManager.ResetToSystem()` on logout |
| `Login.razor` | Calls `cultureManager.InitializeFromLogin(result.Culture)` |

**Resolution chain (final):**

```text
Construction:  SystemCulture (env var) → BrowserCulture (WASM runtime) → en-US
Post-login:    result.Culture (from server) → SystemCulture → BrowserCulture → en-US
Post-logout:   SystemCulture (env var) → BrowserCulture (WASM runtime) → en-US
```

**Verification:** Build ✅ (0 warnings), Tests ✅ (211/211)

> **Commit:** `TBD`

### Phase 9: Culture-Aware Formatting

- [ ] Ensure dates render per culture (FluentUI date components)
- [ ] Ensure numbers render per culture
- [ ] Verify pluralization rules work per culture

### Phase 10: Testing & Validation

- [ ] Test culture resolution from login response (DSM preference)
- [ ] Test fallback to browser language when login response lacks culture
- [ ] Test fallback to English for missing translations
- [ ] Test browser culture detection on first visit
- [ ] Test server-side messages are localized in API responses
- [ ] Test FluentValidation messages are localized (French)
- [ ] Test culture propagates via Accept-Language header to server
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

```text
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

// Initialize CultureManager at startup (resolves culture from login response or browser fallback)
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
| DSM language code not recognized | `DsmLanguageToCultureConverter` falls back to `"en-US"` for unknown codes; user can override in UI |
| `SYNO.Core.UserSettings` response is massive (~1400 lines) | Only deserialize `Personal.lang`, `Personal.dateFormat`, `Personal.timeFormat`; use `System.Text.Json` with selective property extraction |
| DSM date/time format uses PHP-style strings (`Y/m/d`, `H:i`) | `PhpDateFormatToDotNetConverter` / `PhpTimeFormatToDotNetConverter` map PHP tokens to .NET equivalents |
| DSM timezone format (`"Amsterdam"` vs `"Europe/Amsterdam"`) | `DsmTimezoneToIanaConverter` maps DSM timezone names to IANA tz database names |

---

## 7. Decisions Log

| Date | Decision | Rationale |
|---|---|---|
| 2026-05-28 | Dedicated `Globalization` assembly | Separation of concerns; shared resources across server + WASM; zero project references |
| 2026-05-28 | Single `SharedResource.resx` | Simpler management; ~100 keys total is manageable in one file |
| 2026-05-28 | `LocalizationKeys.cs` (not Designer.cs) | No namespace collisions; explicit key constants; works with `ResourcesPath` |
| 2026-05-28 | Custom `ICultureManager` (no third-party library) | .NET 10 native `loadAllSatelliteResources` makes `Blazor.WebAssembly.DynamicCulture` unnecessary |
| 2026-05-28 | No page reload on language switch | Event-based re-render; better UX |
| 2026-05-28 | No localStorage for persistence | DSM is single source of truth; language follows DSM preferences, not browser state |
| 2026-05-28 | Migrate user-facing strings from `Constants` | `const string` cannot be localized; `ApplicationConstants` and `ValidationConstants` should only hold technical values |
| 2026-05-29 | Key class named `L` (not `LocalizationKeys`) | Shorter import: `L.Error.FailedToLoadWebsites` vs `LocalizationKeys.Error.FailedToLoadWebsites` |
| 2026-05-29 | Flat keys in resx (`Login_PageTitle`) | Avoids nested resource file complexity; matches `L.Login.PageTitle` constant value |
| 2026-05-30 | Use `SYNO.Core.Region.Language` + `SYNO.Core.Region.NTP` for DSM prefs | Verified via browser network inspection; `SYNO.Core.Desktop.Preferences` doesn't exist or is undocumented |
| 2026-05-30 | Per-user language via `SYNO.Core.UserSettings` | `Personal.lang` contains the user's individual language preference; `SYNO.Core.Region.Language` is system-level fallback |
| 2026-05-30 | Four-tier culture resolution | per-user lang (if != "def") → system language (if != "def") → browser Accept-Language → en-US |
| 2026-05-30 | `DsmLanguageToCultureConverter` utility class | DSM uses 3-letter codes (`"enu"`, `"fra"`, `"deu"`) that need mapping to .NET `CultureInfo` names (`"en-US"`, `"fr-FR"`, `"de-DE"`) |
| 2026-05-30 | Best-effort fetch of DSM prefs | The three APIs may not exist on all DSM versions; failure is non-blocking and falls through to browser Accept-Language |
| 2026-05-31 | Use `SYNO.Core.UserSettings` with method `get` (not `apply`) | `apply` requires a payload body (error 114); `get` needs no payload and returns all user settings including `Personal.lang`, `Personal.dateFormat`, `Personal.timeFormat` |
| 2026-06-01 | Extract `Personal.dateFormat` and `Personal.timeFormat` from UserSettings | Verified via live API: `Personal` section contains PHP-style format strings (`Y/m/d`, `h:i a`) that need conversion to .NET format equivalents |
| 2026-06-03 | Remove `DsmTimezoneToIanaConverter`, `SupplangToCultureConverter`, and all timezone/supported-languages plumbing | Neither timezone nor supported languages are consumed by the app; `AuthenticationResult` only carries `Culture` |
| 2026-06-08 | Phase 8 localizes remaining hardcoded UI strings in code-behind | Phase 5 localized markup but missed C# event handler strings (toasts, confirmations, working states) |
| 2026-06-08 | `ILocalizer` abstraction hides `IStringLocalizer` from consumer projects | Prevents direct Microsoft dependency leaks and enforces consistent usage pattern via single indexer |
| 2026-06-08 | `AuthenticationResult.Culture` uses `{ get; set; }` | System.Text.Json can't deserialize into get-only properties without parameterized constructor matching |
| 2026-06-08 | `IsAuthenticated` marked `[JsonIgnore]` | Redundant alias for `Success` — pollutes JSON response |
| 2026-06-08 | Revert to ASP.NET Core default camelCase JSON | `PropertyNamingPolicy = null` caused deserialization mismatch with WASM client |
| 2026-06-08 | `DsmLanguageToCultureConverter` returns `null` for `"def"` | `"def"` means "use browser default" — should not force English |
| 2026-06-08 | `InitializeFromLogin`/`ResetToSystem` are synchronous `void` | No I/O involved — `Task` return type was interface-only convention |
| 2026-06-08 | Browser culture via `CultureInfo.CurrentUICulture` | WASM runtime auto-sets from Accept-Language header — no JS interop needed |
| 2026-06-08 | Delete `culture.js` | Tried to overwrite read-only `navigator.language` — failed silently |
| 2026-06-08 | Static fields pre-resolve to `CultureInfo?` | Avoids repeated env var lookups and string comparisons at runtime |
| 2026-06-08 | `FindMatchingCulture` uses `CultureInfo` + `TwoLetterISOLanguageName` | Proper .NET API for parent language matching — no manual `Split('-')` |
| 2026-06-08 | `GlobalizationSettings` in Globalization assembly | Static settings belong where the resources are — not in Ui project |
| 2026-06-08 | `GlobalizationExtensions` in `Ui/Extensions/` | ASP.NET Core extensions belong in the project that uses them |
| 2026-06-08 | `ResetToSystem()` resets to system resolution chain | Logout should restore system/browser culture — not just browser |

---

## 8. Next Steps

1. Begin Phase 9: Verify culture-aware formatting (dates, numbers, pluralization)
2. Begin Phase 10: End-to-end testing & validation
