namespace UsbOverIp.Shared.Models;

/// <summary>
/// UDP broadcast payload for server discovery.
/// </summary>
public class ServerAnnouncement
{
    /// <summary>
    /// Message type identifier (always "UsbServerAnnouncement")
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// Server's unique identifier
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Server's hostname/display name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Port for REST API
    /// </summary>
    public required int ApiPort { get; set; }

    /// <summary>
    /// Server software version
    /// </summary>
    public required string Version { get; set; }
}
