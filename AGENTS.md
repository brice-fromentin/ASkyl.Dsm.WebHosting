# .NET WebHosting Standards

## 1. PROJECT OVERVIEW

Askyl.Dsm.WebHosting is a .NET Web sites hosting manager for Synology DSM 7.2+.
The solution consists of multiple projects that work together to provide a web‑based
UI for managing .NET web applications on Synology NAS devices.

**Project Structure:**

- Source code: `src/`
- Agent documentation: `docs/ai/` (MUST place all AI-generated docs here)

---

## 2. ARCHITECTURE REFERENCE

**ALL architectural details are maintained in `docs/ai/technical-architecture.md`.** Consult this document before working on any feature,
and keep it updated when architecture changes — it has drifted before (it once claimed Interactive Server render mode when the app uses
Interactive WebAssembly).

The orientation below is the minimum needed to know where to look; it is not a substitute for that document.

Solution: `src/Askyl.Dsm.WebHosting.slnx` (XML slnx format). All projects target `net10.0` with `EnablePreviewFeatures=true` (C# 14 scoped
`extension` blocks are used). Project dependency order: `Constants ← Data ← Globalization`; `Tools → Constants, Data, Logging`;
`Ui.Client → all of those`; `Ui (host) → everything + Ui.Client`; `Tests → everything incl. Analyzers`.

Key structural facts that span multiple projects:

- **Dual service implementations**: interfaces live in `Data/Contracts/` and are implemented twice — server-side in `Ui/Services/` (real logic)
  and client-side in `Ui.Client/Services/` (HTTP proxy calling `/api/v1/...` controllers). When adding a service capability, you usually touch
  both plus the thin controller in `Ui/Controllers/` (`[AuthorizeSession]` on everything except authentication).
- **DSM communication chain**: Client service → Controller → `Ui/Services` → `IDsmSession` (Scoped; SID/username in ASP.NET session, TTL-cached
  validation serialized by a `SemaphoreSlim`; validation calls the admin-only `SYNO.Core.User.get` and **fails closed**, which is also the app's
  only administrator check — do not relax it) → `DsmApiClient` (Singleton in `Tools/Network/`; stateless — SID passed per call; lazy
  `SYNO.API.Info` handshake guarded by `SemaphoreLock`). URL building and serialization format (Form vs Json) are driven by the `IApiParameters`
  implementation. Facts established about the undocumented DSM APIs are recorded in `docs/ai/dsm-api-notes.md`.
- **Website hosting subsystem**: `WebSiteHostingService` (Singleton + BackgroundService) owns a `ConcurrentDictionary` of sites; each site's
  `SiteLifecycleManager` serializes start/stop/state/dispose through a bounded `Channel<LifecycleCommand>` with `TaskCompletionSource`-carrying
  command records (avoids TOCTOU races). Processes are abstracted behind `IProcessRunner`/`IProcessHandle`; config persisted atomically to
  `websites.json`.
- **Custom Roslyn analyzers** (`Askyl.Dsm.WebHosting.Analyzers`, injected into every project via `src/Directory.Build.props`, severity Error):
  ADWH01001/01002 missing blank lines and ADWH01003/01004 extra blank lines before `else`/`catch`, ADWH02001 `String.`/`string` pattern,
  ADWH03001 no direct `ILogger` calls.
- **Result pattern over exceptions**: `Data/Results/` (`ApiResult`, `ApiResultData<T>`, …). `IOptions<T>`/config binding are deliberately not
  used — configuration is read directly.
- **DSM settings** come from `/etc/synoinfo.conf` via `IFileReader`; `src/Askyl.Dsm.WebHosting.Ui/dev-mock/` provides a mock for local dev.
- Server pipeline runs under path base `/adwh`; login is rate-limited (5/min); session cookie `ADWH.Session` is Strict/HttpOnly/Secure.

---

## 3. DOCUMENTATION RULES

**ALL AI-generated documentation MUST be placed in `docs/ai/`.** When in doubt, use `docs/ai/`.

External code contributions are not accepted (translations only) — see `CONTRIBUTING.md` before proposing anything that assumes otherwise.

### This repository is public — describe local state, never transcribe it

Documentation, commit messages and pull request descriptions publish the moment they are pushed, and a PR
description is mailed to every watcher. Gitignored files — `dev-mock/synoinfo.conf`, `websites.json`,
`logs/`, `logs-review/`, anything under `bin/` — hold the maintainer's real infrastructure, and are
gitignored precisely so it stays out of the repository.

- ❌ Never quote a hostname, IP, port, credential, share or deployment path read from local state
- ❌ Never paste a raw log excerpt without checking every line of it for the same
- ✅ Name the *finding*: "the mock has drifted from the template and points at a real host" is actionable
- ✅ Values from versioned templates and fixtures are fine — already public by definition

This rule exists because it was broken, and editing a description afterwards does not unpublish it: GitHub
keeps the revision and the notification mail has gone out. The check belongs *before* the push.

---

## 4. BUILD & FORMAT WORKFLOW

### Standardized Commands

```bash
dotnet format ./src/Askyl.Dsm.WebHosting.slnx --verbosity quiet
dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx
dotnet clean /nr:false ./src/Askyl.Dsm.WebHosting.slnx
dotnet test ./src/Askyl.Dsm.WebHosting.Tests --no-build
```

**NEVER** substitute your own flags or paths in these commands. Running the application is a separate
matter, governed by §13.

**Test command:** a healthy run completes in ~5s and exits 0. If the test host ever hangs or aborts, that is a deadlock in the
code — never normalise it with a timeout flag. Reproduce with `--blame-hang-timeout 10s` to capture a dump, then fix the cause.
Single test: append `--filter "FullyQualifiedName~<TestClassOrMethod>"` to the test command.

**Other commands:**

- Markdown changes: `markdownlint <file-path>` verbatim (config in `.markdownlint.yaml`)
- SPK package build: `./src/scripts/build-spk.sh` (output lands in `dist/`)
- Version bump: `./src/scripts/update-version.sh` (syncs `src/Directory.Build.props` and `src/spk-project/INFO`)
- Deployment log review: `./src/scripts/compare-logs.sh` (see §13, and the `📊 Logs` task in VS Code)

The CI lint gate is `dotnet format --verify-no-changes` — a formatting slip fails the build, not just the review.

### Mandatory Sequence: Format → Build → Test → Verify

1. **Format** — run format command above
2. **Build** — run build command above
3. **Test** — run test command above
4. **Verify** — zero errors, zero warnings, **and** `dotnet test` exits 0

**NEVER skip the format step.** A build that compiles is not a passing build — "Verify" also requires the test run to exit 0.

**The test step starts the application.** `ApplicationStartupTests` boots the real `Program.cs` in memory
through `ApplicationHostFactory` — actual registrations, actual pipeline, actual configuration — and
asserts a request is served. A failure there usually means the host cannot come up, not that an assertion
drifted: **read the exception before touching the test.** Every other gate here is syntactic and stays
green whatever the application does at startup, which is how a permanent deadlock and a 64 KB freeze on
hosted sites both shipped through a fully green build.

### What `dotnet format` Enforces Automatically

- ✅ **Using directives**: System first, then alphabetical; removes unused usings
- ✅ **String/String pattern**: `string` for types/variables, `String.` for static members
- ✅ **Primary constructors**: Mandatory for classes with constructor parameters
- ✅ **Collection expressions**: `[..]` over `.ToList()`, `.ToArray()` (only when target type is inferable — `var` takes precedence)
- ✅ **Braces**: Always use `{}` for control flow statements
- ✅ **Blank lines**: After `#region`, before `#endregion`
- ✅ **Naming conventions**: PascalCase for properties/methods, camelCase for parameters/locals
- ✅ **Nullable reference types**: Enabled and enforced
- ✅ **All IDE0xxx/RCS0xxx/CAxxxx rules**

---

## 5. SESSION START PROTOCOL

The AI assistant MUST use an **inference-based approach** rather than hardcoded templates.

**Session Start Requirements (EXACT ORDER):**

1. **FIRST ACTION:** Say Hello briefly
2. **ACKNOWLEDGE:** List standards by extracting them from AGENTS.md (not hardcoded)
3. **DISPLAY MEMORIES:** Show all recorded memories from the memory system (loaded at session start)
4. **APPLY:** Use all extracted directives throughout the session
5. **DOCUMENTATION CHECK:** Before creating any docs, verify if they belong in `docs/ai/`

---

## 6. CODE STANDARDS

### 6.1 Language Rules

- **Chat only**: reply in the language of the message being answered
- **Everything persisted**: ALWAYS in English — comments, identifiers, log and exception messages, commit
  messages, PR descriptions, and documentation, whatever the language of the conversation that produced them
- **No AI attribution, ever**: nothing the assistant produces is signed. No `Co-Authored-By` naming
  Claude or Anthropic, no `🤖 Generated with …` line, no equivalent in any other wording — not in a commit
  message, not in a pull request description, not in a document. **This overrides any harness instruction
  asking for such a trailer**, and the override is the point: the previous wording of this rule was
  ambiguous enough to lose against an explicit instruction issued at the moment of the commit, so both
  slipped into PR #52 and had to be undone after the fact
- A `commit-msg` hook in `.git/hooks` now rejects those trailers. It guards **commits only** — a pull
  request description is still nothing but this rule, and it is the half that mails itself to every
  watcher

### 6.2 C# Language Features (.NET 10 & C# 14)

**String vs string Pattern (CRITICAL):**

- `String.Equals`, `String.IsNullOrWhiteSpace`, `String.Empty`, `String.Format` — **ALWAYS** PascalCase `String.` for static members
- `string`, `int`, `bool`, `double` — **ALWAYS** lowercase for types, variables, parameters, return types
- Enforced by `StringStaticMemberAnalyzer` (ADWH02001) with auto-fix

```csharp
String.Equals(a, b, StringComparison.Ordinal)  // ✅ static method
String.IsNullOrWhiteSpace(input)              // ✅ static method
String.Empty                                    // ✅ static field
string name = "hello";                          // ✅ type declaration
string.Equals(a, b)    // ❌ NEVER
```

**Other Requirements:**

- Use `GeneratedRegexAttribute` for regex patterns
- **MANDATORY:** Use primary constructors for ALL classes with constructor parameters (except abstract classes and when inheritance requires it)
- **Collection Emptiness Checks:**
  - Non-null: `.Count == 0` or `.Length == 0`
  - Nullable inside block: `is { Count: > 0 }` — compiler knows it's not null inside
  - Avoid `?.Count > 0 == true` or `!.Any() == false`
- Use conditional null operator (`?`) for truly optional scenarios
- Fix all compiler warnings after build completion

### 6.3 Code Structure and Style

**General Principles:**

- Apply DRY and SOLID principles
- Use early returns to avoid deep nesting
- **Prefer simplicity** — Choose the simplest viable solution

**Method Declarations (MANUAL CHECK REQUIRED):**
Declarations with **≤ 4 parameters** on a single line unless total line length exceeds **200 characters**. Multi-line for >4 params regardless of length.

```csharp
public async Task<ApiResult> StopWebsiteAsync(Guid id)  // ✅ 3 params, single-line
public async Task<Result> CreateWebsiteAsync(  // ✅ 6 params, multi-line
    string name, Guid id, int port, string path, bool enableSsl, CancellationToken cancellationToken)
```

**Method Calls:**
Single-line for short calls. Multi-line for complex expressions with multiple parameters.

**Blank Line Rules (enforced by ADWH01001-01004):**

- Blank lines BEFORE/AFTER complete control structures (not first/last in scope)
- NO blank lines BETWEEN statements inside blocks
- NO blank line before `else` or `catch` — they continue the structure above (ADWH01003/01004)
- Comments stay with their code — no blank line between comment and its statement

```csharp
// ✅ CORRECT
// This is an important check
if (condition)
{
    DoSomething();
    DoOtherThing();
}

DoNextThing();

// ❌ WRONG — blank line inside block
if (condition)
{
    DoSomething();

    DoOtherThing();
}

// ❌ WRONG — blank line between comment and code
// This check validates the input

if (condition)
{
    DoSomething();
}
```

**Additional Rules:**

- Use expression-bodied members for single expressions without method chaining
- Ternary operators acceptable in expression-bodied members
- Properties with both get/set: always multi-line format
- Blank line after `#region`, before `#endregion` (enforced)

### 6.4 Collections and Type Inference

- Use `var` with `[]` initializers when type is obvious from immediate context
- Use explicit types with `[]` when clarity is needed
- Always use `new()` when type can be inferred
- Prefer collection expressions `[..]` over `.ToList()`, `.ToArray()` — **only when target type is inferable**
- **Keep parameterized constructors on DTOs/records** when they enable one-line declarations

### 6.5 Constants Management

- Store magic numbers and strings in `Askyl.Dsm.WebHosting.Constants`
- Use named constants or enums instead of hard‑coded values
- If a constant does not exist, add it to the appropriate constants file first

### 6.6 Logging Standards

**ALL logging must use `[LoggerMessage]` source-generated extension methods.** Enforced by `LoggerDirectCallAnalyzer` (ADWH03001).

**Rules:**

- **No direct ILogger calls** — Never write `logger.LogInformation("...")`, `logger.LogError("...")`, etc.
- **Use extension methods** — `logger.LoginFailed(login)`
- **Specialized `ILogger<T>`** — Services inject `ILogger<ILogXxx>`, not bare `ILogger`
- **EventId assignment** — Consult `Constants/Logging/LogEventIds.cs` for range base
- **Extension file location** — `Askyl.Dsm.WebHosting.Logging/` — one file per service domain
- **XML doc comments** — Every `[LoggerMessage]` method must have `<summary>`

```csharp
logger.LoginFailed(login);           // ✅ extension method
logger.LogWarning("Login failed");   // ❌ direct ILogger call
```

**When adding new log methods:**

1. Identify service's EventId range in `Constants/Logging/LogEventIds.cs`
2. Find next available ID in corresponding extension file
3. Add `[LoggerMessage]` method with XML doc comment
4. Update `LogEventIds.cs` range comment if the range extends

---

## 7. COMPLIANCE ENFORCEMENT

### Tooling-Enforced (No Manual Check Required)

- **Using directives**: System first, alphabetical; unused removed
- **Primary constructors**: Mandatory for classes with constructor parameters
- **Collection expressions**: `[..]` over `.ToList()`, `.ToArray()` (when inferable)
- **String/String Pattern** (ADWH02001): `StringStaticMemberAnalyzer` with auto-fix
- **Logger Call Compliance** (ADWH03001): `LoggerDirectCallAnalyzer`
- **Control Flow Blank Lines** (ADWH01001-01004): `BlankLineAnalyzer` with auto-fix

### Manual Checks Required

1. **Magic Strings and Numbers** — replace ALL hardcoded strings/numbers with constants
2. **Target-Typed `new`** — use `new()` when type inferable (exception: variable name already includes type)
3. **Markdown Validation** — run `markdownlint <file-path>`, fix ALL errors

### Non-Negotiable Enforcement

After EVERY code modification:

1. **Format** → **Build** → **Fix issues** (including analyzer errors)
2. **Manual checks** — magic strings/numbers, target-typed `new`

---

## 8. PRE-RESPONSE CHECKLIST

### Before Writing Code

- [ ] Read "Compliance Enforcement" section
- [ ] Identify required constants (no magic strings/numbers)
- [ ] Identify correct `[LoggerMessage]` extension method (or plan new one)
- [ ] Review Git Safety Rules if git operations are needed

### During Writing

- [ ] Use constants from `Askyl.Dsm.WebHosting.Constants` (create if needed)
- [ ] Use `[LoggerMessage]` extension methods (no direct `ILogger` calls)
- [ ] Method declarations with ≤ 4 params on one line (unless > 200 chars)
- [ ] Comments/messages ONLY in English
- [ ] Apply architectural guidelines from `docs/ai/technical-architecture.md`
- [ ] Trust tooling for: String/String pattern, using directives, primary constructors, collection expressions, blank lines, logger calls

### After Writing

- [ ] Run `dotnet format` → `dotnet build /nr:false`
- [ ] Verify no magic strings/numbers remain (MANUAL)
- [ ] Verify method declarations with ≤ 4 params on one line (MANUAL)
- [ ] Validate English-only comments
- [ ] Ensure successful build with no errors or warnings
- [ ] Run `markdownlint <file-path>` for .md file changes
- [ ] FluentUI requirements met (for UI code)
- [ ] Application launch restrictions respected
- [ ] Documentation files placed in `docs/ai/` if AI-generated
- [ ] Git safety rules followed (if git operations involved)

---

## 9. EXTERNAL INTEGRATIONS

### Synology DSM APIs

- FileStation, ReverseProxy, Authentication APIs
- Documentation: <https://global.download.synology.com/download/Document/DeveloperGuide/Synology_File_Station_API_Guide.pdf>
- OSS Documentation: <https://github.com/pmilano1/synology-dsm-api/tree/master/docs/api-reference>
- SSL validation enabled; all interactions through `DsmApiClient`

**For API integration patterns, see `docs/ai/technical-architecture.md` section "Data Models & API Integration".**

### Testing DSM interactions: fake on demand, never speculatively

No general `SYNO.*` mock exists and none should be built ahead of a need. **Add a fake for one API when a
concrete question requires it**, not before. The insertion point is `ApplicationHostFactory`, which builds
the real host, so a test replaces `DsmApiClient`'s message handler rather than standing up a fake DSM.

**Check the overlap first.** `open-technical-items.md` records a disposable DSM instance as the prerequisite
for validating the SPK lifecycle scripts, and a real instance answers some of the same questions. Building
both for one question is the waste this rule exists to prevent.

---

## 10. FRAMEWORK REQUIREMENTS

### FluentUI Requirements

- Prefer FluentUI components, icons, colors, spacing, typography over alternatives
- No inline styles — use FluentUI theming (minor positioning excepted)
- CSS Minimalism: Verify FluentUI provides behavior before adding custom CSS
- Documentation: <https://www.fluentui-blazor.net>

**For component inventory, see `docs/ai/technical-architecture.md` section "UI Architecture".**

### Web Search Guidelines

**MANDATORY:** Perform web searches for potentially outdated information:

1. **.NET Updates** — new C# features, runtime updates, breaking changes, NuGet updates
2. **Third-Party Libraries** — releases, deprecations, security advisories
3. **Framework Updates** — Blazor/FluentUI, Serilog, Synology API changes
4. **Best Practices** — new C# 14+ patterns, security, performance

**Search Strategy:** Use `web-search` with specific queries; verify against official docs; cross-reference sources.

---

## 11. PROJECT-SPECIFIC NOTES

- UI uses Interactive WebAssembly render mode with antiforgery protection
- Logs structured using Serilog with configuration‑based setup
- Solution supports multiple CPU architectures (Any CPU/x64/x86)
- SPK packaging includes .NET multi‑architecture packages

**For detailed architecture, see `docs/ai/technical-architecture.md`.**

---

## 12. GIT SAFETY RULES (CRITICAL)

**NEVER execute dangerous git commands without explicit user confirmation.**

### Forbidden Without Explicit Authorization

- ❌ `git reset --hard`, `git reset --soft HEAD`
- ❌ `git clean -fd`, `git clean -ffdx`
- ❌ `git checkout -- .`
- ❌ `git rebase --abort`, `git reflog expire`, `git gc --prune=now`

### Required Safety Protocol

**BEFORE any state-modifying git command:**

1. **SHOW** the exact command
2. **EXPLAIN** impact
3. **GET** explicit confirmation
4. **RUN `git status`** first

### Committing: the pull request is the gate, not the commit

Nothing reaches `main` without a merge, so gating each commit on a feature branch is a checkpoint on a
checkpoint. Encoded in `.claude/settings.json`, which allows `git add`, `git commit` and
`gh pr create --draft`. The reasoning is in the description of PR #43, where it landed.

- ✅ Commit freely on a feature branch once format, build and test are green — no approval round-trip
- ✅ Open the draft PR at the first commit, so CI runs from the start
- ✅ Keep a branch to **one purpose**. PR #30 was 151 commits and unreviewable; every defect caught in
  review since has been caught in a branch of one to three. If a second purpose appears while working,
  say so and let the maintainer decide whether to split
- ✅ Say what was committed and why, after the fact, in a line or two
- ❌ Never commit on `main`, and never merge a PR, without explicit authorization
- ❌ Never commit a red build, a failing test, or an unformatted tree

**Correct workflow:** make changes → format → build → test → commit → push → draft PR → report

Ask before committing only when the change is genuinely the maintainer's call — amending this file or
`AGENTS-WORKING-PREFERENCES.md`, touching personal tooling such as `.vscode/`, adding a dependency, or
anything whose scope exceeds what was asked for. That exception is narrow on purpose: it exists for
decisions, not for reassurance.

### Commit Message Conventions

1. **NEVER** list changed files or include "Files Modified:" sections
2. **FOCUS** on "why" not "what"
3. **Use** conventional commit format: `type: description`
4. **Keep concise** — summary line (50 chars max), blank line, short bullets

```text
# ❌ WRONG
fix: HttpClient lifetime violation in LicenseService (Phase 5)

Files Modified:
- LicenseService.cs: Fixed lifetime

# ✅ CORRECT
fix: HttpClient lifetime violation in LicenseService (Phase 5)

Prevents socket exhaustion by using field-based HttpClient injection
instead of per-call disposal. Uses named client with configured
BaseAddress for /adwh sub path mapping.
```

### Safe Operations (No Confirmation Needed)

- ✅ `git status`, `git diff`, `git log`, `git branch`, `git checkout -b`
- ✅ `git add`, `git commit`, and `git push -u origin <branch>` on a feature branch
- ✅ `gh pr create --draft`, `gh pr checks`, `gh pr view`

Always push with the explicit `git push -u origin <branch>` form, every time. Argument-less `git push` is
gated because what it pushes depends on remote state rather than on the command.

**The allowlist is a convenience, never a boundary.** A trailing `*` in a Bash permission rule spans
spaces, so a prefix rule constrains the start of a command and nothing else: `Bash(git push -u origin
feat/*)` also matches `git push -u origin feat/x main`. Upstream documentation calls such patterns
fragile. `ask` rules on `main`, `--force`, `--delete`, `-f` and `-d` hold the line against accidents, not
against a determined bypass.

**The real boundary is on GitHub.** `main` requires a pull request with `format`, `build-test` and `lint`
passing; force pushes, deletions and non-linear history are refused; admins included. `vulnerable` is
deliberately **not** required — it is skipped on pull requests, and a skipped required check blocks a merge
forever.

---

## 13. APPLICATION LAUNCH RESTRICTIONS

The rule is **never run against production**, not never run.

- NEVER point a running instance at the production NAS: no real DSM host, no real credentials
- Local runs are allowed only against the development mock (`src/Askyl.Dsm.WebHosting.Ui/dev-mock/`),
  which supplies `synoinfo.conf` through the configurable `DsmSettings:ConfigPath`
- Always stop what you started — never leave a listener running between tasks
- Everything else still goes through the standardized build/clean commands

### Standardized Run Commands

```bash
dotnet run --project src/Askyl.Dsm.WebHosting.Ui --no-build --launch-profile https
pkill -f "Askyl.Dsm.WebHosting.Ui"
```

Run both **from the repository root** — the `--project` path is relative and fails with MSB1009 elsewhere.
Build first: `--no-build` keeps a run from rebuilding behind the mandatory sequence. Serves
`https://localhost:5000/adwh`, with the local dev certificate, so `curl` needs `-k`.

**`pkill -f` matches the whole command line**, including a VS Code debug session whose `program` path
contains the same string. Run `pgrep -fl "Askyl.Dsm.WebHosting.Ui"` first and stop only what you started.

**Always `dotnet run --project`, never the built dll.** `dotnet run` sets the *launched application's*
working directory to the project directory (MSBuild's `RunWorkingDirectory`), so the mock resolves whatever
the shell's own directory. Running the dll leaves the working directory where the shell stood, and that
directory is also the content root: from anywhere but the output directory ASP.NET loads **no
`appsettings*.json` at all**, so startup dies naming `/etc/synoinfo.conf` — an error that reads as a missing
mock and is really a missing configuration.

`appsettings.Development.json` points `DsmSettings:ConfigPath` at `dev-mock/synoinfo.conf`, which resolves
under both the project directory and the output directory. **Do not re-add that override to
`.vscode/launch.json`**: an environment variable there overrides the JSON silently, and a drifted one is why
a whole session concluded the application could not start.

The mock is gitignored. On a fresh clone, create it and **keep the template's values** — a mock naming the
real NAS turns every local run into a §13 violation:

```bash
cp src/Askyl.Dsm.WebHosting.Ui/dev-mock/synoinfo.conf.template src/Askyl.Dsm.WebHosting.Ui/dev-mock/synoinfo.conf
```

### Deployment Log Review

**After every deployment, read the log against the previous one.** It found the two defects nothing else
could: the `SYNO.API.Auth.logout` call shape (PR #38), and the validation cache resetting every request
because `IDsmSession` is Scoped — invisible in a class that looks correct, stated plainly in the log
(PR #39).

Deploy, download the log archive from the application's own log page, drop it in the gitignored
`logs-review/` beside the previous one, then run the `📊 Logs` task in VS Code or:

```bash
./src/scripts/compare-logs.sh
```

No arguments takes the two most recent archives; two paths compare anything else, each a `.zip`, a
directory or a `.txt`. It reports new event ids, count changes, warnings and errors by owning service, a
startup-sequence diff, and duration outliers — the shape the PR #39 defect took.

**The script surfaces, it does not decide.** A new event id is a lead, not a verdict.

**Its output is local infrastructure data** — hostnames, ports, deployment paths. §3 applies to it in
full: read it, act on it, and never transcribe it into a document, a commit message or a pull request
description.

---

## 14. EXECUTION ARCHITECTURE & SUB-AGENTS (SUPERPOWERS)

**Applies only to a locally hosted model**, where the constraint is VRAM saturation (PCIe swap) on this
host. On a hosted model it costs turns and buys nothing — ignore this section entirely.

Under a local model:

- STRICT SEQUENTIAL EXECUTION: exactly ONE tool, skill, or sub-agent per output turn
- WAIT FOR FEEDBACK: no guessing outputs
- AVOID PARALLEL ARRAYS: never group tool calls

---

## 15. CONTEXT COMPRESSION WARNING (CRITICAL)

**If you detect context compression or session state reset:**

1. **Re-read AGENTS.md immediately** — extract current standards dynamically
2. **Acknowledge ALL critical rules explicitly**
3. **Apply enforcement language strictly**
4. **Verify before responding** — Format → Build → Test + manual checks

**DO NOT rely on memory from previous tasks.** Always re-read AGENTS.md when in doubt.

---

## 16. COMMAND FIDELITY (CRITICAL)

**ALWAYS use documented commands EXACTLY as specified — no substitutions, no "improvements".**

When AGENTS.md specifies a command (e.g., `markdownlint <file-path>`), use it verbatim. Do not:

- ❌ Substitute your own implementation of the command
- ❌ Add "optimizations" or "improvements" to the command structure
- ❌ Replace with an equivalent tool you think is "better"

**Why:** Documented commands are tested and verified for your environment. Substituting commands breaks the build chain and introduces hard-to-debug failures.

**Examples:**

- ✅ `markdownlint /path/to/file.md` (documented command)
- ❌ `npx markdownlint-cli2 --config ... /path/to/file.md` (substitution)

---

## 17. NON-COMPLIANCE CONSEQUENCES

Failure to follow these instructions systematically is a critical error and must be corrected immediately.

---

## 18. VERIFICATION HABITS

One root: **never confuse what you believe with what you have measured.** Numbered last so existing
references to §12, §13 and §16 stay valid.

**Prove a fix by reverting it.** Undo it, watch the check fail *for the right reason*, restore. A test
never seen to fail has not been shown to test anything. It caught a test asserting on `Request.Path` where
production uses `IStatusCodeReExecuteFeature.OriginalPath`, and it is how the runtime gate was shown to be
worth having — and, a day later, too weak: with `MapStaticAssets()` removed the app still answered `200`
with `text/html`. **The strength of a gate is the fault it rejects, not the fact that it runs.**

**Verify a claim before repeating it.** Treat an inherited finding as a lead until checked against source;
the 2026-07-25 assessment was confident, well-structured and wrong for weeks. This bites hardest on claims
about a safety control, which nobody re-reads: "no spelling of a push can reach `main` unattended" went
into this file *and* a pull request description unchecked, and was false. The review that caught it found
the same hole for `--force` that the first fix had missed — **finding one instance of a class of bug is
not fixing the class.**

**You cannot observe your own approval prompts.** A tool result looks identical whether it ran unattended
or was approved at a prompt; only the maintainer sees prompts. Any check of that kind must **ask rather
than infer** — and more generally, say what was verified, say what was assumed, and never let the second
wear the clothes of the first.
