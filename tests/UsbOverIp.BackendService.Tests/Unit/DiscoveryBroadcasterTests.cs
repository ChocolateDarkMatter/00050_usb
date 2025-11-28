using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using UsbOverIp.BackendService.Discovery;
using UsbOverIp.Shared.Models;
using Xunit;

namespace UsbOverIp.BackendService.Tests.Unit;

public class DiscoveryBroadcasterTests
{
    private readonly Mock<ILogger<DiscoveryBroadcaster>> _mockLogger;
    private readonly Mock<IOptions<ServerConfiguration>> _mockOptions;

    public DiscoveryBroadcasterTests()
    {
        _mockLogger = new Mock<ILogger<DiscoveryBroadcaster>>();
        _mockOptions = new Mock<IOptions<ServerConfiguration>>();
    }

    [Fact]
    public void Constructor_InitializesWithConfiguration()
    {
        // Arrange
        var config = new ServerConfiguration
        {
            ServerId = Guid.NewGuid(),
            ServerName = "TestServer",
            ApiPort = 50051,
            DiscoveryPort = 50052,
            BroadcastIntervalSeconds = 5
        };
        _mockOptions.Setup(o => o.Value).Returns(config);

        // Act
        var broadcaster = new DiscoveryBroadcaster(_mockLogger.Object, _mockOptions.Object);

        // Assert
        Assert.NotNull(broadcaster);
    }

    [Fact]
    public void BuildAnnouncement_CreatesValidServerAnnouncement()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var config = new ServerConfiguration
        {
            ServerId = serverId,
            ServerName = "TestServer",
            ApiPort = 50051,
            DiscoveryPort = 50052,
            BroadcastIntervalSeconds = 5
        };
        _mockOptions.Setup(o => o.Value).Returns(config);

        var broadcaster = new DiscoveryBroadcaster(_mockLogger.Object, _mockOptions.Object);

        // Act
        var announcement = broadcaster.BuildAnnouncement();

        // Assert
        Assert.NotNull(announcement);
        Assert.Equal("UsbServerAnnouncement", announcement.Type);
        Assert.Equal(serverId, announcement.Id);
        Assert.Equal("TestServer", announcement.Name);
        Assert.Equal(50051, announcement.ApiPort);
        Assert.NotNull(announcement.Version);
    }

    [Fact]
    public async Task StartAsync_DoesNotThrow()
    {
        // Arrange
        var config = new ServerConfiguration
        {
            ServerId = Guid.NewGuid(),
            ServerName = "TestServer",
            ApiPort = 50051,
            DiscoveryPort = 50052,
            BroadcastIntervalSeconds = 5
        };
        _mockOptions.Setup(o => o.Value).Returns(config);

        var broadcaster = new DiscoveryBroadcaster(_mockLogger.Object, _mockOptions.Object);

        // Act & Assert
        await broadcaster.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StopAsync_DoesNotThrow()
    {
        // Arrange
        var config = new ServerConfiguration
        {
            ServerId = Guid.NewGuid(),
            ServerName = "TestServer",
            ApiPort = 50051,
            DiscoveryPort = 50052,
            BroadcastIntervalSeconds = 5
        };
        _mockOptions.Setup(o => o.Value).Returns(config);

        var broadcaster = new DiscoveryBroadcaster(_mockLogger.Object, _mockOptions.Object);

        // Act & Assert
        await broadcaster.StopAsync(CancellationToken.None);
    }
}
