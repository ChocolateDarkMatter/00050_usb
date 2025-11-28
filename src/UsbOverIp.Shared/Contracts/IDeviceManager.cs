using UsbOverIp.Shared.Models;

namespace UsbOverIp.Shared.Contracts;

/// <summary>
/// Interface for managing USB device state and operations.
/// </summary>
public interface IDeviceManager
{
    /// <summary>
    /// Gets all USB devices with their current state.
    /// </summary>
    /// <returns>List of USB devices</returns>
    Task<List<UsbDevice>> GetDevicesAsync();

    /// <summary>
    /// Gets a specific device by bus ID.
    /// </summary>
    /// <param name="busId">Bus ID of the device</param>
    /// <returns>Device information or null if not found</returns>
    Task<UsbDevice?> GetDeviceAsync(string busId);

    /// <summary>
    /// Shares a device for remote access.
    /// </summary>
    /// <param name="busId">Bus ID of the device</param>
    Task ShareDeviceAsync(string busId);

    /// <summary>
    /// Unshares a device (stops remote access).
    /// </summary>
    /// <param name="busId">Bus ID of the device</param>
    Task UnshareDeviceAsync(string busId);

    /// <summary>
    /// Attaches a device to a client.
    /// </summary>
    /// <param name="busId">Bus ID of the device</param>
    /// <param name="clientIp">IP address of the client</param>
    Task AttachDeviceAsync(string busId, string clientIp);

    /// <summary>
    /// Detaches a device from a client.
    /// </summary>
    /// <param name="busId">Bus ID of the device</param>
    Task DetachDeviceAsync(string busId);

    /// <summary>
    /// Refreshes device state from usbipd-win.
    /// </summary>
    Task RefreshDevicesAsync();
}
