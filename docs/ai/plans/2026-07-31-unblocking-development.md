# Session Agenda — Unblocking Autonomous Development

**Lifespan:** delete this file once the items are done or rejected. It is an agenda, not a reference.

**Goal of the session:** remove what forces an assistant to stop and ask, without removing what actually
protects the repository. Success is measured, not asserted — see Verifying It Worked.

## Why

Across the 2026-07-25 → 2026-07-29 sessions (PRs #30-#39), roughly **35 exchanges** were needed. Their
breakdown is the argument for this work:

| Type | Count | Did it change an outcome? |
|---|---|---|
| Mechanical approvals (`go`, `approved`, `merge`) | ~19 | No — every one was approved |
| CI status relays | ~6 | No — pollable |
| Substantive corrections | ~5 | **Yes, every time** |
| Decisions only the maintainer can make | ~4 | Yes |
| Direction setting | ~5 | Yes |

Over half the interaction was ceremony. Meanwhile the five corrections caught real errors: the upgrade
data-loss claim (twice), a misread of the framework install path, an inflated severity on archive
extraction, and a failure to read `AGENTS.md` at all.

The process spends its budget where it does not matter and leaves what does matter to instinct.

## Decisions — settled 2026-08-03

- **D1: mock the DSM API surface, in stages.** Stage A lifts §13 for local runs and proves the
  application boots and serves a request against `dev-mock/` — no `SYNO.*` fake needed for that. Stage B
  adds fakes one API at a time, when a concrete question requires one.
- **D2: the proposed allowlist, as written.** Applied in `.claude/settings.json`, with an `ask` list for
  what stays gated (it also overrides the unrestricted `git push` allow in `settings.local.json`).
- **D3: scope §14 to local-model setups.** Kept, but explicitly inapplicable on a hosted model.
- **D4: delete the 2026-07-25 assessment.** Done; `open-technical-items.md` carries what was still live.

The reasoning behind each is preserved below.

### D5. Single source of instructions (raised during the session, not in the original agenda)

`CLAUDE.md` duplicated `AGENTS.md` as a summary that told the assistant to consult the original "when in
doubt" — which is the mechanism behind one of the five substantive corrections ("a failure to read
`AGENTS.md` at all"), not inattention. `AGENTS-WORKING-PREFERENCES.md`, meanwhile, was loaded by OpenCode
only and was invisible to Claude Code.

Settled: `CLAUDE.md` becomes two `@` imports and holds no duplicated rule. The content it carried that
`AGENTS.md` lacked — the annexe commands, the cross-project architecture orientation, the CONTRIBUTING
rule — moved into `AGENTS.md`, where both harnesses see it. `opencode.json` was aligned with D2 at the
same time; it still had `git add` and `git commit` at `ask`, so D2 would otherwise have landed for one
harness out of two.

**Unverified until the next session:** that the imports actually load. Check with `/context` under
**Memory files**.

### D1. How should the application become runnable?

`AGENTS.md` §13 forbids launching it. That single rule caused, in one session: an unverified CSP change,
an unverifiable `SYNO.API.Auth.logout` call shape, `XForwardedProto` cut from a PR, `UseHttpsRedirection`
left open, and client-side validation untested in either direction.

Note what §13 did **not** prevent: P0-1 (a permanent deadlock) and P0-2 (hosted sites freezing at 64 KB)
both shipped, precisely because nothing in the process ever ran the thing.

Options:

1. **Mock the DSM API surface locally.** `dev-mock/synoinfo.conf` and the configurable
   `DsmSettings:ConfigPath` already exist; what is missing is a fake for the `SYNO.*` calls. Highest
   effort, best isolation, and makes CI able to run the app one day.
2. **A dedicated test NAS.** No mocking work, exercises the real DSM, but needs hardware and credentials
   the assistant can reach.
3. **A development mode against the real NAS**, restricted to read-only APIs. Cheapest, but the blast
   radius is a production appliance.

Whatever is chosen, §13 should become "never run against production" rather than "never run".

### D2. How far should permissions be loosened?

The key point: **the pull request is the review gate, not the commit.** Nothing reaches `main` without a
merge. Gating each commit on a feature branch is a checkpoint on a checkpoint.

Proposed allowlist (`.claude/settings.json`):

```text
git add, git commit, git checkout -b, git branch
gh pr create --draft, gh pr checks, gh pr view
dotnet format / build / test, markdownlint
```

Proposed to stay gated:

```text
gh pr merge                      outward-facing, effectively irreversible
git push origin main             same
git reset --hard, git clean -fd, git checkout -- , rm -rf, push --force
adding NuGet dependencies
```

The `/fewer-permission-prompts` skill can generate a first draft of this from the actual transcripts
rather than from guesswork.

### D3. Does `AGENTS.md` §14 still apply?

"Exactly ONE tool per turn, never group tool calls" exists to prevent VRAM saturation on a locally hosted
model. On a hosted model it roughly doubles turn count for no benefit, and it was violated for the first
third of a session without anyone noticing — which is its own evidence.

Delete it, or scope it explicitly to local-model setups.

### D4. Does the 2026-07-25 assessment get deleted?

Everything still live in it now exists in `docs/ai/open-technical-items.md` with per-entry evidence. It
self-declares "delete once triaged" and has been wrong on several claims. Keeping it risks a future reader
citing findings that were disproved.

## Mechanical work, in dependency order

Ordered by unlock per unit of effort. Items 1-3 are cheap and unblock immediately; item 4 is the real
project.

1. ~~**Apply the permission allowlist** (D2).~~ Done — `.claude/settings.json`. Extended 2026-08-03: the
   post-merge check showed every read-only diagnostic and every file edit still cost an approval, so the
   inspection set §12 already calls safe was encoded and `defaultMode` set to `acceptEdits`.

   **Extended again the same day, after D2 was found to have landed only halfway.** The permissions were
   loosened; `AGENTS.md` §12 was not. It still read "Never auto-commit / MUST ask before committing / wait
   for approval", and `AGENTS.md` outranks a settings file, so a compliant assistant kept asking for every
   commit — the exact ceremony D2 exists to delete. It was caught the way it had to be: the assistant did
   ask, and the maintainer asked why. §12 now states that the pull request is the gate, with a narrow
   exception for changes that are genuinely the maintainer's call (amending the standards files, personal
   tooling, dependencies, scope creep).

   Two lessons worth generalising. **Loosening a permission does nothing while a higher-precedence document
   still forbids the act** — settings and standards have to move together, and the standards file wins.
   And **the allowlist was incomplete in a way that guaranteed one approval per PR anyway**: no `git push`
   of any form was allowed, so opening the draft PR that D2 designates as the review gate required a prompt.
   Pushes are now allowed scoped by branch prefix, with an `ask` rule catching any push that mentions
   `main`.

   **That pairing is itself a corrected error, and the third lesson.** The prefix list was first shipped
   with the claim that "no spelling of a push can reach `main` unattended" — asserted, never checked. The
   pre-merge review checked it: a trailing `*` in a Bash permission rule matches any sequence of characters
   including spaces, so `Bash(git push -u origin feat/*)` also matches `git push -u origin feat/x main`.
   Claude Code's documentation states plainly that patterns constraining command arguments are fragile, and
   uses `Bash(git push *)` as an example of an *ask* rule rather than an allow. A positive list of prefixes
   is not a boundary; the `ask` rule is what holds, and only against accidents. **An assertion about a
   security control is worth exactly what its verification is worth** — this one was worth nothing until it
   was read against the documentation.

   The same review then found the half of the hole the first fix had missed: `git push -u origin feat/x
   --force` matches the allow rule for exactly the same reason, and the pre-existing
   `Bash(git push --force *)` never fires because it requires the flag to follow `git push` immediately.
   `--force`, `--delete`, `-f` and `-d` are now caught in any position. Finding one instance of a class of
   bug is not fixing the class — the first fix felt complete because it closed the case that had been
   *noticed*, not because anything had enumerated the rest.
2. ~~**Remove or scope §14** (D3).~~ Done — scoped to local-model setups.
3. ~~**Start `docs/ai/dsm-api-notes.md`.**~~ Done. Synology does not document the Core APIs, so every fact learned
   about them currently lives in chat history and has to be re-asked. Seed it with what is already
   established: `SYNO.Core.User.get` is admin-only and is therefore the application's entire
   administrator gate; `SYNO.API.Auth.logout` at version 6 works and returns 200. Cheap, and it stops the
   same questions recurring.
4. ~~**Make the application runnable** (D1), stage A.~~ Done 2026-08-03 — and the application boots.

   **The 2026-08-03 entry point recorded here was wrong, and its error is the lesson.** It claimed a local run
   throws from `DsmSettingsService.ResolveAndValidateConfigPath` because `appsettings.Development.json` carries
   only Serilog. That conclusion came from reading `appsettings*.json` and `launchSettings.json` and calling the
   configuration understood. It never opened `.vscode/launch.json`, which set
   `DsmSettings__ConfigPath=./dev-mock/synoinfo.conf` in the debugger's environment — and environment variables
   are layered over the JSON providers, so F5 had been booting fine for a month. Configuration lives wherever a
   provider reaches, not only where the repository's own files are: check the launcher too.

   What stage A actually needed was to move that override somewhere every launch path sees. It now lives in
   `appsettings.Development.json`, and `launch.json` no longer duplicates it. One relative value serves both
   paths, because `dev-mock/` sits in the project directory *and* is copied to the output by the `csproj`, while
   MSBuild's `RunWorkingDirectory` is the project directory and the debugger's `cwd` is the output directory.

   Verified by running it, not by reading it:

   | Launch path | Working directory | Result |
   |---|---|---|
   | `dotnet run --launch-profile https` | project directory | `GET https://localhost:5000/adwh` → **200**, 82 ms |
   | raw dll, `ASPNETCORE_ENVIRONMENT` only (the F5 conditions) | `bin/Debug/net10.0` | `GET http://localhost:5000/adwh` → **200** |

   The debug log confirms which file was read — `Resolving DSM settings from: dev-mock/synoinfo.conf
   (configured: true)`, without the `./` that `launch.json` used, so the new setting is demonstrably the one in
   effect. Both listeners were stopped afterwards.

   Two things surfaced only because the thing ran, and neither is fixed here:

   - **The local `dev-mock/synoinfo.conf` has drifted from the versioned template**, naming a real host and
     port where the template uses loopback values. The file is gitignored, so it drifted invisibly. Nothing was
     sent to it — `DsmApiClient`'s `SYNO.API.Info` handshake is lazy and only the anonymous login page was
     requested — but an instance configured toward production is what §13 exists to prevent. Reset it from the
     template.
   - **`websites.json` carries a real deployment entry** whose binary is absent locally, so it fails to start
     with `ApplicationBinaryNotFound`. Harmless, but it means local state is a copy of production state.

   Neither is quoted here on purpose. This document is public, and the first draft of this section named the
   host, the port and the deployed site — a maintainer's infrastructure, republished from private local files
   into a public repository and a pull request description. **Findings about local state get described, never
   transcribed.** The finding is the drift; the values are not needed to act on it.

   Stage B remains: fakes for the `SYNO.*` calls, added one at a time when a concrete question needs one. The
   first question to need one is login, which is the boundary this run stopped at.
5. ~~**Add one runtime gate.**~~ Done 2026-08-15. `ApplicationStartupTests` boots the real `Program.cs`
   in memory through `ApplicationHostFactory` and asserts that `/adwh` is served, that the security
   headers are present, and that a `[AuthorizeSession]` route refuses an unauthenticated caller.

   **It rides inside `dotnet test`, so nothing else changed** — not the four-step sequence, not the CI
   workflow, not a command in §4. That was the deciding argument for an in-process
   `WebApplicationFactory` over a script that launches the process and curls it: no new step, no port, no
   listener to kill, and a stack trace instead of a timeout. The rejected route also collided with §13's
   "always stop what you started", since `pkill -f` takes the maintainer's debugger with it.

   Two things worth keeping:

   - **The entry point is `App`, not `Program`.** Both the host and the WebAssembly client declare a
     top-level `Program`, so `WebApplicationFactory<Program>` is ambiguous from the test assembly. The
     type argument only locates the entry-point assembly, so any public host type serves.
   - **All mutable state goes to one throwaway directory.** The factory writes a synthetic
     `synoinfo.conf` there from `FakeDsmSettings`' constants and redirects `WebSitesConfigurationService`
     to it, so a run never reads the host's real `/etc/synoinfo.conf` and never starts a real site.

   **Proven the way the plan asked.** Removing `AddSingleton<IDsmSettingsService, DsmSettingsService>()`
   left `dotnet build` at **zero errors and zero warnings** and the other 546 tests green; only these
   turned red, naming the missing registration. A fault that would previously have reached a deployment
   now stops at the mandatory sequence. Registration restored, the suite passes.

   **A first draft of the gate asserted only `200` and `text/html`, which the maintainer challenged as too
   weak. He was right.** An error page, an empty shell and a host page whose assets failed to resolve all
   satisfy that. The gate now parses the response and requires two markers that cannot appear by accident:
   `<base href="/adwh/">`, and a *fingerprinted* `_framework/blazor.web.<hash>.js`, whose hash only a
   resolved static asset manifest produces. Proven differentially: with `MapStaticAssets()` removed, the
   build stayed clean, the `200`/`text/html` assertion still passed, and only the new one failed. **The
   strength of a gate is the fault it rejects, not the fact that it runs.**

   Checking what the gate actually received also surfaced a defect no test could have seen: every 404
   returns an empty body. Recorded in `open-technical-items.md`.

   One dependency was added for this: `Microsoft.AspNetCore.Mvc.Testing`, authorised by the maintainer.
6. **Formalise the deployment log review.** Reading a deployed log found the PR #39 cache defect, which
   was invisible in source — the class looks correct until you notice `IDsmSession` is Scoped. That habit
   is worth more than any rule currently in `AGENTS.md`. Write it down as a step: deploy, pull the log,
   compare against the previous one.

## Post-merge check — run 2026-08-03, five of five pass

Run from a clean session against `origin/main` at `85936ca`. The imports load — `X > Y` and the revised §13
were both answered with no file opened. Only allowlisted commands ran without a prompt. Paired tool calls drew
no objection, so §14 is correctly inapplicable on a hosted model. `docs/ai/` holds `dsm-api-notes.md` and no
longer holds the 2026-07-25 assessment. The machine has ASP.NET Core 10.0.10 and no `/etc/synoinfo.conf`.

One flaw in the check itself, worth carrying into the next one: **item 2 cannot be verified by the assistant
alone.** A tool result looks identical whether it was auto-approved or approved at a prompt, so an assistant
reporting "nothing prompted" is asserting something it has no way to observe — and this one did, wrongly, until
the maintainer corrected it. Only the maintainer sees prompts. Any future check of this kind must ask rather
than infer.

## Verifying it worked

Do not declare this done on the basis that the changes were made. Measure the next feature session:

- **Approval round-trips per PR: target under 2** (currently ~2 per PR plus a merge, times ten PRs).
- ~~**Runtime questions answerable without asking the maintainer.**~~ **Met, 2026-08-03.** Both named
  candidates were settled in the same run that closed stage A, from a local log rather than a relayed
  deployment log: `Program.cs:121` registers `ForwardedHeaders.XForwardedFor` only, so `X-Forwarded-Proto` is
  never processed; `app.UseHttpsRedirection()` at line 167 therefore logs `Failed to determine the https port
  for redirect` and passes the request through — observed, with `http://localhost:5000/adwh` answering 200 and
  no redirect. The middleware is inert rather than protective. Recorded in `open-technical-items.md`.
- ~~**A deliberately introduced runtime fault is caught by the gates**, not by a deployment.~~ **Met,
  2026-08-15.** Removing a service registration that only startup resolves left the build at zero errors
  and zero warnings, and the 546 pre-existing tests green; the three new startup tests failed and named
  the missing registration. The fault was invisible to every gate that existed before item 5.

## What must not change

These worked and should survive any process revision:

- **CI as the trust boundary.** Format, build, test, markdownlint and the vulnerable-package scan already
  gate `main` correctly.
- **Draft PR opened at the first commit.** `build.yml` only triggers on `pull_request`/`push` to `main`,
  so a branch without an open PR runs no CI at all. That is how PR #30 carried a red suite and ~38
  never-executing tests for a month.
- **Proving every fix by reverting it.** Temporarily undo the fix, watch the test fail for the right
  reason, restore. This caught several tests that would otherwise have passed for the wrong reason,
  including one asserting against `Request.Path` when production uses
  `IStatusCodeReExecuteFeature.OriginalPath`.
- **Small, single-purpose branches.** PR #30 was 151 commits and unreviewable; #31-#39 were one to three
  each.
- **Verifying claims before repeating them.** The 2026-07-25 assessment was confident, well-structured and
  wrong in several places. Treat any inherited finding as a lead until checked against source.
