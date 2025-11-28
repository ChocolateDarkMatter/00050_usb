using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace UsbOverIp.ClientAgent.Services;

/// <summary>
/// Client for running local USBIP commands on the client machine.
/// </summary>
public class LocalUsbipClient
{
    private readonly ILogger<LocalUsbipClient> _logger;

    public LocalUsbipClient(ILogger<LocalUsbipClient> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Attaches a remote USB device to the local machine.
    /// </summary>
    /// <param name="serverIp">The IP address of the USBIP server</param>
    /// <param name="busId">The bus ID of the device to attach</param>
    public async Task AttachDeviceAsync(string serverIp, string busId)
    {
        if (string.IsNullOrWhiteSpace(serverIp))
            throw new ArgumentException("Server IP cannot be empty", nameof(serverIp));
        if (string.IsNullOrWhiteSpace(busId))
            throw new ArgumentException("Bus ID cannot be empty", nameof(busId));

        try
        {
            _logger.LogInformation("Attaching device {BusId} from server {ServerIp}", busId, serverIp);
            await RunUsbipCommandAsync($"attach -r {serverIp} -b {busId}");
            _logger.LogInformation("Successfully attached device {BusId}", busId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error attaching device {BusId} from {ServerIp}", busId, serverIp);
            throw;
        }
    }

    /// <summary>
    /// Detaches a USB device from the local machine.
    /// </summary>
    /// <param name="port">The port number of the attached device (use busId for now)</param>
    public async Task DetachDeviceAsync(string busId)
    {
        if (string.IsNullOrWhiteSpace(busId))
            throw new ArgumentException("Bus ID cannot be empty", nameof(busId));

        try
        {
            _logger.LogInformation("Detaching device {BusId}", busId);

            // First, we need to find the port number for this busId
            var port = await GetPortForBusIdAsync(busId);
            if (port == null)
            {
                _logger.LogWarning("Device {BusId} is not attached locally", busId);
                throw new InvalidOperationException($"Device {busId} is not attached to this machine");
            }

            await RunUsbipCommandAsync($"detach -p {port}");
            _logger.LogInformation("Successfully detached device {BusId} (port {Port})", busId, port);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detaching device {BusId}", busId);
            throw;
        }
    }

    /// <summary>
    /// Lists all locally attached USBIP devices.
    /// </summary>
    public async Task<List<string>> ListAttachedDevicesAsync()
    {
        try
        {
            var output = await RunUsbipCommandAsync("port");
            var devices = new List<string>();

            // Parse output to extract bus IDs
            // Example output:
            // Port 00: <Port in Use> at Full Speed(12Mbps)
            //        Imported USB device 1-2 from 172.21.103.181
            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line.Contains("Imported USB device"))
                {
                    var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < parts.Length; i++)
                    {
                        if (parts[i] == "device" && i + 1 < parts.Length)
                        {
                            devices.Add(parts[i + 1]);
                            break;
                        }
                    }
                }
            }

            return devices;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing attached devices");
            return new List<string>();
        }
    }

    /// <summary>
    /// Gets the port number for a given bus ID.
    /// </summary>
    private async Task<string?> GetPortForBusIdAsync(string busId)
    {
        try
        {
            var output = await RunUsbipCommandAsync("port");

            // Parse output to find the port for this busId
            // Example output:
            // Port 00: <Port in Use> at Full Speed(12Mbps)
            //        Imported USB device 1-2 from 172.21.103.181
            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            string? currentPort = null;

            foreach (var line in lines)
            {
                if (line.StartsWith("Port "))
                {
                    // Extract port number (e.g., "Port 00:" -> "00")
                    var parts = line.Split(new[] { ' ', ':' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        currentPort = parts[1];
                    }
                }
                else if (line.Contains($"Imported USB device {busId}"))
                {
                    return currentPort;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding port for device {BusId}", busId);
            return null;
        }
    }

    /// <summary>
    /// Checks if USBIP client tools are available.
    /// </summary>
    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            await RunUsbipCommandAsync("version");
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<string> RunUsbipCommandAsync(string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "usbip",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };

        var outputBuilder = new System.Text.StringBuilder();
        var errorBuilder = new System.Text.StringBuilder();

        process.OutputDataReceived += (sender, args) =>
        {
            if (args.Data != null)
                outputBuilder.AppendLine(args.Data);
        };

        process.ErrorDataReceived += (sender, args) =>
        {
            if (args.Data != null)
                errorBuilder.AppendLine(args.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var error = errorBuilder.ToString();
            _logger.LogError("usbip command failed with exit code {ExitCode}: {Error}",
                process.ExitCode, error);
            throw new InvalidOperationException($"usbip command failed: {error}");
        }

        return outputBuilder.ToString();
    }
}
