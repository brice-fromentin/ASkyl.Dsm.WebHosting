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

1. ~~**Apply the permission allowlist** (D2).~~ Done — `.claude/settings.json`.
2. ~~**Remove or scope §14** (D3).~~ Done — scoped to local-model setups.
3. ~~**Start `docs/ai/dsm-api-notes.md`.**~~ Done. Synology does not document the Core APIs, so every fact learned
   about them currently lives in chat history and has to be re-asked. Seed it with what is already
   established: `SYNO.Core.User.get` is admin-only and is therefore the application's entire
   administrator gate; `SYNO.API.Auth.logout` at version 6 works and returns 200. Cheap, and it stops the
   same questions recurring.
4. **Make the application runnable** (D1). The real project. Everything unverifiable traces back here.
5. **Add one runtime gate.** Once item 4 lands, the mandatory sequence should end with something that
   starts the application and asserts it serves a request. Today every gate is local and syntactic —
   format, zero warnings, blank lines, `String.` vs `string`, parameters per line. All pass, always. Not
   one asks whether the feature works.
6. **Formalise the deployment log review.** Reading a deployed log found the PR #39 cache defect, which
   was invisible in source — the class looks correct until you notice `IDsmSession` is Scoped. That habit
   is worth more than any rule currently in `AGENTS.md`. Write it down as a step: deploy, pull the log,
   compare against the previous one.

## Post-merge check — run this in a fresh session

Once the PR is merged, start a new session at the repository root and paste:

```text
Read docs/ai/plans/2026-07-31-unblocking-development.md and run the post-merge check.
```

Everything below must be answered from a clean context. The point is to catch a configuration that looks
right in a diff and does nothing in practice — the imports especially, which cannot be verified in the
session that wrote them.

1. **Imports loaded.** Without opening any file, state what the arrow notation `X > Y` means in this
   repository, and what §13 now permits. The first fact lives only in `AGENTS-WORKING-PREFERENCES.md`,
   the second only in `AGENTS.md`. Needing to read a file to answer means the imports did not load, and
   the assistant is running with no project instructions at all — stop and fix that before anything else.
   Maintainer side: `/context` must list `CLAUDE.md` under **Memory files**.
2. **Permissions.** Create a throwaway branch and an empty commit:
   `git checkout -b chore/verify-unblocking` then `git commit --allow-empty -m "chore: verify permissions"`.
   Neither should raise an approval prompt. Then clean up: `git checkout main`, `git branch -D chore/verify-unblocking`.
   A prompt on the branch creation or the commit means `.claude/settings.json` is not being read. A prompt
   on `git checkout main` is expected — plain `git checkout` is deliberately not in the allowlist.
3. **§14 scoped.** Issue two independent tool calls in a single turn. If nothing objects, the local-model
   rule is correctly inapplicable here.
4. **Repository state.** `docs/ai/` contains `dsm-api-notes.md` and no longer contains
   `2026-07-25-codebase-assessment.md`.
5. **Ready for stage A.** `dotnet --list-runtimes` shows an ASP.NET Core 10 runtime, and
   `/etc/synoinfo.conf` does **not** exist — local configuration must come from `dev-mock/` only.

Report each as pass or fail with the evidence. Do not repair anything before reporting: a silent fix
hides which part of the setup was wrong.

## Verifying it worked

Do not declare this done on the basis that the changes were made. Measure the next feature session:

- **Approval round-trips per PR: target under 2** (currently ~2 per PR plus a merge, times ten PRs).
- **Runtime questions answerable without asking the maintainer.** Pick a concrete one already open —
  `XForwardedProto`, or `UseHttpsRedirection` — and see whether it can be settled without a human relaying
  a log.
- **A deliberately introduced runtime fault is caught by the gates**, not by a deployment. If it is not,
  item 5 did not land.

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
