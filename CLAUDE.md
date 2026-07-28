# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

`AGENTS.md` is the authoritative standards document for this repo; `docs/ai/technical-architecture.md` is the detailed architecture reference.
This file summarizes the essentials — when in doubt, consult those.

## Commands

Run from the repo root, exactly as written (no substitutions — see AGENTS.md §16 "Command Fidelity"):

```bash
dotnet format ./src/Askyl.Dsm.WebHosting.slnx --verbosity quiet
dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx
dotnet clean /nr:false ./src/Askyl.Dsm.WebHosting.slnx
dotnet test ./src/Askyl.Dsm.WebHosting.Tests --no-build
```

- Mandatory sequence after every code change: **Format → Build → Test → Verify** — zero errors, zero warnings, and
  `dotnet test` exits 0. Never skip format.
- A healthy test run takes ~5s and exits 0. If the host hangs or aborts, that is a deadlock in the code, not a tooling
  quirk — reproduce with `--blame-hang-timeout 10s` to capture a dump, then fix the cause (see AGENTS.md §4).
- Single test: append `--filter "FullyQualifiedName~<TestClassOrMethod>"` to the test command.
- **NEVER** use `dotnet run` or launch the application (AGENTS.md §13).
- Markdown changes: validate with `markdownlint <file-path>` verbatim (config in `.markdownlint.yaml`).
- SPK package build: `./src/scripts/build-spk.sh` (output lands in `dist/`). Version bump: `./src/scripts/update-version.sh` (syncs `src/Directory.Build.props` and `src/spk-project/INFO`).
- CI lint gate is `dotnet format --verify-no-changes`.

## Architecture

.NET web-hosting manager for Synology DSM 7.2+, packaged as an `.spk`: a Blazor **Interactive WebAssembly** app (FluentUI) that manages third-party .NET web apps on a NAS —
process start/stop, .NET runtime installation, DSM reverse-proxy config, file browsing.
All projects target `net10.0` with `EnablePreviewFeatures=true` (C# 14 scoped `extension` blocks are used).

Solution: `src/Askyl.Dsm.WebHosting.slnx` (XML slnx format). Project dependency order:
`Constants ← Data ← Globalization`; `Tools → Constants, Data, Logging`; `Ui.Client → all of those`; `Ui (host) → everything + Ui.Client`; `Tests → everything incl. Analyzers`.

Key structural facts that span multiple projects:

- **Dual service implementations**: interfaces live in `Data/Contracts/` and are implemented twice — server-side in `Ui/Services/` (real logic) and client-side in
  `Ui.Client/Services/` (HTTP proxy calling `/api/v1/...` controllers). When adding a service capability, you usually touch both plus the thin controller in
  `Ui/Controllers/` (`[AuthorizeSession]` on everything except authentication).
- **DSM communication chain**: Client service → Controller → `Ui/Services` → `IDsmSession` (Scoped; SID/username in ASP.NET session, TTL-cached validation serialized by a `SemaphoreSlim`;
  validation calls the admin-only `SYNO.Core.User.get` and **fails closed**, which is also the app's only administrator check — do not relax it) →
  `DsmApiClient` (Singleton in `Tools/Network/`; stateless — SID passed per call; lazy `SYNO.API.Info` handshake guarded by `SemaphoreLock`).
  URL building and serialization format (Form vs Json) are driven by the `IApiParameters` implementation.
- **Website hosting subsystem**: `WebSiteHostingService` (Singleton + BackgroundService) owns a `ConcurrentDictionary` of sites; each site's `SiteLifecycleManager`
  serializes start/stop/state/dispose through a bounded `Channel<LifecycleCommand>` with `TaskCompletionSource`-carrying command records (avoids TOCTOU races).
  Processes are abstracted behind `IProcessRunner`/`IProcessHandle`; config persisted atomically to `websites.json`.
- **Custom Roslyn analyzers** (`Askyl.Dsm.WebHosting.Analyzers`, injected into every project via `src/Directory.Build.props`, severity Error):
  ADWH01001/01002 missing blank lines and ADWH01003/01004 extra blank lines before `else`/`catch`, ADWH02001 `String.`/`string` pattern, ADWH03001 no direct `ILogger` calls.
- **Result pattern over exceptions**: `Data/Results/` (`ApiResult`, `ApiResultData<T>`, …). `IOptions<T>`/config binding are deliberately not used — configuration is read directly.
- **DSM settings** come from `/etc/synoinfo.conf` via `IFileReader`; `src/Askyl.Dsm.WebHosting.Ui/dev-mock/` provides a mock for local dev.
- Server pipeline runs under path base `/adwh`; login is rate-limited (5/min); session cookie `ADWH.Session` is Strict/HttpOnly/Secure.

## Code conventions (the non-obvious ones)

- **`String.` vs `string`**: PascalCase `String.Equals(...)`, `String.IsNullOrWhiteSpace(...)`, `String.Empty` for static members; lowercase `string` for types/variables/parameters. Analyzer-enforced.
- **Logging**: never call `logger.LogInformation(...)` etc. directly. Use `[LoggerMessage]` source-generated extension methods in `Askyl.Dsm.WebHosting.Logging/` (one file per service domain),
  inject `ILogger<ILogXxx>` marker types, pick EventIds from the ranges in `Constants/Logging/LogEventIds.cs`, and give every log method an XML `<summary>`.
- **No magic strings/numbers**: constants go in `Askyl.Dsm.WebHosting.Constants` — add the constant first if missing.
- Primary constructors are mandatory for classes with constructor parameters (except abstract/inheritance-constrained). Use `[GeneratedRegex]` for all regex.
- Blank lines before/after complete control structures; never between statements inside a block; never between a comment and its statement. Blank line after `#region`, before `#endregion`.
- Method declarations with ≤ 4 parameters stay on one line (unless > 200 chars); > 4 parameters always multi-line.
- Collection emptiness: `.Count == 0` / `.Length == 0`, or `is { Count: > 0 }` for nullables.
- All code, comments, and commit messages in English.

## Git rules

- Never commit without asking; show the proposed message and wait for approval.
- Never run destructive git commands (`git reset --hard`, `git clean -fd`, `git checkout -- .`, etc.) without explicit user confirmation.
- Commit messages: conventional `type: description`, ≤ 50-char summary, focus on *why*, never list changed files, and **no AI attribution/co-author trailers**.

## Documentation

- ALL AI-generated documentation goes in `docs/ai/`.
- `docs/ai/technical-architecture.md` is the architecture reference — consult before feature work, and keep it updated when architecture changes
  (it has drifted before; e.g. it once claimed Interactive Server render mode when the app uses Interactive WebAssembly).
- External code PRs are not accepted (translations only) — see CONTRIBUTING.md.

## UI

Prefer FluentUI Blazor components, icons, colors, spacing, and typography; no inline styles (use FluentUI theming); verify FluentUI doesn't already provide a behavior before adding custom CSS.
