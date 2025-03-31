using System.Net;
using System.Net.Sockets;

namespace NetworkScanner;

internal sealed class Scanner
{
    private readonly Pinger[] _Pingers;

    public Scanner(IPAddress GatewayIP, int Timeout)
    {
        var pingers = new List<Pinger>(255);
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var ip in GatewayIP.EnumSubnetIPs())
            pingers.Add(new(ip, Timeout));
        _Pingers = [.. pingers];
    }

    public int PermanentTimeout { get; set; }

    public Task ScanAsync(CancellationToken Cancel) => PermanentTimeout <= 0
        ? SingleScanAsync(Cancel)
        : ScanLoopAsync(PermanentTimeout, Cancel);

    private static readonly Comparer<IPAddress> __IpComparer = Comparer<IPAddress>.Create(Ex.CompareTo);

    private async Task SingleScanAsync(CancellationToken Cancel)
    {
        var printer = Printer.Instance;
        printer.Clear();

        var pings = _Pingers.Select(p => p.PingAsync(Cancel));

        var ping_results = await pings.WhenAll().ConfigureAwait(false);

        foreach (var (ip, timeout) in ping_results.Where(p => p.timeout >= 0).OrderBy(p => p.ip, __IpComparer))
        {
            var pinger = _PingersPool[ip];
            var host = pinger.HostName;
            var line = printer.WriteLine(host is null ? $"[{timeout,4}ms]{ip}" : $"[{timeout,4}ms]{ip} - {host}");
        }

        printer.EndPrint();
    }

    private async Task ScanLoopAsync(int Timeout, CancellationToken Cancel)
    {
        while (!Cancel.IsCancellationRequested)
        {
            await SingleScanAsync(Cancel).ConfigureAwait(false);
            await Task.Delay(Timeout, Cancel).ConfigureAwait(false);
        }

        Cancel.ThrowIfCancellationRequested();
    }

    public static async Task<(IPAddress ip, long Timeout, string HostName, bool Success)> PingAddressAsync(IPAddress ip)
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
}