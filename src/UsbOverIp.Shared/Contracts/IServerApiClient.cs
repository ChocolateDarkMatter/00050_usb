using UsbOverIp.Shared.Models;

namespace UsbOverIp.Shared.Contracts;

/// <summary>
/// Interface for communicating with the USB-over-IP backend server API.
/// </summary>
public interface IServerApiClient
{
    /// <summary>
    /// Gets the health status of the server.
    /// </summary>
    /// <returns>Health response</returns>
    Task<HealthResponse> GetHealthAsync();

    /// <summary>
    /// Gets all devices from the server.
    /// </summary>
    /// <returns>Device list response</returns>
    Task<DeviceListResponse> GetDevicesAsync();

    /// <summary>
    /// Shares a device on the server.
    /// </summary>
    /// <param name="busId">Bus ID of the device</param>
    /// <returns>Updated device information</returns>
    Task<UsbDevice> ShareDeviceAsync(string busId);

    /// <summary>
    /// Unshares a device on the server.
    /// </summary>
    /// <param name="busId">Bus ID of the device</param>
    /// <returns>Updated device information</returns>
    Task<UsbDevice> UnshareDeviceAsync(string busId);

    /// <summary>
    /// Attaches a device from the server to this client.
    /// </summary>
    /// <param name="busId">Bus ID of the device</param>
    /// <returns>Attach response</returns>
    Task<AttachResponse> AttachDeviceAsync(string busId);

    /// <summary>
    /// Detaches a device from this client.
    /// </summary>
    /// <param name="busId">Bus ID of the device</param>
    /// <returns>Attach response</returns>
    Task<AttachResponse> DetachDeviceAsync(string busId);

    /// <summary>
    /// Sets the base URL of the server.
    /// </summary>
    /// <param name="baseUrl">Base URL (e.g., "http://192.168.1.100:50051")</param>
    void SetServerUrl(string baseUrl);
}
