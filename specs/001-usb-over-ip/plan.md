# Implementation Plan: USB-over-IP Solution for Windows

**Branch**: `001-usb-over-ip` | **Date**: 2025-11-27 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-usb-over-ip/spec.md`

## Summary

Build a VirtualHere-like USB-over-IP solution for Windows consisting of:
1. **UsbBackendService** - A Windows Service that manages USB device sharing via usbipd-win, exposes a REST API, and broadcasts presence for LAN discovery
2. **UsbClientAgent** - A WPF tray application that discovers servers, displays available devices, and allows users to attach/detach remote USB devices

The solution leverages usbipd-win as the USB/IP protocol engine, wrapping it with a user-friendly management layer.

## Technical Context

**Language/Version**: C# 12 / .NET 8.0
**Primary Dependencies**:
- Microsoft.Extensions.Hosting.WindowsServices (Worker Service)
- ASP.NET Core Minimal APIs (REST endpoints)
- Hardcodet.NotifyIcon.Wpf (System tray)
- Serilog.AspNetCore (Logging)
- System.Text.Json (JSON serialization)

**Storage**: JSON configuration files (appsettings.json, device-config.json)
**Testing**: xUnit, Moq, FluentAssertions
**Target Platform**: Windows 10/11 (x64), Windows Server 2019+
**Project Type**: Multi-project (Windows Service + WPF Desktop)
**Performance Goals**:
- Device attachment <30 seconds
- Server discovery <10 seconds
- USB latency overhead <50ms on LAN
**Constraints**:
- Requires usbipd-win installed
- LAN-only (no WAN/internet)
- Administrator privileges for service installation
**Scale/Scope**:
- 10 simultaneous devices per server
- 3 simultaneous server connections per client

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

The constitution template has placeholder values. Applying general best practices:

| Principle | Status | Notes |
|-----------|--------|-------|
| Library-First | ✅ Pass | Shared library contains models, contracts, utilities |
| CLI Interface | ✅ Pass | Service supports `--install`, `--uninstall` arguments |
| Test-First | ✅ Pass | xUnit tests for all business logic |
| Integration Testing | ✅ Pass | Contract tests for API, mock usbipd for unit tests |
| Observability | ✅ Pass | Serilog structured logging with configurable levels |
| Simplicity | ✅ Pass | Minimal dependencies, standard patterns |

## Project Structure

### Documentation (this feature)

```text
specs/001-usb-over-ip/
├── plan.md              # This file
├── research.md          # Phase 0 output (completed)
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (OpenAPI spec)
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
src/
├── UsbOverIp.Shared/              # Shared library
│   ├── Models/
│   │   ├── UsbDevice.cs
│   │   ├── ServerInfo.cs
│   │   ├── AttachmentInfo.cs
│   │   └── ServerAnnouncement.cs
│   ├── Contracts/
│   │   ├── IUsbipdClient.cs
│   │   └── IDeviceManager.cs
│   └── Utilities/
│       └── NetworkHelper.cs
│
├── UsbOverIp.BackendService/      # Windows Service (Server)
│   ├── Program.cs
│   ├── Worker.cs
│   ├── appsettings.json
│   ├── UsbipdClient/
│   │   ├── UsbipdClient.cs
│   │   └── UsbipdStateParser.cs
│   ├── DeviceManager/
│   │   ├── DeviceManager.cs
│   │   └── DeviceStateStore.cs
│   ├── Discovery/
│   │   └── DiscoveryBroadcaster.cs
│   └── Api/
│       ├── DevicesEndpoints.cs
│       └── HealthEndpoints.cs
│
├── UsbOverIp.ClientAgent/         # WPF Tray Application (Client)
│   ├── App.xaml
│   ├── App.xaml.cs
│   ├── MainWindow.xaml
│   ├── MainWindow.xaml.cs
│   ├── appsettings.json
│   ├── ViewModels/
│   │   ├── MainViewModel.cs
│   │   ├── DeviceTreeViewModel.cs
│   │   └── SettingsViewModel.cs
│   ├── Views/
│   │   ├── DeviceTreeView.xaml
│   │   └── SettingsView.xaml
│   ├── Services/
│   │   ├── ServerDiscoveryService.cs
│   │   ├── ServerApiClient.cs
│   │   └── AutoStartManager.cs
│   └── Behaviors/
│       └── MinimizeToTrayBehavior.cs
│
└── UsbOverIp.sln                  # Solution file

tests/
├── UsbOverIp.Shared.Tests/
│   └── Models/
├── UsbOverIp.BackendService.Tests/
│   ├── Unit/
│   │   ├── UsbipdClientTests.cs
│   │   ├── DeviceManagerTests.cs
│   │   └── DiscoveryBroadcasterTests.cs
│   ├── Integration/
│   │   └── ApiEndpointsTests.cs
│   └── Contract/
│       └── UsbipdOutputParsingTests.cs
└── UsbOverIp.ClientAgent.Tests/
    └── Unit/
        ├── ServerDiscoveryServiceTests.cs
        └── ServerApiClientTests.cs
```

**Structure Decision**: Multi-project structure with shared library. The solution has three projects:
1. `UsbOverIp.Shared` - Class library with models, contracts, utilities
2. `UsbOverIp.BackendService` - Worker Service for the server component
3. `UsbOverIp.ClientAgent` - WPF application for the client component

This structure enables code reuse while maintaining clear separation between server and client concerns.

## Complexity Tracking

No constitution violations requiring justification. The three-project structure is the minimum required for this feature (shared code + server + client).
