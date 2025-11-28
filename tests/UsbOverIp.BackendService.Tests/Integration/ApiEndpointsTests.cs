using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using UsbOverIp.BackendService.Services;
using UsbOverIp.Shared.Contracts;
using UsbOverIp.Shared.Models;
using Xunit;

namespace UsbOverIp.BackendService.Tests.Integration;

/// <summary>
/// Integration tests for REST API endpoints.
/// </summary>
public class ApiEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IUsbipdClient> _mockUsbipdClient;

    public ApiEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _mockUsbipdClient = new Mock<IUsbipdClient>();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace IUsbipdClient with mock
                var usbipdDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IUsbipdClient));
                if (usbipdDescriptor != null)
                {
                    services.Remove(usbipdDescriptor);
                }
                services.AddSingleton(_mockUsbipdClient.Object);

                // Replace IDeviceManager with one using mock client
                var deviceManagerDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IDeviceManager));
                if (deviceManagerDescriptor != null)
                {
                    services.Remove(deviceManagerDescriptor);
                }
                services.AddSingleton<IDeviceManager>(sp =>
                {
                    var logger = sp.GetRequiredService<ILogger<DeviceManager>>();
                    return new DeviceManager(_mockUsbipdClient.Object, logger);
                });
            });
        });
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsOk()
    {
        // Arrange
        _mockUsbipdClient
            .Setup(x => x.ListDevicesAsync())
            .ReturnsAsync(new List<UsbDevice>());

        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var healthResponse = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(healthResponse);
        Assert.Equal("ok", healthResponse.Status);
        Assert.NotNull(healthResponse.Version);
    }

    [Fact]
    public async Task DevicesEndpoint_ReturnsDeviceList()
    {
        // Arrange
        var expectedDevices = new List<UsbDevice>
        {
            new UsbDevice
            {
                BusId = "1-1",
                VendorId = "046D",
                ProductId = "C534",
                Description = "Test Device",
                IsShared = true,
                IsAttached = false
            }
        };

        _mockUsbipdClient
            .Setup(x => x.ListDevicesAsync())
            .ReturnsAsync(expectedDevices);

        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/devices");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var deviceListResponse = await response.Content.ReadFromJsonAsync<DeviceListResponse>();
        Assert.NotNull(deviceListResponse);
        Assert.Single(deviceListResponse.Devices);
        Assert.Equal("1-1", deviceListResponse.Devices[0].BusId);
    }

    [Fact]
    public async Task ShareDeviceEndpoint_CallsBindDevice()
    {
        // Arrange
        var busId = "1-1";
        var device = new UsbDevice
        {
            BusId = busId,
            VendorId = "046D",
            ProductId = "C534",
            Description = "Test Device"
        };

        _mockUsbipdClient
            .Setup(x => x.ListDevicesAsync())
            .ReturnsAsync(new List<UsbDevice> { device });

        _mockUsbipdClient
            .Setup(x => x.BindDeviceAsync(busId))
            .Returns(Task.CompletedTask);

        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsync($"/api/devices/{busId}/share", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _mockUsbipdClient.Verify(x => x.BindDeviceAsync(busId), Times.Once);
    }

    [Fact]
    public async Task UnshareDeviceEndpoint_CallsUnbindDevice()
    {
        // Arrange
        var busId = "2-1";
        var device = new UsbDevice
        {
            BusId = busId,
            VendorId = "1234",
            ProductId = "5678",
            Description = "Test Device 2"
        };

        _mockUsbipdClient
            .Setup(x => x.ListDevicesAsync())
            .ReturnsAsync(new List<UsbDevice> { device });

        _mockUsbipdClient
            .Setup(x => x.UnbindDeviceAsync(busId))
            .Returns(Task.CompletedTask);

        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsync($"/api/devices/{busId}/unshare", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _mockUsbipdClient.Verify(x => x.UnbindDeviceAsync(busId), Times.Once);
    }

    [Fact]
    public async Task ShareDevice_WhenUsbipdClientThrows_ReturnsInternalServerError()
    {
        // Arrange
        var busId = "1-1";
        _mockUsbipdClient
            .Setup(x => x.ListDevicesAsync())
            .ReturnsAsync(new List<UsbDevice>
            {
                new UsbDevice
                {
                    BusId = busId,
                    VendorId = "046D",
                    ProductId = "C534",
                    Description = "Test Device"
                }
            });

        _mockUsbipdClient
            .Setup(x => x.BindDeviceAsync(busId))
            .ThrowsAsync(new InvalidOperationException("usbipd-win error"));

        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsync($"/api/devices/{busId}/share", null);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task DevicesEndpoint_ReturnsEmptyList_WhenNoDevices()
    {
        // Arrange
        _mockUsbipdClient
            .Setup(x => x.ListDevicesAsync())
            .ReturnsAsync(new List<UsbDevice>());

        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/devices");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var deviceListResponse = await response.Content.ReadFromJsonAsync<DeviceListResponse>();
        Assert.NotNull(deviceListResponse);
        Assert.Empty(deviceListResponse.Devices);
    }
}
