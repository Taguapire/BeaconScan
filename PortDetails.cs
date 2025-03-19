using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeaconScan
{
    public class PortDetails
    {
        public string? Protocol { get; set; }
        public int PortNumber { get; set; }
        public string? ServiceName { get; set; }
    }
}
