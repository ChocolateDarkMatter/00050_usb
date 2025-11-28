namespace UsbOverIp.Shared.Models;

/// <summary>
/// Request to attach a device to a client.
/// </summary>
public class AttachRequest
{
    /// <summary>
    /// IP address of the requesting client
    /// </summary>
    public required string ClientAddress { get; set; }
}
