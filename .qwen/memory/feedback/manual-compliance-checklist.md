---
name: Run manual compliance checklist after every code change
description: Must execute the manual compliance checklist from AGENTS.md §7 after every code modification, before responding to user
type: feedback
---

After making ANY code change (not just at the end), run the manual compliance checklist from AGENTS.md §7 "Compliance Enforcement" BEFORE responding to the user:

1. **String/String pattern** — grep for `string\.` in modified files (CRITICAL — most frequently violated)
2. **Magic strings/numbers** — verify all literals replaced with constants
3. **Logger calls** — verify `[LoggerMessage]` extensions used (no direct ILogger calls)
4. **Control flow blank lines** — verify blank lines before/after control flow
5. **Target-typed `new`** — verify `new()` used when type is inferred
6. **Method declaration format** — ≤4 params on one line (unless >200 chars)

**Why:** The user caught standards violations in GlobalizationSettings.cs after I "fixed" a bug — I applied the functional fix but skipped the compliance checklist. The `dotnet format` command only enforces tooling rules, not manual checks. Missing these creates technical debt and requires rework.

**How to apply:** After every edit sequence (before saying "done" or responding), explicitly verify each manual check item. If any fail, fix them immediately. This is non-negotiable — functional correctness and standards compliance are equally important.
