# Codebase Assessment — 2026-07-25

Point-in-time review of `ASkyl.Dsm.WebHosting` at branch `cleanup/doc-stability-and-code-hardening` (HEAD `2f57fab`).
This is a snapshot audit, not a living document — do not maintain it. Delete it once the items are triaged.

Scope: architecture, documentation accuracy, security, packaging/CI, test suite integrity, and an overall judgement.
Every finding below was verified against source or by executing the project's own documented commands.

## Addendum — resolved on this branch after the snapshot

The findings below are recorded as first observed. Four have since been addressed:

- **P0-1 (command-loop deadlock) — fixed.** Command dispatch now runs inside a per-command error boundary that releases
  the waiting caller before logging, so a handler fault can no longer kill the loop or strand callers. Validation returns
  a result instead of throwing, all three `TaskCompletionSource` instances use `RunContinuationsAsynchronously`, and
  `EnsureLoopStarted` uses double-checked locking, restoring the channel's `SingleReader` contract.
- **The test-suite illusion — fixed, and `--blame-hang-timeout` is gone entirely.** With the deadlock resolved, the full
  suite runs 521/521 and exits 0 in ~5s **without** the flag, which disproves the documented claim for the whole suite,
  not just a subset. The flag was therefore removed from the standard command in `AGENTS.md`, `CLAUDE.md`,
  `.opencode/command/test.md` (which still carried the original false wording) and both CI workflows; the docs now keep
  only a one-line note that a hang means a deadlock to diagnose, never a flag to adopt. Removing the flag is a stronger
  fix than the "compare passed against discovered" rule this report originally recommended — that rule only had leverage
  because the flag let a killed host print `Réussi! 493`. With no flag, exit code 0 is sufficient on its own.
  Because dropping the flag also drops the CI hang guard, both workflow jobs gained `timeout-minutes` (15 / 45) so a
  future deadlock fails fast instead of running to GitHub's 360-minute default.
  The mandatory sequence is now Format → Build → Test → Verify, with Verify requiring the test run to exit 0.
  Removing the deadlock exposed 11 tests that had never actually run: 8 whose fixture could not satisfy the boundary
  rule, and 3 that asserted via `Moq.Verify` on static `[LoggerMessage]` extension methods — invalid by construction,
  and doubly so because ADWH03001 forbids the usual `ILogger.Log` workaround.
- **P0-4 (bypassable boundary check) — moot; the check was removed by decision.** The rule was wrong in premise: DSM
  reports `ApplicationRealPath` as `/volume{n}/{AnyShare}/…` (set from FileStation `real_path` in
  `WebSiteConfigurationDialog.razor`), so a user may host from *any* shared folder. There is no `/shared/` convention in
  DSM at all, and `/web/` is merely the Web Station share. As written the rule blocked most legitimate deployments —
  `/volume1/MyApps/site.dll` was denied — so it was deleted along with its ten tests rather than repaired.
  **Residual exposure:** nothing now constrains which `.dll` is spawned beyond the service account's filesystem
  permissions, and `ApplicationRealPath` arrives in the request body rather than from FileStation. The control that
  actually matters here is the administrator authorization in P0-3, which remains open.
- **Magic strings in the boundary check — moot** for the same reason.

Everything else below stands, in particular P0-2 (undrained stdout/stderr pipes) and P0-3 (no privilege check plus
fail-open session validation), which pre-date this branch and exist on `main` too.

---

## 1. Bottom line

The project has excellent *form* and unreliable *function*. Formatting is clean, the build is warning-free, three custom
Roslyn analyzers enforce house style at `Error` severity, and 493 tests report green. Underneath that, the core feature —
starting and supervising a hosted website — contains a permanent deadlock, hosted sites freeze once they log ~64 KB to
stdout, and any authenticated DSM user (not just an administrator) can execute code as the package's service account.

The most damaging single fact is not any individual bug. It is that the test suite **appears** green while ~38 tests never
execute, `dotnet test` exits `1`, and the cause was misdiagnosed four times and then written into `AGENTS.md` as an
environment quirk. A false explanation in the authoritative standards document teaches every future contributor and every
AI agent to accept the symptom. That is the highest-leverage thing to fix, because it is what allowed the rest.

Assessment of the stated status: `docs/ai/technical-architecture.md:69` says **"Status: Production-ready"** while
`src/spk-project/INFO:15` says **`beta="yes"`**. The package file is right and the document is wrong.

---

## 2. Verification baseline

Commands were run exactly as documented in `AGENTS.md` §4 (no substitutions).

| Command | Result |
|---|---|
| `dotnet format ... --verify-no-changes` | Pass, exit 0 |
| `dotnet build /nr:false ...` | Pass — **0 errors, 0 warnings** |
| `dotnet test ... --blame-hang-timeout 10s` | **Exit 1.** "493 passed" but run aborted, test host killed |
| `dotnet test --list-tests` | **531 tests discovered** |
| `markdownlint` on README/AGENTS/CLAUDE/architecture | Pass, no output |

531 discovered versus 493 reported means **~38 tests never run**. Confirmed by isolating the three affected classes:

| Test class | Tests | Actually pass | Result |
|---|---|---|---|
| `SiteLifecycleManagerPathValidationTests` | 10 | **0** | hangs on first test, blocks the rest |
| `SiteLifecycleManagerTests` | 21 | **1** | hangs at `StartAsync_WhenStopped_StartsProcessViaRunner` |
| `WebSiteHostingServiceTests` | 11 | **7** | hangs at `AddWebsiteAsync_Succeeds_WithValidConfiguration` |
| Analyzers + Globalization subset (control) | 97 | 97 | **exits cleanly, no hang, no dump** |

The control run disproves the documented explanation. `AGENTS.md:41` and `CLAUDE.md:20` state the xUnit VSTest adapter on
.NET 10 "does not exit after tests complete". It exits perfectly well when the hosting tests are excluded. The hang is a
product defect, not a tooling defect.

---

## 3. Critical findings

### P0-1 — `SiteLifecycleManager`'s command loop has no error boundary; any exception deadlocks the site permanently

`src/Askyl.Dsm.WebHosting.Ui/Services/SiteLifecycleManager.cs:132-161`

`ProcessSiteCommandsAsync` dispatches commands inside a `switch` with **no `try`/`catch` anywhere in the loop**, and each
caller waits on a `TaskCompletionSource` that only the handler completes:

```csharp
case StartCommand start:
    start.Result.SetResult(ProcessStartCommand());   // throws => SetResult never runs
    break;
```

When a handler throws, three things happen at once:

1. `SetResult` is skipped, so the caller's `await tcs.Task` (line 56) **never completes** — and `StartAsync`/`StopAsync`
   accept no `CancellationToken`, so there is no timeout and no way out.
2. The exception propagates out of both `while` loops, **terminating the single consumer loop**.
3. `_commandLoop` stays non-null, so `EnsureLoopStarted` (line 124) never restarts it. Every later `StartAsync`,
   `StopAsync` and `GetRuntimeStateAsync` on that site queues a command nobody will ever read.

The site is bricked for the lifetime of the process. Because the channel is bounded at 16
(`WebSiteConstants.CommandChannelCapacity`), the first 16 subsequent calls hang forever and the rest fail fast.

**Confirmed production trigger.** `ProcessStartCommand` calls `ValidateApplicationPath()` at line 174, which throws
`UnauthorizedAccessException` — and it sits *outside* the `try` block that begins at line 192:

```csharp
ValidateApplicationPath();                       // line 174 — throws, uncaught
if (!File.Exists(configuration.ApplicationRealPath)) { ... }
try { _process = processRunner.Start(startInfo); }   // line 192 — the try starts here
```

Any `ApplicationRealPath` not under `/volume*/…/shared/` or `/volume*/…/web/` therefore deadlocks the site.

**Blast radius, in increasing severity:**

- The HTTP request for "start website" hangs forever; the browser spins with no error.
- The site becomes permanently unmanageable — start, stop and status all hang.
- `WebSiteHostingService.GetAllWebsitesAsync` (`WebSiteHostingService.cs:55-60`) awaits `GetRuntimeStateAsync()` for every
  site with **no cancellation token**. One bricked site therefore **hangs the entire dashboard listing**, permanently.
- Worst case: `WebSiteHostingService.StartAsync` (line 235-242) awaits `StartEligibleSitesAsync()` **before**
  `base.StartAsync`, and `IsEnabled`/`AutoStart` both default to `true`
  (`Data/Domain/WebSites/WebSiteConfiguration.cs:30,32`). A single persisted site with a bad path means the hosted service
  never finishes starting, so **the application never begins serving traffic**. A bad value in `websites.json` makes the
  package unbootable, with no timeout and no diagnostic.

This same defect is what breaks the test suite. In `SiteLifecycleManagerTests` the config uses `Path.GetTempPath()`
(line 40) — not under `/volume` — so every test that reaches `ProcessStartCommand` deadlocks. In
`WebSiteHostingServiceTests` the path *is* valid but `ILoggerFactory.CreateLogger` is never mocked, so Moq returns `null`
and the `logger.ApplicationBinaryNotFound(...)` call throws a `NullReferenceException` — a different exception, the same
deadlock. One structural defect, two triggers.

**Fix:** wrap the dispatch in `try`/`catch`, and fault the pending `TaskCompletionSource` instead of dropping it. Restart
or permanently fail the loop deliberately rather than by accident. Convert `ValidateApplicationPath` to return a result
rather than throw (which is what the existing tests already assert — see P0-4). Add
`TaskCreationOptions.RunContinuationsAsynchronously` to all three `TaskCompletionSource` instances (lines 49, 72, 93) so
caller continuations do not execute inline on the consumer loop. Give `StartAsync`/`StopAsync` a `CancellationToken`.

### P0-2 — Hosted websites freeze once they write ~64 KB to stdout or stderr

`src/Askyl.Dsm.WebHosting.Ui/Services/SiteLifecycleManager.cs:362-363`

`CreateProcessStartInfo` sets `RedirectStandardOutput = true` and `RedirectStandardError = true`, but **nothing ever reads
those pipes**. `SystemProcessRunner.Start` does not call `BeginOutputReadLine`, and `IProcessHandle`
(`Tools/Infrastructure/ProcessHandle.cs`) exposes no output member at all. Verified by grep: the only stream reads in the
solution are in `VersionsDetectorService`.

When the OS pipe buffer fills (64 KB on Linux), the child process blocks on write and stops serving requests. Every
ASP.NET Core application logs to console by default, so this affects the product's primary use case — it is a matter of
uptime, not of load. The process still reports `HasExited == false`, so the dashboard shows the site as healthy while it
is hung.

The README roadmap lists "Route applications `stdout` and `stderr` to accessible logs" as a *future feature*. It is not a
missing feature; it is an active defect. Either drain both pipes on background tasks (and forward to Serilog, which gets
the roadmap item for free), or set both redirect flags to `false`.

### P0-3 — Any authenticated DSM user can execute code as the service account (no privilege check)

`src/Askyl.Dsm.WebHosting.Ui/Authorization/AuthorizeSessionAttribute.cs:15-25` and all controllers

`[AuthorizeSession]` is the only gate, and it checks *only* that a DSM SID is still alive. There is no role, admin or
group check anywhere in the codebase — no `AddAuthorization`, no policy, no admin flag. Reverse-proxy and ACL calls are
implicitly permission-checked because they carry the user's SID to DSM, but two capabilities are purely local and run with
the `AskylWebHosting` service account's privileges regardless of who asked:

- `WebsiteHostingController.AddWebsite` + `StartWebsite` → spawns `dotnet <path>` — arbitrary code execution.
- `FrameworkManagementController.Install/Uninstall` → local filesystem writes and deletes.

A non-admin DSM user gains code execution as a system user in the `http` group — something DSM would not let them do
directly. This is privilege escalation (OWASP A01).

**Amplifier — session validation fails open.** `Ui/Services/DsmSession.cs:111-120` treats *only* a null response or DSM
error `-4` as invalid; every other outcome sets `_sessionValid = true`. `SYNO.Core.User.get` normally requires admin
rights, so a non-admin session receives a permission error — which this code reads as "valid". The fail-open branch admits
precisely the users who should be rejected.

**Fix:** fail closed (`response?.Success == true` only), determine administrator status at login and store it, deny
non-admins in the filter, and restrict the package to administrators in the DSM privilege manifest.

### P0-4 — The directory boundary check is bypassable, and the field it guards is never validated

`src/Askyl.Dsm.WebHosting.Ui/Services/SiteLifecycleManager.cs:279-298`

The check is a prefix test plus two substring tests, with **no path normalisation**:

```csharp
if (!path.StartsWith("/volume", ...)) throw ...;
string? parentDir = Path.GetDirectoryName(path);
if (parentDir is null || (!parentDir.Contains("/shared/") && !parentDir.Contains("/web/"))) throw ...;
```

`Path.GetDirectoryName` does not resolve `..`. Verified: `/volume1/web/../../../usr/lib/evil.dll` yields
`parentDir = "/volume1/web/../../../usr/lib"`, which starts with `/volume` and contains `/web/` — **the check passes** —
and the path resolves to `/usr/lib/evil.dll`. `StartsWith("/volume")` also admits `/volumefoo`.

Compounding this, `WebSiteConfigurationValidator`
(`Globalization/Validators/WebSiteConfigurationValidator.cs`) validates `ApplicationPath` but **never
`ApplicationRealPath`** — the field actually passed to `dotnet` and to this check. The one field that drives process
execution is unvalidated at the API boundary. Neither field rejects `..`, even though
`ValidationConstants.PathTraversalLiteral` exists and `FileSystemService.IsPathValid` does this correctly.

Three secondary notes on the same code path:

- The hardcoded `"/volume"`, `"/shared/"`, `"/web/"` literals violate the project's own no-magic-strings rule
  (`AGENTS.md` §6.5) — in security-critical code.
- User-supplied environment variables are applied *after* the framework's own
  (`SiteLifecycleManager.cs:367-373`), so `ASPNETCORE_URLS` can be overridden to bind `0.0.0.0` and bypass the reverse
  proxy. `DOTNET_STARTUP_HOOKS` / `LD_PRELOAD` are likewise unfiltered.
- Validation throwing rather than returning a result contradicts the documented Result-pattern architecture — and the ten
  tests added alongside it all assert `Assert.False(result.Success)`, i.e. the author's intent *was* a failure result.

---

## 4. Security findings (beyond the P0 items)

| Sev | Finding | Location |
|---|---|---|
| Medium | **Reflected XSS.** `originalPath` is interpolated unescaped into a `text/html` response; CSP allows `'unsafe-inline'`, so it executes. Unauthenticated. | `Ui/Endpoints/ErrorEndpoints.cs:43-61` |
| Medium | **Login rate limiter is one global window**, not per-IP — any anonymous caller can consume 5/min and lock out every user. `UseForwardedHeaders` is absent, so even per-IP partitioning would see only nginx's address. | `Ui/Program.cs:109-118` |
| Medium | **Logout never revokes the DSM SID.** Local session values are cleared; no `SYNO.API.Auth` logout call exists, so a captured SID stays valid on the NAS. | `Ui/Services/AuthenticationService.cs:39-44` |
| Low | Zip-slip prefix check lacks a trailing separator; symlink/hardlink tar entries are extracted unvalidated; no runtime hash verification at install time. Mitigated because archives come from Microsoft over HTTPS. | `Tools/Infrastructure/ArchiveExtractorService.cs:47-63` |
| Low | `[AuthorizeSession]` denies with `ForbidResult` but no auth scheme is registered, so denial throws and becomes a 500 instead of 401/403. Fails closed, but noisily. | `Ui/Authorization/AuthorizeSessionAttribute.cs:23` |
| Low | `InstallFrameworkAsync` skips the `IsValidVersionFormat` check that `Uninstall` performs. | `Ui/Services/FrameworkManagementService.cs:20-51` |
| Low | `FluentValidation.AspNetCore` **11.3.1** against `FluentValidation` **12.1.1** — major-version mismatch on a package the author has deprecated. Auto-validation is the only server-side input gate; verify it still binds. | `Ui.csproj:17` vs `Globalization.csproj:14` |
| Info | `CSP` includes `'unsafe-inline'` and `'unsafe-eval'`; Blazor WASM needs only `'wasm-unsafe-eval'`. | `Ui/Program.cs` |
| Info | Session ID is not rotated on login (fixation); low risk given `HttpOnly`/`Secure`/`Strict` and no client-trusted material. | `Ui/Services/DsmSession.cs` |

### Controls verified as correct

Worth stating plainly, because a lot here is done well: `IsPathValid` correctly rejects `..` plus single- and
double-encoded variants and is applied on both entry points (`FileSystemService.cs:51,101,229-257`); credentials go in
POST form bodies, never query strings (`AuthLoginParameters`); no secrets are logged or returned; all DSM calls are
hardcoded HTTPS with **no** certificate-validation bypass anywhere; no SSRF vector (host/port come only from
`/etc/synoinfo.conf`, API paths from the handshake table); session cookie is `HttpOnly` + `Secure` + `SameSite=Strict`
with a 30-minute idle timeout; the validation TTL cache is per-scope, not static, so no cross-user poisoning; security
headers are set on every response; error pages leak only exception type names; `UseShellExecute=false` with a fixed
`dotnet` filename means no shell injection; `FileManagerService` sanitises path segments properly; server-side
FluentValidation auto-validation *is* wired (`Program.cs:51,54`) with `[ApiController]`; config writes are atomic
(temp file + `File.Move` overwrite).

---

## 5. Architecture assessment

The layering (`Constants ← Data ← Globalization`, `Tools`, `Ui.Client`, `Ui`) is clean and the dependency direction is
respected. The Result pattern, the constants discipline, source-generated `[LoggerMessage]`, immutable records for DSM
models, and `SemaphoreLock` are all sound and consistently applied. DI lifetimes are correct and match the documentation
exactly — one of the few tables that survived verification intact.

Three architectural criticisms:

**The channel-based lifecycle manager solved the wrong half of the problem.** It was introduced to eliminate TOCTOU races,
and it does serialise state mutation. But it has no error boundary (P0-1) and its loop start is itself racy:
`_commandLoop ??= ProcessSiteCommandsAsync()` (line 124) is called from three public methods, and `??=` on a plain field
is not atomic. Two concurrent callers can both start a loop, violating the channel's own `SingleReader = true` contract.
The class comment claims "Disposal waits for pending commands to drain"; `Dispose()` (line 108-120) does not wait at all —
it queues a command and returns, so on shutdown the process can exit before child processes are killed, orphaning them.
`UpdateInstanceAsync` (`WebSiteHostingService.cs:342`) unconditionally disposes the manager, whose `DisposeCommand`
force-kills the running process — so a rename, which `ConfigurationRequiresRestart` deliberately classifies as *not*
needing a restart, still SIGKILLs the site and races a new process onto the same port.

**"Dual service implementations" is coupling described as abstraction.** Prerendering is disabled
(`App.razor:63`, `new InteractiveWebAssemblyRenderMode(false)`) and the host project contains only two `.razor` files, so
no component can ever bind to a server-side implementation. The shared interfaces have no polymorphic call site; they just
force every signature change into two projects. The leak is visible: `Ui.Client/Services/FileSystemService.cs`
implements `SetHttpGroupPermissionsAsync` as `throw new NotSupportedException(...)`. An interface that cannot be
implemented on both sides is not a shared contract.

**No liveness concept.** A web-hosting manager's central question is "is the site actually serving?" The answer here is
only "does the PID exist". No health checks, no port probe, no HTTP check (grep confirms no `AddHealthChecks`). Combined
with P0-2, a hung site displays as running. The README lists this as a roadmap item; it is closer to a core requirement.

Smaller items: `async void UpdateHtmlLangAndDir` (`Ui.Client/Services/CultureManager.cs:278`) can crash the WASM app with
an uncatchable exception; `_ = ShowWebSiteConfigurationDialogAsync(instance)` (`Home.razor:118`) discards a task and its
exceptions; `HttpClientExtensions` never disposes `HttpResponseMessage`; `WebSitesConfigurationService`
`EnsureServiceInitializationAsync` write-tests `AppContext.BaseDirectory` rather than the configured directory;
`AddWebsiteAsync` performs two side effects (ACLs, reverse-proxy rule) before persisting and has no compensating rollback,
so a later failure leaves an orphaned DSM proxy rule.

On the positive side, the client HTTP proxies are better than typical: `GetJsonOrDefaultAsync`/`PostJsonOrDefaultAsync`
give every call an explicit localized fallback, so API failures degrade to messages rather than exceptions.

---

## 6. Documentation assessment

`docs/ai/technical-architecture.md` is 868 lines and has absorbed roughly thirty corrective commits
(`docs: fix test file count`, `docs: remove rot-prone content`, `docs: fix service contract signatures`, …). It is still
wrong in material ways. This is not a writing problem; it is a structural one — the document restates facts the code
already expresses, so it decays continuously.

| Doc claim | Reality |
|---|---|
| "Hybrid rendering mode (InteractiveServer + InteractiveWebAssembly)" (`:51,86,95`) | WebAssembly only. No `AddInteractiveServerComponents` exists. `:560` states it correctly — the document contradicts itself. Also "Blazor Hybrid" (README `:42`) is the MAUI/WPF model, not this. |
| `OperationTimer` "used across ReverseProxyManagerService, FrameworkManagementService, WebSiteHostingService, SiteLifecycleManager, DownloaderService, DotnetVersionService, WebSitesConfigurationService" (`:277,494`) | **One** usage in the entire solution: `DsmApiClient.cs:137`. |
| All persistent data lives under `/var/packages/AskylWebHosting/var/` and "survives package upgrades" (`:750-759`) | `websites.json`, `logs/`, and downloaded runtimes are all written relative to the app directory. Only `websites.json` is backed up by `preupgrade`. **Logs and downloaded runtimes are silently lost on every upgrade.** |
| "Deployment is entirely manual"; CI pipeline "planned"/"would operate" (`:798-817`) | `.github/workflows/build.yml` already implements the described verify/release split, SPK build and release attachment. |
| "the server does not expose trace identifiers in API responses"; surfacing `X-Request-ID` is a "future enhancement" (`:705,714`) | `RequestTrackingMiddleware.cs:13-15` already generates and echoes it. Separately, `HttpContext.Items["RequestId"]` is written and never read, so the "propagation" is hollow. |
| "Status: Production-ready" (`:69`) | `src/spk-project/INFO:15` declares `beta="yes"`. |
| "Full CancellationToken support across all async operations" (`:57`) | `IVersionsDetectorService.GetInstalledVersionsAsync()` takes no token; `DotnetVersionService.GetInstalledVersionsAsync` accepts one and never forwards it (`:25-41,124`); `ILicenseService`/`ITreeContentService` take none. |
| HTTPS port 7121 is an "SSL-enabled alternative" (`:769`) | Declared in `adwh.sc`, but `start-stop-status` binds `http://0.0.0.0:7120` only. Nothing listens on 7121. |
| Analyzer range "ADWH01001-03001" (`:79,131,167`) | `BlankLineAnalyzer.cs:14-15` also ships **ADWH01003** and **ADWH01004**, documented nowhere — including in `CLAUDE.md`. |
| `preinst` does "architecture detection" (`:776`); `postuninst` "final cleanup" (`:781`) | Arch detection is in `postinst`; `postuninst` is literally `exit 0`. |
| Resource keys as `L.*` (`:257`) | The class is `LK`. |
| `SystemProcessHandle` lifetime "Transient" (`:291`) | Not DI-registered at all; constructed by `SystemProcessRunner`. |

Accurate and verified: the DI lifetime tables, middleware pipeline order, session cookie configuration, Serilog
configuration, the 16-interface contract inventory (method names all correct), the `.editorconfig` severity table, the
thin-controller claim (301 lines across six files, all one-line delegations), the "no component rendering tests" note, and
the `Download.ChannelVersion` dual-purpose coupling.

Other documentation issues:

- `AGENTS.md:41` / `CLAUDE.md:20` state a **false** root cause for the test hang (see §2). This is the most harmful
  sentence in the repository.
- `AGENTS.md` §6.5 forbids magic strings; the security boundary check hardcodes three (see P0-4). §11 was corrected from
  "Interactive Server" to "Interactive WebAssembly" in the uncommitted working copy, but the architecture document still
  carries the old claim in three places.
- `docs/ai/plans/2025-07-23-codebase-hardening.md` is dated 2025 in its filename while the work it describes landed
  2026-07-23, and it still shows 32 unchecked boxes for tasks that are demonstrably done.
- `src/Askyl.Dsm.WebHosting.Analyzers.Tests/` is a ghost directory containing only stale `bin/`/`obj/` — the residue of
  reverted commit `004650d`. Not in the solution, not mentioned anywhere.

---

## 7. Packaging and CI

**The release pipeline cannot work, for two independent reasons.**

1. `dotnet test` exits `1` (measured). `build.yml`'s `verify` job therefore fails on every push and PR to `main`, and the
   trigger is deterministic on Linux too — `Path.GetTempPath()` is `/tmp`, which is not under `/volume`. This resolves the
   question of whether the hang is dev-machine-specific: it is not.
2. `verify` is gated `if: "!startsWith(github.ref, 'refs/tags/v')"` and `release` declares `needs: verify` without
   `always()`. On a tag push `verify` is skipped, so GitHub Actions skips `release` too. The release path is dead as
   written.

Meanwhile `.github/workflows/dotnet-ci.yml` duplicates restore/build on the same triggers with its **Test step commented
out** (`:32-33`), and its `dotnet list package --vulnerable` step does not fail on findings (verified: exit 0). The likely
practical effect is a green badge from the workflow that runs no tests, beside a red one that does. Delete
`dotnet-ci.yml` or make it the real gate. `build.yml` also configures a runtime cache in `verify` but never builds the
SPK there, and omits the markdownlint step the architecture document promises.

**Shell scripts.** `build-spk.sh` is genuinely good: `set -euo pipefail`, quoted expansions, `rm -rf` only on readonly
variables, and SHA512 verification on every runtime download including cached ones. Minor nits only (`local x=$(cmd)`
masks exit codes; `jq -r` yields the string `"null"` for a missing key, which the `[ -z ]` check at `:71` won't catch).

The SPK lifecycle scripts, which run **as root on the user's NAS**, are weaker — none use `set -e`/`-u`/`pipefail`:

- **Real bug:** in `src/spk-project/scripts/postupgrade`, the `log_fatal_with_temp` call in the runtime-verification block
  is split across two lines **without a trailing backslash**. The second line executes as a standalone command (exit 127)
  and the user-facing failure message is never written. `postinst` has the backslash; `postupgrade` lost it.
- **No install-time integrity check.** `install_dotnet_runtime` (`common-functions.sh:212-262`) just untars the bundled
  archive; SHA512 is verified only at build time. A corrupted `.spk` installs silently, caught at best by the
  `dotnet --info` smoke test.
- `pkill -f "Askyl.Dsm.WebHosting.Ui"` as root kills anything matching that string; prefer the PID file.
- `stop_app` escalates to `kill -9` after 2 s, and force-killing the host orphans hosted-site children — there is no child
  cleanup.

Version management is fine: `Directory.Build.props` `0.6.0` and `INFO` `version="0.6.0"` are **in sync**.

---

## 8. Test suite reality

427 declared test methods, 710 assertions, ~1.7 per test — reasonable density, and the analyzer, globalization, converter
and result-type tests are genuinely good. But:

- **~38 tests never execute** and the suite exits `1` (§2). Everything covering the website-hosting subsystem — the
  product's reason to exist — is in that set.
- The 235 lines of path-boundary tests added in commit `beff3b2` (2026-07-23) have **never passed**. They were written,
  committed, and reported green while hanging. They assert `Assert.False(result.Success)`; the code throws instead.
- `AuthorizeSessionAttribute` — the single most security-critical class — has **no tests**. Neither do the six
  controllers, `ProcessRunner`, `ProcessHandle`, `ProcessTerminator`, `DownloaderService`,
  `RequestTrackingMiddleware`, `ErrorEndpoints`, or either FluentValidation validator.
- No Blazor component rendering tests exist (bunit is referenced but used only in the navigation-guard test) — correctly
  acknowledged in both the architecture document and the README roadmap.
- `ResourceCompletenessTests` hardcodes `fr-FR` (`:41,61`), so the "drop in a `.resx`, zero code changes" story is real at
  runtime but any new culture is silently untested for key parity.

The history shows the hang was fought four times as an infrastructure problem — `7a04b85 test: … fix test runner hang`,
`a50113a fix: prevent WebSiteHostingServiceTests hang by serializing with [Collection]`, `f895e98 fix: disable test
collection parallelization` (later reverted), and finally `89a7817 docs: add --blame-hang-timeout to test command in
AGENTS.md`. The last commit is where a defect became a documented standard.

---

## 9. Honest overall assessment

**What is genuinely good.** The engineering instincts on display are strong. Layering is disciplined and the dependency
graph is acyclic and enforced. The Result pattern is applied consistently rather than decoratively. Constants hygiene is
better than most production codebases. Source-generated logging with per-domain EventId ranges is the right call.
`build-spk.sh` is careful, hash-verifying, multi-architecture work. The localization design — deferred message
resolution, satellite-assembly discovery, DSM-driven culture with date/time format cloning — is more thoughtful than
almost any comparable project. `FileSystemService.IsPathValid` handles double-encoding, which most people miss. Solo
authorship of 388 commits over nineteen months to this level of internal consistency is real achievement.

**The central problem is that the quality apparatus measures the wrong things.** Consider the effort distribution:

| Concern | LOC |
|---|---|
| Blank-line placement enforcement (analyzer + code fix + tests) | **1,258** |
| All three custom analyzers + their tests | **1,909** |
| The entire website-hosting subsystem (the product) | **1,254** |

The project has invested slightly more code in enforcing where blank lines go than in the feature it exists to deliver —
and the blank-line machinery works flawlessly while the hosting subsystem deadlocks. Every gate in `AGENTS.md` is a
*local, syntactic* gate: format, build, zero warnings, magic strings, blank lines, `String.` versus `string`, parameter
counts per line. All of them pass. Not one of them asks "does the feature work end to end?" The mandatory sequence is
Format → Build → Verify, where "Verify" means *zero warnings* — not zero failing tests, and certainly not "the app
starts". `AGENTS.md` §13 forbids ever running the application, so nothing in the documented process could ever have caught
P0-1 or P0-2.

Two process rules actively worked against discovery. §14's "exactly ONE tool per turn, never group tool calls" removes the
breadth needed to notice that 531 discovered tests became 493 reported. §16's "command fidelity — use documented commands
verbatim, no improvements" is reasonable in intent, but here the documented command *embeds the workaround*: anyone who
runs it sees "Réussi! 493" and moves on. The false explanation beside it closes the loop. A rule meant to ensure
reproducibility instead guaranteed that everyone reproduced the same blind spot.

**On the AI-assistant experiment.** The README states the project is "an experimental sandbox for evaluating AI-driven
coding assistants", which makes this the most interesting finding of the review. The experiment produced code that is
*locally* immaculate and *globally* broken, and that is a predictable outcome of the incentive structure: the agent was
given dozens of precise, checkable, per-edit rules and no end-to-end success criterion. It optimised exactly what it was
measured on. The commit history reads accordingly — long runs of `refactor:`/`revert:` pairs churning style
(`use null-coalescing assignment in EnsureLoopStarted`, which introduced the loop-start race; `revert: use Task.Delay
instead of TaskCompletionSource gate`), thirty commits patching a document that restates the code, and four attempts to
suppress a deadlock's symptoms without once reading the loop that caused it. `beff3b2` is the experiment in miniature: a
commit titled `security: enforce directory boundary` that added a bypassable check, a permanent deadlock, and 235 tests
that have never run — and it passed every gate in `AGENTS.md`.

**Process risks.** The branch is **144 commits ahead of `main`**, whose last commit was a month ago (2026-06-23). Nearly
40% of the project's history is unmerged, which makes the diff increasingly unreviewable and undermines CI, since CI gates
`main`. Dependabot was removed (`0647a14`) so dependency updates are now manual — and there is already a
FluentValidation major-version mismatch. Bus factor is one.

**Where this actually stands.** As a personal project on a NAS, run by its author, with sites in `/volume*/…/web/`, it
probably works day to day — which is why the defects have survived. As something to publish to Synology Package Center
(a roadmap item), it is not close: a bad path in `websites.json` makes it unbootable, hosted sites freeze after enough
log output, and any DSM user can run code as a system account. `beta="yes"` in `INFO` is the honest label.

**The one change with the most leverage** is not any bug fix. It is deleting the false explanation from `AGENTS.md:41` and
`CLAUDE.md:20` and replacing "Verify = zero warnings" with "Verify = zero warnings **and** `dotnet test` exits 0 with
discovered count == passed count". Every other finding here follows from the fact that a red suite was allowed to look
green.

---

## 10. Prioritized remediation

**Immediate (correctness and safety):**

1. Add a `try`/`catch` around the `SiteLifecycleManager` command dispatch that faults the pending
   `TaskCompletionSource`; make `ValidateApplicationPath` return a result instead of throwing (P0-1).
2. Drain or disable the redirected stdout/stderr pipes (P0-2).
3. Fail closed in `DsmSession.ValidateSessionAsync`; add an administrator check to `[AuthorizeSession]` (P0-3).
4. Normalise with `Path.GetFullPath` before the boundary check; validate `ApplicationRealPath` in the validator; reject
   `ASPNETCORE_URLS`, `DOTNET_STARTUP_HOOKS` and `LD_PRELOAD` in user environment variables (P0-4).
5. HTML-encode `originalPath` in `ErrorEndpoints`; drop `'unsafe-inline'`/`'unsafe-eval'` from CSP.
6. Fix the missing line-continuation in `postupgrade`.

**Short term:**

1. Correct `AGENTS.md:41` / `CLAUDE.md:20`, and redefine "Verify" to include a passing test run with a discovered-count
   check. Then re-run the suite and fix whatever the 38 tests actually reveal.
2. Repair CI: make `release` use `always()`/`needs.*.result`, delete or fix `dotnet-ci.yml`, add markdownlint.
3. Partition the login rate limiter and add `UseForwardedHeaders` with `KnownProxies`.
4. Call DSM `auth.logout` on logout.
5. Merge the branch to `main` in reviewable slices.
6. Add `RunContinuationsAsynchronously`; fix the `EnsureLoopStarted` race; make `Dispose` actually drain, or correct its
   comment; stop force-killing on non-restart config updates.
7. Move `websites.json`, logs and runtimes under the package `var/` directory, or document that logs and runtimes are lost
   on upgrade.

**Documentation (do less, not more):**

1. Cut `technical-architecture.md` hard. Delete every section that enumerates files, methods, LOC, lifetimes or usage
   sites — that is what caused thirty fix-up commits. Keep only what code cannot express: the DSM authentication and
   session-validation flow, the culture-resolution priority chain, the `Download.ChannelVersion` dual-purpose coupling,
   the SPK layout and upgrade-persistence behaviour, and the deliberate decisions (no `IOptions<T>`, Result over
   exceptions, singleton `DsmApiClient`).
2. Fix the render-mode claim in all three places; drop "Blazor Hybrid" everywhere; change "Production-ready" to beta.
3. Document ADWH01003/01004; delete the ghost `Analyzers.Tests` directory; correct the stale hardening plan's checkboxes.

**Product gaps worth promoting from the roadmap:** a real liveness check (HTTP probe, not PID existence), and surfacing
hosted-site stdout/stderr — which item 2 of the immediate list delivers most of.
