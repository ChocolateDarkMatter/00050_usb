using UsbOverIp.Shared.Models;

namespace UsbOverIp.Shared.Contracts;

/// <summary>
/// Interface for interacting with usbipd-win CLI.
/// </summary>
public interface IUsbipdClient
{
    /// <summary>
    /// Lists all USB devices visible to usbipd-win.
    /// </summary>
    /// <returns>List of USB devices</returns>
    Task<List<UsbDevice>> ListDevicesAsync();

    /// <summary>
    /// Binds (shares) a USB device for remote access.
    /// </summary>
    /// <param name="busId">Bus ID of the device</param>
    Task BindDeviceAsync(string busId);

    /// <summary>
    /// Unbinds (stops sharing) a USB device.
    /// </summary>
    /// <param name="busId">Bus ID of the device</param>
    Task UnbindDeviceAsync(string busId);

    /// <summary>
    /// Attaches a device to a remote client.
    /// </summary>
    /// <param name="busId">Bus ID of the device</param>
    /// <param name="clientIp">IP address of the client</param>
    Task AttachDeviceAsync(string busId, string clientIp);

    /// <summary>
    /// Detaches a device from a remote client.
    /// </summary>
    /// <param name="busId">Bus ID of the device</param>
    Task DetachDeviceAsync(string busId);

    /// <summary>
    /// Checks if usbipd-win is available on the system.
    /// </summary>
    /// <returns>True if available, false otherwise</returns>
    Task<bool> IsAvailableAsync();

    /// <summary>
    /// Gets the usbipd-win version.
    /// </summary>
    /// <returns>Version string or null if not available</returns>
    Task<string?> GetVersionAsync();
}
