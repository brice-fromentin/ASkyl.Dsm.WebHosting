# Architecture Gaps Fixes Plan — July 2026

**Source:** `docs/ai/arch-gaps-2026-07-01.md`
**Date:** 2026-07-02

## Overview

This plan addresses all 8 identified architecture gaps, organized by priority. Each fix is tagged as [CODE] (code changes required) or [DOC] (documentation updates only).

| # | Gap | Priority | Target |
|---|-----|----------|--------|
| 1 | Broken error endpoints | Critical | [CODE] |
| 2 | Deployment/packaging documentation | High | [DOC] |
| 3 | CI/CD pipeline | High | [CODE + DOC] |
| 4 | Test strategy gaps | Significant | [CODE + DOC] |
| 5 | Configuration management documentation | Significant | [DOC] |
| 6 | Performance metrics | Minor | [DOC] |
| 7 | Error correlation tracing | Minor | [CODE + DOC] |
| 8 | Dead code removal | Minor | [CODE] |

---

## Critical: Gap #1 — Broken Error Handling Endpoints [CODE]

**Problem:** `UseExceptionHandler("/Error")` and
`UseStatusCodePagesWithReExecute("/not-found")` in `Program.cs` point to
non-existent routes. In production, unhandled exceptions or 4xx status codes
produce undefined behavior (infinite redirect loop or blank response).

**Impact:**

- Unhandled exceptions from services that omit try/catch
- FluentValidation model validation failures (HTTP 400 → re-execution to `/not-found` → failure)
- `AuthorizeSessionAttribute` returning `ForbidResult()` (HTTP 403 → same loop)

**Fix:** Implement two minimal endpoints with content negotiation:

1. **New file:** `Ui/Endpoints/ErrorEndpoints.cs`
   - Map `/Error` — handles unhandled exceptions (500s)
   - Map `/not-found` — handles 4xx status codes
   - Use `Request.Headers.Accept` to return JSON for API calls, HTML for browser navigation

2. **Verify:** `Program.cs` middleware ordering is correct

---

## High: Gap #2 — Deployment/Packaging Documentation [DOC]

**Problem:** The SPK packaging pipeline is a multi-phase process entirely undocumented in `technical-architecture.md`.

**Fix:** Add "Deployment & Packaging" section to `technical-architecture.md` covering:

- Complete build pipeline from code to SPK (pre-flight checks, .NET runtime download, application publish, SPK assembly)
- Runtime selection strategy at install time (fat package, thin extract per architecture)
- Nginx reverse proxy integration mechanism (`adwh-alias.conf`)
- Service account and permissions model (`AskylWebHosting` system user in `http` group)
- Data persistence paths (`/var/packages/AskylWebHosting/var/`)
- Port configuration flow (`adwh.sc`: HTTP 7120, HTTPS 7121)
- Lifecycle scripts table (preinst/postinst/preupgrade/postupgrade/preuninst/postuninst/start-stop-status/common-functions.sh)
- Version management dual-source constraint (`Directory.Build.props` + `spk-project/INFO`)

**Note:** README.md already covers user-facing build and install steps. This doc is developer-focused.

---

## High: Gap #3 — CI/CD Pipeline [CODE + DOC]

**Problem:** Entirely manual workflow — developer runs `build-spk.sh` locally, copies `.spk` to target NAS via Package Center. No automated testing, no versioned artifacts, no code signing.

**Fix:**

### Code: GitHub Actions Workflow

- **New file:** `.github/workflows/build.yml` with two trigger paths:

  - **Regular push/PR:** restore → format check → build → test (with `--blame-hang-timeout 10s`). No SPK packaging.
  - **Tag push (`v*`):** full build → run `build-spk.sh` → create GitHub release with SPK artifact

- Uses `actions/cache` to cache downloaded .NET runtimes between runs
- Uses `${{ github.token }}` — no secrets needed for release creation

### Script Portability: `build-spk.sh`

- Add `gzip` fallback when `pigz` is unavailable
- Ensure all dependencies (`curl`, `tar`, `dotnet`, `jq`, `awk`) are available on Ubuntu runners; add `apt-get install pigz jq` step to workflow

### Automated Release on Tag Push

- On `git push v*` tag → auto-create GitHub release with SPK artifact
- Uses `gh release create` with changelog from commit messages
- Developer workflow: `git tag v0.7.0 && git push origin v0.7.0`

### Documentation

- Add "CI/CD Pipeline" section to `technical-architecture.md` describing workflow triggers, stages, artifact retention, and release process

---

## Significant: Gap #4 — Test Strategy Gaps [CODE + DOC]

**Problem:** Anti-patterns in existing tests; controllers documented as needing tests but are thin routing wrappers.

### Code Fixes

- Remove ineffective `[Collection("WebSiteHostingService")]` without matching `[CollectionDefinition]`
- Fix temp directory leaks — implement `IDisposable.Dispose()` cleanup for:
  - `WebSiteHostingServiceTests` (creates `_tempDir`, never cleans)
  - `ArchiveExtractorServiceTests` (creates temp dirs, no `Dispose()`)
  - `FileManagerServiceTests` (creates temp dir, no visible cleanup)
- Replace magic string assertions with structured result property checks

**Note:** Controller tests are NOT needed. All 6 controllers
(`AuthenticationController`, `FileManagementController`,
`FrameworkManagementController`, `LogDownloadController`,
`RuntimeManagementController`, `WebsiteHostingController`) are thin routing
wrappers — each action is a single `=> Ok(await service.XxxAsync(...))` with no
business logic, branching, or transformation. Services are already tested.

### Documentation Fixes

- Update `technical-architecture.md` to document controller architecture: thin routing wrappers delegating all behavior to services
- Remove `FluentAssertions` from listed test frameworks (not in csproj; all assertions use xUnit `Assert.*`)

---

## Significant: Gap #5 — Configuration Management [CODE + DOC]

**Problem:** Undocumented configuration structure, dual-purpose `ChannelVersion` constraint, and hardcoded DSM settings path prevents local debugging.

### Code Fixes

- Add `DsmSettings.ConfigPath` to appsettings.json (defaulting to `/etc/synoinfo.conf`)
- Update `DsmSettingsService` to read path from configuration instead of hardcoded value;
  if configured path doesn't exist in Development environment, throw once with clear
  instructions (template location and copy command), then cache acknowledgment to avoid repeat
- Create `dev-mock/` folder with sample `synoinfo.conf` template; add `dev-mock/` to `.gitignore`
- Update `launchSettings.json` — add `DsmSettings__ConfigPath: "./dev-mock/synoinfo.conf"` environment variable to the Development profile
- Update `build-spk.sh`: ensure `dev-mock/` and any `*Development.json` are removed from publish output before SPK assembly

### Documentation Fixes

- Add "Configuration" section to `technical-architecture.md` covering:
  - Dual appsettings.json structure (server `Ui/appsettings.json`, client `Ui.Client/wwwroot/appsettings.json`)
  - `Download.ChannelVersion` dual-purpose constraint: build-time runtime download version + runtime application version detection
  - Direct configuration access pattern (`builder.Configuration[]`, no IOptions used anywhere)
  - `DsmSettingsService` configurable path for local debugging support
  - Layered configuration merge semantics: `.Development.json` overlays `appsettings.json`; developers must periodically sync new keys from base to their local override file

---

## Minor: Gap #6 — Performance Metrics [DOC]

**Problem:** No performance targets defined despite documented caching strategy.

**Fix:** Add "Performance" section to `technical-architecture.md` covering:

- Response time targets for API endpoints (<200ms typical)
- Memory usage guidelines for long-running hosting service
- Connection pool sizing rationale for `HttpClient` instances
- Existing caching strategy summary (lazy-init, 1-min TTL session validation, instance cache)

---

## Minor: Gap #7 — Error Correlation Tracing [CODE + DOC]

**Problem:** No request ID or trace ID propagation across UI → API controller
→ service → DSM API call chain. Serilog's `WithActivity` enricher adds IDs but
they're not surfaced to clients for support ticket correlation.

### Code Fix

- Add middleware to propagate `X-Request-ID` header through the full request chain
- Surface request ID in JSON error responses for client-side correlation

### Documentation Fix

- Add "Request Tracing" section to `technical-architecture.md` describing propagation mechanism and client usage

---

## Minor: Gap #8 — Dead Code Removal [CODE]

**Problem:** `MandatorySettingMissingException` is defined in `Data/Exceptions/` but never thrown anywhere in the codebase.

**Fix:** Remove unused exception type and any associated references.
