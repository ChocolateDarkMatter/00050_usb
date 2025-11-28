using UsbOverIp.Shared.Contracts;

namespace UsbOverIp.BackendService;

/// <summary>
/// Background worker that periodically refreshes USB device state.
/// </summary>
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IDeviceManager _deviceManager;
    private readonly TimeSpan _refreshInterval = TimeSpan.FromSeconds(10);

    public Worker(ILogger<Worker> logger, IDeviceManager deviceManager)
    {
        _logger = logger;
        _deviceManager = deviceManager;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("USB Device Monitor Worker started");

        // Initial device scan
        try
        {
            await _deviceManager.RefreshDevicesAsync();
            var devices = await _deviceManager.GetDevicesAsync();
            _logger.LogInformation("Initial device scan found {DeviceCount} devices", devices.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during initial device scan");
        }

        // Periodic refresh loop
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_refreshInterval, stoppingToken);

                await _deviceManager.RefreshDevicesAsync();
                var devices = await _deviceManager.GetDevicesAsync();
                _logger.LogDebug("Device refresh: {DeviceCount} devices found", devices.Count);
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during device refresh");
            }
        }

        _logger.LogInformation("USB Device Monitor Worker stopped");
    }
}
