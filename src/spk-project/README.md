# Synology SPK Package Structure

This directory contains the minimal structure required to create a Synology package (.spk).

**Author:** Brice FROMENTIN  
**Maintainer:** Askyl

## File Structure

```
spk-project/
├── INFO                    # Package properties file (required)
├── package.tgz            # Application files archive (generated during build)
├── scripts/               # Lifecycle scripts (required)
│   ├── preinst           # Executed before installation
│   ├── postinst          # Executed after installation
│   ├── preuninst         # Executed before uninstallation
│   ├── postuninst        # Executed after uninstallation
│   └── start-stop-status # Service control
├── conf/                  # Additional configurations (required)
│   ├── privilege         # Privilege configuration
│   └── resource          # Resource configuration
├── package/               # Application content (will be compressed to package.tgz)
│   ├── ui/               # User interface files
│   └── bin/              # Binaries and executables
├── PACKAGE_ICON.PNG       # 64x64 icon (required)
├── PACKAGE_ICON_256.PNG   # 256x256 icon (required)
└── LICENSE               # Package license (optional)
```

## Package Build

To create the .spk file, use the build script:

```bash
./build-spk.sh
```

The `.spk` file will be generated in the `build/` directory.

## Important Notes

- Scripts in `scripts/` must be executable
- The `INFO` file contains package metadata
- The `package/` directory contains all files that will be installed on the system
- Icons must be in PNG format with the specified dimensions

## References

- [Official Synology Documentation](https://help.synology.com/developer-guide/synology_package/introduction.html)
- [DSM Developer Guide](https://help.synology.com/developer-guide/)
