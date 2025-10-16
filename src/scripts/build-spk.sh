#!/bin/bash

set -euo pipefail

# -----------------------------------------------------------------------------
# Helper Functions
# -----------------------------------------------------------------------------

# Cleans unwanted build artifacts from a directory.
#
# @param $1 The target directory to clean.
clean_package_artifacts() {
    local target_dir="$1"

    if [ ! -d "$target_dir" ]; then
        return
    fi

    echo "🧹 Cleaning artifacts from: $target_dir"
    find "$target_dir" -name ".DS_Store" -delete 2>/dev/null || true
    find "$target_dir" -name "Thumbs.db" -delete 2>/dev/null || true
    find "$target_dir" -name "*.tmp" -delete 2>/dev/null || true
    find "$target_dir" -name "*~" -delete 2>/dev/null || true
    find "$target_dir" -name ".vscode" -type d -exec rm -rf {} + 2>/dev/null || true
    find "$target_dir" -name ".vs" -type d -exec rm -rf {} + 2>/dev/null || true
}

# Checks for required command-line tools.
check_dependencies() {
    echo "🔎 Checking dependencies..."

    local missing_deps=0

    for cmd in curl tar dotnet jq awk pigz; do
        if ! command -v "$cmd" &> /dev/null; then
            printf "❌ Error: Required command '%s' is not installed.\n" "$cmd" >&2
            missing_deps=1
        fi
    done

    if [ "$missing_deps" -eq 1 ]; then
        exit 1
    fi

    echo "✅ All required commands are available."
}

# Checks for required directories.
check_directories() {
    echo "🔎 Checking required directories..."
    if [ ! -d "$SPK_DIR/package" ]; then
        printf "❌ Error: 'package' directory not found in %s.\n" "$SPK_DIR" >&2
        exit 1
    fi
    echo "✅ Required directories are present."
}

# Extracts the .NET channel version from the installer's appsettings.json.
extract_dotnet_channel_version() {
    echo "⚙️  Extracting .NET channel version..." >&2

    local appsettings_path="$PROJECT_DIR/src/Askyl.Dsm.WebHosting.Ui/appsettings.json"

    if [ ! -f "$appsettings_path" ]; then
        printf "❌ Error: Installer appsettings.json not found at %s\n" "$appsettings_path" >&2
        return 1
    fi

    local version=$(jq -r '.Download.ChannelVersion' "$appsettings_path")

    if [ -z "$version" ]; then
        printf "❌ Error: Could not extract ChannelVersion from %s\n" "$appsettings_path" >&2
        return 1
    fi

    echo "$version"
}

# Gets the SHA512 checksum of a file in a portable way (Linux/macOS).
#
# @param $1 The path to the file.
get_sha512_checksum() {
    if command -v "sha512sum" &> /dev/null; then
        sha512sum "$1" | awk '{print $1}'
    elif command -v "shasum" &> /dev/null; then
        shasum -a 512 "$1" | awk '{print $1}'
    else
        printf "❌ Error: Neither 'sha512sum' nor 'shasum' command found.\n" >&2
        exit 1
    fi
}

# Fetches the releases.json file, using a 24-hour cache.
# The JSON content is printed to stdout.
#
# @return 0 on success, 1 on failure.
get_releases_json() {
    local releases_json_path="$RUNTIMES_DOWNLOAD_DIR/releases-${DOTNET_CHANNEL_VERSION}.json"
    local download_required=0

    if [ ! -f "$releases_json_path" ]; then
        download_required=1
    elif [ -n "$(find "$releases_json_path" -mtime +0)" ]; then
        echo "ℹ️ Cached release information is older than 24 hours. Re-downloading." >&2
        download_required=1
    fi

    if [ "$download_required" -eq 1 ]; then
        echo "⬇️  Fetching release information for .NET ${DOTNET_CHANNEL_VERSION}..." >&2

        local releases_url="https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/${DOTNET_CHANNEL_VERSION}/releases.json"
        local releases_json=$(curl -s -L "$releases_url")

        if [ -z "$releases_json" ]; then
            printf "❌ Error: Failed to download release information from %s\n" "$releases_url" >&2
            return 1
        fi

        echo "$releases_json" > "$releases_json_path"
        echo "$releases_json"
    else
        echo "✅ Using cached release information for .NET ${DOTNET_CHANNEL_VERSION}." >&2
        cat "$releases_json_path"
    fi
}

# Downloads a file and verifies its checksum. Skips download if the file
# already exists and is valid.
#
# @param $1 The destination path for the file.
# @param $2 The download URL.
# @param $3 The expected SHA512 hash.
download_and_verify() {
    local dest_path="$1"
    local url="$2"
    local expected_hash="$3"
    local filename=$(basename "$dest_path")

    if [ -f "$dest_path" ]; then
        echo "🔎 File '$filename' already exists. Verifying checksum..."

        local existing_hash=$(get_sha512_checksum "$dest_path")

        if [ "$existing_hash" == "$expected_hash" ]; then
            echo "✅ Checksum matches for '$filename'. Skipping download."
            return 0
        else
            echo "⚠️ Checksum mismatch for '$filename'. Re-downloading..."
            rm "$dest_path"
        fi
    fi

    echo "⬇️  Downloading '$filename'..."
    curl -s -L -o "$dest_path" "$url"

    echo "🔎 Verifying checksum for '$filename'..."
    local downloaded_hash=$(get_sha512_checksum "$dest_path")

    if [ "$downloaded_hash" != "$expected_hash" ]; then
        printf "❌ FATAL: Checksum mismatch for %s\n" "$filename" >&2
        printf "  Expected: %s\n" "$expected_hash" >&2
        printf "  Got:      %s\n" "$downloaded_hash" >&2
        return 1
    fi

    echo "✅ Checksum for '$filename' is valid."
}


# Downloads the .NET runtimes for all required architectures.
download_dotnet_runtimes() {
    echo "🧹 Preparing runtimes download directory..."
    mkdir -p "$RUNTIMES_DOWNLOAD_DIR"

    local releases_json=$(get_releases_json)

    if [ -z "$releases_json" ]; then
        printf "❌ Error: Could not get release information.\n" >&2
        return 1
    fi

    # Get the latest runtime version string from the top-level property
    local latest_version_string=$(echo "$releases_json" | jq -r '."latest-runtime"')
    if [ -z "$latest_version_string" ]; then
        printf "❌ Error: Could not find 'latest-runtime' in release information for .NET %s\n" "$DOTNET_CHANNEL_VERSION" >&2
        return 1
    fi

    echo "ℹ️ Latest runtime version: $latest_version_string"

    # Find the release object that matches this version
    local latest_release_json=$(echo "$releases_json" | jq ".releases[] | select(.\"release-version\" == \"$latest_version_string\")")

    if [ -z "$latest_release_json" ]; then
        printf "❌ Error: Could not find release object for version %s\n" "$latest_version_string" >&2
        return 1
    fi

    local files_json=$(echo "$latest_release_json" | jq '.["aspnetcore-runtime"].files')
    local archs=("linux-arm" "linux-arm64" "linux-x64")

    for arch in "${archs[@]}"; do
        echo "--- Processing architecture: $arch ---"

        local file_info=$(echo "$files_json" | jq ".[] | select(.rid == \"$arch\" and (.name | test(\"composite\") | not))")
        
        if [ -z "$file_info" ]; then
            printf "⚠️ Warning: Could not find file info for architecture %s in version %s\n" "$arch" "$latest_version_string" >&2
            continue
        fi

        local filename=$(echo "$file_info" | jq -r .name)
        local url=$(echo "$file_info" | jq -r .url)
        local hash=$(echo "$file_info" | jq -r .hash)
        local dest_path="$RUNTIMES_DOWNLOAD_DIR/$filename"

        if ! download_and_verify "$dest_path" "$url" "$hash"; then
            return 1
        fi
    done

    echo "✅ All .NET runtimes are downloaded and verified."
}

# Gets the size of a file in bytes using du.
#
# @param $1 The path to the file.
get_file_size_mib() {
    du -m "$1" | awk '{print $1}'
}

# Creates the final SPK package.
create_spk_package() {
    echo "📦 Creating final SPK package..."

    # Final cleanup of package artifacts before packaging
    clean_package_artifacts "$SPK_DIR/package"

    # Go to SPK directory to package from the correct context
    cd "$SPK_DIR"

    # Create package.tgz from package/ directory content
    echo "Creating package.tgz..."
    cd package
    tar -cf - . | pigz -2 > ../package.tgz
    cd ..

    # Create SPK file
    echo "Creating SPK file: $SPK_FILENAME"
    tar -cf "$BUILD_DIR/${SPK_FILENAME}" \
    INFO \
    package.tgz \
    scripts \
    conf \
    PACKAGE_ICON.PNG \
    PACKAGE_ICON_256.PNG \
    LICENSE

    # Clean up temporary archive
    rm -f package.tgz

    local spk_path="$BUILD_DIR/${SPK_FILENAME}"
    local spk_size_mib=$(get_file_size_mib "$spk_path")

    printf "✅ SPK package created successfully!\n"
    printf "   -> Path: %s\n" "$spk_path"
    printf "   -> Size: %s MiB\n" "$spk_size_mib"
}

# -----------------------------------------------------------------------------
# Variable Definitions
# -----------------------------------------------------------------------------
readonly PROJECT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
readonly SPK_DIR="$PROJECT_DIR/src/spk-project"
readonly BUILD_DIR="$PROJECT_DIR/dist"
readonly PACKAGE_NAME="AskylDsmWebHosting"
readonly UI_PROJECT_DIR="$PROJECT_DIR/src/Askyl.Dsm.WebHosting.Ui"
readonly UI_PUBLISH_DIR="$SPK_DIR/package/admin-ui"
readonly RUNTIMES_DOWNLOAD_DIR="$SPK_DIR/package/runtimes/downloads"
readonly SRC_DIR="$PROJECT_DIR/src"

# -----------------------------------------------------------------------------
# Pre-flight Checks (Fail-Fast)
# -----------------------------------------------------------------------------
check_dependencies
check_directories

# -----------------------------------------------------------------------------
# Main Build Script
# -----------------------------------------------------------------------------

# Capture start time
START_TIME=$(date +%s)

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

# Extract .NET Channel Version
DOTNET_CHANNEL_VERSION=$(extract_dotnet_channel_version)
if [ $? -ne 0 ]; then
    exit 1
fi
echo "Found .NET Channel Version: $DOTNET_CHANNEL_VERSION"

# Clean and prepare UI directory in package
echo "🧹 Cleaning and preparing UI directory..."
rm -rf "$UI_PUBLISH_DIR"/*
mkdir -p "$UI_PUBLISH_DIR"

# Download .NET Runtimes
if ! download_dotnet_runtimes; then
    exit 1
fi

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

create_spk_package

# -----------------------------------------------------------------------------
# Finalization
# -----------------------------------------------------------------------------

# Calculate and display execution time
END_TIME=$(date +%s)
DURATION=$((END_TIME - START_TIME))
MINUTES=$((DURATION / 60))
SECONDS=$((DURATION % 60))

echo "🎉 Build completed!"
printf "⏱️  Execution time: %02d:%02d\n" "$MINUTES" "$SECONDS"