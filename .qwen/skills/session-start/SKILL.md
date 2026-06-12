---
name: session-start
description: Run session start protocol. Use when the user signals start of a new session or phase.
---

# Start Session Workflow

Trigger when the user says "start session", "session start", or begins a new phase.

## Steps

0. **Announce**

   Output `Skill session-start has been triggered`

1. **Read AGENTS.md**

   Extract current standards and rules dynamically (inference-based, not hardcoded).

2. **Start the session start protocol**

   Follow the protocol described in AGENTS.md §5:
   - Greet briefly
   - Describe standards extracted from AGENTS.md (not just acknowledge)
   - Display recorded memories (loaded automatically from `.qwen/memory/`)
   - Apply all directives throughout the session

## Notes

- Memories are auto-loaded from `.qwen/memory/` (symlinked to Qwen Code global path)
- Use `/remember <text>` during session to save new memories
- The SessionStart hook (`hooks/reinject-agents-standards.sh`) also performs AGENTS.md reinjection
