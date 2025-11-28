
# USB-over-IP Project (Windows ↔ Windows, C#-Only)
**Goal:** Build a VirtualHere-like USB-over-IP solution for Windows using C# for everything, leveraging `usbipd-win` as the low-level USB/IP engine.

---

## 1. High-Level Architecture

### 1.1 Components

1. **USB Host Machine (“Server”)**
   - Runs:
     - **UsbBackendService** (Windows Service)
     - **UsbServerApi** (HTTP/gRPC API hosted by the service)
   - Responsibilities:
     - Interact with `usbipd-win` to:
       - Enumerate USB devices
       - Bind/export USB devices
       - Stop exporting USB devices
     - Maintain internal state (which device is shared, which client owns it)
     - Enforce access-control rules
     - Broadcast presence (discovery) on the LAN

2. **Client Machine (“Client”)**
   - Runs:
     - **UsbClientAgent** (Windows tray app + optional background service)
   - Responsibilities:
     - Discover servers (LAN broadcast or mDNS)
     - Query available devices from servers
     - Attach/detach devices via the server’s API
     - Provide UI for user to:
       - See device tree
       - Request access
       - Set auto-connect rules

3. **usbipd-win**
   - External dependency installed on any machine acting as a USB host or client.
   - Provides:
     - USB/IP daemon and drivers
     - CLI (`usbipd.exe`) and gRPC API for device operations
   - Your code will **call** this, not replace it.

---

## 2. Process & Data Flow

### 2.1 Device Sharing Flow

1. **Server startup**
   - `UsbBackendService` starts (Windows Service).
   - Checks for `usbipd-win` availability (version, status).
   - Starts internal HTTP/gRPC API (`UsbServerApi`) on a configurable port (e.g., 50051).
   - Starts discovery broadcaster (UDP broadcast or mDNS).

2. **Client startup**
   - `UsbClientAgent` (tray app) starts at user login.
   - Starts discovery listener to detect available servers.
   - When servers are found, queries `/servers/{id}/devices` to list devices.

3. **User selects a device**
   - In the client UI, the user selects:
     - Server (e.g., `DESKTOP-USBHOST`)
     - Device (e.g., `Logitech USB Receiver`)
   - Client calls `POST /servers/{id}/devices/{deviceId}/attach` on the server’s API.

4. **Server attaches device to client**
   - `UsbBackendService` receives the request.
   - Validates ACLs: can this client IP/identity attach this device?
   - Uses `usbipd-win` gRPC/CLI to **export** and **attach** the device to the requesting client IP.
   - Updates internal state: device is now in “attached” state for Client X.

5. **Client system**
   - Windows loads the normal driver for the now-attached USB/IP device.
   - From the client’s perspective, the device is “locally plugged in.”

6. **Detach / release**
   - User clicks “Detach” in the tray UI OR the client disconnects.
   - Client calls `POST /servers/{id}/devices/{deviceId}/detach`.
   - Server calls `usbipd-win` to detach and/or unbind the device.
   - Internal state updates: device becomes free or available for another client.

### 2.2 Discovery Flow (LAN Broadcast)

**Server side:**
- Periodically (e.g., every 5 seconds), send a small UDP broadcast packet:
  ```json
  {
    "type": "UsbServerAnnouncement",
    "name": "DESKTOP-USBHOST",
    "id": "server-guid-here",
    "apiPort": 50051,
    "version": "0.1.0"
  }
  ```

**Client side:**
- Listen on the same UDP port for announcements.
- Maintain a dictionary of known servers and last-seen timestamps.
- If a server is not seen for N seconds, mark as offline.

(You can later add alternative discovery: static IPs, mDNS, etc.)

---

## 3. Network & API Design

### 3.1 Internal API between Client and Server

Use **HTTP + JSON** or **gRPC**. For C#, gRPC is very convenient, but HTTP+JSON is easier to inspect.

#### Option A – HTTP+JSON (REST-ish)

Base URL example: `http://{server-ip}:50051/api`

**Endpoints:**

1. `GET /api/health`
   - Returns basic status:
     ```json
     { "status": "ok", "version": "0.1.0", "hostname": "DESKTOP-USBHOST" }
     ```

2. `GET /api/devices`
   - Returns list of USB devices visible/managed by `usbipd-win`:
     ```json
     [
       {
         "id": "1-1",
         "description": "Logitech USB Receiver",
         "busId": "1-1",
         "vid": "046D",
         "pid": "C534",
         "class": "HID",
         "shared": true,
         "attachedTo": "192.168.1.25"
       }
     ]
     ```

3. `POST /api/devices/{id}/share`
   - Body:
     ```json
     {
       "mode": "exclusive",   // future: "multi"
       "allowedClients": ["192.168.1.25"]
     }
     ```
   - Server response: success/failure, device state.

4. `POST /api/devices/{id}/unshare`
   - Stop exporting the device (call into usbipd).

5. `POST /api/devices/{id}/attach`
   - Body:
     ```json
     {
       "clientAddress": "192.168.1.25"
     }
     ```
   - Server:
     - Validates clientAddress against ACLs.
     - Uses `usbipd-win` to attach the device to that client.
   - Response: updated device state.

6. `POST /api/devices/{id}/detach`
   - Detach the device from current client.

7. `GET /api/config`
   - Get server-wide settings (ports, ACL mode, etc.).

8. `POST /api/config`
   - Update settings (admin only).


#### Option B – gRPC

Define a `.proto` file with services like:

```proto
service UsbServer {
  rpc GetHealth(HealthRequest) returns (HealthResponse);
  rpc ListDevices(ListDevicesRequest) returns (ListDevicesResponse);
  rpc ShareDevice(ShareDeviceRequest) returns (DeviceResponse);
  rpc UnshareDevice(UnshareDeviceRequest) returns (DeviceResponse);
  rpc AttachDevice(AttachDeviceRequest) returns (DeviceResponse);
  rpc DetachDevice(DetachDeviceRequest) returns (DeviceResponse);
}
```

Then generate C# client/server stubs and implement the logic inside `UsbBackendService`.


### 3.2 Protocol Between Your Backend and usbipd-win

At first, use **the existing interfaces**:

1. **CLI invocation** (simple, but less elegant):
   - `usbipd.exe list`
   - `usbipd.exe bind --busid 1-1`
   - `usbipd.exe unbind --busid 1-1`
   - `usbipd.exe wsl attach --busid 1-1 --ip 192.168.1.25`
   - etc.

2. **gRPC API** (preferred long-term):
   - Use the `usbipd-win` gRPC definitions (if exposed) to call device operations directly.

You can wrap these calls in a `UsbipdClient` class with methods like:
- `Task<List<UsbDevice>> ListDevicesAsync()`
- `Task BindDeviceAsync(string busId)`
- `Task AttachDeviceAsync(string busId, string clientIp)`
- `Task DetachDeviceAsync(string busId)`


---

## 4. Repository Structure

A suggested monorepo layout:

```text
usb-over-ip/
├─ src/
│  ├─ UsbBackendService/           # Windows Service on host machine
│  │   ├─ Program.cs
│  │   ├─ ServiceHost.cs
│  │   ├─ UsbipdClient/            # wrapper around usbipd CLI or gRPC
│  │   ├─ DeviceManager/           # internal state & logic
│  │   ├─ Discovery/               # UDP broadcaster
│  │   └─ Api/                     # Minimal REST/gRPC server
│  │
│  ├─ UsbClientAgent/              # Tray app on client machine
│  │   ├─ Program.cs
│  │   ├─ UI/                      # WPF or WinForms
│  │   ├─ Discovery/               # UDP listener
│  │   └─ ServerClient/            # HTTP/gRPC client to UsbBackendService
│  │
│  ├─ Shared/                      # Shared models & utilities
│  │   ├─ Models/
│  │   ├─ Contracts/               # DTOs, gRPC or JSON contracts
│  │   └─ Utilities/
│  │
│  └─ Tools/
│      └─ UsbipdCliWrapper/        # Optional separate project for CLI integration
│
├─ docs/
│  ├─ architecture.md
│  ├─ api-design.md
│  └─ discovery-protocol.md
│
├─ tests/
│  ├─ UsbBackendService.Tests/
│  └─ UsbClientAgent.Tests/
│
├─ build/
│  ├─ ci-scripts/
│  └─ installers/                  # WiX / MSIX configurations
│
├─ .editorconfig
├─ .gitignore
├─ README.md
└─ LICENSE
```

---

## 5. C# Project Details

### 5.1 UsbBackendService (Windows Service)

**Type:** .NET Worker Service or Windows Service

**Key responsibilities:**
- Host a small web server (Kestrel) for the API.
- Start the UDP broadcast task for discovery.
- Periodically poll `usbipd-win` to sync device list.
- Maintain an in-memory (or small DB) state of devices, clients, and current ownership.

**Important classes:**
- `DeviceManager`
- `UsbipdClient` (CLI or gRPC wrapper)
- `DiscoveryBroadcaster`
- `ApiHost` (for REST/gRPC)

### 5.2 UsbClientAgent (Tray App)

**Type:** WPF or WinForms app

**Key responsibilities:**
- Start on user login (shortcut in Startup or installer config).
- Listen for server announcements (UDP listener).
- Maintain list of discovered servers & their device lists.
- Provide **device tree UI**:
  - Servers → Devices
  - Right-click “Attach/Detach”
  - Show statuses and errors.

**Important classes:**
- `DiscoveryListener`
- `ServerClient` (HTTP/gRPC client)
- `MainWindow` (WPF UI)
- `DeviceTreeViewModel` (MVVM) or similar

---

## 6. Internal Models & Contracts

Shared models (in `Shared/Models`):

```csharp
public class UsbDeviceInfo
{
    public string Id { get; set; }           // e.g. busid "1-1"
    public string Description { get; set; }  // e.g. "Logitech USB Receiver"
    public string BusId { get; set; }
    public string VendorId { get; set; }
    public string ProductId { get; set; }
    public string DeviceClass { get; set; }

    public bool IsShared { get; set; }
    public bool IsAttached { get; set; }
    public string AttachedTo { get; set; }   // client IP or null
}
```

```csharp
public class ServerAnnouncement
{
    public string Type { get; set; } = "UsbServerAnnouncement";
    public string Name { get; set; }
    public string Id { get; set; }
    public string Version { get; set; }
    public int ApiPort { get; set; }
}
```

These DTOs can be reused by both backend and client UI.

---

## 7. Security Considerations

### 7.1 Minimal v1

- LAN-only usage, no authentication.
- Use a configurable port and optional IP allowlist.
- Optionally require a shared secret token in HTTP headers:
  ```http
  X-UsbServer-Token: <secret>
  ```

### 7.2 Later Enhancements

- Mutual TLS (server certificate, client certificates).
- User auth via Windows users/groups or custom user database.
- Per-device ACLs stored in a small local database (e.g., LiteDB or SQLite).

---

## 8. Milestone Plan

### Milestone 1 – Bare Minimum (Local only)
- [ ] Install `usbipd-win` manually on a dev machine.
- [ ] Create `UsbipdClient` class that can:
  - [ ] List devices (parse `usbipd.exe list`).
  - [ ] Bind/unbind a device.
- [ ] Create a console version of `UsbBackendService` that prints device list.
- [ ] Create a minimal console `UsbClientAgent` that calls the backend over localhost HTTP to attach/detach.

### Milestone 2 – Real LAN Operation
- [ ] Implement discovery (broadcast + listener).
- [ ] Implement REST API in `UsbBackendService`:
  - [ ] `/api/health`
  - [ ] `/api/devices`
  - [ ] `/api/devices/{id}/attach`
  - [ ] `/api/devices/{id}/detach`
- [ ] Implement real attach/detach using `usbipd-win` and connect from another Windows machine.

### Milestone 3 – GUI + Tray
- [ ] Build WPF UI with:
  - [ ] Server list
  - [ ] Device tree
  - [ ] Attach/Detach buttons
  - [ ] Status indicators
- [ ] Add system tray integration (notify icon, context menu).

### Milestone 4 – Polishing & Security
- [ ] Add persistent server list & device preferences.
- [ ] Add simple auth (shared secret token).
- [ ] Add basic logging + error reporting.
- [ ] Package installers for server & client.

---

## 9. Example Code Sketches

### 9.1 UDP Discovery Announcement (Server)

```csharp
public class DiscoveryBroadcaster
{
    private readonly UdpClient _udpClient;
    private readonly IPEndPoint _broadcastEndpoint;
    private readonly CancellationToken _token;

    public DiscoveryBroadcaster(int port, CancellationToken token)
    {
        _udpClient = new UdpClient
        {
            EnableBroadcast = true
        };
        _broadcastEndpoint = new IPEndPoint(IPAddress.Broadcast, port);
        _token = token;
    }

    public async Task RunAsync(ServerAnnouncement announcement)
    {
        var json = JsonSerializer.Serialize(announcement);
        var bytes = Encoding.UTF8.GetBytes(json);

        while (!_token.IsCancellationRequested)
        {
            await _udpClient.SendAsync(bytes, bytes.Length, _broadcastEndpoint);
            await Task.Delay(TimeSpan.FromSeconds(5), _token);
        }
    }
}
```

### 9.2 UDP Discovery Listener (Client)

```csharp
public class DiscoveryListener
{
    private readonly UdpClient _udpClient;
    private readonly int _port;
    private readonly CancellationToken _token;

    public event Action<ServerAnnouncement, IPEndPoint> ServerDiscovered;

    public DiscoveryListener(int port, CancellationToken token)
    {
        _port = port;
        _udpClient = new UdpClient(_port);
        _token = token;
    }

    public async Task RunAsync()
    {
        while (!_token.IsCancellationRequested)
        {
            var result = await _udpClient.ReceiveAsync();
            var json = Encoding.UTF8.GetString(result.Buffer);
            var announcement = JsonSerializer.Deserialize<ServerAnnouncement>(json);

            if (announcement?.Type == "UsbServerAnnouncement")
            {
                ServerDiscovered?.Invoke(announcement, result.RemoteEndPoint);
            }
        }
    }
}
```

---

## 10. Next Steps

1. Initialize the repo with the structure above.
2. Set up the `UsbBackendService` as a .NET Worker Service running Kestrel for the API.
3. Implement `UsbipdClient` using CLI first, then consider gRPC integration later.
4. Build out the UDP discovery and confirm server/client can see each other.
5. Add minimal UI to attach/detach a single device, then iterate.

This outline should give you a clear roadmap to build a full C#-based USB-over-IP solution on top of `usbipd-win`, with plenty of room to refine, optimize, and eventually swap out the low-level engine if you choose to in the future.
