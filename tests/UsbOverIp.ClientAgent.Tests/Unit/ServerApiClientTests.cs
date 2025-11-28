using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Moq;
using Moq.Protected;
using UsbOverIp.ClientAgent.Services;
using UsbOverIp.Shared.Models;
using Xunit;

namespace UsbOverIp.ClientAgent.Tests.Unit;

/// <summary>
/// Unit tests for ServerApiClient.
/// </summary>
public class ServerApiClientTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpHandler;
    private readonly HttpClient _httpClient;
    private readonly ServerApiClient _apiClient;

    public ServerApiClientTests()
    {
        _mockHttpHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpHandler.Object);
        _apiClient = new ServerApiClient(_httpClient);
        _apiClient.SetServerUrl("http://localhost:50051");
    }

    [Fact]
    public async Task GetHealthAsync_ReturnsHealthResponse()
    {
        // Arrange
        var expectedResponse = new HealthResponse
        {
            Status = "ok",
            Version = "1.0.0",
            Hostname = "test-server",
            UsbipdAvailable = true,
            DeviceCount = 2,
            SharedDeviceCount = 1,
            AttachedDeviceCount = 0
        };

        SetupHttpResponse(HttpStatusCode.OK, expectedResponse);

        // Act
        var response = await _apiClient.GetHealthAsync();

        // Assert
        Assert.NotNull(response);
        Assert.Equal("ok", response.Status);
        Assert.Equal("test-server", response.Hostname);
        Assert.Equal(2, response.DeviceCount);
    }

    [Fact]
    public async Task GetDevicesAsync_ReturnsDeviceList()
    {
        // Arrange
        var expectedDevices = new List<UsbDevice>
        {
            new UsbDevice
            {
                BusId = "1-1",
                VendorId = "046D",
                ProductId = "C534",
                Description = "Test Device 1",
                IsShared = true,
                IsAttached = false
            },
            new UsbDevice
            {
                BusId = "2-1",
                VendorId = "1234",
                ProductId = "5678",
                Description = "Test Device 2",
                IsShared = false,
                IsAttached = false
            }
        };

        var expectedResponse = new DeviceListResponse
        {
            Devices = expectedDevices,
            ServerInfo = new ServerInfo
            {
                Id = Guid.NewGuid(),
                Hostname = "test-server",
                IpAddress = "192.168.1.100",
                ApiPort = 50051,
                Version = "1.0.0",
                IsOnline = true,
                LastSeen = DateTime.UtcNow,
                Devices = expectedDevices
            }
        };

        SetupHttpResponse(HttpStatusCode.OK, expectedResponse);

        // Act
        var response = await _apiClient.GetDevicesAsync();

        // Assert
        Assert.NotNull(response);
        Assert.Equal(2, response.Devices.Count);
        Assert.Equal("1-1", response.Devices[0].BusId);
        Assert.Equal("Test Device 1", response.Devices[0].Description);
    }

    [Fact]
    public async Task ShareDeviceAsync_CallsCorrectEndpoint()
    {
        // Arrange
        var busId = "1-1";
        var expectedDevice = new UsbDevice
        {
            BusId = busId,
            VendorId = "046D",
            ProductId = "C534",
            Description = "Test Device",
            IsShared = true,
            IsAttached = false
        };

        SetupHttpResponse(HttpStatusCode.OK, expectedDevice);

        // Act
        var device = await _apiClient.ShareDeviceAsync(busId);

        // Assert
        Assert.NotNull(device);
        Assert.Equal(busId, device.BusId);
        Assert.True(device.IsShared);

        VerifyHttpRequest(
            HttpMethod.Post,
            $"http://localhost:50051/api/devices/{busId}/share");
    }

    [Fact]
    public async Task UnshareDeviceAsync_CallsCorrectEndpoint()
    {
        // Arrange
        var busId = "1-1";
        var expectedDevice = new UsbDevice
        {
            BusId = busId,
            VendorId = "046D",
            ProductId = "C534",
            Description = "Test Device",
            IsShared = false,
            IsAttached = false
        };

        SetupHttpResponse(HttpStatusCode.OK, expectedDevice);

        // Act
        var device = await _apiClient.UnshareDeviceAsync(busId);

        // Assert
        Assert.NotNull(device);
        Assert.Equal(busId, device.BusId);
        Assert.False(device.IsShared);

        VerifyHttpRequest(
            HttpMethod.Post,
            $"http://localhost:50051/api/devices/{busId}/unshare");
    }

    [Fact]
    public async Task AttachDeviceAsync_CallsCorrectEndpoint()
    {
        // Arrange
        var busId = "1-1";
        var expectedResponse = new AttachResponse
        {
            Success = true,
            Message = "Device attached successfully",
            Device = new UsbDevice
            {
                BusId = busId,
                VendorId = "046D",
                ProductId = "C534",
                Description = "Test Device",
                IsShared = true,
                IsAttached = true
            }
        };

        SetupHttpResponse(HttpStatusCode.OK, expectedResponse);

        // Act
        var response = await _apiClient.AttachDeviceAsync(busId);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Device);
        Assert.True(response.Device.IsAttached);

        VerifyHttpRequest(
            HttpMethod.Post,
            $"http://localhost:50051/api/devices/{busId}/attach");
    }

    [Fact]
    public async Task DetachDeviceAsync_CallsCorrectEndpoint()
    {
        // Arrange
        var busId = "1-1";
        var expectedResponse = new AttachResponse
        {
            Success = true,
            Message = "Device detached successfully",
            Device = new UsbDevice
            {
                BusId = busId,
                VendorId = "046D",
                ProductId = "C534",
                Description = "Test Device",
                IsShared = true,
                IsAttached = false
            }
        };

        SetupHttpResponse(HttpStatusCode.OK, expectedResponse);

        // Act
        var response = await _apiClient.DetachDeviceAsync(busId);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Device);
        Assert.False(response.Device.IsAttached);

        VerifyHttpRequest(
            HttpMethod.Post,
            $"http://localhost:50051/api/devices/{busId}/detach");
    }

    [Fact]
    public async Task GetHealthAsync_ThrowsOnHttpError()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.InternalServerError, new { error = "Server error" });

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            async () => await _apiClient.GetHealthAsync());
    }

    [Fact]
    public void SetServerUrl_UpdatesBaseUrl()
    {
        // Arrange
        var newUrl = "http://192.168.1.100:50051";

        // Act
        _apiClient.SetServerUrl(newUrl);

        // Assert
        // The next request should use the new URL
        SetupHttpResponse(HttpStatusCode.OK, new HealthResponse
        {
            Status = "ok",
            Version = "1.0.0",
            Hostname = "test",
            UsbipdAvailable = true
        });

        var _ = _apiClient.GetHealthAsync();

        VerifyHttpRequest(HttpMethod.Get, $"{newUrl}/api/health");
    }

    #region Helper Methods

    private void SetupHttpResponse<T>(HttpStatusCode statusCode, T content)
    {
        var json = JsonSerializer.Serialize(content);
        var httpResponse = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);
    }

    private void VerifyHttpRequest(HttpMethod method, string uri)
    {
        _mockHttpHandler.Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == method &&
                    req.RequestUri!.ToString() == uri),
                ItExpr.IsAny<CancellationToken>());
    }

    #endregion
}
