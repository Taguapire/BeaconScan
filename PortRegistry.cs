using System;
using System.Collections.Generic;
using System.Linq;

namespace BeaconScan
{
    // Clase encargada de manejar el registro de puertos
    public class PortRegistry
    {
        // Lista privada que almacena la información de los puertos
        private readonly List<PortInfo> ports;

        // Constructor que inicializa el registro con los puertos predefinidos
        public PortRegistry()
        {
            ports = new List<PortInfo>
            {
                // Puertos comunes
                new PortInfo { Protocol = "TCP", PortNumber = 21, ServiceName = "FTP" },
                new PortInfo { Protocol = "TCP", PortNumber = 22, ServiceName = "SSH" },
                new PortInfo { Protocol = "TCP", PortNumber = 23, ServiceName = "Telnet" },
                new PortInfo { Protocol = "TCP", PortNumber = 53, ServiceName = "DNS" },
                new PortInfo { Protocol = "TCP", PortNumber = 80, ServiceName = "HTTP" },
                new PortInfo { Protocol = "TCP", PortNumber = 135, ServiceName = "RPC" },
                new PortInfo { Protocol = "TCP", PortNumber = 139, ServiceName = "NetBIOS" },
                new PortInfo { Protocol = "TCP", PortNumber = 143, ServiceName = "IMAP" },
                new PortInfo { Protocol = "TCP", PortNumber = 443, ServiceName = "HTTPS" },
                new PortInfo { Protocol = "TCP", PortNumber = 8080, ServiceName = "HTTP Proxy" },

                // Puertos específicos para cámaras y video
                new PortInfo { Protocol = "TCP/UDP", PortNumber = 554, ServiceName = "RTSP (Streaming)" },
                new PortInfo { Protocol = "TCP", PortNumber = 37777, ServiceName = "Dahua Camera Service" },
                new PortInfo { Protocol = "TCP", PortNumber = 8000, ServiceName = "HTTP Alternate (Cámaras)" },
                new PortInfo { Protocol = "UDP", PortNumber = 3702, ServiceName = "WS-Discovery (ONVIF)" },

                // Puertos específicos para bases de datos
                new PortInfo { Protocol = "TCP", PortNumber = 5432, ServiceName = "PostgreSQL" },
                new PortInfo { Protocol = "TCP", PortNumber = 3306, ServiceName = "MySQL" },
                new PortInfo { Protocol = "TCP", PortNumber = 1521, ServiceName = "Oracle DB" },
                new PortInfo { Protocol = "TCP", PortNumber = 1433, ServiceName = "Microsoft SQL Server" },
                new PortInfo { Protocol = "UDP", PortNumber = 1434, ServiceName = "Microsoft SQL Monitor" },
                new PortInfo { Protocol = "TCP", PortNumber = 50000, ServiceName = "IBM Db2 (Puertos Predeterminados)" },
                new PortInfo { Protocol = "TCP", PortNumber = 50001, ServiceName = "IBM Db2 (Alternativo)" },

                // Puertos específicos para escritorio remoto
                new PortInfo { Protocol = "TCP", PortNumber = 3389, ServiceName = "RDP" },
                new PortInfo { Protocol = "TCP", PortNumber = 5938, ServiceName = "TeamViewer" },
                new PortInfo { Protocol = "TCP", PortNumber = 5900, ServiceName = "VNC" },
                new PortInfo { Protocol = "UDP", PortNumber = 3478, ServiceName = "TeamViewer Relay" },
                new PortInfo { Protocol = "TCP", PortNumber = 6568, ServiceName = "AnyDesk Direct Connection" },

                // Puertos industriales y adicionales
                new PortInfo { Protocol = "TCP", PortNumber = 502, ServiceName = "Modbus (Automatización Industrial)" },
                new PortInfo { Protocol = "UDP", PortNumber = 162, ServiceName = "SNMP Trap" },
                new PortInfo { Protocol = "TCP", PortNumber = 1194, ServiceName = "OpenVPN" },
                new PortInfo { Protocol = "UDP", PortNumber = 123, ServiceName = "NTP" },
                new PortInfo { Protocol = "TCP", PortNumber = 25, ServiceName = "SMTP" },
                new PortInfo { Protocol = "TCP", PortNumber = 110, ServiceName = "POP3" },
                new PortInfo { Protocol = "TCP", PortNumber = 445, ServiceName = "Microsoft-DS (SMB)" },

                // Puertos para servicios modernos y específicos
                new PortInfo { Protocol = "TCP", PortNumber = 465, ServiceName = "SMTP over SSL" },
                new PortInfo { Protocol = "TCP", PortNumber = 993, ServiceName = "IMAP over SSL" },
                new PortInfo { Protocol = "TCP", PortNumber = 995, ServiceName = "POP3 over SSL" },
                new PortInfo { Protocol = "TCP", PortNumber = 2049, ServiceName = "NFS" },
                new PortInfo { Protocol = "TCP", PortNumber = 8883, ServiceName = "MQTT over SSL (IoT)" },

                // Puertos adicionales para sistemas y redes
                new PortInfo { Protocol = "UDP", PortNumber = 69, ServiceName = "TFTP" },
            };
        }

        // Método para buscar un puerto por número y devolver su información
        public PortInfo FindByPortNumber(int portNumber)
        {
            return ports.FirstOrDefault(p => p.PortNumber == portNumber)
                   ?? new PortInfo
                   {
                       Protocol = "Unknown",
                       PortNumber = portNumber,
                       ServiceName = "Unknown Service"
                   };
        }

        // Método para agregar nuevos puertos al registro dinámicamente
        public void AddPort(string protocol, int portNumber, string serviceName)
        {
            if (!ports.Any(p => p.PortNumber == portNumber))
            {
                ports.Add(new PortInfo
                {
                    Protocol = protocol,
                    PortNumber = portNumber,
                    ServiceName = serviceName
                });
            }
        }

        // Método para obtener todos los puertos en el registro
        public IEnumerable<PortInfo> GetAllPorts()
        {
            return ports;
        }

        public IEnumerable<int> GetRegisteredPortNumbers()
        {
            return ports.Select(p => p.PortNumber);
        }
    }

    // Clase PortInfo para modelar la información de cada puerto
    public class PortInfo
    {
        public string? Protocol { get; set; } // Ahora requiere valor
        public int PortNumber { get; set; }
        public string? ServiceName { get; set; } // Ahora requiere valor
    }
}