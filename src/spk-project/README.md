# Askyl DSM Web Hosting - Synology SPK Package

This directory contains the complete structure for creating a Synology package (.spk) for the Askyl DSM Web Hosting application.

**Author:** Brice FROMENTIN  
**Maintainer:** Brice FROMENTIN  
**Target DSM:** 7.2+

## Overview

Askyl DSM Web Hosting is a .NET web hosting management system designed to run on Synology NAS devices. This package provides:

- .NET web application running on port 7120
- HTTPS reverse proxy via nginx on port 7121
- Automated service management and port configuration

## Package Structure

```
spk-project/
├── INFO                            # Package metadata and configuration
├── package.tgz                     # Application files archive (auto-generated)
├── scripts/                        # Lifecycle management scripts
│   ├── preinst                     # Pre-installation script
│   ├── postinst                    # Post-installation script
│   ├── preuninst                   # Pre-uninstallation script
│   ├── postuninst                  # Post-uninstallation script
│   ├── start-stop-status           # Service control script
│   └── common-functions.sh         # Shared functions library
├── conf/                           # SPK configuration files
│   ├── privilege                   # User privilege configuration
│   └── resource                    # Resource allocation (ports, nginx)
├── package/                        # Application content (packaged to package.tgz)
│   ├── etc/                        # Configuration files
│   │   ├── adwh-reverse-proxy.conf # Nginx HTTPS proxy config
│   │   └── adwh.sc                 # Port configuration file
│   └── admin-ui/                   # Published .NET application files
├── PACKAGE_ICON.PNG                # 72x72 package icon
├── PACKAGE_ICON_256.PNG            # 256x256 high-res icon
└── LICENSE                         # Package license
```

## Key Features

### DSM Integration
- **Admin Link**: Direct access via DSM menu using official `adminport`, `adminurl`, and `adminprotocol` fields
- **Port Management**: Automatic port conflict checking with `checkport="7121,7120"`
- **Service Control**: Integrated start/stop/status management through DSM

### Network Configuration
- **Port 7121**: HTTPS nginx reverse proxy (user-facing)
- **Port 7120**: .NET application backend (internal)
- **SSL/TLS**: Uses Synology's default certificates with modern TLS protocols

### Security Features
- HTTPS-only access with strict security headers
- Modern TLS 1.2/1.3 protocols and secure cipher suites
- HSTS, X-Frame-Options, and other security headers
- WebSocket support for real-time features

## Building the Package

Use the provided build script to create the SPK package:

```bash
# From the project root directory
./build-spk.sh
```

The generated `.spk` file will be available in the `dist/` directory.

## Installation

1. Upload the generated `.spk` file to your Synology NAS via Package Center
2. Install the package through DSM's Package Center
3. Access the application via the DSM menu item "Askyl DSM Web Hosting"
4. The application will be available at `https://YOUR_NAS_IP:7121/`

## Technical Requirements

- **DSM Version**: 7.2 or higher
- **Architecture**: x86_64 (64-bit Intel/AMD processors only)
- **Ports**: 7120 (internal), 7121 (HTTPS proxy)
- **Services**: nginx service restart during install/uninstall

## Configuration Files

### INFO
Contains package metadata, port configuration, and DSM integration settings.

### conf/resource
Defines nginx configuration and port allocation for the Synology resource manager.

### package/etc/adwh.sc
Port service configuration file following official Synology documentation for firewall and port forwarding integration.

## Environment Variables

The package scripts use various environment variables for configuration and Synology DSM integration:

### Synology Package Variables (Automatically Set)

| Variable | Description | Example Value |
|----------|-------------|---------------|
| `SYNOPKG_PKGNAME` | Package name as defined in INFO file | `AskylWebHosting` |
| `SYNOPKG_PKGDEST` | Package installation directory | `/var/packages/AskylWebHosting/target` |
| `SYNOPKG_DSM_ARCH` | Target DSM architecture | `x86_64` |
| `SYNOPKG_PKG_PROGRESS_PATH` | Installation progress file path | `/tmp/synopkg_progress.XXXXX` |
| `SYNOPKG_TEMP_LOGFILE` | Temporary log file for user messages | `/tmp/synopkg_temp.log` |

### Package-Defined Variables

| Variable | Description | Default Value | Usage |
|----------|-------------|---------------|-------|
| `PACKAGE_DIR` | Package installation directory | `$SYNOPKG_PKGDEST` or `/var/packages/AskylWebHosting/target` | Application files location |
| `VAR_DIR` | Package variable data directory | `/var/packages/AskylWebHosting/var` | Runtime data, logs, PID files |
| `LOG_FILE` | Default log file path | `$VAR_DIR/logs/install.log` | Installation and upgrade logs |

### Application-Specific Variables

| Variable | Description | Value | Usage |
|----------|-------------|-------|-------|
| `ASPNETCORE_ENVIRONMENT` | .NET environment mode | `Production` | Application configuration |
| `ASPNETCORE_URLS` | .NET application binding | `http://0.0.0.0:7120` | Internal application port |
| `APP_DIR` | Application directory | `$PACKAGE_DIR/admin-ui` | .NET application files |
| `APP_NAME` | Application executable name | `Askyl.Dsm.WebHosting.Ui` | Main DLL name |
| `PID_FILE` | Service process ID file | `$VAR_DIR/admin-ui.pid` | Process management |
| `SERVICE_LOG_FILE` | Service control logs | `$VAR_DIR/logs/service.log` | Start/stop operations |
| `APP_LOG_FILE` | Application output logs | `$VAR_DIR/logs/application.log` | .NET app stdout/stderr |

### Debug and Beta Mode Variables

| Variable | Description | Values | Usage |
|----------|-------------|--------|-------|
| `_IS_BETA_MODE` | Beta mode detection (internal) | `true`/`false` | Debug logging behavior |
| `_BETA_MODE_INITIALIZED` | Beta mode init flag (internal) | `true`/`false` | Performance optimization |

### Usage Examples

```bash
# Access package directory
echo "Application files in: $PACKAGE_DIR/admin-ui"

# Check variable directory
ls -la "$VAR_DIR/logs/"

# Update installation progress (in scripts)
update_progress 0.5 "Installing .NET runtime"

# Log to specific file
log_info "Custom message" "/path/to/custom.log"
```

### Beta Mode Behavior

When `beta="yes"` in the INFO file:
- Debug logs go to `/tmp/adwh-debug.log`
- More verbose logging for troubleshooting

When `beta="no"` (release mode):
- Debug logs go to standard log files
- Production-level logging

## Development Notes

- All service and configuration files are centralized in `package/etc/`
- Scripts reference the unified `/var/packages/AskylWebHosting/target/etc/` path
- The package uses official Synology APIs and follows DSM developer guidelines
- No custom DSM UI components - uses native admin link integration

## References

- [Official Synology Developer Guide](https://help.synology.com/developer-guide/)
- [Synology Package Structure](https://help.synology.com/developer-guide/synology_package/introduction.html)
- [DSM Integration Guide](https://help.synology.com/developer-guide/integrate_dsm/integration.html)
- [Port Configuration](https://help.synology.com/developer-guide/integrate_dsm/ports.html)
