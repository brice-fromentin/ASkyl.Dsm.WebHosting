#!/bin/bash

# Build script for creating Synology SPK package
# Usage: ./build-spk.sh

set -e

PROJECT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SPK_DIR="$PROJECT_DIR/src/spk-project"
BUILD_DIR="$PROJECT_DIR/dist"
PACKAGE_NAME="AskylDsmWebHosting"
UI_PROJECT_DIR="$PROJECT_DIR/src/Askyl.Dsm.WebHosting.Ui"
UI_PUBLISH_DIR="$SPK_DIR/package/ui"

echo "🔨 Building Synology SPK package..."

# Create build directory
mkdir -p "$BUILD_DIR"

# Clean and prepare UI directory in package
echo "🧹 Cleaning and preparing UI directory..."
rm -rf "$UI_PUBLISH_DIR"/*
mkdir -p "$UI_PUBLISH_DIR"

# Compile and publish UI project
echo "🏗️  Building and publishing UI project..."
cd "$UI_PROJECT_DIR"
dotnet publish -c Release -o "$UI_PUBLISH_DIR" --self-contained false

# Remove debug files to reduce package size
echo "🧹 Cleaning debug files..."
find "$UI_PUBLISH_DIR" -name "*.pdb" -delete

echo "✅ UI project published to: $UI_PUBLISH_DIR"

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
tar -cf "$BUILD_DIR/${PACKAGE_NAME}.spk" \
    INFO \
    package.tgz \
    scripts \
    conf \
    PACKAGE_ICON.PNG \
    PACKAGE_ICON_256.PNG \
    LICENSE

echo "✅ SPK package created successfully: $BUILD_DIR/${PACKAGE_NAME}.spk"
echo "📁 Package structure:"
tar -tf "$BUILD_DIR/${PACKAGE_NAME}.spk" | head -20

# Clean up
rm -f package.tgz

echo "🎉 Build completed!"
