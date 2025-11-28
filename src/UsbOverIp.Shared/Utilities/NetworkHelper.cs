using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace UsbOverIp.Shared.Utilities;

/// <summary>
/// Helper utilities for network operations.
/// </summary>
public static class NetworkHelper
{
    /// <summary>
    /// Gets all local IPv4 addresses for active network interfaces.
    /// </summary>
    /// <returns>List of local IP addresses</returns>
    public static List<IPAddress> GetLocalIPv4Addresses()
    {
        var addresses = new List<IPAddress>();

        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up)
                continue;

            var ipProps = ni.GetIPProperties();
            foreach (var uip in ipProps.UnicastAddresses)
            {
                if (uip.Address.AddressFamily == AddressFamily.InterNetwork)
                {
                    addresses.Add(uip.Address);
                }
            }
        }

        return addresses;
    }

    /// <summary>
    /// Gets the primary local IPv4 address (first non-loopback address).
    /// </summary>
    /// <returns>Primary IP address or loopback if none found</returns>
    public static IPAddress GetPrimaryIPv4Address()
    {
        var addresses = GetLocalIPv4Addresses();
        return addresses.FirstOrDefault(ip => !IPAddress.IsLoopback(ip)) ?? IPAddress.Loopback;
    }

    /// <summary>
    /// Calculates the broadcast address for a given IP and subnet mask.
    /// </summary>
    /// <param name="address">IP address</param>
    /// <param name="subnetMask">Subnet mask</param>
    /// <returns>Broadcast address</returns>
    public static IPAddress GetSubnetBroadcastAddress(IPAddress address, IPAddress subnetMask)
    {
        var ipBytes = address.GetAddressBytes();
        var maskBytes = subnetMask.GetAddressBytes();
        var broadcastBytes = new byte[ipBytes.Length];

        for (int i = 0; i < ipBytes.Length; i++)
        {
            broadcastBytes[i] = (byte)(ipBytes[i] | ~maskBytes[i]);
        }

        return new IPAddress(broadcastBytes);
    }

    /// <summary>
    /// Gets all broadcast addresses for active network interfaces.
    /// </summary>
    /// <returns>List of broadcast addresses with their source interfaces</returns>
    public static List<(IPAddress LocalAddress, IPAddress BroadcastAddress)> GetBroadcastAddresses()
    {
        var broadcasts = new List<(IPAddress, IPAddress)>();

        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up)
                continue;

            if (!ni.SupportsMulticast)
                continue;

            var ipProps = ni.GetIPProperties();
            var ipv4Props = ipProps.GetIPv4Properties();

            if (ipv4Props == null)
                continue;

            foreach (var uip in ipProps.UnicastAddresses)
            {
                if (uip.Address.AddressFamily == AddressFamily.InterNetwork && uip.IPv4Mask != null)
                {
                    var broadcast = GetSubnetBroadcastAddress(uip.Address, uip.IPv4Mask);
                    broadcasts.Add((uip.Address, broadcast));
                }
            }
        }

        return broadcasts;
    }

    /// <summary>
    /// Gets the hostname of the local machine.
    /// </summary>
    /// <returns>Machine hostname</returns>
    public static string GetHostname()
    {
        return Dns.GetHostName();
    }
}
