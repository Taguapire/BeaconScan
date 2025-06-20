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
        /// Lists the files and directories in a remote folder, omitting "." and "..".
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
                    Console.WriteLine($"Error connecting to the SFTP server: {ex.Message}");
                    throw;
                }
            }

            var remoteFiles = new List<FileItem>();

            // Validate that the remotePath is valid
            if (string.IsNullOrWhiteSpace(remotePath))
            {
                Console.WriteLine("The remote directory is invalid.");
                return remoteFiles;
            }

            // Add ".." only if we are not in the root directory
            if (remotePath != "/")
            {
                remoteFiles.Add(new FileItem
                {
                    Name = "..",
                    Type = "D"
                });
            }

            // List the files in the directory
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
                Console.WriteLine($"Error listing the remote directory: {ex.Message}");
            }

            return remoteFiles;
        }


        /// <summary>
        /// Uploads a file from the local system to the remote server.
        /// </summary>
        public void UploadFile(string localFilePath, string remoteFilePath)
        {
            using (var fileStream = File.OpenRead(localFilePath))
            {
                _sftpClient.UploadFile(fileStream, remoteFilePath);
            }
        }

        /// <summary>
        /// Downloads a file from the remote server to the local system.
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
