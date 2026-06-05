#!/bin/bash
# SessionStart hook (source: compact) - re-injects AGENTS.md after compression

CONTEXT=""

if [ -f "$QWEN_PROJECT_DIR/AGENTS.md" ]; then
    CONTEXT="Project standards re-injected from AGENTS.md:\n\n$(cat "$QWEN_PROJECT_DIR/AGENTS.md")"
fi

if [ -z "$CONTEXT" ]; then
    CONTEXT="Context compacted. AGENTS.md not found."
fi

jq -n --arg msg "$CONTEXT" '{
    hookSpecificOutput: {
        additionalContext: $msg
    }
}'

exit 0
