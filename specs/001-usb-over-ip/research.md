# Research: USB-over-IP Solution for Windows

**Feature Branch**: `001-usb-over-ip`
**Date**: 2025-11-27

## 1. usbipd-win Integration

### Decision
Use usbipd-win CLI via `System.Diagnostics.Process` invocation with JSON state parsing.

### Rationale
- usbipd-win does not expose a public gRPC API; CLI is the stable interface
- `usbipd state` command provides machine-parseable JSON output (v2.2.0+)
- CLI commands are well-documented and straightforward to wrap
- Open-source C# project provides confidence in stability

### Key Commands
| Command | Purpose | Syntax |
|---------|---------|--------|
| `state` | JSON output of all device state | `usbipd state` |
| `list` | Human-readable device list | `usbipd list` |
| `bind` | Export/share a device | `usbipd bind --busid=<BUSID>` |
| `unbind` | Stop sharing a device | `usbipd unbind --busid=<BUSID>` |
| `attach` | Attach to client | `usbipd attach --busid=<BUSID> --ip <IP>` |
| `detach` | Detach from client | `usbipd detach --busid=<BUSID>` |

### Implementation Pattern
```csharp
public class UsbipdClient
{
    public async Task<List<UsbDevice>> ListDevicesAsync()
        => await ExecuteAndParseJson("state");

    public async Task BindDeviceAsync(string busId)
        => await ExecuteCommand($"bind --busid={busId}");

    public async Task AttachDeviceAsync(string busId, string clientIp)
        => await ExecuteCommand($"attach --busid={busId} --ip {clientIp}");

    public async Task DetachDeviceAsync(string busId)
        => await ExecuteCommand($"detach --busid={busId}");
}
```

### Alternatives Considered
- **PowerShell Module**: Available but adds complexity via `System.Management.Automation`
- **Direct gRPC**: Not publicly exposed; internal API only
- **Table output parsing**: Less reliable than JSON; use as fallback only

### Prerequisites
- Windows 10 Build 19041+ or Windows Server 2019 v1809+
- usbipd-win installed via MSI or winget
- TCP port 3240 open for USB/IP protocol

---

## 2. UDP Broadcast Discovery

### Decision
Implement dual-socket UDP broadcast pattern with multi-interface support; use mDNS (net-mdns) as fallback.

### Rationale
- UDP broadcast is simple and effective for LAN discovery
- Multi-interface enumeration ensures all network adapters are covered
- mDNS provides industry-standard fallback with reserved port 5353
- No external service dependencies required

### Port Selection
**Primary**: Port 50050 (dynamic range, low conflict probability)
**Fallback**: mDNS on port 5353 (standard, pre-configured in most firewalls)

### Implementation Pattern
```csharp
// Enumerate all interfaces for comprehensive broadcast
foreach(NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
{
    if(ni.OperationalStatus == OperationalStatus.Up &&
       ni.SupportsMulticast &&
       ni.GetIPProperties().GetIPv4Properties() != null)
    {
        foreach(var uip in ni.GetIPProperties().UnicastAddresses)
        {
            if(uip.Address.AddressFamily == AddressFamily.InterNetwork)
            {
                var local = new IPEndPoint(uip.Address, 0);
                var udpc = new UdpClient(local) { EnableBroadcast = true };
                udpc.Send(data, data.Length,
                    new IPEndPoint(GetSubnetBroadcast(uip.Address, uip.IPv4Mask), 50050));
            }
        }
    }
}
```

### Firewall Configuration
- Windows Defender Firewall rule required for UDP port 50050
- Private profile only (not Public for security)
- Network Discovery feature should be enabled

### Alternatives Considered
- **mDNS only**: Better for cross-platform but more complex
- **SSDP**: Standard but more overhead
- **Manual IP entry**: Always available as user fallback

---

## 3. Windows Service Architecture

### Decision
Use .NET Worker Service with `BackgroundService` base class and `Microsoft.Extensions.Hosting.WindowsServices`.

### Rationale
- Modern .NET pattern with built-in DI, configuration, and logging
- Cross-platform compatibility (can run as Linux systemd if needed)
- Single-file deployment possible
- Built-in graceful shutdown with configurable timeout

### Service-to-UI Communication
**Decision**: Named Pipes (`NamedPipeServerStream`)

**Rationale**:
- No TCP port conflicts
- Efficient for local IPC
- Windows security integration (ACLs)
- No network stack overhead

### Installation Pattern
Self-installing via `sc.exe`:
```csharp
if (args.Contains("--install"))
{
    var exePath = Process.GetCurrentProcess().MainModule.FileName;
    Process.Start("sc", $"create UsbBackendService binPath= \"{exePath}\" start= auto");
}
```

### Alternatives Considered
- **TopShelf**: Third-party, less community support now
- **Traditional ServiceBase**: No DI, deprecated patterns
- **TCP for IPC**: Port conflicts, more overhead

---

## 4. WPF Client Tray Application

### Decision
Use Hardcodet.NotifyIcon.Wpf for WPF-native system tray integration with MVVM support.

### Rationale
- Pure WPF control without Windows Forms interop
- Full MVVM support with data binding and commands
- Rich feature set: custom popups, tooltips, context menus
- Actively maintained with multiple package options

### Key Patterns

**Single Instance Enforcement** (Mutex):
```csharp
_instanceMutex = new Mutex(true, "Local\\UsbClientAgent_SingleInstance", out bool createdNew);
if (!createdNew) { /* Already running */ }
```

**Minimize to Tray**:
```csharp
private void MainWindow_StateChanged(object sender, EventArgs e)
{
    if (WindowState == WindowState.Minimized)
    {
        this.Hide();
        this.ShowInTaskbar = false;
    }
}
```

**Windows Startup** (Registry):
```csharp
using (var key = Registry.CurrentUser.OpenSubKey(
    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", writable: true))
{
    key.SetValue("UsbClientAgent", appPath);
}
```

### Alternatives Considered
- **Windows Forms NotifyIcon**: Framework mismatch, context menu issues
- **H.NotifyIcon**: Newer but smaller community

---

## 5. Logging Strategy

### Decision
Serilog with async file sinks and JSON formatting.

### Rationale
- Structured logging with typed properties
- Multiple sinks: file (human-readable), JSON (machine-parseable)
- Async writing prevents blocking
- Industry standard for modern .NET

### Configuration
```csharp
.UseSerilog((context, services, configuration) => configuration
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .WriteTo.File(
        path: "logs/service-.txt",
        rollingInterval: RollingInterval.Day,
        buffered: true)
    .WriteTo.File(
        path: "logs/service-.json",
        rollingInterval: RollingInterval.Day,
        formatter: new JsonFormatter()))
```

### Alternatives Considered
- **NLog**: High performance but less structured logging support
- **log4net**: Outdated API, verbose XML configuration
- **Built-in logging**: Limited sinks, not production-ready

---

## 6. Configuration Management

### Decision
Layered configuration with appsettings.json + environment variables.

### Rationale
- Hierarchy: command-line > env vars > appsettings.Environment > appsettings.json
- Environment-specific configs without code changes
- Options pattern for strongly-typed settings
- No registry pollution

### Critical Note
For Windows Services, set working directory before loading config:
```csharp
Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
```

---

## Technology Stack Summary

| Component | Technology | Package/Version |
|-----------|------------|-----------------|
| Language | C# 12 | .NET 8.0 |
| Server Framework | Worker Service | Microsoft.Extensions.Hosting.WindowsServices |
| Client UI | WPF | .NET 8.0-windows |
| System Tray | Hardcodet.NotifyIcon.Wpf | Latest |
| HTTP API | ASP.NET Core Minimal API | Microsoft.AspNetCore.App |
| Logging | Serilog | Serilog.AspNetCore |
| Configuration | Microsoft.Extensions.Configuration | Built-in |
| JSON | System.Text.Json | Built-in |
| USB/IP Backend | usbipd-win | External dependency |

---

## Network Architecture

| Protocol | Port | Direction | Purpose |
|----------|------|-----------|---------|
| UDP Broadcast | 50050 | Server → LAN | Discovery announcements |
| HTTP REST | 50051 | Client → Server | API for device operations |
| USB/IP | 3240 | Bidirectional | USB data transfer (usbipd-win) |
| Named Pipes | N/A | Local | Service-to-management UI |
