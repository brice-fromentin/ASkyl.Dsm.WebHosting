#!/bin/bash

# Build script for creating Synology SPK package
# Usage: ./build-spk.sh

set -e

# Capture start time
START_TIME=$(date +%s)

# Function to clean unwanted files from package
clean_package_artifacts() {
    local target_dir="$1"
    if [ -d "$target_dir" ]; then
        echo "🧹 Cleaning artifacts from: $target_dir"
        # Remove macOS artifacts
        find "$target_dir" -name ".DS_Store" -delete 2>/dev/null || true
        # Remove Windows thumbnails
        find "$target_dir" -name "Thumbs.db" -delete 2>/dev/null || true
        # Remove temporary files
        find "$target_dir" -name "*.tmp" -delete 2>/dev/null || true
        find "$target_dir" -name "*~" -delete 2>/dev/null || true
        # Remove IDE artifacts
        find "$target_dir" -name ".vscode" -type d -exec rm -rf {} + 2>/dev/null || true
        find "$target_dir" -name ".vs" -type d -exec rm -rf {} + 2>/dev/null || true
    fi
}

PROJECT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SPK_DIR="$PROJECT_DIR/src/spk-project"
BUILD_DIR="$PROJECT_DIR/dist"
PACKAGE_NAME="AskylDsmWebHosting"
UI_PROJECT_DIR="$PROJECT_DIR/src/Askyl.Dsm.WebHosting.Ui"
UI_PUBLISH_DIR="$SPK_DIR/package/admin-ui"
INSTALLER_DIR="$SPK_DIR/package/installer"
SRC_DIR="$PROJECT_DIR/src"

echo "🔨 Building Synology SPK package..."

# Clean and create build directory
echo "🧹 Cleaning dist directory..."
rm -rf "$BUILD_DIR"
mkdir -p "$BUILD_DIR"

# Extract version from INFO file
VERSION=$(grep '^version=' "$SPK_DIR/INFO" | cut -d'"' -f2)
echo "📋 Package version: $VERSION"

# Generate timestamp
TIMESTAMP=$(date +"%Y%m%d-%H%M%S")
echo "⏰ Build timestamp: $TIMESTAMP"

# Determine SPK filename with version and timestamp
SPK_FILENAME="${PACKAGE_NAME}-v${VERSION}-${TIMESTAMP}.spk"
echo "📦 SPK filename: $SPK_FILENAME"

# Clean and prepare UI directory in package
echo "🧹 Cleaning and preparing UI directory..."
rm -rf "$UI_PUBLISH_DIR"/*
mkdir -p "$UI_PUBLISH_DIR"

# Clean and prepare installer directory in package
echo "🧹 Cleaning and preparing installer directory..."
rm -rf "$INSTALLER_DIR"/*
mkdir -p "$INSTALLER_DIR"

# Compile and publish UI project
echo "🏗️  Building and publishing UI project..."
cd "$UI_PROJECT_DIR"
dotnet publish -c Release -o "$UI_PUBLISH_DIR" --self-contained false

# Remove debug files to reduce package size
echo "🧹 Cleaning debug files..."
find "$UI_PUBLISH_DIR" -name "*.pdb" -delete

# Clean package artifacts
clean_package_artifacts "$SPK_DIR/package"

echo "✅ UI project published to: $UI_PUBLISH_DIR"

# Clean installer directory before Docker build
clean_package_artifacts "$INSTALLER_DIR"

# Build DotnetInstaller with Docker for multiple architectures
echo "🐳 Building DotnetInstaller with Docker for multiple architectures..."
cd "$PROJECT_DIR"
docker build -f src/Dockerfile --output="$INSTALLER_DIR" src

# Final cleanup of package artifacts
clean_package_artifacts "$SPK_DIR/package"

echo "✅ DotnetInstaller binaries built to: $INSTALLER_DIR"

# List built architectures
echo "📁 Built architectures:"
ls -la "$INSTALLER_DIR"

# Go to SPK directory
cd "$SPK_DIR"

# Create package.tgz from package/ directory content
echo "📦 Creating package.tgz..."
if [ -d "package" ]; then
    cd package
    tar -czf "../package.tgz" .
    cd ..
else
    echo "⚠️  Warning: package/ directory not found, creating empty package.tgz"
    tar -czf "package.tgz" --files-from /dev/null
fi

# Create SPK file
echo "🏗️  Creating SPK file..."
tar -cf "$BUILD_DIR/${SPK_FILENAME}" \
    INFO \
    package.tgz \
    scripts \
    conf \
    PACKAGE_ICON.PNG \
    PACKAGE_ICON_256.PNG \
    LICENSE

echo "✅ SPK package created successfully: $BUILD_DIR/${SPK_FILENAME}"
echo "📁 Package structure:"
tar -tf "$BUILD_DIR/${SPK_FILENAME}" | head -20

# Clean up
rm -f package.tgz

# Calculate and display execution time
END_TIME=$(date +%s)
DURATION=$((END_TIME - START_TIME))
MINUTES=$((DURATION / 60))
SECONDS=$((DURATION % 60))

echo "🎉 Build completed!"
printf "⏱️  Execution time: %02d:%02d\n" $MINUTES $SECONDS
