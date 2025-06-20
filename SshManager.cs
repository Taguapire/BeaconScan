using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SSHNET = Renci.SshNet;

namespace BeaconScan
{
    public class SshManager : IDisposable
    {
        private SSHNET.SshClient _sshClient;
        // Marked as nullable because they are initialized later
        private SSHNET.ShellStream? _shellStream;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _disposed = false;

        public SshManager(string host, string username, string password)
        {
            _sshClient = new SSHNET.SshClient(host, username, password);
        }

        public void Connect()
        {
            if (!_sshClient.IsConnected)
            {
                _sshClient.Connect();
            }
        }

        public void Disconnect()
        {
            if (_shellStream != null)
            {
                _cancellationTokenSource?.Cancel();
                _shellStream.Close();
                _shellStream.Dispose();
                _shellStream = null;
            }
            if (_sshClient.IsConnected)
            {
                _sshClient.Disconnect();
            }
        }

        /// <summary>
        /// Executes a command and returns the result.
        /// </summary>
        public string RunCommand(string command)
        {
            if (!_sshClient.IsConnected)
                throw new InvalidOperationException("SSH is not connected.");
            var cmd = _sshClient.RunCommand(command);
            return cmd.Result;
        }

        /// <summary>
        /// Starts an interactive shell and registers a callback to receive the output.
        /// </summary>
        public void StartInteractiveShell(Action<string> onDataReceived)
        {
            if (!_sshClient.IsConnected)
                throw new InvalidOperationException("SSH is not connected.");

            // Creates the ShellStream with basic parameters (terminal type "xterm")
            _shellStream = _sshClient.CreateShellStream("xterm", 80, 24, 800, 600, 1024);
            _cancellationTokenSource = new CancellationTokenSource();

            // Reads shell data asynchronously
            Task.Run(async () => await ReadShellStreamAsync(onDataReceived, _cancellationTokenSource.Token));
        }

        private async Task ReadShellStreamAsync(Action<string> onDataReceived, CancellationToken token)
        {
            byte[] buffer = new byte[1024];
            while (!token.IsCancellationRequested && _shellStream != null)
            {
                if (_shellStream.DataAvailable)
                {
                    int bytesRead = await _shellStream.ReadAsync(buffer, 0, buffer.Length, token);
                    if (bytesRead > 0)
                    {
                        string text = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        onDataReceived?.Invoke(text);
                    }
                }
                else
                {
                    await Task.Delay(200, token);
                }
            }
        }

        /// <summary>
        /// Sends a line of text (command) to the interactive shell.
        /// </summary>
        public void SendInput(string input)
        {
            if (_shellStream == null)
                throw new InvalidOperationException("Shell stream is not initialized.");
            _shellStream.WriteLine(input);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Disconnect();
                _sshClient.Dispose();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        public string ExecuteCommand(string command)
        {
            if (!_sshClient.IsConnected)
            {
                throw new InvalidOperationException("There is no active SSH connection.");
            }

            try
            {
                var cmd = _sshClient.CreateCommand(command); // `_sshClient` is your SshClient instance
                return cmd.Execute(); // Executes the command and returns the output
            }
            catch (Exception ex)
            {
                throw new Exception($"Error executing the command '{command}': {ex.Message}");
            }
        }
    }
}
