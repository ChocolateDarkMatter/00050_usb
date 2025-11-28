# USB-over-IP Deployment Guide

This guide provides detailed instructions for deploying the USB-over-IP solution in various scenarios.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Building the Project](#building-the-project)
3. [Backend Server Deployment](#backend-server-deployment)
4. [Client Agent Deployment](#client-agent-deployment)
5. [Network Configuration](#network-configuration)
6. [Security Considerations](#security-considerations)
7. [Production Deployment](#production-deployment)
8. [Troubleshooting](#troubleshooting)

## Prerequisites

### All Systems

1. **Install .NET 8.0 Runtime**

   Download from: https://dotnet.microsoft.com/download/dotnet/8.0

   Or using winget:
   ```powershell
   winget install Microsoft.DotNet.Runtime.8
   ```

2. **Install usbipd-win**

   ```powershell
   winget install --id dorssel.usbipd-win
   ```

   Verify installation:
   ```powershell
   usbipd --version
   ```

### Backend Server Requirements

- Windows 10/11 or Windows Server 2016+
- Administrator privileges (for Windows Service)
- At least 100 MB free disk space
- Network connectivity with open ports 50051-50052

### Client Requirements

- Windows 10/11
- At least 50 MB free disk space
- Network connectivity to server

## Building the Project

### 1. Clone the Repository

```powershell
git clone <repository-url>
cd 00050_usbhere
```

### 2. Build for Release

```powershell
dotnet build -c Release src\UsbOverIp.sln
```

The built binaries will be located in:
- Backend: `src\UsbOverIp.BackendService\bin\Release\net8.0\`
- Client: `src\UsbOverIp.ClientAgent\bin\Release\net8.0-windows\`

### 3. Run Tests (Optional)

```powershell
dotnet test src\UsbOverIp.sln
```

Expected results:
- Total: 49 tests
- Passed: 45
- Skipped: 4 (requires usbipd-win to be running)

## Backend Server Deployment

### Option 1: Windows Service (Recommended for Production)

#### 1.1 Copy Files

Copy the entire Release folder to the target server:
```powershell
# From build machine
xcopy /E /I src\UsbOverIp.BackendService\bin\Release\net8.0 \\server\c$\UsbOverIp\Backend

# Or use robocopy for larger deployments
robocopy src\UsbOverIp.BackendService\bin\Release\net8.0 \\server\c$\UsbOverIp\Backend /E /MT:8
```

#### 1.2 Configure the Service

Edit `appsettings.json` on the server:

```json
{
  "ServerConfiguration": {
    "ServerId": "00000000-0000-0000-0000-000000000000",
    "ServerName": "Production-USB-Server-01",
    "ApiPort": 50051,
    "DiscoveryPort": 50052,
    "BroadcastIntervalSeconds": 5
  }
}
```

Important settings:
- **ServerId**: Leave as zeros for auto-generation or set a unique GUID
- **ServerName**: A friendly name shown to clients
- **ApiPort**: REST API port (default: 50051)
- **DiscoveryPort**: UDP broadcast port (default: 50052)

#### 1.3 Install the Service

Run PowerShell as Administrator:

```powershell
cd C:\UsbOverIp\Backend\scripts
.\install-service.ps1
```

With custom name:
```powershell
.\install-service.ps1 -ServiceName "UsbServer01" -DisplayName "USB Server 01" -Description "USB sharing for Building A"
```

#### 1.4 Start the Service

```powershell
.\start-service.ps1
```

Or using Windows Services:
```powershell
Start-Service -Name UsbOverIpBackendService
```

#### 1.5 Verify Installation

```powershell
# Check service status
Get-Service -Name UsbOverIpBackendService

# Check logs
Get-Content C:\UsbOverIp\Backend\logs\service-*.txt -Tail 50

# Test API
Invoke-RestMethod -Uri "http://localhost:50051/api/health"
```

### Option 2: Console Application (Development/Testing)

For development or testing purposes:

```powershell
cd src\UsbOverIp.BackendService\bin\Release\net8.0
.\UsbOverIp.BackendService.exe
```

The service will run in console mode with live log output.

## Client Agent Deployment

### Option 1: Manual Installation

#### 1.1 Copy Files

```powershell
xcopy /E /I src\UsbOverIp.ClientAgent\bin\Release\net8.0-windows \\client\c$\UsbOverIp\Client
```

#### 1.2 Create Desktop Shortcut

```powershell
$WshShell = New-Object -comObject WScript.Shell
$Shortcut = $WshShell.CreateShortcut("$env:USERPROFILE\Desktop\USB Client.lnk")
$Shortcut.TargetPath = "C:\UsbOverIp\Client\UsbOverIp.ClientAgent.exe"
$Shortcut.WorkingDirectory = "C:\UsbOverIp\Client"
$Shortcut.Save()
```

#### 1.3 Run the Client

```powershell
cd C:\UsbOverIp\Client
.\UsbOverIp.ClientAgent.exe
```

The client will:
- Start minimized to system tray
- Automatically discover servers on the network
- Display available USB devices

### Option 2: Auto-Start with Windows

To configure auto-start:

1. Run the client application
2. Right-click the system tray icon
3. Check "Start with Windows"

Or manually add to startup:

```powershell
$StartupFolder = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Startup"
$WshShell = New-Object -comObject WScript.Shell
$Shortcut = $WshShell.CreateShortcut("$StartupFolder\USB Client.lnk")
$Shortcut.TargetPath = "C:\UsbOverIp\Client\UsbOverIp.ClientAgent.exe"
$Shortcut.WorkingDirectory = "C:\UsbOverIp\Client"
$Shortcut.Save()
```

### Option 3: Group Policy Deployment (Enterprise)

For enterprise deployments using GPO:

1. Copy binaries to network share: `\\fileserver\software\UsbOverIp\`
2. Create GPO: Computer Configuration > Policies > Windows Settings > Scripts > Startup
3. Add script to install to local machine:

```powershell
# install-client.ps1
$Source = "\\fileserver\software\UsbOverIp\Client"
$Dest = "C:\Program Files\UsbOverIp\Client"

if (-not (Test-Path $Dest)) {
    New-Item -Path $Dest -ItemType Directory -Force
}

robocopy $Source $Dest /E /MT:8 /R:3 /W:5

# Add to all users startup
$StartupFolder = "C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Startup"
$WshShell = New-Object -comObject WScript.Shell
$Shortcut = $WshShell.CreateShortcut("$StartupFolder\USB Client.lnk")
$Shortcut.TargetPath = "$Dest\UsbOverIp.ClientAgent.exe"
$Shortcut.WorkingDirectory = $Dest
$Shortcut.Save()
```

## Network Configuration

### Firewall Rules

#### Backend Server

Open required ports on the backend server:

```powershell
# Run as Administrator

# API Port (TCP)
New-NetFirewallRule `
    -DisplayName "USB-over-IP API" `
    -Direction Inbound `
    -Protocol TCP `
    -LocalPort 50051 `
    -Action Allow `
    -Profile Domain,Private

# Discovery Port (UDP)
New-NetFirewallRule `
    -DisplayName "USB-over-IP Discovery" `
    -Direction Inbound `
    -Protocol UDP `
    -LocalPort 50052 `
    -Action Allow `
    -Profile Domain,Private

# USB/IP Protocol (handled by usbipd-win)
New-NetFirewallRule `
    -DisplayName "USB/IP Protocol" `
    -Direction Inbound `
    -Protocol TCP `
    -LocalPort 3240 `
    -Action Allow `
    -Profile Domain,Private
```

For public networks (use with caution):
```powershell
# Add -Profile Public to each rule above
```

#### Client Machines

Usually no firewall changes needed on clients. If experiencing issues:

```powershell
# Allow outbound UDP for discovery
New-NetFirewallRule `
    -DisplayName "USB-over-IP Client Discovery" `
    -Direction Outbound `
    -Protocol UDP `
    -RemotePort 50052 `
    -Action Allow
```

### Network Topology

#### Same Subnet (Recommended)
- Server: 192.168.1.10
- Clients: 192.168.1.x
- Discovery works automatically via UDP broadcast

#### Multiple Subnets
- Configure router to forward UDP broadcasts, or
- Manually configure server IP in client (feature for future version)

#### VPN/Remote Access
- Ensure VPN supports UDP broadcast
- Or use direct IP configuration
- Consider security implications (see below)

## Security Considerations

### Network Security

1. **Firewall Rules**: Only open required ports
2. **Network Segmentation**: Place USB servers on dedicated VLAN
3. **Access Control**: Use Windows Firewall to restrict client IPs

Example - Restrict to specific subnet:
```powershell
New-NetFirewallRule `
    -DisplayName "USB-over-IP API - Restricted" `
    -Direction Inbound `
    -Protocol TCP `
    -LocalPort 50051 `
    -RemoteAddress 192.168.10.0/24 `
    -Action Allow
```

### Service Account

By default, the service runs as Local System. For enhanced security:

1. Create dedicated service account:
```powershell
$Password = ConvertTo-SecureString "SecurePassword123!" -AsPlainText -Force
New-LocalUser -Name "UsbServiceAccount" -Password $Password -PasswordNeverExpires
Add-LocalGroupMember -Group "Administrators" -Member "UsbServiceAccount"
```

2. Configure service to use this account:
   - Open `services.msc`
   - Find "USB-over-IP Backend Service"
   - Properties > Log On tab
   - Select "This account" and enter credentials

### USB Device Security

1. **Device Binding**: Only share necessary devices
2. **Client Authentication**: Monitor logs for unauthorized access
3. **Device Policies**: Use Windows policies to restrict USB device types

### Audit and Monitoring

Enable comprehensive logging:

Edit `appsettings.json`:
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "UsbOverIp": "Debug"
      }
    }
  }
}
```

Monitor logs regularly:
```powershell
# Watch for device attachment events
Get-Content C:\UsbOverIp\Backend\logs\service-*.json | ConvertFrom-Json |
    Where-Object { $_.MessageTemplate -like "*Attach*" } |
    Select-Object Timestamp, Message
```

## Production Deployment

### High Availability

For critical environments, deploy multiple servers:

1. **Server 1**: Primary USB server (192.168.1.10)
2. **Server 2**: Secondary USB server (192.168.1.11)

Each server manages its own USB devices. Clients automatically discover both.

### Load Balancing

For many clients accessing the same devices:
- Not currently supported (devices are exclusive)
- Consider separate servers for different device groups
- Use server naming convention: `USB-Server-Printers`, `USB-Server-Scanners`

### Backup and Recovery

#### Configuration Backup

```powershell
# Backup script
$BackupPath = "\\fileserver\backups\usb-server\$(Get-Date -Format 'yyyy-MM-dd')"
New-Item -Path $BackupPath -ItemType Directory -Force

Copy-Item "C:\UsbOverIp\Backend\appsettings.json" $BackupPath
Copy-Item "C:\UsbOverIp\Backend\logs\*" $BackupPath -Recurse
```

#### Disaster Recovery

1. Reinstall usbipd-win
2. Install .NET 8.0 Runtime
3. Restore binaries and configuration
4. Run install script
5. Verify service startup

### Monitoring and Alerts

#### Health Check Monitoring

```powershell
# health-check.ps1
$Response = Invoke-RestMethod -Uri "http://localhost:50051/api/health"
if ($Response.Status -ne "ok") {
    Send-MailMessage -To "admin@company.com" -Subject "USB Server Health Check Failed" -Body "Server is not healthy"
}
```

Schedule with Task Scheduler to run every 5 minutes.

#### Event Log Integration

The service writes to Windows Event Log. Create custom views:

1. Open Event Viewer
2. Create Custom View
3. Filter by: Event Source = "UsbOverIp.BackendService"
4. Save as "USB Server Events"

### Update Procedure

1. **Announce Maintenance**: Notify users
2. **Backup Configuration**: Run backup script
3. **Stop Service**: `Stop-Service UsbOverIpBackendService`
4. **Deploy New Version**: Copy new binaries
5. **Verify Config**: Ensure appsettings.json is preserved
6. **Start Service**: `Start-Service UsbOverIpBackendService`
7. **Verify**: Check health endpoint and logs

## Troubleshooting

### Backend Service Issues

#### Service Won't Start

```powershell
# Check service status
Get-Service UsbOverIpBackendService

# Check Event Viewer
Get-EventLog -LogName Application -Source "UsbOverIp.BackendService" -Newest 20

# Check service logs
Get-Content C:\UsbOverIp\Backend\logs\service-*.txt -Tail 100

# Verify usbipd-win
usbipd --version

# Test port availability
Test-NetConnection -ComputerName localhost -Port 50051
```

#### Port Already in Use

```powershell
# Find process using port 50051
Get-NetTCPConnection -LocalPort 50051 | Select-Object -Property LocalAddress, LocalPort, RemoteAddress, RemotePort, State, OwningProcess
Get-Process -Id (Get-NetTCPConnection -LocalPort 50051).OwningProcess
```

### Client Issues

#### Cannot Discover Servers

```powershell
# Verify network connectivity
Test-NetConnection -ComputerName <server-ip> -Port 50051

# Test UDP connectivity (requires PowerShell 7+)
Test-Connection -ComputerName <server-ip> -UDPPort 50052

# Check firewall
Get-NetFirewallRule | Where-Object { $_.DisplayName -like "*USB*" }
```

#### Device Attachment Fails

```powershell
# Check if device is shared
usbipd list

# Verify client IP is correct
ipconfig

# Check server logs for errors
# On server:
Get-Content C:\UsbOverIp\Backend\logs\service-*.txt -Tail 50 | Select-String "error"
```

### Network Issues

#### Discovery Works But API Fails

- Firewall blocking TCP 50051
- Router/switch filtering traffic
- Check Windows Defender

```powershell
# Temporarily disable firewall for testing (re-enable after!)
Set-NetFirewallProfile -Profile Domain,Public,Private -Enabled False

# Test API
Invoke-RestMethod -Uri "http://<server-ip>:50051/api/health"

# Re-enable firewall
Set-NetFirewallProfile -Profile Domain,Public,Private -Enabled True
```

### Performance Issues

#### Slow Device Response

1. Check network latency:
```powershell
Test-Connection -ComputerName <server-ip> -Count 100
```

2. Check USB device status on server:
```powershell
usbipd list
```

3. Check for network congestion
4. Consider moving server/client to same subnet

#### High CPU Usage

1. Check for device polling issues
2. Review log levels (reduce from Debug to Information)
3. Increase cache timeout in DeviceManager

## Support and Maintenance

### Log Locations

- Backend Service: `C:\UsbOverIp\Backend\logs\`
- Client Agent: Local app data (future version)
- Windows Event Log: Application > UsbOverIp.BackendService

### Regular Maintenance Tasks

- **Daily**: Review logs for errors
- **Weekly**: Check service status and disk space
- **Monthly**: Review device usage and clean old logs
- **Quarterly**: Update to latest version
- **Annually**: Review security configurations

### Getting Help

1. Check logs first
2. Review troubleshooting section
3. Check GitHub issues
4. Create new issue with:
   - Log excerpts (relevant errors)
   - Configuration (redact sensitive info)
   - Environment details (OS version, network setup)
   - Steps to reproduce

## Appendix: Automation Scripts

### Mass Deployment Script

```powershell
# deploy-backend.ps1
param(
    [Parameter(Mandatory=$true)]
    [string[]]$ServerList
)

foreach ($Server in $ServerList) {
    Write-Host "Deploying to $Server..."

    # Copy files
    robocopy ".\bin\Release\net8.0" "\\$Server\c$\UsbOverIp\Backend" /E /MT:8

    # Install service remotely
    Invoke-Command -ComputerName $Server -ScriptBlock {
        cd C:\UsbOverIp\Backend\scripts
        .\install-service.ps1
        .\start-service.ps1
    }

    Write-Host "Deployment to $Server complete!"
}
```

### Health Check Script

```powershell
# check-all-servers.ps1
$Servers = @("server1", "server2", "server3")

foreach ($Server in $Servers) {
    try {
        $Response = Invoke-RestMethod -Uri "http://${Server}:50051/api/health" -TimeoutSec 5
        Write-Host "$Server : OK (${$Response.DeviceCount} devices)" -ForegroundColor Green
    }
    catch {
        Write-Host "$Server : FAILED - $($_.Exception.Message)" -ForegroundColor Red
    }
}
```
