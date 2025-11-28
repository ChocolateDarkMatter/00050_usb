namespace UsbOverIp.Shared.Models;

/// <summary>
/// Represents a physical USB device connected to a server machine.
/// </summary>
public class UsbDevice
{
    /// <summary>
    /// Unique identifier from usbipd-win (e.g., "1-1", "2-3-1")
    /// </summary>
    public required string BusId { get; set; }

    /// <summary>
    /// USB Vendor ID in hex (e.g., "046D")
    /// </summary>
    public required string VendorId { get; set; }

    /// <summary>
    /// USB Product ID in hex (e.g., "C534")
    /// </summary>
    public required string ProductId { get; set; }

    /// <summary>
    /// Human-readable device name
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// USB device class (e.g., "HID", "Mass Storage")
    /// </summary>
    public string? DeviceClass { get; set; }

    /// <summary>
    /// Whether device is exported for remote access
    /// </summary>
    public bool IsShared { get; set; }

    /// <summary>
    /// Whether device is currently attached to a client
    /// </summary>
    public bool IsAttached { get; set; }

    /// <summary>
    /// IP address of attached client, null if not attached
    /// </summary>
    public string? AttachedToClientIp { get; set; }
}
