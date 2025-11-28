using Microsoft.Extensions.Logging;
using Moq;
using UsbOverIp.BackendService.Services;
using UsbOverIp.Shared.Contracts;
using UsbOverIp.Shared.Models;
using Xunit;

namespace UsbOverIp.BackendService.Tests.Unit;

/// <summary>
/// Unit tests for DeviceManager.
/// </summary>
public class DeviceManagerTests
{
    private readonly Mock<IUsbipdClient> _mockUsbipdClient;
    private readonly Mock<ILogger<DeviceManager>> _mockLogger;

    public DeviceManagerTests()
    {
        _mockUsbipdClient = new Mock<IUsbipdClient>();
        _mockLogger = new Mock<ILogger<DeviceManager>>();
    }

    [Fact]
    public async Task GetDevicesAsync_ReturnsDevicesFromUsbipdClient()
    {
        // Arrange
        var expectedDevices = new List<UsbDevice>
        {
            new UsbDevice
            {
                BusId = "1-1",
                VendorId = "046D",
                ProductId = "C534",
                Description = "Test Device"
            }
        };

        _mockUsbipdClient
            .Setup(x => x.ListDevicesAsync())
            .ReturnsAsync(expectedDevices);

        var manager = new DeviceManager(_mockUsbipdClient.Object, _mockLogger.Object);

        // Act
        var devices = await manager.GetDevicesAsync();

        // Assert
        Assert.Single(devices);
        Assert.Equal("1-1", devices[0].BusId);
        _mockUsbipdClient.Verify(x => x.ListDevicesAsync(), Times.Once);
    }

    [Fact]
    public async Task ShareDeviceAsync_CallsBindOnUsbipdClient()
    {
        // Arrange
        var busId = "1-1";
        _mockUsbipdClient
            .Setup(x => x.BindDeviceAsync(busId))
            .Returns(Task.CompletedTask);

        _mockUsbipdClient
            .Setup(x => x.ListDevicesAsync())
            .ReturnsAsync(new List<UsbDevice>());

        var manager = new DeviceManager(_mockUsbipdClient.Object, _mockLogger.Object);

        // Act
        await manager.ShareDeviceAsync(busId);

        // Assert
        _mockUsbipdClient.Verify(x => x.BindDeviceAsync(busId), Times.Once);
    }

    [Fact]
    public async Task UnshareDeviceAsync_CallsUnbindOnUsbipdClient()
    {
        // Arrange
        var busId = "2-1";
        _mockUsbipdClient
            .Setup(x => x.UnbindDeviceAsync(busId))
            .Returns(Task.CompletedTask);

        _mockUsbipdClient
            .Setup(x => x.ListDevicesAsync())
            .ReturnsAsync(new List<UsbDevice>());

        var manager = new DeviceManager(_mockUsbipdClient.Object, _mockLogger.Object);

        // Act
        await manager.UnshareDeviceAsync(busId);

        // Assert
        _mockUsbipdClient.Verify(x => x.UnbindDeviceAsync(busId), Times.Once);
    }

    [Fact]
    public async Task GetDeviceAsync_WhenDeviceExists_ReturnsDevice()
    {
        // Arrange
        var devices = new List<UsbDevice>
        {
            new UsbDevice
            {
                BusId = "1-1",
                VendorId = "046D",
                ProductId = "C534",
                Description = "Device 1"
            },
            new UsbDevice
            {
                BusId = "2-1",
                VendorId = "1234",
                ProductId = "5678",
                Description = "Device 2"
            }
        };

        _mockUsbipdClient
            .Setup(x => x.ListDevicesAsync())
            .ReturnsAsync(devices);

        var manager = new DeviceManager(_mockUsbipdClient.Object, _mockLogger.Object);

        // Act
        var device = await manager.GetDeviceAsync("2-1");

        // Assert
        Assert.NotNull(device);
        Assert.Equal("2-1", device.BusId);
        Assert.Equal("Device 2", device.Description);
    }

    [Fact]
    public async Task GetDeviceAsync_WhenDeviceDoesNotExist_ReturnsNull()
    {
        // Arrange
        var devices = new List<UsbDevice>
        {
            new UsbDevice
            {
                BusId = "1-1",
                VendorId = "046D",
                ProductId = "C534",
                Description = "Device 1"
            }
        };

        _mockUsbipdClient
            .Setup(x => x.ListDevicesAsync())
            .ReturnsAsync(devices);

        var manager = new DeviceManager(_mockUsbipdClient.Object, _mockLogger.Object);

        // Act
        var device = await manager.GetDeviceAsync("99-99");

        // Assert
        Assert.Null(device);
    }

    [Fact]
    public async Task RefreshDevicesAsync_UpdatesDeviceCache()
    {
        // Arrange
        var initialDevices = new List<UsbDevice>
        {
            new UsbDevice
            {
                BusId = "1-1",
                VendorId = "046D",
                ProductId = "C534",
                Description = "Device 1"
            }
        };

        var updatedDevices = new List<UsbDevice>
        {
            new UsbDevice
            {
                BusId = "1-1",
                VendorId = "046D",
                ProductId = "C534",
                Description = "Device 1"
            },
            new UsbDevice
            {
                BusId = "2-1",
                VendorId = "1234",
                ProductId = "5678",
                Description = "Device 2"
            }
        };

        _mockUsbipdClient
            .SetupSequence(x => x.ListDevicesAsync())
            .ReturnsAsync(initialDevices)
            .ReturnsAsync(updatedDevices);

        var manager = new DeviceManager(_mockUsbipdClient.Object, _mockLogger.Object);

        // Act
        var before = await manager.GetDevicesAsync();
        await manager.RefreshDevicesAsync();
        var after = await manager.GetDevicesAsync();

        // Assert
        Assert.Single(before);
        Assert.Equal(2, after.Count);
    }
}

