# Askyl DSM Web Hosting - Synology SPK Package

This directory contains the complete structure for creating a Synology package (.spk) for the Askyl DSM Web Hosting application.

**Author:** Brice FROMENTIN  
**Maintainer:** Brice FROMENTIN  
**Version:** 1.0.0  
**Target DSM:** 7.2+

## Overview

Askyl DSM Web Hosting is a .NET web hosting management system designed to run on Synology NAS devices. This package provides:

- .NET web application running on port 7120
- HTTPS reverse proxy via nginx on port 7121
- Official Synology DSM menu integration using `admin*` fields
- Automated service management and port configuration

## Package Structure

```
spk-project/
├── INFO                      # Package metadata and configuration
├── package.tgz              # Application files archive (auto-generated)
├── scripts/                 # Lifecycle management scripts
│   ├── preinst             # Pre-installation script
│   ├── postinst            # Post-installation script  
│   ├── preuninst           # Pre-uninstallation script
│   ├── postuninst          # Post-uninstallation script
│   └── start-stop-status   # Service control script
├── conf/                    # SPK configuration files
│   ├── privilege           # User privilege configuration
│   └── resource            # Resource allocation (ports, nginx)
├── package/                 # Application content (packaged to package.tgz)
│   ├── etc/                # Configuration and service files
│   │   ├── adwh-dsm-services      # Main service control script
│   │   ├── adwh-reverse-proxy.conf # Nginx HTTPS proxy config
│   │   └── adwh.sc               # Port configuration file
│   └── ui/                 # Published .NET application files
├── PACKAGE_ICON.PNG         # 72x72 package icon
├── PACKAGE_ICON_256.PNG     # 256x256 high-res icon
└── LICENSE                  # Package license
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

## Development Notes

- All service and configuration files are centralized in `package/etc/`
- Scripts reference the unified `/var/packages/AskylDsmWebHosting/target/etc/` path
- The package uses official Synology APIs and follows DSM developer guidelines
- No custom DSM UI components - uses native admin link integration

## References

- [Official Synology Developer Guide](https://help.synology.com/developer-guide/)
- [Synology Package Structure](https://help.synology.com/developer-guide/synology_package/introduction.html)
- [DSM Integration Guide](https://help.synology.com/developer-guide/integrate_dsm/integration.html)
- [Port Configuration](https://help.synology.com/developer-guide/integrate_dsm/ports.html)
