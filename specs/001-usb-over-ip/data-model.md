# Data Model: USB-over-IP Solution

**Feature Branch**: `001-usb-over-ip`
**Date**: 2025-11-27

## Entities

### UsbDevice

Represents a physical USB device connected to a server machine.

| Field | Type | Description | Validation |
|-------|------|-------------|------------|
| `BusId` | string | Unique identifier from usbipd-win (e.g., "1-1", "2-3-1") | Required, format: `\d+-[\d-]+` |
| `VendorId` | string | USB Vendor ID in hex (e.g., "046D") | Required, 4 hex chars |
| `ProductId` | string | USB Product ID in hex (e.g., "C534") | Required, 4 hex chars |
| `Description` | string | Human-readable device name | Required, max 256 chars |
| `DeviceClass` | string | USB device class (e.g., "HID", "Mass Storage") | Optional |
| `IsShared` | bool | Whether device is exported for remote access | Default: false |
| `IsAttached` | bool | Whether device is currently attached to a client | Default: false |
| `AttachedToClientIp` | string? | IP address of attached client, null if not attached | IPv4 format when set |

**State Transitions**:
```
[Not Shared] --share--> [Shared/Available]
[Shared/Available] --attach--> [Shared/Attached]
[Shared/Attached] --detach--> [Shared/Available]
[Shared/*] --unshare--> [Not Shared]
```

**Uniqueness**: `BusId` is unique per server (assigned by USB subsystem)

---

### ServerInfo

Represents a server machine sharing USB devices.

| Field | Type | Description | Validation |
|-------|------|-------------|------------|
| `Id` | Guid | Unique server identifier | Required, generated at first run |
| `Hostname` | string | Machine hostname | Required, max 255 chars |
| `IpAddress` | string | Server's primary IP address | Required, IPv4 format |
| `ApiPort` | int | Port for REST API | Required, range 1-65535 |
| `Version` | string | Server software version | Semantic version format |
| `IsOnline` | bool | Whether server is currently reachable | Runtime state |
| `LastSeen` | DateTime | Last discovery announcement received | UTC timestamp |
| `Devices` | List<UsbDevice> | List of managed USB devices | Populated on query |

**Uniqueness**: `Id` is globally unique (GUID)

---

### ServerAnnouncement

UDP broadcast payload for server discovery.

| Field | Type | Description | Validation |
|-------|------|-------------|------------|
| `Type` | string | Message type identifier | Always "UsbServerAnnouncement" |
| `Id` | Guid | Server's unique identifier | Required |
| `Name` | string | Server's hostname/display name | Required |
| `ApiPort` | int | Port for REST API | Required |
| `Version` | string | Server software version | Required |

**Wire Format**: JSON (UTF-8 encoded)

---

### AttachmentInfo

Represents an active connection between a client and a USB device.

| Field | Type | Description | Validation |
|-------|------|-------------|------------|
| `DeviceBusId` | string | Bus ID of the attached device | Required |
| `ClientIpAddress` | string | IP address of the client | Required, IPv4 format |
| `ServerId` | Guid | ID of the server hosting the device | Required |
| `AttachedAt` | DateTime | When attachment was established | UTC timestamp |
| `State` | AttachmentState | Current connection state | Enum value |

**AttachmentState Enum**:
- `Pending` - Attachment requested, in progress
- `Active` - Device actively attached and usable
- `Disconnecting` - Detachment in progress
- `Failed` - Attachment failed (with error info)

---

### ClientPreferences

Persistent settings for the client application.

| Field | Type | Description | Validation |
|-------|------|-------------|------------|
| `KnownServers` | List<ServerEndpoint> | Manually added server addresses | Optional |
| `AutoConnectRules` | List<AutoConnectRule> | Rules for automatic device attachment | Optional |
| `MinimizeToTray` | bool | Minimize to tray on close | Default: true |
| `StartWithWindows` | bool | Launch at Windows startup | Default: false |
| `LogLevel` | string | Logging verbosity level | Enum: Error, Warning, Info, Debug |

**ServerEndpoint**:
| Field | Type | Description |
|-------|------|-------------|
| `Address` | string | IP address or hostname |
| `Port` | int | API port |

**AutoConnectRule**:
| Field | Type | Description |
|-------|------|-------------|
| `ServerId` | Guid | Target server |
| `DeviceBusId` | string | Target device (or "*" for any) |
| `VendorProductFilter` | string? | VID:PID filter (e.g., "046D:C534") |
| `Enabled` | bool | Whether rule is active |

---

### ServerConfiguration

Persistent settings for the server service.

| Field | Type | Description | Validation |
|-------|------|-------------|------------|
| `ServerId` | Guid | Unique server identifier | Generated once |
| `ApiPort` | int | REST API listening port | Default: 50051 |
| `DiscoveryPort` | int | UDP broadcast port | Default: 50050 |
| `BroadcastIntervalSeconds` | int | Discovery announcement interval | Default: 5, range 1-60 |
| `ClientTimeoutSeconds` | int | Timeout for crashed client detection | Default: 30 |
| `AutoShareDevices` | List<DeviceShareConfig> | Devices to auto-share on startup | Optional |
| `LogLevel` | string | Logging verbosity level | Enum: Error, Warning, Info, Debug |

**DeviceShareConfig**:
| Field | Type | Description |
|-------|------|-------------|
| `BusId` | string? | Specific bus ID to share |
| `VendorProductFilter` | string? | VID:PID pattern (e.g., "046D:*") |
| `AutoShare` | bool | Automatically share matching devices |

---

## API Response Models

### DeviceListResponse

```json
{
  "devices": [
    {
      "busId": "1-1",
      "vendorId": "046D",
      "productId": "C534",
      "description": "Logitech USB Receiver",
      "deviceClass": "HID",
      "isShared": true,
      "isAttached": false,
      "attachedToClientIp": null
    }
  ],
  "serverInfo": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "hostname": "DESKTOP-USBHOST",
    "version": "1.0.0"
  }
}
```

### HealthResponse

```json
{
  "status": "ok",
  "version": "1.0.0",
  "hostname": "DESKTOP-USBHOST",
  "usbipdAvailable": true,
  "usbipdVersion": "4.3.0",
  "deviceCount": 5,
  "sharedDeviceCount": 2,
  "attachedDeviceCount": 1
}
```

### AttachRequest

```json
{
  "clientAddress": "192.168.1.25"
}
```

### AttachResponse

```json
{
  "success": true,
  "device": { /* UsbDevice object */ },
  "message": "Device attached successfully"
}
```

### ErrorResponse

```json
{
  "error": true,
  "code": "DEVICE_IN_USE",
  "message": "Device is currently attached to another client",
  "details": {
    "attachedTo": "192.168.1.30"
  }
}
```

---

## Relationships

```
┌─────────────────────────────────────────────────────────────────┐
│                          ServerInfo                              │
│  (represents a server on the network)                           │
├─────────────────────────────────────────────────────────────────┤
│  - Id (PK)                                                       │
│  - Hostname, IpAddress, ApiPort, Version                        │
│                                                                  │
│  1 ─────────────< UsbDevice                                     │
│                   (devices connected to this server)            │
│                                                                  │
│  1 ─────────────< AttachmentInfo                                │
│                   (active client connections)                    │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                          UsbDevice                               │
│  (a USB device on a server)                                     │
├─────────────────────────────────────────────────────────────────┤
│  - BusId (PK within server)                                     │
│  - VendorId, ProductId, Description, DeviceClass                │
│  - IsShared, IsAttached, AttachedToClientIp                     │
│                                                                  │
│  0..1 ────────── AttachmentInfo                                 │
│                  (if attached, tracks connection details)       │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                      ClientPreferences                           │
│  (stored locally on client machine)                             │
├─────────────────────────────────────────────────────────────────┤
│  - KnownServers: List<ServerEndpoint>                           │
│  - AutoConnectRules: List<AutoConnectRule>                      │
│  - UI preferences (tray, startup, logging)                      │
└─────────────────────────────────────────────────────────────────┘
```

---

## Storage Strategy

### Server Side
- **Runtime State**: In-memory dictionary of UsbDevice keyed by BusId
- **Persistent Config**: `appsettings.json` for ServerConfiguration
- **Device Sharing Config**: `device-config.json` for auto-share rules

### Client Side
- **Runtime State**: In-memory collections of discovered servers and attachments
- **Persistent Config**: `appsettings.json` for ClientPreferences
- **Known Servers**: Stored in preferences file, survives restarts

### File Locations
- Server: `%ProgramData%\UsbOverIp\Server\`
- Client: `%AppData%\UsbOverIp\Client\`
