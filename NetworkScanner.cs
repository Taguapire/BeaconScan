using System;
using System.Collections.Generic;
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
        public static async Task<List<PortInfo>> ScanPortsAsync(string ip, IEnumerable<int> portNumbers, CancellationToken cancellationToken)
        {
            var openPorts = new List<PortInfo>();
            var portRegistry = new PortRegistry(); // Instancia para obtener información de puertos

            var tasks = new List<Task>();
            foreach (var port in portNumbers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                tasks.Add(Task.Run(async () =>
                {
                    bool isTcpOpen = await IsTcpPortOpenAsync(ip, port, 3000);
                    bool isUdpOpen = await IsUdpPortOpenAsync(ip, port, 3000);

                    if (isTcpOpen || isUdpOpen)
                    {
                        var portDetails = portRegistry.FindByPortNumber(port);
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
        public static async Task<List<string>> ScanNetworkAsync(string baseIp, CancellationToken cancellationToken)
        {
            var activeIps = new List<string>();

            // Validar si la base IP es válida antes de proceder.
            if (string.IsNullOrWhiteSpace(baseIp) || !baseIp.Contains("."))
            {
                Console.WriteLine("Invalid base IP provided for network scan.");
                return activeIps; // Retornar una lista vacía si la base IP no es válida.
            }

            // Nos aseguramos de que baseIp termine con un punto (e.g., "192.168.8.")
            if (!baseIp.EndsWith("."))
            {
                baseIp = baseIp.Substring(0, baseIp.LastIndexOf('.') + 1);
            }

            var tasks = new List<Task>();

            // Se instancia el PortRegistry para obtener los puertos registrados.
            var portRegistry = new PortRegistry();
            var registeredPorts = portRegistry.GetRegisteredPortNumbers();

            for (int i = 1; i <= 254; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string ip = baseIp + i; // Construimos cada dirección IP en el rango basado en la base IP proporcionada.

                tasks.Add(Task.Run(async () =>
                {
                    using (var ping = new System.Net.NetworkInformation.Ping())
                    {
                        bool ipAdded = false;
                        try
                        {
                            // Timeout configurado a 300ms (puede ajustarse según las necesidades).
                            var reply = await ping.SendPingAsync(ip, 300);
                            if (reply.Status == System.Net.NetworkInformation.IPStatus.Success)
                            {
                                lock (activeIps)
                                {
                                    activeIps.Add(ip);
                                }
                                ipAdded = true;
                            }
                        }
                        catch (Exception ex) when (!(ex is OperationCanceledException))
                        {
                            Console.WriteLine($"[Ping] Error for {ip}: {ex.Message}");
                        }
                        // Si no se detecta por ping, intentamos el escaneo de puertos.
                        if (!ipAdded)
                        {
                            var openPorts = await ScanPortsAsync(ip, registeredPorts, cancellationToken);
                            if (openPorts.Count > 0)
                            {
                                lock (activeIps)
                                {
                                    activeIps.Add(ip);
                                }
                            }
                        }
                    }
                }, cancellationToken));
            }

            await Task.WhenAll(tasks);
            return activeIps;
        }
    }
}
