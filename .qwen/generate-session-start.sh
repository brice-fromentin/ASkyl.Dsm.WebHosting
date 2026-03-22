#!/bin/bash

# Script to generate a dynamic session start message based on QWEN.md standards
# This avoids hardcoding and automatically extracts rules from the project documentation

QWEN_FILE="${1:-$(pwd)/QWEN.md}"

if [[ ! -f "$QWEN_FILE" ]]; then
    echo "Error: QWEN.md not found at $QWEN_FILE" >&2
    exit 1
fi

# Extract key sections from QWEN.md using grep and sed
echo "Hello! 👋"
echo ""
echo "I'm ready to help you with the project."
echo ""

# Check for training data cutoff section
TRAINING_CUTOFF=$(grep -A2 "Training Data Cutoff:" "$QWEN_FILE" | head -1)
if [[ -n "$TRAINING_CUTOFF" ]]; then
    echo "**Training Data Cutoff:** $(echo "$TRAINING_CUTOFF" | sed 's/.*: //')"
else
    echo "**Training Data Cutoff:** Not specified in QWEN.md"
fi

echo ""
echo "What would you like me to work on?"
