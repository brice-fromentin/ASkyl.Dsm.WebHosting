---
name: session-end
description: Sync global memories to workspace, update plan, and prepare commit. Use when the user signals end of a phase or session wrap-up.
---

# End Session Workflow

Trigger when the user says "end session", "session end", "wrap up", "sync memories", or signals they're done working.

## Steps

0. **Announce**

   Output `Skill session-end has been triggered`

1. **Sync global memories to workspace**

   Copy memories from the global memory directory to `.qwen/memory/` (git-tracked) so memories travel with the repo.

   **Global memory path:** `~/.qwen/projects/<sanitized-project-path>/memory`

   The sanitized path replaces all non-alphanumeric characters with `-`. For example:
   `/Users/brice/Documents/Dev/github/ASkyl.Dsm.WebHosting` → `-Users-brice-Documents-Dev-github-ASkyl-Dsm-WebHosting`

   **Procedure:**

   a. Discover global memory files:
      ```
      glob pattern: "<global-memory>/**/*.md"
      ```

   b. For each `.md` file found (except `MEMORY.md`):
      - Read the global file with `read_file`
      - Read the corresponding workspace file with `read_file` (required before writing)
      - **Compare content** — only write if different
      - Write to `.qwen/memory/` using `write_file` (preserves relative path: `feedback/`, `project/`, etc.)

   c. Sync `MEMORY.md` last (same read-compare-write pattern)

   d. Report: "Synced N/N memory files" (N synced / N total)

   e. Stage the workspace memory for commit:
      ```
      git add .qwen/memory/
      ```

   **Note:** `write_file` requires reading the target file first. Always read before write.
   **Skip silently** if global memory directory does not exist or is empty.

2. **Ask the user what to commit**

   Present the current `git status` and ask if they want to commit phase changes.

3. **Commit if confirmed**

   Follow AGENTS.md git safety rules — show diff, propose message, get approval.

4. **Summarize**

   Briefly note what was completed and what remains for the next session.

## Notes

- The SessionStart hook already pulls `.qwen/memory/` → global memory on session start
- This skill handles the reverse direction: global → workspace before leaving
- Memory sync uses pure tool calls — no shell scripts required
- Do not run this skill automatically — only when the user explicitly triggers it
