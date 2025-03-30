using System.Net;
using System.Net.Sockets;

namespace NetworkScanner;

internal static class Ex
{
    public static int CompareTo(this IPAddress address, IPAddress other)
    {
        var address_bytes = address.GetAddressBytes();
        var other_bytes = other.GetAddressBytes();

        for (var i = 0; i < address_bytes.Length; i++)
            if (address_bytes[i].CompareTo(other_bytes[i]) is not 0 and var comparison)
                return comparison;

        return 0;
    }

    public static IPAddress IncrementIPAddress(this IPAddress address)
    {
        var address_bytes = address.GetAddressBytes();
        for (var i = address_bytes.Length - 1; i >= 0; i--)
            if (address_bytes[i] >= 255)
                address_bytes[i] = 0;
            else
            {
                address_bytes[i]++;
                break;
            }

        return new(address_bytes);
    }

    public static IEnumerable<IPAddress> GetSubnetIPs(this IPAddress GatewayIP)
    {
        NetworkInterface? network_interface = null;
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up)
                continue;

            var properties = ni.GetIPProperties();
            var gateway_addresses_info = properties.GatewayAddresses;

            foreach (var address in gateway_addresses_info.Select(a => a.Address))
            {
                if (!address.Equals(GatewayIP))
                    continue;

                network_interface = ni;
                break;
            }

            if (network_interface is not null)
                break;
        }

        if (network_interface is null)
            throw new InvalidOperationException("Network interface not found for the given gateway IP.");

        var unicast_address = network_interface
            .GetIPProperties()
            .UnicastAddresses
            .FirstOrDefault(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork);

        if (unicast_address is null)
            throw new InvalidOperationException("Unicast address not found for the network interface.");

        var subnet_mask = unicast_address.IPv4Mask;
        var ip_address = unicast_address.Address;

        var ip_bytes = ip_address.GetAddressBytes();
        var mask_bytes = subnet_mask.GetAddressBytes();

        var network_address = new byte[ip_bytes.Length];
        for (var i = 0; i < network_address.Length; i++)
            network_address[i] = (byte)(ip_bytes[i] & mask_bytes[i]);

        var broadcast_address = new byte[ip_bytes.Length];
        for (var i = 0; i < broadcast_address.Length; i++)
            broadcast_address[i] = (byte)(network_address[i] | ~mask_bytes[i]);

        var end_ip = new IPAddress(broadcast_address);

        for (var ip = new IPAddress(network_address).IncrementIPAddress(); ip.CompareTo(end_ip) < 0; ip = ip.IncrementIPAddress())
            yield return ip;
    }

    public static Task WhenAll(this IEnumerable<Task> tasks) => Task.WhenAll(tasks);

    public static Task<T[]> WhenAll<T>(this IEnumerable<Task<T>> tasks) => Task.WhenAll(tasks);
}