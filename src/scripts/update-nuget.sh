#!/bin/sh
# Ensures dotnet-outdated-tool is installed and runs it to update NuGet packages.

set -e # Exit immediately if a command exits with a non-zero status.

# Get the script's directory and navigate to project root
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

echo "Checking for dotnet-outdated-tool..."
if ! dotnet tool list -g | grep -q 'dotnet-outdated-tool'; then
    echo "Installing dotnet-outdated-tool..."
    dotnet tool install --global dotnet-outdated-tool
else
    echo "dotnet-outdated-tool is already installed."
fi

echo "Updating NuGet packages for Askyl.Dsm.WebHosting.slnx..."
cd "$PROJECT_ROOT"
dotnet outdated ./Askyl.Dsm.WebHosting.slnx -u

echo "NuGet package update process finished."