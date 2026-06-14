#!/bin/bash
set -euo pipefail
# SessionStart hook - re-injects AGENTS.md context + syncs workspace memories
# Uses git to resolve project root (no $QWEN_PROJECT_DIR dependency)

PROJECT_ROOT="${QWEN_PROJECT_DIR:-$(git rev-parse --show-toplevel 2>/dev/null)}"

if [ -z "$PROJECT_ROOT" ] || [ ! -f "$PROJECT_ROOT/AGENTS.md" ]; then
    jq -n '{hookSpecificOutput: {additionalContext: "Context compacted. AGENTS.md not found."}}'
    exit 0
fi

# Build global memory path
GLOBAL_MEMORY="$HOME/.qwen/projects/$(echo "$PROJECT_ROOT" | sed 's|[^a-zA-Z0-9]|-|g')/memory"
WORKSPACE_MEMORY="$PROJECT_ROOT/.qwen/memory"

# Pull workspace memories into global memory (so Qwen loads them)
if [ -d "$WORKSPACE_MEMORY" ]; then
    mkdir -p "$GLOBAL_MEMORY"
    while IFS= read -r -d '' f; do
        relpath="${f#$WORKSPACE_MEMORY/}"
        target="$GLOBAL_MEMORY/$relpath"
        mkdir -p "$(dirname "$target")"
        cp -- "$f" "$target" 2>/dev/null || true
    done < <(find "$WORKSPACE_MEMORY" -name '*.md' -type f -print0 2>/dev/null || true)
fi

# Re-inject AGENTS.md context via null-delimited pipe to avoid argument length limits
CONTEXT="$(cat -- "$PROJECT_ROOT/AGENTS.md")"

jq -n --arg msg "Project standards re-injected from AGENTS.md:\n\n${CONTEXT}" '{
    hookSpecificOutput: {
        additionalContext: $msg
    }
}'

exit 0
