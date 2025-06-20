using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeaconScan
{
    public class FileItem
    {
        public string? Name { get; set; }
        public string? Type { get; set; }// "D" for Directory, "F" for File
        // Property used for zebra striping: true for even rows, false for odd ones.
        public bool IsEven { get; set; }
    }
}
