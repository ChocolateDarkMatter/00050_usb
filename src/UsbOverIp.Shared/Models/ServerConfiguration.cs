namespace UsbOverIp.Shared.Models;

/// <summary>
/// Configuration for a device to auto-share on startup.
/// </summary>
public class DeviceShareConfig
{
    /// <summary>
    /// Specific bus ID to share (optional)
    /// </summary>
    public string? BusId { get; set; }

    /// <summary>
    /// VID:PID pattern (e.g., "046D:*") (optional)
    /// </summary>
    public string? VendorProductFilter { get; set; }

    /// <summary>
    /// Automatically share matching devices
    /// </summary>
    public bool AutoShare { get; set; }
}

/// <summary>
/// Persistent settings for the server service.
/// </summary>
public class ServerConfiguration
{
    /// <summary>
    /// Unique server identifier (generated once)
    /// </summary>
    public Guid ServerId { get; set; }

    /// <summary>
    /// Server display name (default: hostname)
    /// </summary>
    public string ServerName { get; set; } = Environment.MachineName;

    /// <summary>
    /// REST API listening port (default: 50051)
    /// </summary>
    public int ApiPort { get; set; } = 50051;

    /// <summary>
    /// UDP broadcast port (default: 50050)
    /// </summary>
    public int DiscoveryPort { get; set; } = 50050;

    /// <summary>
    /// Discovery announcement interval in seconds (default: 5, range 1-60)
    /// </summary>
    public int BroadcastIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// Timeout for crashed client detection in seconds (default: 30)
    /// </summary>
    public int ClientTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Devices to auto-share on startup
    /// </summary>
    public List<DeviceShareConfig> AutoShareDevices { get; set; } = new();

    /// <summary>
    /// Logging verbosity level
    /// </summary>
    public string LogLevel { get; set; } = "Information";
}
