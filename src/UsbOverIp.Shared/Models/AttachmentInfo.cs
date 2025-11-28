namespace UsbOverIp.Shared.Models;

/// <summary>
/// Current state of an attachment connection.
/// </summary>
public enum AttachmentState
{
    /// <summary>
    /// Attachment requested, in progress
    /// </summary>
    Pending,

    /// <summary>
    /// Device actively attached and usable
    /// </summary>
    Active,

    /// <summary>
    /// Detachment in progress
    /// </summary>
    Disconnecting,

    /// <summary>
    /// Attachment failed (with error info)
    /// </summary>
    Failed
}

/// <summary>
/// Represents an active connection between a client and a USB device.
/// </summary>
public class AttachmentInfo
{
    /// <summary>
    /// Bus ID of the attached device
    /// </summary>
    public required string DeviceBusId { get; set; }

    /// <summary>
    /// IP address of the client
    /// </summary>
    public required string ClientIpAddress { get; set; }

    /// <summary>
    /// ID of the server hosting the device
    /// </summary>
    public required Guid ServerId { get; set; }

    /// <summary>
    /// When attachment was established (UTC)
    /// </summary>
    public DateTime AttachedAt { get; set; }

    /// <summary>
    /// Current connection state
    /// </summary>
    public AttachmentState State { get; set; }
}
