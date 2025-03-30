using System.Net;
using System.Net.Sockets;

var gateway_ip = args
    .Select(s => (success: IPAddress.TryParse(s, out var ip), ip))
    .Where(s => s.success)
    .Select(s => s.ip).FirstOrDefault();

gateway_ip ??= GetGatewayIP();

Console.WriteLine($"Gateway: {gateway_ip}");

var ping_tasks = GetSubnetIPs(gateway_ip).Select(PingAddressAsync);

var results = await Task.WhenAll(ping_tasks);

foreach (var (ip, timeout, host, _) in results.Where(r => r.Success).OrderBy(r => r.ip, Comparer<IPAddress>.Create(Ex.CompareTo)))
    Console.WriteLine($"{ip,-15}[{timeout,4}ms] - {host}");

return;

static async Task<(IPAddress ip, long Timeout, string HostName, bool Success)> PingAddressAsync(IPAddress ip)
{
    using var ping = new Ping();
    var result = await ping.SendPingAsync(ip, 500).ConfigureAwait(false);
    if (result.Status != IPStatus.Success)
        return (ip, -1, null!, false);

    try
    {
        var host_entry = await Dns.GetHostEntryAsync(ip).ConfigureAwait(false);
        return (ip, result.RoundtripTime, host_entry.HostName, true);
    }
    catch (SocketException)
    {
        return (ip, result.RoundtripTime, "Unknown", true);
    }

}

static IPAddress GetGatewayIP()
{
    using var ping = new Ping();
    var ip = new IPAddress([77, 88, 55, 242]);
    var reply = ping.Send(ip, 500, [1, 2, 3, 4, 5], new() { Ttl = 1 });

    return reply.Address;
}

static IEnumerable<IPAddress> GetSubnetIPs(IPAddress GatewayIP)
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

    for (var ip = new IPAddress(network_address); ip.CompareTo(end_ip) <= 0; ip = IncrementIPAddress(ip))
        yield return ip;
}

static IPAddress IncrementIPAddress(IPAddress address)
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
}
