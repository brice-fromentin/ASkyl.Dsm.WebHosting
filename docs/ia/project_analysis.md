# Detailed Project Analysis

## Askyl.Dsm.WebHosting.Uiz-Old (Main UI Application)

This is a Blazor Server application that provides the main user interface for managing .NET runtime installations and web site hosting.

### Key Components:

**Controllers:**
- `LogDownloadController.cs` - Handles log file downloads

**Services:**
- `DotnetVersionService.cs` - Manages .NET version operations
- `FileSystemService.cs` - File system operations
- `FrameworkManagementService.cs` - Framework management
- `LicenseService.cs` - License handling
- `LogDownloadService.cs` - Log download functionality
- `ProcessInfo.cs` - Process information tracking
- `TemporaryTokenService.cs` - Temporary token generation
- `WebSiteHostingService.cs` - Web site hosting operations

**Models:**
- ASP.NET releases and versions
- License information
- Installation and operation results
- Web site configurations

**Components:**
- Main application layout (`App.razor`)
- Navigation routes (`Routes.razor`)
- Main layout (`MainLayout.razor`)
- Various UI components:
  - Home page (`Home.razor`)
  - Login page (`Login.razor`)
  - ASP.NET releases dialog
  - .NET versions dialog
  - File selection dialog
  - Web site configuration dialog
  - Licenses dialog
  - Auto data grid controls
  - Loading overlay

## Askyl.Dsm.WebHosting.Ui (API Layer)

REST API controller layer that provides backend services.

### Key Components:

**Controllers:**
- `HelloWorldController.cs` - Basic test endpoint

**Services:**
- Various services for API operations

## Askyl.Dsm.WebHosting.Ui.Client (Client-side Blazor)

Client-side Blazor application that runs in the browser.

### Key Components:

**Components:**
- Home page (`Home.razor`)
- Not found page (`NotFound.razor`)
- Main layout (`MainLayout.razor`)
- Auto data grid controls
- Loading overlay

## Askyl.Dsm.WebHosting.Tools (Utility Layer)

Contains utility services and helpers for system operations.

### Key Services:

- `ReverseProxyManager.cs` - Manages reverse proxy configurations
- `WebSitesConfigurationService.cs` - Handles web site configurations
- `SemaphoreLock.cs` - Thread synchronization
- `Configuration.cs` - Runtime configuration management
- `Downloader.cs` - File downloading utilities
- `FileSystem.cs` - File system operations
- `GzUnTar.cs` - Archive extraction utilities
- `VersionsDetector.cs` - Version detection
- `DsmApiClient.cs` - DSM API client
- `ApiResponseExtensions.cs` - API response extensions

## Askyl.Dsm.WebHosting.Data (Data Layer)

Contains data models, interfaces, and exceptions.

### Key Components:

**Models:**
- Web site configurations and instances
- Framework information
- ASP.NET Core release information
- Process information
- Login model

**Interfaces:**
- `IReverseProxyManager.cs` - Reverse proxy management interface
- `IWebSitesConfigurationService.cs` - Web sites configuration interface

**Attributes:**
- `DsmParameterNameAttribute.cs` - Custom attribute for parameter naming

**Exceptions:**
- `FileStationApiException.cs` - File station API exception
- `LastReleaseUninstallException.cs` - Last release uninstall exception
- `MissingChannelConfigurationException.cs` - Missing channel configuration exception

## Askyl.Dsm.WebHosting.Logging (Logging Layer)

Provides logging functionality for the application.

### Key Components:

**HelloWorld:**
- Logging extensions for hello world functionality

## Askyl.Dsm.WebHosting.SourceGenerators (Source Generators)

Contains source generators for code generation.

### Key Components:

- `CloneGenerator.cs` - Clone generation logic
- `GenerateCloneAttribute.cs` - Attribute for clone generation

## Askyl.Dsm.WebHosting.Constants (Constants Layer)

Defines constants used throughout the application.

## Askyl.Dsm.WebHosting.DotnetInstaller (Runtime Installer)

Application for installing .NET runtimes.

### Key Components:

- Main program file with installation logic

## Build and Deployment

The solution includes:

- Dockerfile for containerization
- .dockerignore file
- Scripts for building solutions and SPK packages
- SPK packaging configuration

## Key Functionality

1. **Runtime Management**: Installation, detection, and management of .NET versions and ASP.NET Core releases
2. **Web Hosting**: Configuration and management of web sites with reverse proxy support
3. **User Interface**: Blazor-based UI for managing all aspects of the hosting system
4. **System Integration**: Communication with DSM API and file system operations