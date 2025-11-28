using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using UsbOverIp.Shared.Contracts;
using UsbOverIp.Shared.Models;

namespace UsbOverIp.BackendService.Services;

/// <summary>
/// Client for interacting with usbipd-win CLI.
/// </summary>
public class UsbipdClient : IUsbipdClient
{
    private readonly ILogger<UsbipdClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public UsbipdClient(ILogger<UsbipdClient> logger)
    {
        _logger = logger;
    }

    public async Task<List<UsbDevice>> ListDevicesAsync()
    {
        try
        {
            var output = await RunUsbipdCommandAsync("state");

            if (string.IsNullOrWhiteSpace(output))
            {
                _logger.LogWarning("usbipd state returned empty output");
                return new List<UsbDevice>();
            }

            var stateOutput = JsonSerializer.Deserialize<UsbipdStateOutput>(output, _jsonOptions);
            if (stateOutput == null || stateOutput.Devices == null)
            {
                _logger.LogWarning("Failed to deserialize usbipd state output");
                return new List<UsbDevice>();
            }

            return stateOutput.Devices.Select(MapToUsbDevice).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing USB devices from usbipd");
            throw;
        }
    }

    public async Task BindDeviceAsync(string busId)
    {
        try
        {
            _logger.LogInformation("Binding device {BusId}", busId);
            await RunUsbipdCommandAsync($"bind --busid {busId}");
            _logger.LogInformation("Successfully bound device {BusId}", busId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error binding device {BusId}", busId);
            throw;
        }
    }

    public async Task UnbindDeviceAsync(string busId)
    {
        try
        {
            _logger.LogInformation("Unbinding device {BusId}", busId);
            await RunUsbipdCommandAsync($"unbind --busid {busId}");
            _logger.LogInformation("Successfully unbound device {BusId}", busId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unbinding device {BusId}", busId);
            throw;
        }
    }

    public async Task AttachDeviceAsync(string busId, string clientIp)
    {
        if (string.IsNullOrWhiteSpace(busId))
            throw new ArgumentException("Bus ID cannot be empty", nameof(busId));
        if (string.IsNullOrWhiteSpace(clientIp))
            throw new ArgumentException("Client IP cannot be empty", nameof(clientIp));

        try
        {
            _logger.LogInformation("Attaching device {BusId} to client {ClientIp}", busId, clientIp);
            await RunUsbipdCommandAsync($"attach --busid {busId} --client {clientIp}");
            _logger.LogInformation("Successfully attached device {BusId} to {ClientIp}", busId, clientIp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error attaching device {BusId} to {ClientIp}", busId, clientIp);
            throw;
        }
    }

    public async Task DetachDeviceAsync(string busId)
    {
        if (string.IsNullOrWhiteSpace(busId))
            throw new ArgumentException("Bus ID cannot be empty", nameof(busId));

        try
        {
            _logger.LogInformation("Detaching device {BusId}", busId);
            await RunUsbipdCommandAsync($"detach --busid {busId}");
            _logger.LogInformation("Successfully detached device {BusId}", busId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detaching device {BusId}", busId);
            throw;
        }
    }

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var version = await GetVersionAsync();
            return !string.IsNullOrEmpty(version);
        }
        catch
        {
            return false;
        }
    }

    public async Task<string?> GetVersionAsync()
    {
        try
        {
            var output = await RunUsbipdCommandAsync("--version");
            return output?.Trim();
        }
        catch
        {
            return null;
        }
    }

    private async Task<string> RunUsbipdCommandAsync(string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "usbipd",
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
            _logger.LogError("usbipd command failed with exit code {ExitCode}: {Error}",
                process.ExitCode, error);
            throw new InvalidOperationException($"usbipd command failed: {error}");
        }

        return outputBuilder.ToString();
    }

    private UsbDevice MapToUsbDevice(UsbipdDeviceInfo deviceInfo)
    {
        return new UsbDevice
        {
            BusId = deviceInfo.BusId,
            VendorId = deviceInfo.VendorId.ToString("X4"),
            ProductId = deviceInfo.ProductId.ToString("X4"),
            Description = deviceInfo.Description,
            DeviceClass = ExtractDeviceClass(deviceInfo.InstanceId),
            IsShared = deviceInfo.PersistedGuid != null || deviceInfo.IsForced,
            IsAttached = deviceInfo.ClientIPAddress != null,
            AttachedToClientIp = deviceInfo.ClientIPAddress
        };
    }

    private string? ExtractDeviceClass(string instanceId)
    {
        // Instance ID format: USB\VID_046D&PID_C534\...
        // We could parse this to determine device class, but for now just return null
        // Device class is not readily available from usbipd state output
        return null;
    }
}

/// <summary>
/// Root object for 'usbipd state' JSON output.
/// </summary>
internal class UsbipdStateOutput
{
    public List<UsbipdDeviceInfo> Devices { get; set; } = new();
}

/// <summary>
/// Device information from 'usbipd state' JSON output.
/// </summary>
internal class UsbipdDeviceInfo
{
    public required string BusId { get; set; }
    public string? ClientIPAddress { get; set; }
    public string? ClientId { get; set; }
    public required string Description { get; set; }
    public required string InstanceId { get; set; }
    public bool IsForced { get; set; }
    public string? PersistedGuid { get; set; }
    public string? StubInstanceId { get; set; }
    public int VendorId { get; set; }
    public int ProductId { get; set; }
}
