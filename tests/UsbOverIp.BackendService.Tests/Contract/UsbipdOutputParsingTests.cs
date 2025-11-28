using System.Text.Json;
using Xunit;

namespace UsbOverIp.BackendService.Tests.Contract;

/// <summary>
/// Contract tests for parsing usbipd-win CLI output.
/// Ensures we correctly parse the JSON output from 'usbipd state'.
/// </summary>
public class UsbipdOutputParsingTests
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void ParseState_EmptyDeviceList_ReturnsEmptyList()
    {
        // Arrange
        var json = """
        {
          "Devices": []
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<UsbipdStateOutput>(json, _jsonOptions);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Devices);
    }

    [Fact]
    public void ParseState_SingleDevice_ParsesAllFields()
    {
        // Arrange
        var json = """
        {
          "Devices": [
            {
              "BusId": "1-1",
              "ClientIPAddress": null,
              "ClientId": null,
              "Description": "USB Input Device",
              "InstanceId": "USB\\VID_046D&PID_C534\\5&2A8F8A4&0&1",
              "IsForced": false,
              "PersistedGuid": null,
              "StubInstanceId": null,
              "VendorId": 1133,
              "ProductId": 50484
            }
          ]
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<UsbipdStateOutput>(json, _jsonOptions);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Devices);

        var device = result.Devices[0];
        Assert.Equal("1-1", device.BusId);
        Assert.Equal("USB Input Device", device.Description);
        Assert.Null(device.ClientIPAddress);
        Assert.Equal(1133, device.VendorId);
        Assert.Equal(50484, device.ProductId);
        Assert.False(device.IsForced);
    }

    [Fact]
    public void ParseState_AttachedDevice_ParsesClientIP()
    {
        // Arrange
        var json = """
        {
          "Devices": [
            {
              "BusId": "2-1",
              "ClientIPAddress": "192.168.1.100",
              "ClientId": null,
              "Description": "USB Mass Storage",
              "InstanceId": "USB\\VID_1234&PID_5678\\ABC123",
              "IsForced": false,
              "PersistedGuid": "{12345678-1234-1234-1234-123456789012}",
              "StubInstanceId": "ROOT\\VHCI\\0000",
              "VendorId": 4660,
              "ProductId": 22136
            }
          ]
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<UsbipdStateOutput>(json, _jsonOptions);

        // Assert
        Assert.NotNull(result);
        var device = result.Devices[0];
        Assert.Equal("192.168.1.100", device.ClientIPAddress);
        Assert.NotNull(device.PersistedGuid);
        Assert.NotNull(device.StubInstanceId);
    }

    [Fact]
    public void ParseState_MultipleDevices_ParsesAll()
    {
        // Arrange
        var json = """
        {
          "Devices": [
            {
              "BusId": "1-1",
              "ClientIPAddress": null,
              "ClientId": null,
              "Description": "Device 1",
              "InstanceId": "USB\\VID_0001&PID_0001\\1",
              "IsForced": false,
              "PersistedGuid": null,
              "StubInstanceId": null,
              "VendorId": 1,
              "ProductId": 1
            },
            {
              "BusId": "2-1",
              "ClientIPAddress": "192.168.1.50",
              "ClientId": null,
              "Description": "Device 2",
              "InstanceId": "USB\\VID_0002&PID_0002\\2",
              "IsForced": true,
              "PersistedGuid": "{AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA}",
              "StubInstanceId": "ROOT\\VHCI\\0001",
              "VendorId": 2,
              "ProductId": 2
            }
          ]
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<UsbipdStateOutput>(json, _jsonOptions);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Devices.Count);
        Assert.Equal("1-1", result.Devices[0].BusId);
        Assert.Equal("2-1", result.Devices[1].BusId);
        Assert.Null(result.Devices[0].ClientIPAddress);
        Assert.Equal("192.168.1.50", result.Devices[1].ClientIPAddress);
    }

    [Fact]
    public void VendorId_ConvertsToHexString()
    {
        // Arrange
        var device = new UsbipdDeviceInfo
        {
            BusId = "1-1",
            Description = "Test",
            VendorId = 1133, // 0x046D in hex
            ProductId = 50484, // 0xC534 in hex
            InstanceId = "TEST"
        };

        // Act
        var vendorHex = device.VendorId.ToString("X4");
        var productHex = device.ProductId.ToString("X4");

        // Assert
        Assert.Equal("046D", vendorHex);
        Assert.Equal("C534", productHex);
    }
}

/// <summary>
/// Root object for 'usbipd state' JSON output.
/// </summary>
public class UsbipdStateOutput
{
    public List<UsbipdDeviceInfo> Devices { get; set; } = new();
}

/// <summary>
/// Device information from 'usbipd state' JSON output.
/// </summary>
public class UsbipdDeviceInfo
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
