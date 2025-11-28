namespace UsbOverIp.Shared.Models;

/// <summary>
/// Represents a server machine sharing USB devices.
/// </summary>
public class ServerInfo
{
    /// <summary>
    /// Unique server identifier
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Machine hostname
    /// </summary>
    public required string Hostname { get; set; }

    /// <summary>
    /// Server's primary IP address
    /// </summary>
    public required string IpAddress { get; set; }

    /// <summary>
    /// Port for REST API
    /// </summary>
    public required int ApiPort { get; set; }

    /// <summary>
    /// Server software version
    /// </summary>
    public required string Version { get; set; }

    /// <summary>
    /// Whether server is currently reachable (runtime state)
    /// </summary>
    public bool IsOnline { get; set; }

    /// <summary>
    /// Last discovery announcement received (UTC)
    /// </summary>
    public DateTime LastSeen { get; set; }

    /// <summary>
    /// List of managed USB devices (populated on query)
    /// </summary>
    public List<UsbDevice> Devices { get; set; } = new();
}
