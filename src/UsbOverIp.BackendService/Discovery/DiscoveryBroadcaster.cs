using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UsbOverIp.Shared.Models;

namespace UsbOverIp.BackendService.Discovery;

/// <summary>
/// Background service that broadcasts server availability on the local network via UDP.
/// </summary>
public class DiscoveryBroadcaster : BackgroundService
{
    private readonly ILogger<DiscoveryBroadcaster> _logger;
    private readonly ServerConfiguration _config;
    private UdpClient? _udpClient;

    public DiscoveryBroadcaster(
        ILogger<DiscoveryBroadcaster> logger,
        IOptions<ServerConfiguration> config)
    {
        _logger = logger;
        _config = config.Value;
    }

    /// <summary>
    /// Builds a server announcement message for broadcasting.
    /// </summary>
    public ServerAnnouncement BuildAnnouncement()
    {
        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "1.0.0";

        return new ServerAnnouncement
        {
            Type = "UsbServerAnnouncement",
            Id = _config.ServerId,
            Name = _config.ServerName,
            ApiPort = _config.ApiPort,
            Version = version
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Discovery Broadcaster starting on port {Port}", _config.DiscoveryPort);

        try
        {
            _udpClient = new UdpClient();
            _udpClient.EnableBroadcast = true;

            var broadcastEndpoint = new IPEndPoint(IPAddress.Broadcast, _config.DiscoveryPort);
            var announcement = BuildAnnouncement();
            var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(announcement);

            _logger.LogInformation(
                "Broadcasting server {ServerName} ({ServerId}) on port {Port} every {Interval} seconds",
                _config.ServerName,
                _config.ServerId,
                _config.DiscoveryPort,
                _config.BroadcastIntervalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _udpClient.SendAsync(jsonBytes, jsonBytes.Length, broadcastEndpoint);
                    _logger.LogDebug("Broadcast sent: {ServerName} on port {ApiPort}",
                        announcement.Name, announcement.ApiPort);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send broadcast announcement");
                }

                await Task.Delay(
                    TimeSpan.FromSeconds(_config.BroadcastIntervalSeconds),
                    stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Discovery broadcaster encountered an error");
        }
        finally
        {
            _udpClient?.Dispose();
            _logger.LogInformation("Discovery Broadcaster stopped");
        }
    }

    public override void Dispose()
    {
        _udpClient?.Dispose();
        base.Dispose();
    }
}
