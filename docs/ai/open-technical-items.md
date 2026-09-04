# Open Technical Items

Defects, gaps and drift. **Features are not tracked here** — they belong in the README roadmap.

This document is meant to be kept current: close items as they land, and add nothing that has not been
checked against source. It replaces the 2026-07-25 assessment, a dated snapshot that was wrong on several
claims and has been deleted.

Every entry below was verified on 2026-07-29 against `main`, with the file and line that shows it.
Nothing here is inherited on trust.

## Security

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

**TODO, prerequisite for the three SPK items above: a disposable DSM instance.** None of them can be
confirmed or fixed with confidence by reading — they need an install, a stop and an upgrade actually run.
Mocking cannot substitute, since `synopkg` and the package lifecycle are the thing under test. A Virtual
DSM under Virtual Machine Manager is the candidate (one free instance per host, Btrfs volume required).
Recorded, not planned: it shares its physical machine with production, and reachable credentials are a
separate decision from the hardware.

### The website lifecycle path still drops the cancellation token

`Ui/Services/WebSiteHostingService.cs`. `AddWebsiteAsync` and `UpdateWebsiteAsync` now hand their token to
persistence, but not to the instance work that follows: `AddInstanceAsync` and `UpdateInstanceAsync` declare
no `CancellationToken` parameter at all, and the `StartWebsiteAsync` / `StopWebsiteAsync` calls inside them
are made without one although both accept it. `GetAllWebsitesAsync` and `StartEligibleSitesAsync` drop it
the same way.

This is not a parameter that was forgotten. Those paths end in `SiteLifecycleManager.StartAsync()` and
`StopAsync()`, which take no token by design — operations are serialized through a bounded `Channel` with
`TaskCompletionSource`-carrying command records. Making cancellation meaningful means deciding what
cancelling a queued lifecycle command does to the command already running, which is a change to that
protocol rather than an argument to pass along.

Worth weighing against the architecture document's claim of "full `CancellationToken` support across all
async operations", already listed as drift below.

### A failed rule deletion still orphans the rule on removal

`Ui/Services/WebSiteHostingService.cs`. `RemoveInstanceAsync` now restores the reverse-proxy rule when the
configuration removal fails, so a failed removal no longer strands a running site with nothing routing to
it. One hole is left open, on the other branch: deleting the rule is **best effort**, so if DSM refuses the
deletion the removal continues, the configuration is removed, and the rule stays behind — orphaned and
invisible to this application, which is the state the compensation elsewhere exists to prevent.

That is a deliberate trade rather than an oversight: failing the removal instead would make a site
impossible to delete for as long as DSM refuses, which is worse for the user in front of it. Closing it
properly means somewhere to record "this rule is known to be stale", which does not exist today. The
`ReverseProxyDeletionFailed` log line is the only trace.

Worth re-reading against a real deployment now that PR #57 landed: the scope disposal defect made
`DeleteReverseProxyRuleAsync` report failure for deletions that had in fact succeeded, so this branch was
being taken constantly and for the wrong reason. How often it fires for a *real* DSM refusal is unknown.

Also unverified, and it decides how the restore behaves at the edge: whether `SYNO.Core.ReverseProxy`
accepts a create for a rule that already exists. The restore is guarded on the deletion having succeeded
precisely so it never has to find out.

### Fire-and-forget async on the client

- `Ui.Client/Services/CultureManager.cs:278` — `private async void UpdateHtmlLangAndDir`. An exception
  here cannot be caught by the caller and will fault the WebAssembly app.
- `Ui.Client/Components/Pages/Home.razor:118` — `_ = ShowWebSiteConfigurationDialogAsync(instance)`
  discards the task and any exception it carries.

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

### Cosmetic: the status code page reports `errorCode: 500` whatever the status

`Ui/Endpoints/ErrorEndpoints.cs`. `HandleStatusCode` builds `new ApiResult(false, …)` without an error
code, so the JSON branch always serialises `ApiErrorCode.Failure`, including on a 404. `ApiErrorCode`
already carries `NotFound`, `Unauthorized`, `BadRequest` and `Forbidden` at their HTTP values, so the
status could simply be mapped.

Pre-existing but unreachable until the re-execution was fixed, and still harmless: every client path in
`HttpClientExtensions` and `Ui.Client/Services/AuthenticationService.cs` short-circuits on
`IsSuccessStatusCode` before reading the body, so nothing consumes the field. The message wording is wrong
for the same reason — "Resource not found" on a 403 — and both are the same one-line fix.

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

`ProcessHandle`, `ProcessTerminator`, `DownloaderService`, `RequestTrackingMiddleware` and the six
controllers have no tests. `AuthorizeSessionAttribute` gained them alongside its status code fix — the
pass-through, both refusal paths and the cancellation token it forwards. The two FluentValidation validators are exercised indirectly through service tests but not
directly. `ProcessRunner` and `ErrorEndpoints` gained tests in PRs #32 and #33.

`ResourceCompletenessTests` hardcodes `fr-FR`, so a newly added culture would be silently untested for key
parity — which undercuts the "drop in a `.resx`, zero code changes" story.

### The EventId registry claims an accuracy nothing checks

`Constants/Logging/LogEventIds.cs` documents a range per service, and §6.6 asks for it to be updated by
hand. That instruction has failed twice: `WebSitesConfigurationService` declared two ids that no longer
existed after PR #59 deleted them, and `DsmSettingsService` had been declaring one fewer than it used since
`2800006` was added — visible in every deployment log, unnoticed for as long. Nothing in format, build or
test observes any of it, because the ranges are prose beside constants that **nothing reads**: the file is
`static class` with no accessibility modifier, the grep for `LogEventIds.` is empty, and its own summary
admits the source generator inlines literal values instead.

**Decided: make the registry enforceable rather than delete it.** The design, with the parts already
verified:

- `LoggerMessageAttribute` survives into metadata. Measured on the real assembly: 197 methods carry it,
  with 197 distinct ids from 1000001 to 7600010. So the check needs **no source parsing and no path
  walking** — reflection over the `Logging` assembly is enough, which is what makes this worth doing at all.
- Replace each documented range with a pair of constants (`…Base`, `…Last`), so the bounds become data
  rather than a sentence.
- Assert three things: no id falls outside every declared range, no two ranges overlap, and each range's
  highest used id equals its `Last`. The third is the one that catches both failures above.

**The trap to avoid**, and the reason the first sketch of this was thrown away: a check that discovers its
own inputs can pass by finding nothing. Parsing `— IDs A–B.` out of a doc comment fails silently the day
someone types a hyphen for the en dash, and a green test that verified nothing is worse than no test. Any
implementation must assert its input set is non-empty before asserting anything about it.

A duplicate-id assertion is the cheap half and stands on its own: two services logging under one id breaks
the by-service grouping `compare-logs.sh` relies on. There are no duplicates today, so it costs nothing now
and guards the future.

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
