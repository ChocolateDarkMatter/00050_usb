using UsbOverIp.Shared.Models;
using Xunit;

namespace UsbOverIp.Shared.Tests.Models;

public class UsbDeviceTests
{
    [Fact]
    public void UsbDevice_CreatesWithRequiredProperties()
    {
        // Arrange & Act
        var device = new UsbDevice
        {
            BusId = "1-1",
            VendorId = "046D",
            ProductId = "C534",
            Description = "Logitech USB Receiver"
        };

        // Assert
        Assert.Equal("1-1", device.BusId);
        Assert.Equal("046D", device.VendorId);
        Assert.Equal("C534", device.ProductId);
        Assert.Equal("Logitech USB Receiver", device.Description);
        Assert.False(device.IsShared);
        Assert.False(device.IsAttached);
        Assert.Null(device.AttachedToClientIp);
    }

    [Fact]
    public void UsbDevice_CanSetOptionalProperties()
    {
        // Arrange & Act
        var device = new UsbDevice
        {
            BusId = "2-3-1",
            VendorId = "1234",
            ProductId = "5678",
            Description = "Test Device",
            DeviceClass = "HID",
            IsShared = true,
            IsAttached = true,
            AttachedToClientIp = "192.168.1.100"
        };

        // Assert
        Assert.Equal("HID", device.DeviceClass);
        Assert.True(device.IsShared);
        Assert.True(device.IsAttached);
        Assert.Equal("192.168.1.100", device.AttachedToClientIp);
    }
}
