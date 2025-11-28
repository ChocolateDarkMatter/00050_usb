using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using UsbOverIp.Shared.Models;

namespace UsbOverIp.ClientAgent.Services;

/// <summary>
/// Service that listens for server announcements via UDP broadcast and manages discovered servers.
/// </summary>
public class ServerDiscoveryService : IDisposable
{
    private readonly ILogger<ServerDiscoveryService> _logger;
    private readonly int _discoveryPort;
    private readonly ConcurrentDictionary<Guid, ServerInfo> _discoveredServers;
    private UdpClient? _udpClient;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _listenerTask;
    private Task? _timeoutTask;

    private const int ServerTimeoutSeconds = 30;

    public event EventHandler<ServerInfo>? ServerDiscovered;
    public event EventHandler<ServerInfo>? ServerOffline;

    public ServerDiscoveryService(ILogger<ServerDiscoveryService> logger, int discoveryPort = 50052)
    {
        _logger = logger;
        _discoveryPort = discoveryPort;
        _discoveredServers = new ConcurrentDictionary<Guid, ServerInfo>();
    }

    /// <summary>
    /// Starts listening for server announcements.
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting server discovery on port {Port}", _discoveryPort);

        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            // Create socket with ReuseAddress option to allow multiple instances or quick restarts
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.Bind(new IPEndPoint(IPAddress.Any, _discoveryPort));
            _udpClient = new UdpClient { Client = socket };

            _listenerTask = Task.Run(() => ListenForAnnouncementsAsync(_cancellationTokenSource.Token), cancellationToken);
            _timeoutTask = Task.Run(() => MonitorServerTimeoutsAsync(_cancellationTokenSource.Token), cancellationToken);
        }
        catch (SocketException ex)
        {
            _logger.LogError(ex, "Failed to bind to discovery port {Port}", _discoveryPort);
            throw;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops listening for server announcements.
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping server discovery");

        _cancellationTokenSource?.Cancel();

        if (_listenerTask != null)
        {
            await _listenerTask;
        }

        if (_timeoutTask != null)
        {
            await _timeoutTask;
        }

        _udpClient?.Dispose();
        _udpClient = null;
    }

    /// <summary>
    /// Gets a snapshot of all discovered servers.
    /// </summary>
    public IReadOnlyList<ServerInfo> GetDiscoveredServers()
    {
        return _discoveredServers.Values.ToList();
    }

    private async Task ListenForAnnouncementsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Listening for server announcements on port {Port}", _discoveryPort);

        while (!cancellationToken.IsCancellationRequested && _udpClient != null)
        {
            try
            {
                var result = await _udpClient.ReceiveAsync(cancellationToken);
                var announcement = JsonSerializer.Deserialize<ServerAnnouncement>(result.Buffer);

                if (announcement == null || announcement.Type != "UsbServerAnnouncement")
                {
                    _logger.LogWarning("Received invalid announcement from {IP}", result.RemoteEndPoint.Address);
                    continue;
                }

                ProcessAnnouncement(announcement, result.RemoteEndPoint.Address.ToString());
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error receiving announcement");
            }
        }

        _logger.LogInformation("Stopped listening for server announcements");
    }

    private void ProcessAnnouncement(ServerAnnouncement announcement, string ipAddress)
    {
        var isNew = !_discoveredServers.ContainsKey(announcement.Id);

        var serverInfo = _discoveredServers.AddOrUpdate(
            announcement.Id,
            _ => new ServerInfo
            {
                Id = announcement.Id,
                Hostname = announcement.Name,
                IpAddress = ipAddress,
                ApiPort = announcement.ApiPort,
                Version = announcement.Version,
                IsOnline = true,
                LastSeen = DateTime.UtcNow
            },
            (_, existing) =>
            {
                existing.Hostname = announcement.Name;
                existing.IpAddress = ipAddress;
                existing.ApiPort = announcement.ApiPort;
                existing.Version = announcement.Version;
                existing.IsOnline = true;
                existing.LastSeen = DateTime.UtcNow;
                return existing;
            });

        if (isNew)
        {
            _logger.LogInformation(
                "Discovered new server: {ServerName} at {IP}:{Port}",
                announcement.Name,
                ipAddress,
                announcement.ApiPort);

            ServerDiscovered?.Invoke(this, serverInfo);
        }
        else
        {
            _logger.LogDebug("Updated server: {ServerName} at {IP}", announcement.Name, ipAddress);
        }
    }

    private async Task MonitorServerTimeoutsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

                var now = DateTime.UtcNow;
                var timeoutThreshold = now.AddSeconds(-ServerTimeoutSeconds);

                foreach (var kvp in _discoveredServers)
                {
                    var server = kvp.Value;
                    if (server.IsOnline && server.LastSeen < timeoutThreshold)
                    {
                        server.IsOnline = false;
                        _logger.LogWarning(
                            "Server {ServerName} ({IP}) has timed out",
                            server.Hostname,
                            server.IpAddress);

                        ServerOffline?.Invoke(this, server);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error monitoring server timeouts");
            }
        }
    }

    /// <summary>
    /// Test helper to simulate receiving an announcement.
    /// </summary>
    public void SimulateAnnouncementReceived(ServerAnnouncement announcement, string ipAddress)
    {
        ProcessAnnouncement(announcement, ipAddress);
    }

    /// <summary>
    /// Test helper to simulate a server timeout.
    /// </summary>
    public void SimulateServerTimeout(ServerInfo server)
    {
        if (_discoveredServers.TryGetValue(server.Id, out var existing))
        {
            existing.IsOnline = false;
            ServerOffline?.Invoke(this, existing);
        }
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _udpClient?.Dispose();
        GC.SuppressFinalize(this);
    }
}
