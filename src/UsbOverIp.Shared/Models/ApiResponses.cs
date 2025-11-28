namespace UsbOverIp.Shared.Models;

/// <summary>
/// Health check response from the server API.
/// </summary>
public class HealthResponse
{
    /// <summary>
    /// Server status (e.g., "ok")
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// Server software version
    /// </summary>
    public required string Version { get; set; }

    /// <summary>
    /// Server hostname
    /// </summary>
    public required string Hostname { get; set; }

    /// <summary>
    /// Whether usbipd-win is available
    /// </summary>
    public bool UsbipdAvailable { get; set; }

    /// <summary>
    /// usbipd-win version
    /// </summary>
    public string? UsbipdVersion { get; set; }

    /// <summary>
    /// Total device count
    /// </summary>
    public int DeviceCount { get; set; }

    /// <summary>
    /// Shared device count
    /// </summary>
    public int SharedDeviceCount { get; set; }

    /// <summary>
    /// Attached device count
    /// </summary>
    public int AttachedDeviceCount { get; set; }
}

/// <summary>
/// Response containing list of devices from the server.
/// </summary>
public class DeviceListResponse
{
    /// <summary>
    /// List of USB devices
    /// </summary>
    public required List<UsbDevice> Devices { get; set; }

    /// <summary>
    /// Server information
    /// </summary>
    public required ServerInfo ServerInfo { get; set; }
}

/// <summary>
/// Response for device attach/detach operations.
/// </summary>
public class AttachResponse
{
    /// <summary>
    /// Whether operation succeeded
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Device information (if successful)
    /// </summary>
    public UsbDevice? Device { get; set; }

    /// <summary>
    /// Human-readable message
    /// </summary>
    public required string Message { get; set; }
}

/// <summary>
/// Error response from the API.
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Always true for error responses
    /// </summary>
    public bool Error { get; set; } = true;

    /// <summary>
    /// Error code (e.g., "DEVICE_IN_USE")
    /// </summary>
    public required string Code { get; set; }

    /// <summary>
    /// Human-readable error message
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// Additional error details
    /// </summary>
    public Dictionary<string, object>? Details { get; set; }
}
