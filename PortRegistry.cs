using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                new PortInfo { Protocol = "TCP", PortNumber = 25, ServiceName = "SMTP" },
                new PortInfo { Protocol = "TCP", PortNumber = 53, ServiceName = "DNS" },
                new PortInfo { Protocol = "TCP", PortNumber = 80, ServiceName = "HTTP" },
                new PortInfo { Protocol = "TCP", PortNumber = 443, ServiceName = "HTTPS" },
                new PortInfo { Protocol = "TCP", PortNumber = 3306, ServiceName = "MySQL" },
                new PortInfo { Protocol = "TCP", PortNumber = 5432, ServiceName = "PostgreSQL" },
                new PortInfo { Protocol = "TCP", PortNumber = 8080, ServiceName = "HTTP Proxy" },

                // Puertos específicos para cámaras y video
                new PortInfo { Protocol = "TCP", PortNumber = 37777, ServiceName = "Dahua Camera Service" },
                new PortInfo { Protocol = "TCP/UDP", PortNumber = 554, ServiceName = "RTSP (Streaming)" },
                new PortInfo { Protocol = "TCP", PortNumber = 8000, ServiceName = "HTTP Alternate (Cámaras)" },
                new PortInfo { Protocol = "UDP", PortNumber = 3702, ServiceName = "WS-Discovery (ONVIF)" },
                // Agrega más puertos específicos si lo consideras necesario...
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

        // Método para agregar nuevos puertos al registro dinámicamente (opcional)
        public void AddPort(string protocol, int portNumber, string serviceName)
        {
            // Evita duplicados antes de agregar
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

        // Método para obtener todos los puertos en el registro (opcional, si necesitas la lista completa)
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
        public string? Protocol { get; set; } // Ahora permite null
        public int PortNumber { get; set; }
        public string? ServiceName { get; set; } // Ahora permite null
    }
}
