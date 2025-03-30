using System.Net;

var gateway_ip = args
    .Select(s => (success: IPAddress.TryParse(s, out var ip), ip))
    .Where(s => s.success)
    .Select(s => s.ip).FirstOrDefault();

gateway_ip ??= GetGatewayIP();

var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cancellation.Cancel();
};

var timeout = 500;

var scanner = new Scanner(gateway_ip, timeout);

scanner.PermanentTimeout = 250;

await scanner.ScanAsync(cancellation.Token);

//Console.WriteLine($"Gateway: {gateway_ip}");

//var ping_tasks = gateway_ip.GetSubnetIPs().Select(Scanner.PingAddressAsync);

//var results = await Task.WhenAll(ping_tasks);

//foreach (var (ip, timeout, host, _) in results.Where(r => r.Success).OrderBy(r => r.ip, Comparer<IPAddress>.Create(Ex.CompareTo)))
//    Console.WriteLine($"{ip,-15}[{timeout,4}ms] - {host}");

return;

static IPAddress GetGatewayIP()
{
    using var ping = new Ping();
    var ip = new IPAddress([77, 88, 55, 242]);
    var reply = ping.Send(ip, 500, [1, 2, 3, 4, 5], new() { Ttl = 1 });

    return reply.Address;
}
