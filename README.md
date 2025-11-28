# USB-over-IP - Open Source USB Device Sharing

A complete solution for sharing USB devices over a network using the usbipd-win infrastructure. This project provides both a backend service for sharing USB devices and a Windows client application for discovering and connecting to remote devices.

## Features

### Backend Server
- **Windows Service**: Runs as a background service on Windows
- **REST API**: Full-featured API for device management
- **UDP Discovery**: Automatic server announcement for easy client discovery
- **usbipd-win Integration**: Leverages the robust usbipd-win implementation
- **Comprehensive Logging**: Structured logging with Serilog (JSON and text formats)
- **Device Management**: Share, unshare, attach, and detach USB devices remotely

### Windows Client
- **Server Discovery**: Automatically discovers servers on the local network
- **System Tray Integration**: Runs in background with system tray icon
- **Auto-start Support**: Configure to start with Windows
- **Device Management UI**: WPF application with intuitive interface
- **Multi-Server Support**: Connect to multiple USB servers simultaneously

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         USB-over-IP System                       │
└─────────────────────────────────────────────────────────────────┘

┌──────────────────────┐                    ┌──────────────────────┐
│   Windows Server     │                    │   Windows Client     │
│                      │                    │                      │
│  ┌────────────────┐  │                    │  ┌────────────────┐  │
│  │ Backend Service│  │    UDP Broadcast   │  │  Client Agent  │  │
│  │                │  │───────(Discovery)──>│  │                │  │
│  │ - REST API     │  │    Port 50052      │  │ - WPF UI       │  │
│  │ - Discovery    │  │                    │  │ - Auto-start   │  │
│  │ - Windows Svc  │  │    REST API        │  │ - System Tray  │  │
│  └────────┬───────┘  │<───(Device Ops)────│  └────────┬───────┘  │
│           │          │    Port 50051      │           │          │
│  ┌────────▼───────┐  │                    │  ┌────────▼───────┐  │
│  │  usbipd-win    │  │    USB/IP Protocol │  │  usbipd-win    │  │
│  │                │  │<───(USB Traffic)───>│  │                │  │
│  └────────┬───────┘  │    Port 3240       │  └────────┬───────┘  │
│           │          │                    │           │          │
│  ┌────────▼───────┐  │                    │  ┌────────▼───────┐  │
│  │  USB Devices   │  │                    │  │   Applications │  │
│  │  (Physical)    │  │                    │  │  (Using Device)│  │
│  └────────────────┘  │                    │  └────────────────┘  │
└──────────────────────┘                    └──────────────────────┘
```

## Prerequisites

### Backend Server
- Windows 10/11 or Windows Server 2016+
- .NET 8.0 Runtime
- [usbipd-win](https://github.com/dorssel/usbipd-win) installed
- Administrator privileges (for Windows Service installation)

### Windows Client
- Windows 10/11
- .NET 8.0 Runtime
- [usbipd-win](https://github.com/dorssel/usbipd-win) installed

## Quick Start

### 1. Install usbipd-win

On both server and client machines:

```powershell
winget install --id dorssel.usbipd-win
```

### 2. Build the Project

```powershell
git clone <repository-url>
cd 00050_usbhere
dotnet build -c Release
```

### 3. Install Backend Service

On the server machine, run PowerShell as Administrator:

```powershell
cd src\UsbOverIp.BackendService\scripts
.\install-service.ps1
.\start-service.ps1
```

The service will:
- Start automatically with Windows
- Listen on port 50051 (API)
- Broadcast on port 50052 (Discovery)
- Write logs to `logs/service-YYYYMMDD.txt`

### 4. Run the Client

On client machines:

```powershell
cd src\UsbOverIp.ClientAgent\bin\Release\net8.0-windows
.\UsbOverIp.ClientAgent.exe
```

The client will:
- Automatically discover servers on the network
- Display available USB devices
- Allow you to attach/detach devices
- Run in the system tray

## Configuration

### Backend Server Configuration

Edit `appsettings.json` in the service directory:

```json
{
  "ServerConfiguration": {
    "ServerId": "00000000-0000-0000-0000-000000000000",
    "ServerName": "MyUsbServer",
    "ApiPort": 50051,
    "DiscoveryPort": 50052,
    "BroadcastIntervalSeconds": 5
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information"
    }
  }
}
```

### Firewall Configuration

Allow the following ports through Windows Firewall:

```powershell
# Discovery Port (UDP)
New-NetFirewallRule -DisplayName "USB-over-IP Discovery" -Direction Inbound -Protocol UDP -LocalPort 50052 -Action Allow

# API Port (TCP)
New-NetFirewallRule -DisplayName "USB-over-IP API" -Direction Inbound -Protocol TCP -LocalPort 50051 -Action Allow

# USB/IP Traffic (TCP) - handled by usbipd-win
New-NetFirewallRule -DisplayName "USB/IP" -Direction Inbound -Protocol TCP -LocalPort 3240 -Action Allow
```

## Project Structure

```
00050_usbhere/
├── src/
│   ├── UsbOverIp.BackendService/      # Windows Service (server)
│   │   ├── Api/                        # REST API endpoints
│   │   ├── Discovery/                  # UDP broadcast service
│   │   ├── Services/                   # Core services (DeviceManager, UsbipdClient)
│   │   ├── scripts/                    # PowerShell installation scripts
│   │   └── Program.cs                  # Service entry point
│   │
│   ├── UsbOverIp.ClientAgent/         # WPF Client Application
│   │   ├── Services/                   # Client services
│   │   ├── ViewModels/                 # MVVM view models
│   │   ├── MainWindow.xaml             # Main UI
│   │   └── App.xaml.cs                 # Application entry point
│   │
│   └── UsbOverIp.Shared/              # Shared library
│       ├── Models/                     # Data models
│       ├── Contracts/                  # Interfaces
│       └── Utilities/                  # Helper classes
│
├── tests/
│   ├── UsbOverIp.BackendService.Tests/
│   ├── UsbOverIp.ClientAgent.Tests/
│   └── UsbOverIp.Shared.Tests/
│
└── README.md
```

## API Reference

### REST API Endpoints

#### Health Check
```
GET /api/health
```
Returns server health status and statistics.

#### List Devices
```
GET /api/devices
```
Returns all USB devices with their current state.

#### Share Device
```
POST /api/devices/{busId}/share
```
Makes a device available for remote attachment.

#### Unshare Device
```
POST /api/devices/{busId}/unshare
```
Stops sharing a device.

#### Attach Device
```
POST /api/devices/{busId}/attach
```
Attaches a shared device to the requesting client.

#### Detach Device
```
POST /api/devices/{busId}/detach
```
Detaches a device from the client.

## Development

### Running Tests

```powershell
dotnet test
```

### Running in Console Mode (Development)

Backend service can run as console application for debugging:

```powershell
cd src\UsbOverIp.BackendService
dotnet run
```

### Building for Release

```powershell
dotnet build -c Release
```

## Technology Stack

- **.NET 8.0**: Core framework
- **ASP.NET Core**: REST API and hosting
- **WPF**: Client user interface
- **Serilog**: Structured logging
- **usbipd-win**: USB/IP infrastructure
- **xUnit**: Unit testing

## Troubleshooting

### Service Won't Start

1. Check Event Viewer: Windows Logs > Application
2. Check service logs: `logs/service-YYYYMMDD.txt`
3. Verify usbipd-win is installed: `usbipd --version`
4. Ensure port 50051 is not in use

### Client Cannot Discover Servers

1. Verify firewall allows UDP port 50052
2. Check server is running: `Get-Service UsbOverIpBackendService`
3. Ensure client and server are on the same network
4. Check server logs for broadcast activity

### Device Attachment Fails

1. Ensure device is shared first
2. Verify firewall allows TCP ports 50051 and 3240
3. Check usbipd-win logs: `usbipd list`
4. Run client as Administrator if needed

### Permission Issues

The service runs as Local System by default. To change:

1. Open `services.msc`
2. Right-click "USB-over-IP Backend Service"
3. Properties > Log On tab
4. Configure the desired account

## Contributing

1. Fork the repository
2. Create a feature branch
3. Write tests for new functionality
4. Ensure all tests pass
5. Submit a pull request

## License

[Specify your license here]

## Acknowledgments

- [usbipd-win](https://github.com/dorssel/usbipd-win) - The excellent USB/IP implementation for Windows
- USB/IP Project - The original Linux USB/IP protocol

## Support

For issues and feature requests, please use the GitHub issue tracker.
