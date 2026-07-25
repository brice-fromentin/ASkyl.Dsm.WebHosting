# ASkyl.Dsm.WebHosting

![.NET 10](https://img.shields.io/badge/.NET-10.0-blue?logo=dotnet)
![DSM 7.2+](https://img.shields.io/badge/DSM-7.2%2B-orange)
![Architectures](https://img.shields.io/badge/Arch-x64%20%7C%20ARMv7%20%7C%20ARMv8-success)
![License](https://img.shields.io/badge/License-GPLv3-blue.svg)

A modern .NET web application hosting manager tailored for Synology DSM 7.2+.

## 📦 Installation

**Prerequisites:** Synology DSM 7.2+, `.spk` package file.

### Build the Package

From the repository root, run:

```bash
./src/scripts/build-spk.sh
```

The `.spk` package will be output to the `dist/` folder. Requires `curl`, `tar`, `dotnet`, `jq`, `awk`, `pigz`.

### Install on DSM

1. Open **Package Center** in DSM
2. Click **Manual Install**
3. Select the `.spk` file from `dist/`
4. Follow the installation wizard

The application will be available from the **DSM Menu**.

---

## 🚀 Overview

`ASkyl.Dsm.WebHosting` provides a lightweight management interface directly integrated into Synology DiskStation Manager to deploy, configure, and monitor .NET-based web applications.

### Key Architecture Features

* **Framework:** Powered by **.NET 10** (`net10.0`).
* **UI Stack:** Built with **Blazor Hybrid (Interactive WebAssembly)** utilizing Microsoft **FluentUI** components.
* **Performance & Security:** Server-side authentication with Interactive WebAssembly ensures security and optimal cold start performance.
* **Multi-Arch Support:** Actively tested on `x64` architectures; built and packaged to support `armv7` and `armv8` systems.

---

> **Note:** This project also serves as an experimental sandbox for evaluating AI-driven coding assistants in professional .NET ecosystem developments.

---

## 🛠️ Roadmap & TODO

### 📦 Macro Features

* [ ] **Certificate Management:** Automated and manual SSL/TLS certificate handling for reverse proxy configurations.
* [ ] **Web Station Integration:** Native hook support with Synology Web Station.
* [ ] **Advanced Logging:** Route applications `stdout` and `stderr` to accessible and downloadable real-time logs.
* [ ] **Deployment Pipelines:** Support direct application deployment from compressed packages (.zip/.tar.gz).
* [ ] **Auto-Restart on Assembly Change:** Detect overwritten assemblies and automatically restart the affected website.
* [ ] **Submit to Synology Package Center:** Get the package reviewed and listed in the official Synology repository.

### 🐛 Micro & Edge Cases

* [ ] **Lifecycle & Uninstallation:** Fix version mismatch bugs during uninstall routines when `Microsoft.NETCore.App` and `Microsoft.AspNetCore.App` run separate revisions.

### 🧪 Technical & Quality Assurance

* [ ] **Integration Testing:** Implement comprehensive test suites for `DownloaderService`.
* [ ] **UI Testing:** Add Blazor component unit tests leveraging `bUnit`.
* [ ] **Exception Handling:** Refactor global error boundaries and implement specialized exceptions management.

### 🏗️ Improvements

* [ ] **Health Checks:** `/health` endpoint, website responsiveness, DSM API connectivity monitoring
* [ ] **Configuration Migration:** Version `websites.json` schema, migration tool, backup/restore
* [ ] **Multi-Language E2E Testing:** Validate localization across all supported cultures
