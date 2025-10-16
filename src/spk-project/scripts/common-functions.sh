#!/bin/bash

# Common functions for SPK package scripts
# Usage: source "$(dirname "$0")/common-functions.sh"

#region Environment Variables

PACKAGE_DIR="${SYNOPKG_PKGDEST:-/var/packages/AskylWebHosting/target}"
VAR_DIR="/var/packages/AskylWebHosting/var"
LOG_FILE="${LOG_FILE:-$VAR_DIR/logs/install.log}"

#endregion

#region Core Logging Functions

# Centralized logging function with timestamp and level formatting
# Usage: log_to <level> <message> [destination_file]
log_to() {
    local level="$1"
    local message="$2"
    local logfile="${3:-$LOG_FILE}"
    echo "$(date '+%Y-%m-%d %H:%M:%S')	$level	$message" >> "$logfile"
}

# Logging functions with standardized levels and optional destination
# Usage: log_info <message> [destination_file]
log_info() {
    log_to "[INF]" "$1" "$2"
}

log_warning() {
    log_to "[WRN]" "$1" "$2"
}

log_error() {
    log_to "[ERR]" "$1" "$2"
}

log_fatal() {
    log_to "[FTL]" "$1" "$2"
}

#endregion

#region Debug Logging (Beta Mode Support)

# Initialize beta mode detection (static-like variable)
_init_beta_mode() {
    if [ -z "$_BETA_MODE_INITIALIZED" ]; then
        local info_file="$(dirname "$(dirname "$0")")/INFO"
        if [ -f "$info_file" ] && grep -q '^beta="yes"' "$info_file" 2>/dev/null;
        then
            _IS_BETA_MODE=true
        else
            _IS_BETA_MODE=false
        fi
        _BETA_MODE_INITIALIZED=true
    fi
}

log_debug() {
    local message="$1"

    # Initialize beta mode detection only once
    _init_beta_mode

    if [ "$_IS_BETA_MODE" = "true" ]; then
        # Beta mode: use separate debug log file
        echo "$(date '+%Y-%m-%d %H:%M:%S')	[DBG]	$message" >> /tmp/adwh-debug.log
    else
        # Release mode: use standard logging system
        log_to "[DBG]" "$message" "$LOG_FILE"
    fi
}

#endregion

#region Package Installation Progress

# Update package installation progress
# Usage: update_progress <progress_value> [description]
# progress_value: float between 0.0 and 1.0 (e.g., 0.5 for 50%)
# description: optional description for logging
update_progress() {
    local progress="$1"
    local description="${2:-}"

    if [ -n "$SYNOPKG_PKG_PROGRESS_PATH" ] && [ -n "$progress" ]; then
        if flock -x "$SYNOPKG_PKG_PROGRESS_PATH" -c "echo $progress > \"$SYNOPKG_PKG_PROGRESS_PATH\"" 2>/dev/null;
        then
            if [ -n "$description" ]; then
                log_debug "Progress updated to $(awk "BEGIN {printf \"%.0f%%\", $progress * 100}") - $description"
            else
                log_debug "Progress updated to $(awk "BEGIN {printf \"%.0f%%\", $progress * 100}")"
            fi
        else
            log_debug "Failed to update progress to $progress"
        fi
    fi
}

#endregion

#region Synology Package Center Integration

# Log to Synology Package Center temp file for user-facing messages
log_temp() {
    local message="$1"
    if [ -n "$SYNOPKG_TEMP_LOGFILE" ]; then
        echo "$message" > "$SYNOPKG_TEMP_LOGFILE"
    fi
}

# Log fatal error with both technical and user-facing messages
log_fatal_with_temp() {
    local technical_message="$1"
    local user_message="$2"
    log_fatal "$technical_message"
    log_temp "$user_message"
}

#endregion

#region Log File Management

# Start logging for script execution
# Usage: start_log <script_name> [logfile]
start_log() {
    local script_name="$1"
    local logfile="${2:-$LOG_FILE}"

    # Create log directory
    mkdir -p "$(dirname "$logfile")"

    # Set global LOG_FILE for subsequent calls
    LOG_FILE="$logfile"

    # Log start message
    log_info "-------------------- STARTING $script_name --------------------"
}

#endregion

#region Process Management

# Check if a process is running by pattern
is_process_running() {
    local pattern="$1"
    pgrep -f "$pattern" >/dev/null 2>&1
}

# Stop process by pattern with logging
stop_process() {
    local pattern="$1"
    local description="${2:-process}"

    if is_process_running "$pattern"; then
        log_info "Found running $description, stopping them"
        pkill -f "$pattern" || true
        log_info "$description stopped"
    else
        log_info "No running $description found"
    fi
}

#endregion

#region File Management

BACKUP_DIR="$VAR_DIR/upgrade-backup"

# Backup file to upgrade-backup directory with logging
# Usage: backup_file <source_file>
backup_file() {
    local source="$1"
    local filename=$(basename "$source")
    local backup_file="$BACKUP_DIR/$filename"

    # Create backup directory if it doesn't exist
    mkdir -p "$BACKUP_DIR"

    if [ -f "$source" ]; then
        cp -f "$source" "$backup_file" && log_info "Backed up $source to $backup_file" || log_error "Failed to backup $source"
    else
        log_info "No file to backup at $source"
    fi
}

# Restore file from upgrade-backup directory with logging
# Usage: restore_file <target_file>
restore_file() {
    local target="$1"
    local filename=$(basename "$target")
    local backup_file="$BACKUP_DIR/$filename"

    if [ -f "$backup_file" ]; then
        if [ -f "$target" ]; then
            log_info "Target $target already exists, overwriting with backup"
        fi
        
        cp -f "$backup_file" "$target" && log_info "Restored $target from $backup_file" || log_error "Failed to restore $target"
    else
        log_info "No backup file found at $backup_file"
    fi
}

#endregion

#region .NET Runtime Management

# Detects architecture and installs the .NET runtime by extracting the correct archive.
install_dotnet_runtime() {
    log_info "Starting .NET runtime installation..."

    # Detect system architecture from OS
    local OS_ARCH=$(uname -m)
    local ARCHIVE_NAME=""
    log_info "Detected OS architecture: $OS_ARCH"

    # Map OS architecture to runtime archive name
    case "$OS_ARCH" in
        x86_64|amd64)
            ARCHIVE_NAME="aspnetcore-runtime-linux-x64.tar.gz"
            ;; 
        aarch64|arm64)
            ARCHIVE_NAME="aspnetcore-runtime-linux-arm64.tar.gz"
            ;; 
        armv7l|armv7)
            ARCHIVE_NAME="aspnetcore-runtime-linux-arm.tar.gz"
            ;; 
        *)
            log_fatal_with_temp "Unsupported OS architecture: $OS_ARCH" \
                               "This package does not support your Synology model's architecture."
            log_debug "Error: Unsupported OS architecture: $OS_ARCH. Supported: x86_64, aarch64, armv7l"
            return 1
            ;; 
    esac

    log_info "Using archive: $ARCHIVE_NAME"

    local RUNTIMES_DIR="$PACKAGE_DIR/runtimes"
    local DOWNLOADS_DIR="$RUNTIMES_DIR/downloads"
    local SOURCE_ARCHIVE="$DOWNLOADS_DIR/$ARCHIVE_NAME"

    if [ ! -f "$SOURCE_ARCHIVE" ]; then
        log_fatal_with_temp "Runtime archive not found at $SOURCE_ARCHIVE" ".NET installation failed - runtime archive is missing."
        log_debug "Error: Runtime archive not found for $OS_ARCH. Looked for $SOURCE_ARCHIVE"
        return 1
    fi

    log_info "Found runtime archive: $SOURCE_ARCHIVE"
    log_info "Extracting to $RUNTIMES_DIR..."

    if tar -xzf "$SOURCE_ARCHIVE" -C "$RUNTIMES_DIR" --strip-components=1;
    then
        log_info ".NET runtime extracted successfully"
    else
        log_fatal_with_temp "Failed to extract runtime archive with exit code $?" ".NET installation failed - could not extract archive."
        log_debug "Error: tar command failed for $SOURCE_ARCHIVE"
        return 1
    fi
}

# Verifies the .NET runtime installation.
verify_dotnet_runtime() {
    log_info "Verifying .NET runtime installation..."
    local RUNTIMES_DIR="$PACKAGE_DIR/runtimes"
    local DOTNET_RUNTIME_PATH="$RUNTIMES_DIR/dotnet"

    if [ -f "$DOTNET_RUNTIME_PATH" ]; then
        log_info ".NET runtime found at: $DOTNET_RUNTIME_PATH"
        
        # Test if dotnet executable works
        if "$DOTNET_RUNTIME_PATH" --info >> /tmp/adwh-install.log 2>&1; then
            log_info ".NET runtime verification successful"
        else
            log_warning ".NET runtime exists but failed version check"
            log_debug "Warning: .NET runtime verification failed. Check /tmp/adwh-install.log for details."
            return 1
        fi
    else
        log_error ".NET runtime not found at expected location: $DOTNET_RUNTIME_PATH"
        log_debug "Error: .NET runtime installation failed. Runtime not found at $DOTNET_RUNTIME_PATH"
        return 1
    fi
}

#endregion
