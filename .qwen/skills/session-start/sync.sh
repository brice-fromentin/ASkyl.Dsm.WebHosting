#!/bin/bash
# Sync workspace memories (.qwen/memory/) to global project memory
# Reverse of end-session/sync.sh — runs at session start to ensure
# committed workspace memories are available to the AI session.
# Usage: bash .qwen/skills/session-start/sync.sh

PROJECT_ROOT="$QWEN_PROJECT_DIR"
GLOBAL_MEMORY="$HOME/.qwen/projects/$(echo "$PROJECT_ROOT" | sed 's|[^a-zA-Z0-9]|-|g')/memory"
WORKSPACE_MEMORY="$PROJECT_ROOT/.qwen/memory"

if [ ! -d "$WORKSPACE_MEMORY" ]; then
    echo "No workspace memory directory found at $WORKSPACE_MEMORY"
    exit 0
fi

# Create global memory directory
mkdir -p "$GLOBAL_MEMORY"

# Sync individual memory files (including subdirectories)
SYNCED=0
while IFS= read -r -d '' f; do
    relpath="${f#$WORKSPACE_MEMORY/}"
    target="$GLOBAL_MEMORY/$relpath"
    mkdir -p "$(dirname "$target")"
    cp "$f" "$target"
    SYNCED=$((SYNCED + 1))
done < <(find "$WORKSPACE_MEMORY" -name '*.md' -type f ! -name 'MEMORY.md' -print0)

# Sync MEMORY.md index last (so index points to valid files)
if [ -f "$WORKSPACE_MEMORY/MEMORY.md" ]; then
    cp "$WORKSPACE_MEMORY/MEMORY.md" "$GLOBAL_MEMORY/MEMORY.md"
    SYNCED=$((SYNCED + 1))
fi

echo "Synced $SYNCED memory files from $WORKSPACE_MEMORY to $GLOBAL_MEMORY"
