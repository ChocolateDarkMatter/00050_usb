using Microsoft.Extensions.Logging;
using Moq;
using UsbOverIp.ClientAgent.Services;
using UsbOverIp.Shared.Models;
using Xunit;

namespace UsbOverIp.ClientAgent.Tests.Unit;

public class ServerDiscoveryServiceTests
{
    private readonly Mock<ILogger<ServerDiscoveryService>> _mockLogger;

    public ServerDiscoveryServiceTests()
    {
        _mockLogger = new Mock<ILogger<ServerDiscoveryService>>();
    }

    [Fact]
    public void Constructor_InitializesWithPort()
    {
        // Arrange & Act
        var service = new ServerDiscoveryService(_mockLogger.Object, 50052);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public async Task StartAsync_DoesNotThrow()
    {
        // Arrange
        var service = new ServerDiscoveryService(_mockLogger.Object, 50052);

        // Act & Assert
        await service.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StopAsync_DoesNotThrow()
    {
        // Arrange
        var service = new ServerDiscoveryService(_mockLogger.Object, 50052);

        // Act & Assert
        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public void GetDiscoveredServers_ReturnsEmptyListInitially()
    {
        // Arrange
        var service = new ServerDiscoveryService(_mockLogger.Object, 50052);

        // Act
        var servers = service.GetDiscoveredServers();

        // Assert
        Assert.NotNull(servers);
        Assert.Empty(servers);
    }

    [Fact]
    public void ServerDiscovered_EventIsRaised_WhenServerIsDiscovered()
    {
        // Arrange
        var service = new ServerDiscoveryService(_mockLogger.Object, 50052);
        ServerInfo? discoveredServer = null;

        service.ServerDiscovered += (sender, server) =>
        {
            discoveredServer = server;
        };

        // Create test announcement
        var announcement = new ServerAnnouncement
        {
            Type = "UsbServerAnnouncement",
            Id = Guid.NewGuid(),
            Name = "TestServer",
            ApiPort = 50051,
            Version = "1.0.0"
        };

        // Act
        service.SimulateAnnouncementReceived(announcement, "192.168.1.100");

        // Assert
        Assert.NotNull(discoveredServer);
        Assert.Equal(announcement.Id, discoveredServer.Id);
        Assert.Equal(announcement.Name, discoveredServer.Hostname);
        Assert.Equal("192.168.1.100", discoveredServer.IpAddress);
        Assert.Equal(announcement.ApiPort, discoveredServer.ApiPort);
        Assert.True(discoveredServer.IsOnline);
    }

    [Fact]
    public void ServerOffline_EventIsRaised_WhenServerTimesOut()
    {
        // Arrange
        var service = new ServerDiscoveryService(_mockLogger.Object, 50052);
        ServerInfo? offlineServer = null;

        service.ServerOffline += (sender, server) =>
        {
            offlineServer = server;
        };

        // First discover a server
        var announcement = new ServerAnnouncement
        {
            Type = "UsbServerAnnouncement",
            Id = Guid.NewGuid(),
            Name = "TestServer",
            ApiPort = 50051,
            Version = "1.0.0"
        };
        service.SimulateAnnouncementReceived(announcement, "192.168.1.100");

        // Get the discovered server
        var discoveredServer = service.GetDiscoveredServers().First();

        // Act - Simulate timeout
        service.SimulateServerTimeout(discoveredServer);

        // Assert
        Assert.NotNull(offlineServer);
        Assert.Equal(discoveredServer.Id, offlineServer.Id);
        Assert.False(offlineServer.IsOnline);
    }
}
