# USB-over-IP - Open Source USB Device Sharing

A complete solution for sharing USB devices over a network using the usbipd-win infrastructure. This project provides both a backend service for sharing USB devices and a Windows client application for discovering and connecting to remote devices.

## Features

### Backend Server
- **Windows Service**: Runs as a background service on Windows
- **REST API**: Full-featured API for device management
- **UDP Discovery**: Automatic server announcement for easy client discovery
- **usbipd-win Integration**: Leverages the robust usbipd-win implementation
- **Comprehensive Logging**: Structured logging with Serilog (JSON and text formats)
- **Device Management**: Share and unshare USB devices for network access

### Windows Client
- **Server Discovery**: Automatically discovers servers on the local network
- **System Tray Integration**: Runs in background with system tray icon
- **Auto-start Support**: Configure to start with Windows
- **Device Management UI**: WPF application with intuitive interface
- **Local USBIP Client**: Attaches/detaches devices locally using USBIP protocol
- **Multi-Server Support**: Connect to different USB servers as needed

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         USB-over-IP System                       │
└─────────────────────────────────────────────────────────────────┘

┌──────────────────────┐                    ┌──────────────────────┐
│   Windows Server     │                    │   Windows Client     │
│                      │                    │                      │
│  ┌────────────────┐  │    UDP Broadcast   │  ┌────────────────┐  │
│  │ Backend Service│  │───────(Discovery)──>│  │  Client Agent  │  │
│  │                │  │    Port 50052      │  │                │  │
│  │ - REST API     │  │                    │  │ - WPF UI       │  │
│  │ - Discovery    │  │    REST API        │  │ - Auto-start   │  │
│  │ - Windows Svc  │  │<──(Share/Unshare)──│  │ - System Tray  │  │
│  └────────┬───────┘  │    Port 50051      │  │ - Local USBIP  │  │
│           │          │                    │  └────────┬───────┘  │
│  ┌────────▼───────┐  │                    │           │          │
│  │  usbipd-win    │  │    USB/IP Protocol │  ┌────────▼───────┐  │
│  │  (Server)      │  │<───(USB Traffic)───│  │   usbip.exe    │  │
│  │                │  │    Port 3240       │  │   (Client)     │  │
│  │ bind/unbind    │  │                    │  │ attach/detach  │  │
│  └────────┬───────┘  │                    │  └────────┬───────┘  │
│           │          │                    │           │          │
│  ┌────────▼───────┐  │                    │  ┌────────▼───────┐  │
│  │  USB Devices   │  │                    │  │   Applications │  │
│  │  (Physical)    │  │                    │  │  (Using Device)│  │
│  └────────────────┘  │                    │  └────────────────┘  │
└──────────────────────┘                    └──────────────────────┘

Workflow:
1. Server shares device (bind) via REST API
2. Client attaches device locally using usbip.exe
3. USB traffic flows over USB/IP protocol (port 3240)
4. Client detaches device locally when done
5. Server unshares device (unbind) via REST API
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
- `usbip.exe` client tool in PATH (included with usbipd-win or available separately)
  - For usbipd-win users: The `usbip.exe` client is typically located at:
    `C:\Program Files\usbipd-win\usbip.exe`
  - Add this directory to your system PATH for automatic detection

## Quick Start

### 1. Install Prerequisites

**On the server machine:**

```powershell
# Install usbipd-win
winget install --id dorssel.usbipd-win
```

**On the client machine:**

```powershell
# Install usbipd-win (for the usbip.exe client tool)
winget install --id dorssel.usbipd-win

# Add usbip.exe to PATH
$usbipPath = "C:\Program Files\usbipd-win"
[Environment]::SetEnvironmentVariable("Path", $env:Path + ";$usbipPath", [System.EnvironmentVariableTarget]::Machine)

# Verify installation
usbip version
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
- Allow you to share/unshare devices on the server
- Allow you to attach/detach devices locally
- Run in the system tray

**Note:** To attach a device:
1. First click "Share" on the device (this runs on the server)
2. Then click "Attach" (this runs `usbip attach` locally on your client)
3. To release the device, click "Detach" then "Unshare"

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

**On the server machine:**

```powershell
# Discovery Port (UDP)
New-NetFirewallRule -DisplayName "USB-over-IP Discovery" -Direction Inbound -Protocol UDP -LocalPort 50052 -Action Allow

# API Port (TCP)
New-NetFirewallRule -DisplayName "USB-over-IP API" -Direction Inbound -Protocol TCP -LocalPort 50051 -Action Allow

# USB/IP Traffic (TCP) - for usbipd-win server
New-NetFirewallRule -DisplayName "USB/IP Server" -Direction Inbound -Protocol TCP -LocalPort 3240 -Action Allow
```

**On the client machine:**

```powershell
# USB/IP Traffic (TCP) - for usbip client
New-NetFirewallRule -DisplayName "USB/IP Client" -Direction Outbound -Protocol TCP -RemotePort 3240 -Action Allow
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
│   │   ├── Services/                   # Client services (API, Discovery, LocalUsbipClient)
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

#### Validate Device for Attachment
```
POST /api/devices/{busId}/attach
```
Validates that a device is shared and ready for client-side attachment.
Note: Actual attachment is performed locally on the client using `usbip attach`.

#### Validate Device for Detachment
```
POST /api/devices/{busId}/detach
```
Validates that a device exists.
Note: Actual detachment is performed locally on the client using `usbip detach`.

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

1. Ensure device is shared first (click "Share" button)
2. Verify `usbip.exe` is in PATH on the client: `usbip version`
3. Verify firewall allows TCP port 3240 on both server and client
4. Check server logs for share status
5. Try manual attachment to test:
   ```powershell
   # From client machine
   usbip attach -r <server-ip> -b <bus-id>
   ```
6. Check attached devices:
   ```powershell
   usbip port
   ```
7. If attachment works manually but not via GUI, check client application logs

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
