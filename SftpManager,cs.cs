using System;
using System.IO;
using System.Collections.Generic;
using SSHNET = Renci.SshNet;
using System.Threading.Tasks;
using Renci.SshNet.Sftp;
using System.Linq;

namespace BeaconScan
{
    public class SftpManager : IDisposable
    {
        private SSHNET.SftpClient _sftpClient;
        private bool _disposed = false;

        public SftpManager(string host, string username, string password)
        {
            _sftpClient = new SSHNET.SftpClient(host, username, password);
        }

        public async Task ConnectAsync()
        {
            await Task.Run(() => Connect());
        }

        public async Task DisconnectAsync()
        {
            await Task.Run(() => Disconnect());
        }

        public async Task<List<FileItem>> GetRemoteFileListAsync(string remotePath)
        {
            return await Task.Run(() => GetRemoteFileList(remotePath));
        }


        public async Task UploadFileAsync(string localFilePath, string remoteFilePath)
        {
            await Task.Run(() => UploadFile(localFilePath, remoteFilePath));
        }

        public async Task DownloadFileAsync(string remoteFilePath, string localFilePath)
        {
            await Task.Run(() => DownloadFile(remoteFilePath, localFilePath));
        }

        public void Connect()
        {
            if (!_sftpClient.IsConnected)
            {
                _sftpClient.Connect();
            }
        }

        public void Disconnect()
        {
            if (_sftpClient.IsConnected)
            {
                _sftpClient.Disconnect();
            }
        }


        /// <summary>
        /// Lista los archivos y carpetas en un directorio remoto, omitiendo "." y "..".
        /// </summary>
        public List<FileItem> GetRemoteFileList(string remotePath)
        {
            if (!_sftpClient.IsConnected)
            {
                try
                {
                    Connect();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al conectar al servidor SFTP: {ex.Message}");
                    throw;
                }
            }

            var remoteFiles = new List<FileItem>();

            // Validar que el remotePath es válido
            if (string.IsNullOrWhiteSpace(remotePath))
            {
                Console.WriteLine("El directorio remoto es inválido.");
                return remoteFiles;
            }

            // Agregar ".." sólo si no estamos en la raíz
            if (remotePath != "/")
            {
                remoteFiles.Add(new FileItem
                {
                    Name = "..",
                    Type = "D"
                });
            }

            // Listar los archivos del directorio
            try
            {
                var files = _sftpClient.ListDirectory(remotePath).ToList();
                foreach (var file in files)
                {
                    if (!string.IsNullOrEmpty(file.Name) && file.Name != "." && file.Name != "..")
                    {
                        remoteFiles.Add(new FileItem
                        {
                            Name = file.Name,
                            Type = file.IsDirectory ? "D" : "F"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al listar el directorio remoto: {ex.Message}");
            }

            return remoteFiles;
        }






        /// <summary>
        /// Sube un archivo desde el sistema local al servidor remoto.
        /// </summary>
        public void UploadFile(string localFilePath, string remoteFilePath)
        {
            using (var fileStream = File.OpenRead(localFilePath))
            {
                _sftpClient.UploadFile(fileStream, remoteFilePath);
            }
        }

        /// <summary>
        /// Descarga un archivo del servidor remoto al sistema local.
        /// </summary>
        public void DownloadFile(string remoteFilePath, string localFilePath)
        {
            using (var fileStream = File.Create(localFilePath))
            {
                _sftpClient.DownloadFile(remoteFilePath, fileStream);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_sftpClient.IsConnected)
                    Disconnect();
                _sftpClient.Dispose();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}
