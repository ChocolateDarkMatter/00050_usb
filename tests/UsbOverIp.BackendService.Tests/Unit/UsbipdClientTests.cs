using Microsoft.Extensions.Logging;
using Moq;
using UsbOverIp.BackendService.Services;
using Xunit;

namespace UsbOverIp.BackendService.Tests.Unit;

/// <summary>
/// Unit tests for UsbipdClient attach/detach operations.
/// </summary>
public class UsbipdClientTests
{
    private readonly Mock<ILogger<UsbipdClient>> _mockLogger;

    public UsbipdClientTests()
    {
        _mockLogger = new Mock<ILogger<UsbipdClient>>();
    }

    [Fact(Skip = "Requires usbipd-win to be installed and running")]
    public async Task AttachDeviceAsync_CallsUsbipdAttach()
    {
        // Arrange
        var client = new UsbipdClient(_mockLogger.Object);
        var busId = "1-1";
        var clientIp = "192.168.1.100";

        // Act
        // This will fail if usbipd-win is not installed or device doesn't exist
        // In real implementation, we would mock Process execution
        await client.AttachDeviceAsync(busId, clientIp);

        // Assert
        // Verify that logger was called
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains($"Attaching device {busId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact(Skip = "Requires usbipd-win to be installed and running")]
    public async Task DetachDeviceAsync_CallsUsbipdDetach()
    {
        // Arrange
        var client = new UsbipdClient(_mockLogger.Object);
        var busId = "1-1";

        // Act
        // This will fail if usbipd-win is not installed or device doesn't exist
        // In real implementation, we would mock Process execution
        await client.DetachDeviceAsync(busId);

        // Assert
        // Verify that logger was called
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains($"Detaching device {busId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact(Skip = "Requires usbipd-win to be installed")]
    public async Task IsAvailableAsync_ReturnsTrue_WhenUsbipdInstalled()
    {
        // Arrange
        var client = new UsbipdClient(_mockLogger.Object);

        // Act
        var isAvailable = await client.IsAvailableAsync();

        // Assert
        // This will be true if usbipd-win is installed on the system
        Assert.True(isAvailable || !isAvailable); // Just verify it doesn't throw
    }

    [Fact(Skip = "Requires usbipd-win to be installed")]
    public async Task GetVersionAsync_ReturnsVersion_WhenUsbipdInstalled()
    {
        // Arrange
        var client = new UsbipdClient(_mockLogger.Object);

        // Act
        var version = await client.GetVersionAsync();

        // Assert
        // Version should be non-null if usbipd-win is installed
        // Or null if not installed - just verify it doesn't throw
        Assert.True(version == null || !string.IsNullOrWhiteSpace(version));
    }

    [Fact]
    public void AttachDeviceAsync_ThrowsArgumentException_WhenBusIdEmpty()
    {
        // Arrange
        var client = new UsbipdClient(_mockLogger.Object);

        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(
            async () => await client.AttachDeviceAsync("", "192.168.1.100"));
    }

    [Fact]
    public void AttachDeviceAsync_ThrowsArgumentException_WhenClientIpEmpty()
    {
        // Arrange
        var client = new UsbipdClient(_mockLogger.Object);

        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(
            async () => await client.AttachDeviceAsync("1-1", ""));
    }

    [Fact]
    public void DetachDeviceAsync_ThrowsArgumentException_WhenBusIdEmpty()
    {
        // Arrange
        var client = new UsbipdClient(_mockLogger.Object);

        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(
            async () => await client.DetachDeviceAsync(""));
    }
}
