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
        public string? Type { get; set; } // "D" para Directorio, "F" para Archivo

        // Propiedad para zebra striping: true para filas pares, false para impares.
        public bool IsEven { get; set; }
    }
}
