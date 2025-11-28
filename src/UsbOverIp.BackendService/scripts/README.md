# USB-over-IP Backend Service - Windows Service Installation

This directory contains PowerShell scripts for installing and managing the USB-over-IP Backend Service as a Windows Service.

## Prerequisites

- Windows 10/11 or Windows Server 2016+
- .NET 8.0 Runtime installed
- Administrator privileges
- usbipd-win installed (https://github.com/dorssel/usbipd-win)

## Building the Service

Before installing, build the project in Release mode:

```powershell
cd C:\Users\ezuserwon\dev\python\00050_usbhere
dotnet build -c Release
```

## Installation

Run PowerShell as Administrator and execute:

```powershell
cd src\UsbOverIp.BackendService\scripts
.\install-service.ps1
```

The script will:
1. Check if the service already exists and remove it if necessary
2. Create a new Windows Service named "UsbOverIpBackendService"
3. Configure it to start automatically with Windows
4. Display instructions for starting the service

### Custom Service Name

You can customize the service name during installation:

```powershell
.\install-service.ps1 -ServiceName "MyUsbService" -DisplayName "My USB Service" -Description "Custom description"
```

## Starting the Service

```powershell
.\start-service.ps1
```

Or using Windows Services:

```powershell
Start-Service -Name UsbOverIpBackendService
```

## Stopping the Service

```powershell
.\stop-service.ps1
```

Or using Windows Services:

```powershell
Stop-Service -Name UsbOverIpBackendService
```

## Uninstalling the Service

```powershell
.\uninstall-service.ps1
```

## Checking Service Status

Using PowerShell:

```powershell
Get-Service -Name UsbOverIpBackendService
```

Using Windows Services Manager:

1. Press `Win + R`
2. Type `services.msc`
3. Find "USB-over-IP Backend Service" in the list

## Configuration

The service reads configuration from:
- `appsettings.json` in the service executable directory

Key configuration options:

```json
{
  "ServerConfiguration": {
    "ServerId": "00000000-0000-0000-0000-000000000000",
    "ServerName": "",
    "ApiPort": 50051,
    "DiscoveryPort": 50052,
    "BroadcastIntervalSeconds": 5
  }
}
```

## Logs

Service logs are written to:
- Text logs: `logs/service-YYYYMMDD.txt`
- JSON logs: `logs/service-YYYYMMDD.json`

Logs are retained for 30 days by default.

## Troubleshooting

### Service won't start

1. Check Event Viewer: Windows Logs > Application
2. Check service logs in the `logs` directory
3. Verify usbipd-win is installed: `usbipd --version`
4. Ensure port 50051 is not in use by another application

### Permission Issues

The service runs under the Local System account by default. If you need different permissions:

1. Open Services Manager (`services.msc`)
2. Right-click on "USB-over-IP Backend Service"
3. Select "Properties" > "Log On" tab
4. Configure the desired account

### Firewall Issues

If clients cannot discover the server:

1. Allow UDP port 50052 (Discovery) through Windows Firewall
2. Allow TCP port 50051 (API) through Windows Firewall

```powershell
# Allow Discovery Port
New-NetFirewallRule -DisplayName "USB-over-IP Discovery" -Direction Inbound -Protocol UDP -LocalPort 50052 -Action Allow

# Allow API Port
New-NetFirewallRule -DisplayName "USB-over-IP API" -Direction Inbound -Protocol TCP -LocalPort 50051 -Action Allow
```

## Running in Console Mode (Development)

For development and testing, you can run the service in console mode:

```powershell
cd src\UsbOverIp.BackendService
dotnet run
```

The same executable works both as a console application and as a Windows Service.
