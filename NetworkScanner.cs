using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using SharpOpenNat;

namespace BeaconScan
{
    static class NetworkScanner
    {
        // Locate base address
        public static string GetLocalBaseIP()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        string ipStr = ip.ToString();
                        var segments = ipStr.Split('.');
                        if (segments.Length == 4)
                        {
                            return $"{segments[0]}.{segments[1]}.{segments[2]}.0";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error obtaining local base IP: {ex.Message}");
            }
            return "unknown";
        }

        // TCP and UDP port scanning
        public static async Task<List<PortDetails>> ScanPortsAsync(string ip, IEnumerable<int> portNumbers, CancellationToken cancellationToken)
        {
            var openPorts = new List<PortDetails>();
            var portRegistry = new PortRegistry();

            var tasks = new List<Task>();
            foreach (var port in portNumbers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                tasks.Add(Task.Run(async () =>
                {
                    bool isTcpOpen = await IsTcpPortOpenAsync(ip, port, 4000);
                    bool isUdpOpen = await IsUdpPortOpenAsync(ip, port, 4000);

                    if (isTcpOpen || isUdpOpen)
                    {
                        var portInfo = portRegistry.FindByPortNumber(port);

                        // Convert PortInfo object to PortDetails for extended information and display formatting
#pragma warning disable CS8601 // Possible null reference assignment.
                        var portDetails = new PortDetails
                        {
                            Protocol = portInfo.Protocol,
                            PortNumber = portInfo.PortNumber,
                            ServiceName = portInfo.ServiceName
                        };
#pragma warning restore CS8601 // Possible null reference assignment.

                        lock (openPorts)
                        {
                            openPorts.Add(portDetails);
                        }
                    }
                }, cancellationToken));
            }

            await Task.WhenAll(tasks);
            return openPorts;
        }


        // Method to check if a TCP port is open on the target host
        private static async Task<bool> IsTcpPortOpenAsync(string ip, int port, int timeout)
        {
            using (TcpClient tcpClient = new TcpClient())
            {
                try
                {
                    var connectTask = tcpClient.ConnectAsync(ip, port);
                    if (await Task.WhenAny(connectTask, Task.Delay(timeout)) == connectTask)
                    {
                        if (tcpClient.Connected)
                        {
                            return true;
                        }
                    }
                }
                catch (SocketException ex)
                {
                    // Log the error and treat ConnectionRefused as a sign that the service is reachable but not accepting connections
                    Console.WriteLine($"[TCP] Error connecting to {ip}:{port} - {ex.Message}");
                    if (ex.SocketErrorCode == SocketError.ConnectionRefused)
                        return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TCP] Unexpected error for {ip}:{port} - {ex.Message}");
                }
                return false;
            }
        }

        // Method to check if a UDP port is open on the target host
        private static async Task<bool> IsUdpPortOpenAsync(string ip, int port, int timeout)
        {
            using (UdpClient udpClient = new UdpClient())
            {
                try
                {
                    udpClient.Connect(ip, port);

                    // Send an empty packet.
                    byte[] sendBytes = new byte[1];
                    udpClient.Send(sendBytes, sendBytes.Length);

                    // We use the asynchronous version to receive data from the stream without blocking the UI thread
                    var receiveTask = udpClient.ReceiveAsync();

                    if (await Task.WhenAny(receiveTask, Task.Delay(timeout)) == receiveTask)
                    {
                        try
                        {
                            var result = await receiveTask;
                            return (result.Buffer != null && result.Buffer.Length > 0);
                        }
                        catch (SocketException se)
                        {
                            Console.WriteLine($"[UDP] SocketException for {ip}:{port} - {se.Message}");
                            return false;
                        }
                    }
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"[UDP] Error connecting/sending to {ip}:{port} - {ex.Message}");
                }
                catch (ObjectDisposedException ex)
                {
                    Console.WriteLine($"[UDP] ObjectDisposedException for {ip}:{port} - {ex.Message}");
                }
                return false;
            }
        }

        // Network scan to detect active IPs in the target subnet

        public static async Task<List<IpItem>> ScanNetworkAsync(string baseIp, bool useSynScan, CancellationToken cancellationToken)
        {
            var activeIps = new List<IpItem>();

            // Validate if the base IP is valid before proceeding
            if (string.IsNullOrWhiteSpace(baseIp) || !baseIp.Contains("."))
            {
                Console.WriteLine("Invalid base IP provided for network scan.");
                return activeIps;
            }

            // Ensure baseIp ends with a dot (e.g., "192.168.8.")
            if (!baseIp.EndsWith("."))
            {
                baseIp = baseIp.Substring(0, baseIp.LastIndexOf('.') + 1);
            }

            // Step 1: Traditional scan using ICMP Ping to identify reachable IPs
            Console.WriteLine("Performing traditional scan with Ping...");
            var pingResults = await PerformPingScanAsync(baseIp, cancellationToken);
            activeIps.AddRange(pingResults);

            // Step 2: Perform optional SYN scan on specified port (default: 80)
            if (useSynScan)
            {
                Console.WriteLine("Executing SYN Scan...");
                var synScanResults = await PerformSynScanAsync(baseIp, 80, cancellationToken); // Port 80 as example
                activeIps.AddRange(synScanResults);
            }

            // Step 3: Integrate UPnP discovery to identify additional network devices
            Console.WriteLine("Searching for UPnP-enabled devices...");
            var upnpResults = await PerformUPnPDiscoveryAsync(activeIps);
            activeIps.AddRange(pingResults);

            // Remove duplicates in case the same IP was discovered through multiple methods
            activeIps = activeIps.GroupBy(ip => ip.IpAddress)
                                 .Select(group => group.First())
                                 .OrderBy(ip => int.Parse(ip.IpAddress.Split('.').Last()))
                                 .ToList();


            // Resolve hostnames for each detected IP
            foreach (var ipItem in activeIps)
            {
                try
                {
                    var hostEntry = System.Net.Dns.GetHostEntry(ipItem.IpAddress);
                    ipItem.Hostname = hostEntry.HostName;
                }
                catch (Exception)
                {
                    ipItem.Hostname ??= "Unknown"; // Fallback if resolution fails
                }
            }

            // Assign IsEven flag to alternate UI row styling
            for (int i = 0; i < activeIps.Count; i++)
            {
                activeIps[i].IsEven = (i % 2 == 0);
            }

            return activeIps;
        }

        private static async Task<List<IpItem>> PerformPingScanAsync(string baseIp, CancellationToken cancellationToken)
        {
            var activeIps = new List<IpItem>();
            var semaphore = new SemaphoreSlim(50); 
            var tasks = new List<Task>();

            for (int i = 1; i <= 254; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string ip = baseIp + i;

                tasks.Add(Task.Run(async () =>
                {
                    await semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        using (var ping = new System.Net.NetworkInformation.Ping())
                        {
                            var reply = await ping.SendPingAsync(ip, 1500); // Timeout configurado a 1500 ms
                            if (reply.Status == System.Net.NetworkInformation.IPStatus.Success)
                            {
                                lock (activeIps)
                                {
                                    activeIps.Add(new IpItem { IpAddress = ip });
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ping error for {ip}: {ex.Message}");
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, cancellationToken));
            }

            await Task.WhenAll(tasks);
            return activeIps;
        }

        public static async Task<List<IpItem>> PerformSynScanAsync(string baseIp, int port, CancellationToken cancellationToken)
        {
            var activeIps = new List<IpItem>();
            var semaphore = new SemaphoreSlim(50); 
            var tasks = new List<Task>();

            for (int i = 1; i <= 254; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string ip = baseIp + i;

                tasks.Add(Task.Run(async () =>
                {
                    await semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        using (var client = new TcpClient())
                        {
                            var connectTask = client.ConnectAsync(ip, port);
                            if (await Task.WhenAny(connectTask, Task.Delay(1500)) == connectTask) // Timeout de 1500 ms
                            {
                                if (client.Connected)
                                {
                                    lock (activeIps)
                                    {
                                        activeIps.Add(new IpItem { IpAddress = ip, Hostname = "SYN Scan Detected" });
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"SYN Scan error for {ip}:{port}: {ex.Message}");
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, cancellationToken));
            }

            await Task.WhenAll(tasks);
            return activeIps;
        }

        private static async Task<List<IpItem>> PerformUPnPDiscoveryAsync(List<IpItem> currentIps)
        {
            var discoveredIps = new List<IpItem>();

            try
            {
                // Set a 5-second timeout
                using var cts = new CancellationTokenSource(5000);

                // Discover a UPnP device on the network
                var device = await OpenNat.Discoverer.DiscoverDeviceAsync(PortMapper.Upnp, cts.Token);

                Console.WriteLine($"UPnP: Device found at {device}");

                // Get the external IP and convert it to string
                var ipAddress = await device.GetExternalIPAsync();
                var ipAddressStr = ipAddress.ToString();

                // Check if the IP is already in the list (compare as strings)
                if (currentIps.All(ipItem => ipItem.IpAddress != ipAddressStr))
                {
                    discoveredIps.Add(new IpItem
                    {
                        IpAddress = ipAddressStr,
                        Hostname = "UPnP Device" // UPnP devices sometimes don't return a hostname
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during UPnP discovery: {ex.Message}");
            }

            return discoveredIps;
        }
    }
}
