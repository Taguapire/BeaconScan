using System;
using System.Collections.Generic;
using System.IO;


namespace BeaconScan
{
    public static class LocalFileHelper
    {
        // Método para obtener una lista de archivos en el directorio especificado
        public static List<FileItem> GetLocalFileList(string localPath)
        {
            var fileList = new List<FileItem>();

            try
            {
                if (Directory.Exists(localPath))
                {
                    var entries = Directory.EnumerateFileSystemEntries(localPath);
                    foreach (var entry in entries)
                    {
                        fileList.Add(new FileItem
                        {
                            Name = Path.GetFileName(entry), // Extraer el nombre del archivo/directorio
                            Type = Directory.Exists(entry) ? "D" : "F" // Determinar tipo (Directorio/Archivo)
                        });
                    }
                }
                else
                {
                    throw new DirectoryNotFoundException($"El directorio {localPath} no existe.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener la lista de archivos: {ex.Message}");
            }

            return fileList;
        }


    }
}
