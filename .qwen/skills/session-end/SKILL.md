---
name: session-end
description: Sync global memories to workspace, update plan, and prepare commit. Use when the user signals end of a phase or session wrap-up.
---

# End Session Workflow

Trigger when the user says "end session", "session end", "wrap up", "sync memories", or signals they're done working.

## Steps

0. **Announce**

   Output `Skill session-end has been triggered`

1. **Sync memories to workspace**

   Run the sync script to copy global memories into `.qwen/memory/`:

   ```bash
   bash "$QWEN_PROJECT_DIR/.qwen/skills/end-session/sync.sh"
   ```

   This merges `~/.qwen/projects/<path>/memory/*.md` → `.qwen/memory/` so memories travel with the repo.

2. **Ask the user what to commit**

   Present the current `git status` and ask if they want to commit phase changes.

3. **Commit if confirmed**

   Follow AGENTS.md git safety rules — show diff, propose message, get approval.

4. **Summarize**

   Briefly note what was completed and what remains for the next session.

## Notes

- The SessionStart hook already pulls `.qwen/memory/` → global memory on session start
- This skill handles the reverse direction: global → workspace before leaving
- Do not run this skill automatically — only when the user explicitly triggers it
