# Open Technical Items

Defects, gaps and drift. **Features are not tracked here** — they belong in the README roadmap.

This document is meant to be kept current: close items as they land, and add nothing that has not been
checked against source. It replaces the 2026-07-25 assessment, a dated snapshot that was wrong on several
claims and has been deleted.

Nothing here is inherited on trust: each entry names the file, and a line where one pins the claim.

Entries are written when the defect is found and rewritten when it changes, so they do not share one
verification date. What is shared is a sweep — every remaining entry was re-checked against `main` on
**2026-09-07**, which is what the line references below reflect. Line numbers are the first thing to go
stale here; re-run the sweep rather than trusting them.

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

### `build-spk.sh` reports success after skipping an architecture

`src/scripts/build-spk.sh`. When a release lists no file for one of the three architectures, the loop warns
and continues, and the function still ends on "All .NET runtimes are downloaded and verified" with a status
of zero. The SPK is then packaged without that architecture, and nothing downstream says so.

Left as it is by the masked-failure fix rather than changed with it: warn-and-continue is what the loop was
written to do, and whether a missing architecture should abort the build is a packaging decision, not a
defect to correct in passing. Measured: with only `linux-x64` present, two warnings are printed and the
build reports success — unchanged before and after that fix.

Closing it means deciding what the package should contain. Failing the build is one answer; recording the
architectures actually bundled, and letting the caller judge, is another.

### The .NET release lookup cannot be cancelled, and owns its own HttpClient

`Tools/Runtime/DownloaderService.cs`. `ProductCollection.GetAsync()`, from
`Microsoft.Deployment.DotNet.Releases` 1.0.2, has three overloads — `()`, `(String)` and `(Uri)` — and
**none takes a `CancellationToken`**. It also builds its own `HttpClient` rather than taking one, so it sits
outside `IHttpClientFactory` and outside any policy configured there. The solution configures none anyway:
no resilience handler, no retry, nothing.

The consequence is not the failure that revealed this. A deployment on 2026-09-21 showed the call refused
in 50 ms — the process's first outbound connection, an `EAGAIN` on connect — and the caller reported it
cleanly. **A refusal is the harmless shape.** A hang is the one that hurts: the token checked before the
call is the only cancellation point, so a stalled connection holds the dialog open and closing it stops
nothing.

Closing this means either wrapping the call so a timeout can be imposed from outside, or replacing the
library call with a direct fetch of the release index through the application's own client, where a timeout
and a policy would apply. The second is more code and takes on a format the library currently owns.

**Left undecided, deliberately:** whether to retry that call at all. The same deployment succeeded on the
next attempt eight seconds later, so a single retry would have made the incident invisible — which is an
argument for it and against it. Hiding a transient network condition is a product decision, not a
correctness one.

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

### The version format check on uninstall is a path guard, not a message

`Ui/Services/FrameworkManagementService.cs`. `UninstallFrameworkAsync` interpolates the caller's version
straight into three paths it then deletes recursively — `host/fxr/{version}` and the two `shared/…` trees.
A previous version of this entry said the value "never reaches a URL or a file path", which is true of
install and false here, and it filed the check as cosmetic. **Do not remove `IsValidVersionFormat` from
that method in the name of symmetry with install.** It is the first of two things standing in front of a
`Directory.Delete(path, recursive: true)`.

Measured, both layers:

- `IsValidVersionFormat` is `^\d+\.\d+(\.\d+)?$`. It accepts `10.0` and `10.0.1`, and rejects `..`,
  `../../etc`, `10.0/../..`, an embedded newline, a trailing space, and anything carrying shell
  punctuation.
- `DeleteDirectory` calls `SanitizeSubdirectoryPath` before doing anything: it splits on both separator
  kinds, throws on a `.` or `..` segment, and only then combines with the normalized root.

So there is no gap today. The entry is kept because the reason there is none was recorded wrongly, and a
reader acting on the old wording would have deleted a control believing it formatted an error string.

Install now validates the format too, which was the original cosmetic half: a malformed version used to
travel as far as `GetReleaseByVersionAsync`, match nothing, throw, and return "operation failed" instead of
naming what was wrong with the input. Nothing about that path touches the filesystem — the downloaded file
name comes from the matched release object, never from the string the caller typed.

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

## Prerequisite for a roadmap feature

### Harden `ArchiveExtractorService` before it extracts anything user-supplied

**Not a defect today. Do not fix it as one.** The weaknesses below are unreachable under the current
threat model, and recording them as security findings would misrepresent the risk.

`Tools/Infrastructure/ArchiveExtractorService.cs:49` has three:

- The zip-slip guard is `absoluteTargetPath.StartsWith(targetDirectory)` with **no trailing separator**,
  and `FileManagerService.GetDirectory("")` returns `Path.GetFullPath(...)`, which never ends in one.
- Only `TarEntryType.Directory` is special-cased; symlink and hardlink entries fall through to
  `ExtractToFile`, which creates the link without validating its target.
- `Tools/Runtime/DownloaderService.cs` verifies no hash, and `install_dotnet_runtime`
  (`common-functions.sh:212`) simply untars. SHA512 is checked only at build time by `build-spk.sh`.

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
