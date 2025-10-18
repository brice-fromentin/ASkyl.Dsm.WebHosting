#!/bin/bash

set -e

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to print colored output
print_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Function to validate version format
validate_version() {
    local version=$1
    if [[ ! $version =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
        print_error "Invalid version format. Expected format: x.x.x (e.g., 1.2.3)"
        return 1
    fi
    return 0
}

# Get script directory (where this script is located)
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

print_info "ASkyl.Dsm.WebHosting Version Update Script"
print_info "=========================================="

# Show current versions first
echo
print_info "Current versions:"
CURRENT_VERSION=$(grep '<Version>' "$PROJECT_ROOT/src/Directory.Build.props" | sed 's/.*<Version>\([^<]*\)<\/Version>.*/\1/')
CURRENT_SPK_VERSION=$(grep 'version=' "$PROJECT_ROOT/src/spk-project/INFO" | sed 's/.*version="\([^"]*\)".*/\1/')
print_info "  Directory.Build.props: $CURRENT_VERSION"
print_info "  SPK INFO file: $CURRENT_SPK_VERSION"

# Prompt for version number
while true; do
    echo
    read -p "Enter the new version number (x.x.x format): " NEW_VERSION

    if validate_version "$NEW_VERSION"; then
        break
    fi
done

echo
print_info "New version will be: $NEW_VERSION"

# Confirm update
echo
read -p "Do you want to proceed with the version update? (y/N): " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    print_warning "Version update cancelled."
    exit 0
fi

# Update Directory.Build.props
print_info "Updating Directory.Build.props..."
DIRECTORY_BUILD_PROPS="$PROJECT_ROOT/src/Directory.Build.props"

if [[ ! -f "$DIRECTORY_BUILD_PROPS" ]]; then
    print_error "Directory.Build.props not found at: $DIRECTORY_BUILD_PROPS"
    exit 1
fi

# Update version in Directory.Build.props
sed -i.tmp "s|<Version>.*</Version>|<Version>$NEW_VERSION</Version>|g" "$DIRECTORY_BUILD_PROPS"
sed -i.tmp "s|<AssemblyVersion>.*</AssemblyVersion>|<AssemblyVersion>$NEW_VERSION.0</AssemblyVersion>|g" "$DIRECTORY_BUILD_PROPS"
sed -i.tmp "s|<FileVersion>.*</FileVersion>|<FileVersion>$NEW_VERSION.0</FileVersion>|g" "$DIRECTORY_BUILD_PROPS"
sed -i.tmp "s|<InformationalVersion>.*</InformationalVersion>|<InformationalVersion>$NEW_VERSION</InformationalVersion>|g" "$DIRECTORY_BUILD_PROPS"
sed -i.tmp "s|<PackageVersion>.*</PackageVersion>|<PackageVersion>$NEW_VERSION</PackageVersion>|g" "$DIRECTORY_BUILD_PROPS"

# Remove temporary file created by sed
rm "$DIRECTORY_BUILD_PROPS.tmp"

print_info "Directory.Build.props updated successfully"

# Update SPK INFO file
print_info "Updating SPK INFO file..."
SPK_INFO_FILE="$PROJECT_ROOT/src/spk-project/INFO"

if [[ ! -f "$SPK_INFO_FILE" ]]; then
    print_error "SPK INFO file not found at: $SPK_INFO_FILE"
    exit 1
fi

# Update version in INFO file
sed -i.tmp "s|version=\".*\"|version=\"$NEW_VERSION\"|g" "$SPK_INFO_FILE"

# Remove temporary file created by sed
rm "$SPK_INFO_FILE.tmp"

print_info "SPK INFO file updated successfully"

# Show updated versions
echo
print_info "Version update completed successfully!"
print_info "Updated versions:"
NEW_BUILD_VERSION=$(grep '<Version>' "$DIRECTORY_BUILD_PROPS" | sed 's/.*<Version>\([^<]*\)<\/Version>.*/\1/')
NEW_SPK_VERSION=$(grep 'version=' "$SPK_INFO_FILE" | sed 's/.*version="\([^"]*\)".*/\1/')
print_info "  Directory.Build.props: $NEW_BUILD_VERSION"
print_info "  SPK INFO file: $NEW_SPK_VERSION"
