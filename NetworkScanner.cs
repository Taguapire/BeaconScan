using Open.Nat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace BeaconScan
{
    static class NetworkScanner
    {
        // Busca direccion Base
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

        // Escaneo de puertos TCP y UDP
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

                        // Convertimos PortInfo a PortDetails
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


        // Método para verificar si un puerto TCP está abierto
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
                    // Registrar error y tratar ConnectionRefused como indicador de que el servicio está activo.
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

        // Método para verificar si un puerto UDP está abierto
        private static async Task<bool> IsUdpPortOpenAsync(string ip, int port, int timeout)
        {
            using (UdpClient udpClient = new UdpClient())
            {
                try
                {
                    udpClient.Connect(ip, port);

                    // Enviar un paquete vacío.
                    byte[] sendBytes = new byte[1];
                    udpClient.Send(sendBytes, sendBytes.Length);

                    // Usamos la versión asíncrona para recibir datos.
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

        // Escaneo de red para IPs activas
        public static async Task<List<IpItem>> ScanNetworkAsync(string baseIp, bool useSynScan, CancellationToken cancellationToken)
        {
            var activeIps = new List<IpItem>();

            // Validar si la base IP es válida antes de proceder
            if (string.IsNullOrWhiteSpace(baseIp) || !baseIp.Contains("."))
            {
                Console.WriteLine("Invalid base IP provided for network scan.");
                return activeIps;
            }

            // Nos aseguramos de que baseIp termine con un punto (e.g., "192.168.8.")
            if (!baseIp.EndsWith("."))
            {
                baseIp = baseIp.Substring(0, baseIp.LastIndexOf('.') + 1);
            }

            // Paso 1: Escaneo tradicional usando Ping
            Console.WriteLine("Realizando escaneo tradicional con Ping...");
            var pingResults = await PerformPingScanAsync(baseIp, cancellationToken);
            activeIps.AddRange(pingResults);

            // Paso 2: Ejecutar SYN Scan opcional
            if (useSynScan)
            {
                Console.WriteLine("Ejecutando SYN Scan...");
                var synScanResults = await PerformSynScanAsync(baseIp, 80, cancellationToken); // Puerto 80 como ejemplo
                activeIps.AddRange(synScanResults);
            }

            // Paso 3: Integración con UPnP
            Console.WriteLine("Buscando dispositivos UPnP...");
            var upnpResults = await PerformUPnPDiscoveryAsync(activeIps);
            activeIps.AddRange(upnpResults);

            // Eliminar duplicados (por si alguna IP aparece en varios métodos)
            activeIps = activeIps.GroupBy(ip => ip.IpAddress)
                                 .Select(group => group.First())
                                 .OrderBy(ip => int.Parse(ip.IpAddress.Split('.').Last()))
                                 .ToList();

            // Resolver hostnames para cada IP detectada
            foreach (var ipItem in activeIps)
            {
                try
                {
                    var hostEntry = System.Net.Dns.GetHostEntry(ipItem.IpAddress);
                    ipItem.Hostname = hostEntry.HostName;
                }
                catch (Exception)
                {
                    ipItem.Hostname = ipItem.Hostname ?? "Unknown"; // Si no se puede resolver, establecer "Unknown"
                }
            }

            // Asignar la propiedad IsEven para alternar colores en la UI
            for (int i = 0; i < activeIps.Count; i++)
            {
                activeIps[i].IsEven = (i % 2 == 0);
            }

            return activeIps;
        }

        private static async Task<List<IpItem>> PerformPingScanAsync(string baseIp, CancellationToken cancellationToken)
        {
            var activeIps = new List<IpItem>();
            var semaphore = new SemaphoreSlim(50); // Limitar a 50 tareas concurrentes
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
            var semaphore = new SemaphoreSlim(50); // Limitar a 50 tareas concurrentes
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
                var nat = new NatDiscoverer();
                var cts = new CancellationTokenSource(5000); // Timeout de 5 segundos
                var device = await nat.DiscoverDeviceAsync(PortMapper.Upnp, cts);

                Console.WriteLine($"UPnP: Dispositivo encontrado en {device}");

                // Agregar el dispositivo UPnP a la lista si no está ya incluido
                var ipAddress = device.GetExternalIPAsync().Result.ToString(); // Obtiene la IP externa del dispositivo
                if (currentIps.All(ipItem => ipItem.IpAddress != ipAddress))
                {
                    discoveredIps.Add(new IpItem
                    {
                        IpAddress = ipAddress,
                        Hostname = "UPnP Device" // Los dispositivos UPnP no siempre tienen un hostname definido
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en descubrimiento UPnP: {ex.Message}");
            }

            return discoveredIps;
        }
    }
}
