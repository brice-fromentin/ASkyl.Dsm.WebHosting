# Open Technical Items

Defects, gaps and drift. **Features are not tracked here** — they belong in the README roadmap.

This document is meant to be kept current: close items as they land, and add nothing that has not been
checked against source. It replaces the 2026-07-25 assessment, a dated snapshot that was wrong on several
claims and has been deleted.

Every entry below was verified on 2026-07-29 against `main`, with the file and line that shows it.
Nothing here is inherited on trust.

## Security

### `[AuthorizeSession]` denies with 500 instead of 401/403

`Ui/Authorization/AuthorizeSessionAttribute.cs`. `ForbidResult` requires a registered authentication
scheme; `Program.cs` calls neither `AddAuthentication` nor `AddAuthorization`, so the denial throws and
surfaces as a 500. It fails closed, but noisily, and gives clients nothing to branch on.

Check how `Ui.Client`'s `AuthenticationNavigationGuard` reacts before changing the status code.

### Session identifier is not rotated on login

`Ui/Services/DsmSession.cs`. No rotation exists anywhere — no regeneration, no clear-and-reissue. Risk is
low because the cookie is HttpOnly, Secure and SameSite=Strict, and nothing of value is stored
pre-authentication, but fixation remains theoretically possible.

## Reliability

### The package stop timeout is shorter than the application's own shutdown

`spk-project/scripts/start-stop-status:88-94` sends SIGTERM, sleeps **2 seconds**, then sends SIGKILL.

The host's graceful shutdown awaits `StopAllSitesAsync`, which stops each hosted site with a timeout of
`WebSiteConstants.DefaultProcessTimeoutSeconds` — **10 seconds**, configurable to 120. Any site that takes
longer than about two seconds to drain means the host is SIGKILLed mid-shutdown, and hosted site processes
are children, so they are orphaned rather than stopped.

This is the orphaning the 2026-07-25 assessment attributed to `SiteLifecycleManager.Dispose`. That
attribution was wrong — `StopAllSitesAsync` does await each stop — but the outcome it predicted is real,
with the script's timeout as the actual cause. Raising the script's wait above the configured site timeout
is the fix.

### `pkill -f` runs as root against a bare pattern

`spk-project/scripts/common-functions.sh:159` — `pkill -f "$pattern" || true`. As root, `-f` matches the
entire command line, so any process whose arguments happen to contain the pattern is killed. A PID file is
already maintained and should be preferred.

### No lifecycle script uses `set -e`, `-u` or `pipefail`

All eight scripts under `spk-project/scripts/` run as root on the user's NAS, and every one of them
continues past a failed command. `build-spk.sh` gets this right at line 3 — the packaging scripts simply
never adopted it.

### `AddWebsiteAsync` applies side effects before persisting, with no rollback

`Ui/Services/WebSiteHostingService.cs:90-114`. ACLs are set (step 1) and the reverse-proxy rule is created
(step 2) before the configuration is persisted (step 3). The catch returns a failure result without
compensating, so a failure after step 2 leaves an orphaned DSM proxy rule. `UpdateWebsiteAsync` has the
same ordering at lines 160-184.

### Fire-and-forget async on the client

- `Ui.Client/Services/CultureManager.cs:278` — `private async void UpdateHtmlLangAndDir`. An exception
  here cannot be caught by the caller and will fault the WebAssembly app.
- `Ui.Client/Components/Pages/Home.razor:118` — `_ = ShowWebSiteConfigurationDialogAsync(instance)`
  discards the task and any exception it carries.

### `HttpClientExtensions` never disposes its responses

`Tools/Extensions/HttpClientExtensions.cs:21,49` — both `GetAsync` and `PostAsync` results are assigned
without `using`, so `HttpResponseMessage` is left to finalization.

### `build-spk.sh` version extraction can silently yield "null"

`src/scripts/build-spk.sh:68-71` — `local version=$(jq -r …)` masks the command's exit status because
`local` supplies its own, defeating `set -e`. And `jq -r` prints the string `null` for a missing key, which
the following `[ -z "$version" ]` check does not catch, so a missing `Download.ChannelVersion` propagates
as the literal text `null`.

### Validation stampede on a cold cache

`Ui/Services/DsmSession.cs`. `_validationLock` is per-instance while `IDsmSession` is Scoped, so on a cache
miss several concurrent requests for one user can each call `SYNO.Core.User`. Bounded and infrequent since
PR #39 introduced the shared cache; per-SID locking would need a semaphore dictionary with lifetime
management. Noted in the code.

### `UseHttpsRedirection` is a permanent no-op behind nginx

`Ui/Program.cs`. DSM's nginx terminates TLS and proxies plain HTTP to port 7120, so ASP.NET logs
`Failed to determine the https port for redirect` (EventId 3) at every startup — visible in two consecutive
deployment logs. Fix is either processing `XForwardedProto` or removing the middleware. Scoped out of
PR #36 because it changes what the middleware observes and nothing in the process could run the application
to confirm the result. AGENTS.md §13 now allows a local run against `dev-mock/`, so this is answerable.

### Cosmetic: install reports a worse error than uninstall for a bad version

`Ui/Services/FrameworkManagementService.cs` — `InstallFrameworkAsync` (lines 20-52) checks only that the
version is non-empty, while `IsValidVersionFormat` is called at line 61 inside `UninstallFrameworkAsync`.

**This is not a security gap.** The version never reaches a URL or a file path: `DownloadVersionToAsync`
hands it to `GetReleaseByVersionAsync`, which does a `String.Equals` against the release list Microsoft
returned (`DownloaderService.cs:135`). A malformed version matches nothing, throws, and is caught into a
generic failure result. The only consequence is that install says "operation failed" where uninstall would
have said "invalid version format".

### Latent: initialization write-tests the wrong directory

`Ui/Services/WebSitesConfigurationService.cs:20,54`. The configuration path honours the injected
`configurationDirectory`, but `EnsureServiceInitializationAsync` checks `AppContext.BaseDirectory`
regardless. Currently harmless — `Program.cs:111` registers the service without a directory, so both
resolve to the same place — and it only diverges under tests, which do supply one.

## Test coverage

`AuthorizeSessionAttribute` has no tests, which is notable for the class that gates every API call. Neither
do `ProcessHandle`, `ProcessTerminator`, `DownloaderService`, `RequestTrackingMiddleware`, or the six
controllers. The two FluentValidation validators are exercised indirectly through service tests but not
directly. `ProcessRunner` and `ErrorEndpoints` gained tests in PRs #32 and #33.

`ResourceCompletenessTests` hardcodes `fr-FR`, so a newly added culture would be silently untested for key
parity — which undercuts the "drop in a `.resx`, zero code changes" story.

## Documentation drift

All in `technical-architecture.md`, all confirmed on 2026-07-29:

| Line | Claim | Reality |
|---|---|---|
| 57 | Full `CancellationToken` support across all async operations | `IVersionsDetectorService.GetInstalledVersionsAsync()` takes none |
| 258 | Resource keys are `L.*` | The class is `LK` |
| 277 | `OperationTimer` used by seven services | Exactly one usage, in `DsmApiClient` |
| 301 | `SystemProcessHandle` is a Transient registration | Not DI-registered; constructed by `SystemProcessRunner` |
| 821 | HTTPS on port 7121 | Declared in `adwh.sc`, but nothing binds it |
| 829 | `preinst` performs architecture detection | It only logs `SYNOPKG_DSM_ARCH`; detection is `uname -m` in `common-functions.sh:216`, called from `postinst` and `postupgrade` |
| 834 | `postuninst` performs final cleanup | The script is literally `exit 0` |
| 843+ | Deployment is manual, CI is planned | `.github/workflows/build.yml` implements it |

`RequestTrackingMiddleware.cs:14` writes `HttpContext.Items[RequestId]` and nothing ever reads it, so the
propagation the document describes does nothing.

## Prerequisite for a roadmap feature

### Harden `ArchiveExtractorService` before it extracts anything user-supplied

**Not a defect today. Do not fix it as one.** The weaknesses below are unreachable under the current
threat model, and recording them as security findings would misrepresent the risk.

`Tools/Infrastructure/ArchiveExtractorService.cs:47-52` has three:

- The zip-slip guard is `absoluteTargetPath.StartsWith(targetDirectory)` with **no trailing separator**,
  and `FileManagerService.GetDirectory("")` returns `Path.GetFullPath(...)`, which never ends in one.
- Only `TarEntryType.Directory` is special-cased; symlink and hardlink entries fall through to
  `ExtractToFile`, which creates the link without validating its target.
- `Tools/Runtime/DownloaderService.cs` verifies no hash, and `install_dotnet_runtime`
  (`common-functions.sh:253`) simply untars. SHA512 is checked only at build time by `build-spk.sh`.

Why none of it is currently exploitable:

1. Extraction targets `AppContext.BaseDirectory/../runtimes` — a **sibling** of `admin-ui`, not the
   application folder. The prefix flaw therefore only admits paths beginning `…/AskylWebHosting/runtimes`,
   which means creating siblings named `runtimes*` under the package root. It cannot reach `admin-ui/`,
   `/etc`, or the hosted sites.
2. Exploiting any of them requires **controlling the archive** — and whoever controls the archive already
   gets to write into the legitimate `runtimes/` tree, which supplies the `dotnet` host binary this
   application executes. The escape is strictly weaker than the sanctioned write it guards, so it grants
   an attacker nothing they do not already have.
3. There is one caller, `FrameworkManagementService.cs:34`, fed by `DownloaderService` from Microsoft over
   HTTPS with certificate validation and no bypass anywhere in the solution.

**When this changes:** the README roadmap includes *"Deployment Pipelines: Support direct application
deployment from compressed packages (.zip/.tar.gz)"*. If that reuses this service, the archives become
user-supplied, premise 2 collapses, and all three become live vulnerabilities at once. Harden the
extractor as part of that work, not before.

## Notes

**Not a repository issue.** `src/Askyl.Dsm.WebHosting.Analyzers.Tests/` holds only gitignored `obj/`
output. Nothing in it is tracked, so no commit can remove it — it is local clutter, cleared with `rm -rf`.
The assessment listed it as a repository problem.

**A design opinion, not a defect.** The assessment argued that the dual server/client service
implementations are coupling described as abstraction. The supporting facts hold — prerendering is
disabled, so no component can bind to a server-side implementation, and
`Ui.Client/Services/FileSystemService.cs` implements one member as `NotSupportedException` — but whether
that warrants restructuring is a judgement call, not something to fix.

**Features live in the README roadmap:** health checks and real liveness, per-site log separation,
auto-restart on assembly change, bUnit component tests, `DownloaderService` integration tests,
configuration migration, certificate management, Web Station integration, and Package Center submission.
