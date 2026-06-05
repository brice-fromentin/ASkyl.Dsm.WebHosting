#!/bin/bash
# Sync global memories to workspace .qwen/memory/
# Usage: bash .qwen/skills/end-session/sync.sh

PROJECT_ROOT="$QWEN_PROJECT_DIR"
GLOBAL_MEMORY="$HOME/.qwen/projects/$(echo "$PROJECT_ROOT" | sed 's|[^a-zA-Z0-9]|-|g')/memory"
WORKSPACE_MEMORY="$PROJECT_ROOT/.qwen/memory"

# Create workspace memory directory
mkdir -p "$WORKSPACE_MEMORY"

if [ ! -d "$GLOBAL_MEMORY" ]; then
    echo "No global memory directory found at $GLOBAL_MEMORY"
    exit 0
fi

# Sync MEMORY.md index
if [ -f "$GLOBAL_MEMORY/MEMORY.md" ]; then
    cp "$GLOBAL_MEMORY/MEMORY.md" "$WORKSPACE_MEMORY/MEMORY.md"
    echo "Synced MEMORY.md"
fi

# Sync individual memory files (including subdirectories)
SYNCED=0
while IFS= read -r -d '' f; do
    relpath="${f#$GLOBAL_MEMORY/}"
    target="$WORKSPACE_MEMORY/$relpath"
    mkdir -p "$(dirname "$target")"
    cp "$f" "$target"
    SYNCED=$((SYNCED + 1))
done < <(find "$GLOBAL_MEMORY" -name '*.md' -type f ! -name 'MEMORY.md' -print0)

# Sync MEMORY.md last (so index points to valid files)
if [ -f "$GLOBAL_MEMORY/MEMORY.md" ]; then
    cp "$GLOBAL_MEMORY/MEMORY.md" "$WORKSPACE_MEMORY/MEMORY.md"
    SYNCED=$((SYNCED + 1))
fi

echo "Synced $SYNCED memory files to $WORKSPACE_MEMORY"
echo "Ready to commit with git add .qwen/memory/"
