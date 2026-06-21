# ASkyl.Dsm.WebHosting

![.NET 10](https://img.shields.io/badge/.NET-10.0-blue?logo=dotnet)
![DSM 7.2+](https://img.shields.io/badge/DSM-7.2%2B-orange)
![Architectures](https://img.shields.io/badge/Arch-x64%20%7C%20ARMv7%20%7C%20ARMv8-success)
![License](https://img.shields.io/badge/License-GPLv3-blue.svg)

A modern .NET web applications and sites hosting manager tailored for Synology DSM 7.2+.

## 🚀 Overview

`ASkyl.Dsm.WebHosting` provides a lightweight management interface directly integrated into Synology DiskStation Manager to deploy, configure, and monitor .NET-based web applications.

### Key Architecture Features
* **Framework:** Powered by **.NET 10** (`net10.0`).
* **UI Stack:** Built with **Blazor Hybrid (Interactive WebAssembly)** utilizing Microsoft **FluentUI** components.
* **Performance & Security:** Uses a hybrid architecture (Server-side authentication coupled with Interactive WebAssembly) ensuring a secure authentication boundary and optimal cold start performance on NAS hardware.
* **Multi-Arch Support:** Actively tested on `x64` architectures; built and packaged to support `armv7` and `armv8` systems.

> **Note:** This project also serves as an experimental sandbox for evaluating AI-driven coding assistants in professional .NET ecosystem developments.

---

## 🛠️ Roadmap & TODO

### 📦 Macro Features
- [ ] **Certificate Management:** Automated and manual SSL/TLS certificate handling for reverse proxy configurations.
- [ ] **Web Station Integration:** Native hook support with Synology Web Station.
- [ ] **Advanced Logging:** Route applications `stdout` and `stderr` to accessible and downloadable real-time logs.
- [ ] **Deployment Pipelines:** Support direct application deployment from compressed packages (.zip/.tar.gz).

### 🐛 Micro & Edge Cases
- [ ] **Lifecycle & Uninstallation:** Fix version mismatch bugs during uninstall routines when `Microsoft.NETCore.App` and `Microsoft.AspNetCore.App` run separate revisions (observed in 9.0.0 RC2).

### 🧪 Technical & Quality Assurance
- [ ] **Integration Testing:** Implement comprehensive test suites for `DsmApiClient`, `FileSystemService`, and `DownloaderService`.
- [ ] **UI Testing:** Add Blazor component unit tests leveraging `bUnit`.
- [ ] **Exception Handling:** Refactor global error boundaries and implement specialized exceptions management.
- [ ] **Architecture Refactoring:** Optimize `DsmApiClient` life cycle management.
