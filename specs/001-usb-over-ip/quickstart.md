# Quickstart Guide: USB-over-IP Solution

## Prerequisites

### Server Machine (USB Host)
1. **Windows 10 Build 19041+** or **Windows Server 2019+**
2. **usbipd-win** installed:
   ```powershell
   winget install --interactive --exact dorssel.usbipd-win
   ```
3. **Administrator privileges** for service installation
4. **Firewall ports open**:
   - TCP 50051 (REST API)
   - UDP 50050 (Discovery broadcast)
   - TCP 3240 (USB/IP protocol - configured by usbipd-win)

### Client Machine
1. **Windows 10/11** (x64)
2. **.NET 8.0 Runtime** installed
3. **Network access** to server machine

---

## Installation

### Server (UsbBackendService)

1. **Download** the latest release or build from source
2. **Install as Windows Service**:
   ```powershell
   # Run as Administrator
   .\UsbOverIp.BackendService.exe --install
   ```
3. **Start the service**:
   ```powershell
   sc start UsbBackendService
   ```
4. **Verify** the service is running:
   ```powershell
   sc query UsbBackendService
   # Or check http://localhost:50051/api/health
   ```

### Client (UsbClientAgent)

1. **Download** the latest release or build from source
2. **Run** `UsbOverIp.ClientAgent.exe`
3. **Optional**: Enable "Start with Windows" in settings

---

## Quick Usage

### Server: Share a USB Device

**Option A: Via API**
```bash
# List devices
curl http://localhost:50051/api/devices

# Share a device
curl -X POST http://localhost:50051/api/devices/1-1/share
```

**Option B: Via Management Console** (if running interactively)
- The service logs to console when run without `--install`
- Use the REST API for device management

### Client: Attach to a Remote Device

1. **Launch** the UsbClientAgent tray application
2. **Wait** for automatic server discovery (or add server manually)
3. **Expand** the server in the device tree
4. **Right-click** a shared device → **"Attach"**
5. **Device appears** in Windows Device Manager as if locally connected

### Client: Detach a Device

1. **Right-click** the attached device in the tray menu
2. Select **"Detach"**
3. Device is released and available for other clients

---

## Configuration

### Server Configuration (`appsettings.json`)

```json
{
  "ServerSettings": {
    "ApiPort": 50051,
    "DiscoveryPort": 50050,
    "BroadcastIntervalSeconds": 5,
    "ClientTimeoutSeconds": 30
  },
  "Serilog": {
    "MinimumLevel": "Information"
  }
}
```

Location: `%ProgramData%\UsbOverIp\Server\appsettings.json`

### Client Configuration (`appsettings.json`)

```json
{
  "ClientSettings": {
    "DiscoveryPort": 50050,
    "MinimizeToTray": true,
    "StartWithWindows": false
  },
  "Serilog": {
    "MinimumLevel": "Information"
  }
}
```

Location: `%AppData%\UsbOverIp\Client\appsettings.json`

---

## Building from Source

### Requirements
- .NET 8.0 SDK
- Visual Studio 2022 or VS Code with C# extension

### Build Steps

```bash
# Clone repository
git clone <repository-url>
cd usb-over-ip

# Restore dependencies
dotnet restore

# Build all projects
dotnet build

# Run tests
dotnet test

# Publish server (self-contained)
dotnet publish src/UsbOverIp.BackendService -c Release -r win-x64 --self-contained

# Publish client (self-contained)
dotnet publish src/UsbOverIp.ClientAgent -c Release -r win-x64 --self-contained
```

---

## Troubleshooting

### Server Won't Start
1. **Check usbipd-win**: Run `usbipd --version` to verify installation
2. **Check ports**: Ensure 50051 and 50050 are not in use
3. **Check logs**: `%ProgramData%\UsbOverIp\Server\logs\`

### Client Can't Discover Servers
1. **Check firewall**: UDP 50050 must be allowed (Private profile)
2. **Check network**: Both machines on same subnet
3. **Manual add**: Try adding server IP:Port manually in settings

### Device Won't Attach
1. **Check device state**: Device must be "Shared" on server
2. **Check exclusive access**: Device may be attached to another client
3. **Check usbipd**: Run `usbipd list` on server to verify state

### USB Device Not Working After Attach
1. **Driver installation**: Windows may need to install drivers
2. **Device compatibility**: Some devices may not work over USB/IP
3. **Network latency**: High latency affects real-time devices

---

## Uninstallation

### Server
```powershell
# Stop and remove service
sc stop UsbBackendService
.\UsbOverIp.BackendService.exe --uninstall

# Remove configuration (optional)
Remove-Item -Recurse -Force "$env:ProgramData\UsbOverIp\Server"
```

### Client
```powershell
# Remove from startup (if enabled)
# Delete application files
# Remove configuration (optional)
Remove-Item -Recurse -Force "$env:AppData\UsbOverIp\Client"
```
