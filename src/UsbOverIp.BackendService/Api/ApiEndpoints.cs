using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UsbOverIp.Shared.Contracts;
using UsbOverIp.Shared.Models;
using UsbOverIp.Shared.Utilities;

namespace UsbOverIp.BackendService.Api;

/// <summary>
/// Defines REST API endpoints for the USB-over-IP server.
/// </summary>
public static class ApiEndpoints
{
    public static void MapApiEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api");

        // Health check endpoint
        api.MapGet("/health", GetHealth);

        // Device listing
        api.MapGet("/devices", GetDevices);

        // Device operations
        api.MapPost("/devices/{busId}/share", ShareDevice);
        api.MapPost("/devices/{busId}/unshare", UnshareDevice);
        api.MapPost("/devices/{busId}/attach", AttachDevice);
        api.MapPost("/devices/{busId}/detach", DetachDevice);
    }

    private static IResult GetHealth(
        IDeviceManager deviceManager,
        ILogger<Program> logger)
    {
        logger.LogDebug("Health check requested");

        // Get device counts (synchronously for health check)
        var devices = deviceManager.GetDevicesAsync().GetAwaiter().GetResult();
        var sharedCount = devices.Count(d => d.IsShared);
        var attachedCount = devices.Count(d => d.IsAttached);

        var response = new HealthResponse
        {
            Status = "ok",
            Version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0",
            Hostname = NetworkHelper.GetHostname(),
            UsbipdAvailable = true, // Will be checked during startup
            UsbipdVersion = null, // TODO: Get from UsbipdClient
            DeviceCount = devices.Count,
            SharedDeviceCount = sharedCount,
            AttachedDeviceCount = attachedCount
        };

        return Results.Ok(response);
    }

    private static async Task<IResult> GetDevices(
        IDeviceManager deviceManager,
        ILogger<Program> logger)
    {
        try
        {
            logger.LogDebug("Listing devices");

            var devices = await deviceManager.GetDevicesAsync();
            var serverInfo = new ServerInfo
            {
                Id = Guid.NewGuid(), // TODO: Load from configuration
                Hostname = NetworkHelper.GetHostname(),
                IpAddress = NetworkHelper.GetPrimaryIPv4Address().ToString(),
                ApiPort = 50051, // TODO: Load from configuration
                Version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0",
                IsOnline = true,
                LastSeen = DateTime.UtcNow,
                Devices = devices
            };

            var response = new DeviceListResponse
            {
                Devices = devices,
                ServerInfo = serverInfo
            };

            logger.LogInformation("Returning {DeviceCount} devices", devices.Count);
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error listing devices");
            return Results.Problem(
                detail: ex.Message,
                statusCode: 500,
                title: "Failed to list devices");
        }
    }

    private static async Task<IResult> ShareDevice(
        string busId,
        IDeviceManager deviceManager,
        ILogger<Program> logger)
    {
        try
        {
            logger.LogInformation("Share device requested: {BusId}", busId);

            // Verify device exists
            var device = await deviceManager.GetDeviceAsync(busId);
            if (device == null)
            {
                logger.LogWarning("Device not found: {BusId}", busId);
                return Results.NotFound(new ErrorResponse
                {
                    Code = "DEVICE_NOT_FOUND",
                    Message = $"Device with BusId '{busId}' not found"
                });
            }

            // Share the device
            await deviceManager.ShareDeviceAsync(busId);

            // Return updated device
            var updatedDevice = await deviceManager.GetDeviceAsync(busId);
            return Results.Ok(updatedDevice);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sharing device {BusId}", busId);
            return Results.Problem(
                detail: ex.Message,
                statusCode: 500,
                title: $"Failed to share device '{busId}'");
        }
    }

    private static async Task<IResult> UnshareDevice(
        string busId,
        IDeviceManager deviceManager,
        ILogger<Program> logger)
    {
        try
        {
            logger.LogInformation("Unshare device requested: {BusId}", busId);

            // Verify device exists
            var device = await deviceManager.GetDeviceAsync(busId);
            if (device == null)
            {
                logger.LogWarning("Device not found: {BusId}", busId);
                return Results.NotFound(new ErrorResponse
                {
                    Code = "DEVICE_NOT_FOUND",
                    Message = $"Device with BusId '{busId}' not found"
                });
            }

            // Unshare the device
            await deviceManager.UnshareDeviceAsync(busId);

            // Return updated device
            var updatedDevice = await deviceManager.GetDeviceAsync(busId);
            return Results.Ok(updatedDevice);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error unsharing device {BusId}", busId);
            return Results.Problem(
                detail: ex.Message,
                statusCode: 500,
                title: $"Failed to unshare device '{busId}'");
        }
    }

    private static async Task<IResult> AttachDevice(
        string busId,
        IDeviceManager deviceManager,
        ILogger<Program> logger,
        HttpContext context)
    {
        try
        {
            // Get client IP from request
            var clientIp = context.Connection.RemoteIpAddress?.ToString();
            if (string.IsNullOrEmpty(clientIp))
            {
                logger.LogWarning("Unable to determine client IP address");
                return Results.BadRequest(new ErrorResponse
                {
                    Code = "INVALID_CLIENT_IP",
                    Message = "Unable to determine client IP address"
                });
            }

            logger.LogInformation("Attach device requested: {BusId} to client {ClientIp}", busId, clientIp);

            // Verify device exists
            var device = await deviceManager.GetDeviceAsync(busId);
            if (device == null)
            {
                logger.LogWarning("Device not found: {BusId}", busId);
                return Results.NotFound(new ErrorResponse
                {
                    Code = "DEVICE_NOT_FOUND",
                    Message = $"Device with BusId '{busId}' not found"
                });
            }

            // Check if device is shared
            if (!device.IsShared)
            {
                logger.LogWarning("Device {BusId} is not shared", busId);
                return Results.BadRequest(new ErrorResponse
                {
                    Code = "DEVICE_NOT_SHARED",
                    Message = $"Device '{busId}' must be shared before it can be attached"
                });
            }

            // Attach the device
            await deviceManager.AttachDeviceAsync(busId, clientIp);

            // Return updated device in AttachResponse format
            var updatedDevice = await deviceManager.GetDeviceAsync(busId);
            return Results.Ok(new AttachResponse
            {
                Success = true,
                Message = "Device attached successfully",
                Device = updatedDevice
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error attaching device {BusId}", busId);
            return Results.Problem(
                detail: ex.Message,
                statusCode: 500,
                title: $"Failed to attach device '{busId}'");
        }
    }

    private static async Task<IResult> DetachDevice(
        string busId,
        IDeviceManager deviceManager,
        ILogger<Program> logger)
    {
        try
        {
            logger.LogInformation("Detach device requested: {BusId}", busId);

            // Verify device exists
            var device = await deviceManager.GetDeviceAsync(busId);
            if (device == null)
            {
                logger.LogWarning("Device not found: {BusId}", busId);
                return Results.NotFound(new ErrorResponse
                {
                    Code = "DEVICE_NOT_FOUND",
                    Message = $"Device with BusId '{busId}' not found"
                });
            }

            // Check if device is attached
            if (!device.IsAttached)
            {
                logger.LogWarning("Device {BusId} is not attached", busId);
                return Results.BadRequest(new ErrorResponse
                {
                    Code = "DEVICE_NOT_ATTACHED",
                    Message = $"Device '{busId}' is not currently attached"
                });
            }

            // Detach the device
            await deviceManager.DetachDeviceAsync(busId);

            // Return updated device in AttachResponse format
            var updatedDevice = await deviceManager.GetDeviceAsync(busId);
            return Results.Ok(new AttachResponse
            {
                Success = true,
                Message = "Device detached successfully",
                Device = updatedDevice
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error detaching device {BusId}", busId);
            return Results.Problem(
                detail: ex.Message,
                statusCode: 500,
                title: $"Failed to detach device '{busId}'");
        }
    }
}
