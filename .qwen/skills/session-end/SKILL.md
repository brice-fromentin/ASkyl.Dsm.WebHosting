---
name: session-end
description: Update plan and prepare commit. Use when the user signals end of a phase or session wrap-up.
---

# End Session Workflow

Trigger when the user says "end session", "session end", "wrap up", or signals they're done working.

## Steps

0. **Announce**

   Output `Skill session-end has been triggered`

1. **Ask the user what to commit**

   Present the current `git status` and ask if they want to commit phase changes.

2. **Commit if confirmed**

   Follow AGENTS.md git safety rules — show diff, propose message, get approval.

3. **Summarize**

   Briefly note what was completed and what remains for the next session.

## Notes

- Memories are auto-persisted to `.qwen/memory/` (no manual sync needed — symlinked)
- Use `/dream` to trigger memory cleanup before leaving
- Do not run this skill automatically — only when the user explicitly triggers it
