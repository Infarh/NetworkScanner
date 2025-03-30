using System.Net;
using System.Net.Sockets;

namespace NetworkScanner;

internal sealed class Pinger(IPAddress ip, int Timeout) : IDisposable
{
    private static readonly byte[] __Buffer = new byte[32];

    private static readonly PingOptions __Options = new() { Ttl = 1 };

    private readonly Ping _Ping = new();

    private readonly TimeSpan _Timeout = TimeSpan.FromMilliseconds(Timeout);

    private readonly string _IpString = ip.ToString();
    private Task? _UpdateHostNameTask;

    public string? HostName { get; private set; }

    public IPAddress IP => ip;

    public async Task<(IPAddress ip, long? timeout)> PingAsync(CancellationToken Cancel)
    {
        var result = await _Ping
            .SendPingAsync(ip, _Timeout, __Buffer, __Options, Cancel)
            .ConfigureAwait(false);

        if (result.Status != IPStatus.Success)
            return (ip, null);

        _UpdateHostNameTask ??= UpdateHostNameAsync(Cancel);

        return (ip, result.RoundtripTime);
    }

    private async Task UpdateHostNameAsync(CancellationToken Cancel)
    {
        HostName = await GetHostNameAsync(Cancel).ConfigureAwait(false);
        _UpdateHostNameTask = null;
    }

    public async Task<string?> GetHostNameAsync(CancellationToken Cancel)
    {
        try
        {
            var host_entry = await Dns.GetHostEntryAsync(_IpString, Cancel).ConfigureAwait(false);
            return host_entry.HostName;
        }
        catch (SocketException)
        {
            return null;
        }
    }

    public override string ToString() => _IpString;

    public void Dispose() => _Ping.Dispose();
}