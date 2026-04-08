# ASkyl.Dsm.WebHosting

.NET Web sites hosting manager for Synology DSM 7.2+. Currently only tested x64 architecture but also built for armv7, armv8.

**Author:** Brice FROMENTIN  
**Framework:** .NET 10 (net10.0)  
**UI:** Blazor Hybrid (Interactive WebAssembly) with FluentUI components

Currently, the project uses a Blazor Hybrid approach (Server-side authentication with Interactive WebAssembly) for security reasons and optimal cold start performance.

This project has also the following goals :

- Test IA for coding assistant.

## TODO

    Macro
        - Certificates management for the reverse proxy configurations.
        - Check required .NET version when configuraring an application.
        - Route applications stdout and errout to downloadable logs.
        - Support deploying from a compressed file.
        - Implement multi language support.
        - ...

    Micro
        - Manage uninstall correctly when versions of Microsoft.NETCore.App & Microsoft.AspNetCore.App are not the same (seen in 9.0.0 rc 2).
        - Enhance logs for the installation and package life-cycle
        - ...
    
    Technical
        - Implement unit tests.
        - ...
