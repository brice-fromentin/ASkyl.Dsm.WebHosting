# .NET Solution Overview

## Project Structure

This solution contains multiple projects organized under the `Askyl.Dsm.WebHosting` namespace:

### Main Projects:
- **Askyl.Dsm.WebHosting.Uiz-Old** - Main UI application (Blazor Server)
- **Askyl.Dsm.WebHosting.Ui** - API controller layer
- **Askyl.Dsm.WebHosting.Ui.Client** - Client-side Blazor application
- **Askyl.Dsm.WebHosting.DotnetInstaller** - .NET runtime installation tool

### Supporting Projects:
- **Askyl.Dsm.WebHosting.Data** - Data models and interfaces
- **Askyl.Dsm.WebHosting.Tools** - Utility services and helpers
- **Askyl.Dsm.WebHosting.Logging** - Logging functionality
- **Askyl.Dsm.WebHosting.SourceGenerators** - Source generators
- **Askyl.Dsm.WebHosting.Constants** - Constants definitions
- **Askyl.Dsm.WebHosting.DotnetInstaller** - .NET runtime installer

## Key Components

### UI Layer (Uiz-Old):
- Blazor Server application with MVC-style controllers
- Contains components for:
  - Home page
  - Login page
  - ASP.NET releases dialog
  - .NET versions dialog
  - File selection dialog
  - Web site configuration dialog
  - Licenses dialog
  - Auto data grid controls
  - Loading overlay

### API Layer (Ui):
- REST API controllers
- HelloWorld controller for basic testing
- Authentication and authorization services

### Tools Layer:
- Reverse proxy management
- Web sites configuration service
- File system operations
- Runtime version detection
- Network communication with DSM API
- Semaphore locking mechanisms
- Downloading and extraction utilities

### Data Layer:
- Models for web sites, frameworks, ASP.NET releases
- Interfaces for reverse proxy and website configuration
- Security models (login)
- Custom attributes and exceptions

## Key Features

1. **.NET Runtime Management**:
   - Installation of .NET versions
   - Version detection and management
   - ASP.NET Core release handling

2. **Web Site Hosting**:
   - Web site configuration management
   - Reverse proxy setup
   - Process monitoring

3. **User Interface**:
   - Blazor-based UI with components
   - Dialogs for various operations
   - File system navigation
   - License management

4. **System Integration**:
   - DSM API communication
   - File system operations
   - Network utilities
   - Logging capabilities

## Technologies Used

- .NET 10 (as mentioned)
- Blazor Server and Client
- ASP.NET Core MVC
- C# with modern language features
- Docker containerization support
- Shell scripting for build processes