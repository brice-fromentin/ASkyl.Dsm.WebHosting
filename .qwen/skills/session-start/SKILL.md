---
name: session-start
description: Sync workspace memories to global project memory, then run session start protocol. Use when the user signals start of a new session or phase.
---

# Start Session Workflow

Trigger when the user says "start session", "session start", or begins a new phase.

## Steps

0. **Announce**

   Output `Skill session-start has been triggered`

1. **Sync workspace memories to global memory**

   Copy memories from `.qwen/memory/` (git-tracked, travels with repo) to the global memory directory so the AI session can load them.

   **Global memory path:** `~/.qwen/projects/<sanitized-project-path>/memory`

   The sanitized path replaces all non-alphanumeric characters with `-`. For example:
   `/Users/brice/Documents/Dev/github/ASkyl.Dsm.WebHosting` → `-Users-brice-Documents-Dev-github-ASkyl-Dsm-WebHosting`

   **Procedure:**

   a. Discover workspace memory files:
      ```
      glob pattern: ".qwen/memory/**/*.md"
      ```

   b. For each `.md` file found (except `MEMORY.md`):
      - Read the workspace file with `read_file`
      - Read the corresponding global file with `read_file` (required before writing)
      - **Compare content** — only write if different
      - Write to global memory using `write_file` (preserves relative path: `feedback/`, `project/`, etc.)

   c. Sync `MEMORY.md` last (same read-compare-write pattern)

   d. Report: "Synced N/N memory files" (N synced / N total)

   **Note:** `write_file` requires reading the target file first. Always read before write.
   **Skip silently** if `.qwen/memory/` does not exist or is empty.

2. **Read AGENTS.md**

   Extract current standards and rules dynamically (inference-based, not hardcoded).

3. **Start the session start protocol**

   Follow the protocol described in AGENTS.md §5:
   - Greet briefly
   - Describe standards extracted from AGENTS.md (not just acknowledge)
   - Display recorded memories
   - Apply all directives throughout the session

## Notes

- The SessionStart hook (`hooks/reinject-agents-standards.sh`) also performs AGENTS.md reinjection
- This skill provides explicit, auditable memory sync via tool calls (no shell scripts)
- The end-session skill handles the reverse direction: global → workspace
