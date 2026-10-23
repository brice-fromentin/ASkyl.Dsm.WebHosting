# Open Technical Items

Defects, gaps and drift. **Features are not tracked here** — they belong in the README roadmap.

This document is meant to be kept current: close items as they land, and add nothing that has not been
checked against source. It replaces the 2026-07-25 assessment, a dated snapshot that was wrong on several
claims and has been deleted.

Nothing here is inherited on trust: each entry names the file, and a line where one pins the claim.

Entries are written when the defect is found and rewritten when it changes, so they do not share one
verification date. What is shared is a sweep — every remaining entry was re-checked against `main` on
**2026-10-23**, which is what the line references below reflect: one had drifted, by the eight lines PR #67
added above it. Line numbers are the first thing to go stale here; re-run the sweep rather than trusting them.

## Security

### The application listens on every IPv4 interface, not only behind nginx

`spk-project/scripts/start-stop-status:9` exports `ASPNETCORE_URLS="http://0.0.0.0:7120"`, and
`spk-project/package/etc/adwh.sc` — the `port-config` protocol file `conf/resource` registers with DSM's firewall and
port forwarding — declares 7120/tcp with `port_forward="yes"`. Nginx reaches the application over loopback
(`adwh-alias.conf`, `proxy_pass http://127.0.0.1:7120`), but nothing in the package makes that the only path: unless
the DSM firewall blocks the `adwh_http` service, a client on the network can reach port 7120 directly over plain HTTP,
bypassing the TLS nginx terminates. Inferred from the binding; not tried against a NAS.

A browser session cannot survive there — the session cookie is `CookieSecurePolicy.Always` (`Ui/Program.cs:37`) — but
a login form posted to that port still carries the credentials in clear. Binding `127.0.0.1:7120` would make the alias
the only way in; whether anything depends on the direct port, and whether `adwh.sc` should keep offering it for
forwarding, is to be checked before changing it.

### Low: a failed login logs the submitted username verbatim

`Ui/Services/AuthenticationService.cs:36,44` call `LoginFailed(login)`, EventId 1000001 at Warning:
`"Login failed for user: {Login}"`. Warning passes every production filter, so the raw string a user typed
is persisted in the log archive — including a password typed into the username field by mistake. Logging
the attempted account is common practice for auditing, which is why this is low; masking or truncating it
would be the change if the trade is judged otherwise.

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

### Three configured Serilog enrichers exist in no referenced package

`Ui/appsettings.json:33-35` (and the Development file) list `WithMachineName`, `WithThreadId` and
`WithActivity` under `Enrich`; `Ui.Client/wwwroot/appsettings.json` lists `WithActivity`. The resolved
Serilog packages in `Ui/obj/project.assets.json` include no `Serilog.Enrichers.*`, so none of the three can
bind. Not observed at runtime: `Serilog.Settings.Configuration` presumably skips the names it cannot resolve.

Nothing is lost that the logs used to carry — no output template renders those properties either — but the
configuration reads as if request correlation were in place. Either reference the enricher packages and add
the properties to the templates, or delete the three entries.

### The installed package keeps all three runtime archives

`src/scripts/build-spk.sh` downloads the three architectures into `package/runtimes/downloads` and tars the
whole `package/` directory. `install_dotnet_runtime` (`common-functions.sh`) extracts the one matching
`uname -m` and removes nothing, so the NAS keeps all three archives — 46.6 to 49.4 MB each in the last
local build — plus the release metadata, for as long as the package is installed.

### A locally built SPK ships the developer's own `websites.json`

`src/Askyl.Dsm.WebHosting.Ui/websites.json` is gitignored — it is written by local runs and holds the local site
configuration — but the Web SDK's default content glob publishes it into `admin-ui/`, and `build-spk.sh` cleans only
`*Development.json` and `dev-mock`. Measured without reading the file: the published copy is byte-identical to the
source (`cmp`), and the last locally built SPK lists `admin-ui/websites.json` in its `package.tgz`.

A fresh install from such a package starts with that site list; an upgrade restores the NAS's own copy through
`preupgrade`/`postupgrade`, so the developer's own NAS is unaffected. CI releases build from a clean checkout and do
not carry it, but none has ever been published (see *No release has ever been published*), so every SPK
installed so far was built locally. Excluding it from publish in the `.csproj`, or deleting it in `build-spk.sh`, closes it.

### Cosmetic: packaging leaves precompressed development settings behind

`build-spk.sh` deletes `*Development.json` from the publish output, which misses the client's
`wwwroot/appsettings.Development.json.br` and `.gz`: both are in the last local publish output, and the published
static-assets endpoints manifest still routes `appsettings.Development.json` to them. The client never loads them in
production and their content is the versioned public file, so nothing leaks; the pattern just does not do what it says.

### Cosmetic: a region-qualified browser language falls through to `en-US` in `App.razor`

`Ui/Components/App.razor`, `ResolveCulture()`. When DSM's language is `def` — follow the browser, which
`SystemDefaults.DefaultLanguage` names as the default — or a code the converter does not map, there is no system
culture, and the first `Accept-Language` tag is compared as a whole against two-letter language names: `fr` matches,
`fr-FR` does not, and the page falls back to `en-US`. The comment there says it takes "the first 2 chars"; the code
does not. It only decides the server-rendered `lang`/`dir` until the client sets its own
(`Ui.Client/Services/CultureManager.cs`). How often real installs run with `def` has not been measured.

### A localized string still reaches a logger at autostart

`Ui/Services/WebSiteHostingService.cs:368`. `StartEligibleSitesAsync` logs `SitesFailedToStart` (EventId
1800014, Warning) with `String.Join(", ", failures.Select(f => f.Message))`, and those messages are built
by the `localizer` — `LK.Error.SiteNotFound`, `LK.Error.OperationFailed` and the lifecycle manager's own.
PR #60 moved the one call it found off a localized string and said the class was closed; this one was
missed. Not measured on a NAS: whether the hosted service's thread carries the DSM culture at that point,
and so whether the line is actually written in French.

### `application.log` is truncated on every start

`spk-project/scripts/start-stop-status:64` starts the host with `> "$APP_LOG_FILE"`, so the console
output captured in `var/logs/application.log` is overwritten at each start. Whatever the process printed
before Serilog was up — an unhandled startup exception, for instance — survives only until the next start,
and a host that fails to boot produces no downloadable archive at all. Appending (`>>`) with a size bound
would keep it.

## Tooling and dependencies

### `compare-logs.sh` cannot see most of what reading a log by hand found

`src/scripts/compare-logs.sh` has still not run on a real archive. It was measured on 2026-09-27 against
synthetic archives in the production format — `appsettings.json`'s template and filters, the
`LogDownloadService` layout — each result reproduced by a second agent trying to refute it:

| Hand-found defect | What the script shows |
|---|---|
| #57 update failing on a sync scope disposal | lead only: section 3 lists the ERR id, never the exception |
| #60 §1 ACL success line naming the wrong path | invisible, unless the ids happen to be new |
| #60 §2 cache hit indistinguishable from a DSM call | invisible, unless the ids happen to be new |
| #60 §3 warning in French | lead only: section 3 lists the WRN id, never the text |
| #68 release lookup refused 50 ms after its first connection | lead only: no timestamp or exception text |
| #38 logout call shape | surfaced: a new id in section 1 |
| #39 validation cache resetting every request | invisible in its real condition, defect on both sides |

The mechanics behind that, each checked against the script:

- **Section 4 never finds the application's boot in production.** It brackets on the literal
  `Application started`, which `Microsoft.Hosting.Lifetime` writes at Information and `Microsoft: Warning`
  filters out. The only such text left is a hosted site's own stdout relayed as 2500002, so the section
  brackets site boots. The application's boot runs from 2800006 `ResolvingConfigPath` to 1800010
  `HostingServiceStarted`. Since 0.7.0 (#76) it opens one line earlier, with 3000001 `ApplicationStarting`.
  That line also names the build (`x.y.z+<sha>`), which is the bracket section 4 needs and the way to tell two
  archives apart. Its position comes from reading `Program.cs`: no real archive has shown it yet.
- **Section 5 times DSM calls, not requests.** Production logs no request duration. The `Nms` pattern
  catches 2000004 `ApiRequest`, the 1600019 timeout setting and relayed site output, and `cut -c1-150` cuts
  each line before the API name and the duration. The comment saying PR #39 "looked exactly like this" is
  false.
- **Section 2 drops rows silently** past `head -12`, ranked by absolute delta, so a 0 → 1 error sinks under
  high-volume noise. An id that disappeared prints with no name: `name_of_id` reads only the new log.
- **Section 3 drops** WRN/ERR/FTL lines with no numeric id, and matches the level text anywhere in the
  line, so a hosted site relaying `[ERR]` is counted under 2500002, while that site's own errors arrive as
  2500002 at Information and are never counted.
- **`collect()` reads only `*.txt`**: `install.log`, `service.log`, `application.log` and the debug log are
  never read. Paths with spaces break its `xargs`.
- **The no-argument mode orders by modification time**, which a plain `cp` resets, and ignores the timestamp
  the archive name carries; it skips directories although its comment says "whatever their form".

Until those are fixed, a real review needs by hand: the 2800006 → 1800010 block of each archive (3000001 →
1800010 from 0.7.0, its first line naming the build); each
section-3 id opened with its exception and the lines before it; WRN/ERR lines without an id; 2500002 lines
carrying `fail:` or `warn:`; the arguments of log lines checked against reality; and `package-logs/`.

### `update-nuget.sh` raises every major, and the analyzer's compiler with them

`src/scripts/update-nuget.sh` runs `dotnet outdated -u` with no version lock. Run on 2026-10-23, it took
FluentUI to 5.x — an API rewrite the client did not then compile against — and `Microsoft.CodeAnalysis` in
the analyzer project to the newest Roslyn, which must stay at or below the compiler of the SDK CI installs:
above it, the analyzer is refused with CS9057 and no ADWH rule applies (comment in
`Askyl.Dsm.WebHosting.Analyzers.csproj`). Both were put back by hand in PR #72. FluentUI has since moved to
5 on purpose, but the script will take the next major the same way. CS9057 was a warning that let the build
pass with every ADWH rule disabled; with warnings now fatal it should stop the build instead (inferred, not
yet observed). `--version-lock Major` and an
exclusion for the analyzer's Roslyn packages would make the script safe to run unattended. The file also has
no execute bit, so it runs as `sh ./src/scripts/update-nuget.sh`.

### FluentUI 5: what the migration leaves unverified

The UI moved from 4.14.5 to 5.0.1 in 0.8.0 (`docs/ai/fluentui-5-migration.md`). The build now fails on any
warning, so a removed component (RZ10012) stops it. `UiSourceAuditTests` and `ComponentRenderingTests`
cover what the build cannot see: silent parameters, CDN-backed components, stale FluentUI 4 tokens and
elements, message boxes fed raw text, the stack gap, and the login and website dialog rendering. What none
of them can see is left here:

- **Appearance as a whole.** The heading sizes, the selected-row colours and the header bar are Fluent 2
  equivalents chosen by name. The icons now take `currentColor` instead of the accent colour, which was left
  as is on purpose. Only a look at the running application settles these.
- **`tree-navigation.js`.** v5 still renders `<fluent-tree-item id expanded selected>`, but the chain from
  `.expanded = true` to the lazy load, and from `.click()` to `OnSelectedChanged`, runs in a browser only.
- **Browser baseline.** The `FluentSelect` dropdown and tooltips rely on CSS anchor positioning, which needs
  Chrome 125, Firefox 147 or Safari 26, or a polyfill the application does not ship.
- **Upstream #5516.** `FluentDataGrid` never unregisters its key listener, and the file and releases dialogs
  create and dispose a grid on every opening.
- **Built-in strings are English.** v5 routes them through `IFluentLocalizer` (confirmation buttons, "This
  field is required", the grid's empty state) but ships English only. v4 hardcoded the same English text, so
  nothing regressed, but nothing is translated either.

### No release has ever been published

The `release` job in `.github/workflows/build.yml` runs on a `v*` tag only, building the SPK with
`build-spk.sh` and attaching it to a GitHub release. On 2026-10-23, `git tag` and `gh release list` both
returned nothing. The job has therefore never produced a release, and the clean-checkout path it describes is
untested end to end. Versions are now bumped in every pull request that ships (AGENTS.md §12), and each build
names its commit (#76). That makes builds traceable, but not published. Tagging is the maintainer's call,
when a version is to be released.

### Low: `update-version.sh` accepts a version that does not go up

`src/scripts/update-version.sh` checks the `x.y.z` format only. A typo that repeats or lowers the current
version passes silently. Under the bump-per-pull-request rule, that is a pull request that looks bumped but
is not. Comparing against the current `<Version>`, which the script already prints, would close it.

## Test coverage

`ProcessHandle`, `ProcessTerminator`, `DownloaderService`, `RequestTrackingMiddleware` and the six
controllers have no tests. `AuthorizeSessionAttribute` gained them alongside its status code fix — the
pass-through, both refusal paths and the cancellation token it forwards. The two FluentValidation validators are exercised indirectly through service tests but not
directly. `ProcessRunner` and `ErrorEndpoints` gained tests in PRs #32 and #33. Component rendering tests exist since
the FluentUI 5 migration (`ComponentRenderingTests`, with bUnit): Login, Home, NotFound and two dialogs. The
other dialogs, FileSelection included, are not rendered by any test.

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
3. There is one caller, `FrameworkManagementService.cs:42`, fed by `DownloaderService` from Microsoft over
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
