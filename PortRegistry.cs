using System;
using System.Collections.Generic;
using System.Linq;

namespace BeaconScan
{
    // Class responsible for managing the port registry
    public class PortRegistry
    {
        // Private list that stores port information
        private readonly List<PortInfo> ports;

        // Constructor that initializes the registry with predefined ports
        public PortRegistry()
        {
            ports = new List<PortInfo>
        {
            // Common ports
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

            // Ports specific to cameras and video
            new PortInfo { Protocol = "TCP/UDP", PortNumber = 554, ServiceName = "RTSP (Streaming)" },
            new PortInfo { Protocol = "TCP", PortNumber = 37777, ServiceName = "Dahua Camera Service" },
            new PortInfo { Protocol = "TCP", PortNumber = 8000, ServiceName = "HTTP Alternate (Cameras)" },
            new PortInfo { Protocol = "UDP", PortNumber = 3702, ServiceName = "WS-Discovery (ONVIF)" },

            // Ports specific to databases
            new PortInfo { Protocol = "TCP", PortNumber = 5432, ServiceName = "PostgreSQL" },
            new PortInfo { Protocol = "TCP", PortNumber = 3306, ServiceName = "MySQL" },
            new PortInfo { Protocol = "TCP", PortNumber = 1521, ServiceName = "Oracle DB" },
            new PortInfo { Protocol = "TCP", PortNumber = 1433, ServiceName = "Microsoft SQL Server" },
            new PortInfo { Protocol = "UDP", PortNumber = 1434, ServiceName = "Microsoft SQL Monitor" },
            new PortInfo { Protocol = "TCP", PortNumber = 50000, ServiceName = "IBM Db2 (Default Ports)" },
            new PortInfo { Protocol = "TCP", PortNumber = 50001, ServiceName = "IBM Db2 (Alternate)" },

            // Ports specific to remote desktop
            new PortInfo { Protocol = "TCP", PortNumber = 3389, ServiceName = "RDP" },
            new PortInfo { Protocol = "TCP", PortNumber = 5938, ServiceName = "TeamViewer" },
            new PortInfo { Protocol = "TCP", PortNumber = 5900, ServiceName = "VNC" },
            new PortInfo { Protocol = "UDP", PortNumber = 3478, ServiceName = "TeamViewer Relay" },
            new PortInfo { Protocol = "TCP", PortNumber = 6568, ServiceName = "AnyDesk Direct Connection" },

            // Industrial and additional ports
            new PortInfo { Protocol = "TCP", PortNumber = 502, ServiceName = "Modbus (Industrial Automation)" },
            new PortInfo { Protocol = "UDP", PortNumber = 162, ServiceName = "SNMP Trap" },
            new PortInfo { Protocol = "TCP", PortNumber = 1194, ServiceName = "OpenVPN" },
            new PortInfo { Protocol = "UDP", PortNumber = 123, ServiceName = "NTP" },
            new PortInfo { Protocol = "TCP", PortNumber = 25, ServiceName = "SMTP" },
            new PortInfo { Protocol = "TCP", PortNumber = 110, ServiceName = "POP3" },
            new PortInfo { Protocol = "TCP", PortNumber = 445, ServiceName = "Microsoft-DS (SMB)" },

            // Modern and specific services
            new PortInfo { Protocol = "TCP", PortNumber = 465, ServiceName = "SMTP over SSL" },
            new PortInfo { Protocol = "TCP", PortNumber = 993, ServiceName = "IMAP over SSL" },
            new PortInfo { Protocol = "TCP", PortNumber = 995, ServiceName = "POP3 over SSL" },
            new PortInfo { Protocol = "TCP", PortNumber = 2049, ServiceName = "NFS" },
            new PortInfo { Protocol = "TCP", PortNumber = 8883, ServiceName = "MQTT over SSL (IoT)" },

            // Additional ports for systems and networks
            new PortInfo { Protocol = "UDP", PortNumber = 69, ServiceName = "TFTP" },

            // Printer-related ports
            new PortInfo { Protocol = "TCP", PortNumber = 515, ServiceName = "LPD (Line Printer Daemon)" },
            new PortInfo { Protocol = "TCP", PortNumber = 631, ServiceName = "IPP (Internet Printing Protocol)" },
            new PortInfo { Protocol = "UDP", PortNumber = 161, ServiceName = "SNMP" },
            new PortInfo { Protocol = "UDP", PortNumber = 162, ServiceName = "SNMP Trap" },
            new PortInfo { Protocol = "UDP", PortNumber = 1900, ServiceName = "SSDP" },
            new PortInfo { Protocol = "TCP", PortNumber = 9100, ServiceName = "RAW (Printing Protocol)" },
            new PortInfo { Protocol = "UDP", PortNumber = 3702, ServiceName = "WS-Discovery" },
        };
        }

        // Method to search for a port by number and return its information
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

        // Method to dynamically add new ports to the registry
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

        // Method to get all ports in the registry
        public IEnumerable<PortInfo> GetAllPorts()
        {
            return ports;
        }

        public IEnumerable<int> GetRegisteredPortNumbers()
        {
            return ports.Select(p => p.PortNumber);
        }
    }

    // PortInfo class to model each port’s information
    public class PortInfo
    {
        public string? Protocol { get; set; } // Now requires a value
        public int PortNumber { get; set; }
        public string? ServiceName { get; set; } // Now requires a value
    }
}