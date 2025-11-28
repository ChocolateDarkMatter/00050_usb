namespace UsbOverIp.Shared.Models;

/// <summary>
/// Server endpoint for manual server entry.
/// </summary>
public class ServerEndpoint
{
    /// <summary>
    /// IP address or hostname
    /// </summary>
    public required string Address { get; set; }

    /// <summary>
    /// API port
    /// </summary>
    public required int Port { get; set; }
}

/// <summary>
/// Rule for automatic device attachment.
/// </summary>
public class AutoConnectRule
{
    /// <summary>
    /// Target server ID
    /// </summary>
    public required Guid ServerId { get; set; }

    /// <summary>
    /// Target device bus ID (or "*" for any)
    /// </summary>
    public required string DeviceBusId { get; set; }

    /// <summary>
    /// VID:PID filter (e.g., "046D:C534") (optional)
    /// </summary>
    public string? VendorProductFilter { get; set; }

    /// <summary>
    /// Whether rule is active
    /// </summary>
    public bool Enabled { get; set; }
}

/// <summary>
/// Persistent settings for the client application.
/// </summary>
public class ClientPreferences
{
    /// <summary>
    /// Manually added server addresses
    /// </summary>
    public List<ServerEndpoint> KnownServers { get; set; } = new();

    /// <summary>
    /// Rules for automatic device attachment
    /// </summary>
    public List<AutoConnectRule> AutoConnectRules { get; set; } = new();

    /// <summary>
    /// Minimize to tray on close (default: true)
    /// </summary>
    public bool MinimizeToTray { get; set; } = true;

    /// <summary>
    /// Launch at Windows startup (default: false)
    /// </summary>
    public bool StartWithWindows { get; set; } = false;

    /// <summary>
    /// Logging verbosity level
    /// </summary>
    public string LogLevel { get; set; } = "Information";
}
