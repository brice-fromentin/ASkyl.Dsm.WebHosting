# Setup Guide

## Prerequisites

- .NET 10.0 SDK
- Qwen Code CLI

## Memory System Setup

The project uses Qwen Code's auto-memory system with git-tracked memories for team sharing. A symbolic link bridges Qwen Code's global memory path to the project's `.qwen/memory/` directory.

### One-Time Setup

Run this command from the project root to create the symlink:

```bash
# Generate sanitized project path (replace non-alphanumeric chars with -)
SANITIZED_PATH=$(echo "$(pwd)" | sed 's/[^a-zA-Z0-9]/-/g')

# Create symlink
ln -s "$(pwd)/.qwen/memory" ~/.qwen/projects/${SANITIZED_PATH}/memory
```

**What this does:**

- Qwen Code saves memories to `~/.qwen/projects/<sanitized-path>/memory/`
- The symlink points that path to `.qwen/memory/` (git-tracked)
- Memories are now version-controlled and shared with the team

### Verify Setup

```bash
# Check symlink exists and points to project directory
ls -la ~/.qwen/projects/$(echo "$(pwd)" | sed 's/[^a-zA-Z0-9]/-/g')/memory
```

Should show: `memory -> <your-project-path>/.qwen/memory`

## Build & Test

```bash
# Format
dotnet format ./src/Askyl.Dsm.WebHosting.slnx --verbosity quiet

# Build
dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx

# Test
dotnet test /nr:false ./src/Askyl.Dsm.WebHosting.slnx
```
