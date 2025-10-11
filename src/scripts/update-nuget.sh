#!/bin/sh
# Ensures dotnet-outdated-tool is installed and runs it to update NuGet packages.

set -e # Exit immediately if a command exits with a non-zero status.

echo "Checking for dotnet-outdated-tool..."
if ! dotnet tool list -g | grep -q 'dotnet-outdated-tool'; then
    echo "Installing dotnet-outdated-tool..."
    dotnet tool install --global dotnet-outdated-tool
else
    echo "dotnet-outdated-tool is already installed."
fi

echo "Updating NuGet packages for Askyl.Dsm.WebHosting.sln..."
dotnet outdated ./src/Askyl.Dsm.WebHosting.sln -u

echo "NuGet package update process finished."