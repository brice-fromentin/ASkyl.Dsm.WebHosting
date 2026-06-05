#!/bin/bash
# SessionStart hook (source: compact) - re-injects AGENTS.md + syncs workspace memories

PROJECT_ROOT="$QWEN_PROJECT_DIR"
GLOBAL_MEMORY="$HOME/.qwen/projects/$(echo "$PROJECT_ROOT" | sed 's|[^a-zA-Z0-9]|-|g')/memory"
WORKSPACE_MEMORY="$PROJECT_ROOT/.qwen/memory"

# Pull workspace memories into global memory (so Qwen loads them)
if [ -d "$WORKSPACE_MEMORY" ]; then
    mkdir -p "$GLOBAL_MEMORY"
    while IFS= read -r -d '' f; do
        relpath="${f#$WORKSPACE_MEMORY/}"
        target="$GLOBAL_MEMORY/$relpath"
        mkdir -p "$(dirname "$target")"
        cp "$f" "$target" 2>/dev/null
    done < <(find "$WORKSPACE_MEMORY" -name '*.md' -type f -print0)
fi

CONTEXT=""

if [ -f "$PROJECT_ROOT/AGENTS.md" ]; then
    CONTEXT="Project standards re-injected from AGENTS.md:\n\n$(cat "$PROJECT_ROOT/AGENTS.md")"
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
