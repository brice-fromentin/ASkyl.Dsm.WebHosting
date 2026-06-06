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

   Run the sync script to copy workspace memories (`.qwen/memory/`) into the global project memory so the AI session can load them:

   ```bash
   bash "$QWEN_PROJECT_DIR/.qwen/skills/session-start/sync.sh"
   ```

   This copies `.qwen/memory/` → `~/.qwen/projects/<path>/memory/` — the reverse of the end-session sync.

2. **Read AGENTS.md**

   Extract current standards and rules dynamically (inference-based, not hardcoded).

3. **Start the session start protocol**

   Follow the protocol described in AGENTS.md §5:
   - Greet briefly
   - Describe standards extracted from AGENTS.md (not just acknowledge)
   - Display recorded memories
   - Apply all directives throughout the session

## Notes

- The SessionStart hook (`hooks/reinject-agents-standards.sh`) also performs this sync automatically
- This skill provides an explicit, auditable step in the session workflow
- The end-session skill handles the reverse direction: global → workspace
