using Microsoft.Extensions.Logging;
using UsbOverIp.Shared.Contracts;
using UsbOverIp.Shared.Models;

namespace UsbOverIp.BackendService.Services;

/// <summary>
/// Manages USB device state and operations.
/// </summary>
public class DeviceManager : IDeviceManager
{
    private readonly IUsbipdClient _usbipdClient;
    private readonly ILogger<DeviceManager> _logger;
    private List<UsbDevice> _cachedDevices = new();
    private DateTime _lastRefresh = DateTime.MinValue;
    private readonly TimeSpan _cacheTimeout = TimeSpan.FromSeconds(5);
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public DeviceManager(IUsbipdClient usbipdClient, ILogger<DeviceManager> logger)
    {
        _usbipdClient = usbipdClient;
        _logger = logger;
    }

    public async Task<List<UsbDevice>> GetDevicesAsync()
    {
        // Auto-refresh if cache is stale
        if (DateTime.UtcNow - _lastRefresh > _cacheTimeout)
        {
            await RefreshDevicesAsync();
        }

        return _cachedDevices.ToList(); // Return a copy to prevent external modification
    }

    public async Task<UsbDevice?> GetDeviceAsync(string busId)
    {
        var devices = await GetDevicesAsync();
        return devices.FirstOrDefault(d => d.BusId == busId);
    }

    public async Task ShareDeviceAsync(string busId)
    {
        try
        {
            _logger.LogInformation("Sharing device {BusId}", busId);
            await _usbipdClient.BindDeviceAsync(busId);

            // Refresh device list to get updated state
            await RefreshDevicesAsync();

            _logger.LogInformation("Successfully shared device {BusId}", busId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to share device {BusId}", busId);
            throw;
        }
    }

    public async Task UnshareDeviceAsync(string busId)
    {
        try
        {
            _logger.LogInformation("Unsharing device {BusId}", busId);
            await _usbipdClient.UnbindDeviceAsync(busId);

            // Refresh device list to get updated state
            await RefreshDevicesAsync();

            _logger.LogInformation("Successfully unshared device {BusId}", busId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unshare device {BusId}", busId);
            throw;
        }
    }

    public async Task AttachDeviceAsync(string busId, string clientIp)
    {
        try
        {
            _logger.LogInformation("Attaching device {BusId} to client {ClientIp}", busId, clientIp);
            await _usbipdClient.AttachDeviceAsync(busId, clientIp);

            // Refresh device list to get updated state
            await RefreshDevicesAsync();

            _logger.LogInformation("Successfully attached device {BusId} to {ClientIp}", busId, clientIp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to attach device {BusId} to {ClientIp}", busId, clientIp);
            throw;
        }
    }

    public async Task DetachDeviceAsync(string busId)
    {
        try
        {
            _logger.LogInformation("Detaching device {BusId}", busId);
            await _usbipdClient.DetachDeviceAsync(busId);

            // Refresh device list to get updated state
            await RefreshDevicesAsync();

            _logger.LogInformation("Successfully detached device {BusId}", busId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detach device {BusId}", busId);
            throw;
        }
    }

    public async Task RefreshDevicesAsync()
    {
        await _refreshLock.WaitAsync();
        try
        {
            _logger.LogDebug("Refreshing device list from usbipd");
            _cachedDevices = await _usbipdClient.ListDevicesAsync();
            _lastRefresh = DateTime.UtcNow;
            _logger.LogDebug("Device list refreshed: {DeviceCount} devices found", _cachedDevices.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh device list");
            throw;
        }
        finally
        {
            _refreshLock.Release();
        }
    }
}
