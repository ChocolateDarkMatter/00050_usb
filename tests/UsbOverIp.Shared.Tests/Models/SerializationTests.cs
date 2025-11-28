using System.Text.Json;
using UsbOverIp.Shared.Models;
using Xunit;

namespace UsbOverIp.Shared.Tests.Models;

public class SerializationTests
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    [Fact]
    public void UsbDevice_SerializesAndDeserializes()
    {
        // Arrange
        var original = new UsbDevice
        {
            BusId = "1-1",
            VendorId = "046D",
            ProductId = "C534",
            Description = "Logitech USB Receiver",
            DeviceClass = "HID",
            IsShared = true,
            IsAttached = false
        };

        // Act
        var json = JsonSerializer.Serialize(original, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<UsbDevice>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.BusId, deserialized.BusId);
        Assert.Equal(original.VendorId, deserialized.VendorId);
        Assert.Equal(original.ProductId, deserialized.ProductId);
        Assert.Equal(original.Description, deserialized.Description);
        Assert.Equal(original.DeviceClass, deserialized.DeviceClass);
        Assert.Equal(original.IsShared, deserialized.IsShared);
        Assert.Equal(original.IsAttached, deserialized.IsAttached);
    }

    [Fact]
    public void ServerAnnouncement_SerializesAndDeserializes()
    {
        // Arrange
        var original = new ServerAnnouncement
        {
            Type = "UsbServerAnnouncement",
            Id = Guid.NewGuid(),
            Name = "TEST-SERVER",
            ApiPort = 50051,
            Version = "1.0.0"
        };

        // Act
        var json = JsonSerializer.Serialize(original, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<ServerAnnouncement>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.Type, deserialized.Type);
        Assert.Equal(original.Id, deserialized.Id);
        Assert.Equal(original.Name, deserialized.Name);
        Assert.Equal(original.ApiPort, deserialized.ApiPort);
        Assert.Equal(original.Version, deserialized.Version);
    }

    [Fact]
    public void DeviceListResponse_SerializesAndDeserializes()
    {
        // Arrange
        var original = new DeviceListResponse
        {
            Devices = new List<UsbDevice>
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
            },
            ServerInfo = new ServerInfo
            {
                Id = Guid.NewGuid(),
                Hostname = "TEST-SERVER",
                IpAddress = "192.168.1.50",
                ApiPort = 50051,
                Version = "1.0.0"
            }
        };

        // Act
        var json = JsonSerializer.Serialize(original, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<DeviceListResponse>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(2, deserialized.Devices.Count);
        Assert.Equal(original.Devices[0].BusId, deserialized.Devices[0].BusId);
        Assert.Equal(original.ServerInfo.Hostname, deserialized.ServerInfo.Hostname);
    }

    [Fact]
    public void AttachmentState_SerializesAsString()
    {
        // Arrange
        var state = AttachmentState.Active;

        // Act
        var json = JsonSerializer.Serialize(state, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<AttachmentState>(json, _jsonOptions);

        // Assert
        Assert.Equal(AttachmentState.Active, deserialized);
    }

    [Fact]
    public void ServerConfiguration_SerializesWithDefaults()
    {
        // Arrange
        var original = new ServerConfiguration
        {
            ServerId = Guid.NewGuid()
        };

        // Act
        var json = JsonSerializer.Serialize(original, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<ServerConfiguration>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(50051, deserialized.ApiPort);
        Assert.Equal(50050, deserialized.DiscoveryPort);
        Assert.Equal(5, deserialized.BroadcastIntervalSeconds);
        Assert.Equal(30, deserialized.ClientTimeoutSeconds);
        Assert.Equal("Information", deserialized.LogLevel);
        Assert.Empty(deserialized.AutoShareDevices);
    }

    [Fact]
    public void ClientPreferences_SerializesWithDefaults()
    {
        // Arrange
        var original = new ClientPreferences();

        // Act
        var json = JsonSerializer.Serialize(original, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<ClientPreferences>(json, _jsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.True(deserialized.MinimizeToTray);
        Assert.False(deserialized.StartWithWindows);
        Assert.Equal("Information", deserialized.LogLevel);
        Assert.Empty(deserialized.KnownServers);
        Assert.Empty(deserialized.AutoConnectRules);
    }
}
