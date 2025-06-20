using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using Microsoft.UI;
using Microsoft.UI.Windowing;

namespace BeaconScan
{
    public sealed partial class MainWindow : Window
    {

        // public static MainWindow Instance { get; private set; }
        
        private string baseIp;
        // Instancia de SshManager
        private SshManager? _sshManager;
        // Instancia de SshManager
        private SftpManager? _sftpManager;

        private string? _selectedIp; // Global variable to store the selected IP
                                     // Local and remote base paths you can adjust according to your environment.
                                     // Global variables
        private string localDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);


        private string remoteDirectory = "~/"; // Default value to be replaced based on user's selection

        private CancellationTokenSource? _cancellationTokenSource; // Declared as nullable

        bool useSynScan = true; // Based on the state of the checkbox

        public MainWindow(Windows.ApplicationModel.PackageVersion version)
        {
            this.InitializeComponent();
            
            ChangeTitleMainBar("Beacon Scam Version " + $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}");
            baseIp = NetworkScanner.GetLocalBaseIP();
            baseIPTextBlock.Text = baseIp;
            bool useSynScan = UseSynScanCheckBox.IsChecked == true;
            // If the base IP is "unknown", disable the Scan button (or any other relevant buttons)
            if (baseIp == "unknown")
            {
                scanButton.IsEnabled = false;

                // Optionally, disable other related buttons:
                // cancelButton.IsEnabled = false;  // If you've assigned a name to the Cancel button, for instance.

                // You can also notify the user or update the status text:
                statusText.Text = "Network information not available.";
            }
        }

        private void ChangeTitleMainBar(string pTitulo)
        {
            // no UIElement is set for titlebar, default titlebar is created which extends to entire non client area
            AppWindow.Title = pTitulo;

            //window.ExtendsContentIntoTitleBar = true;
            // window.SetTitleBar(null);  // optional line as not setting any UIElement as titlebar is same as setting null as titlebar
        }

        public void CustomizeTitleBar(AppWindow appWindow)
        {
            var titleBar = appWindow.TitleBar;

            // Set the background color
            titleBar.BackgroundColor = Colors.Blue;

            // Optional: Set the foreground color
            titleBar.ForegroundColor = Colors.White;
        }


        private async void OnScanButtonClicked(object sender, RoutedEventArgs e)
        {
            var scanButton = sender as Button;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            scanButton.IsEnabled = false;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            _cancellationTokenSource = new CancellationTokenSource();

            statusText.Text = "Please wait, this will take a few minutes";
            progressRing.IsActive = true;
            progressRing.Visibility = Visibility.Visible;
            BeaconStatusBar.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Colors.Yellow);


            // Limpiamos la fuente de datos actual
            ipListView.ItemsSource = null;

            try
            {
                // Retrieve the base IP from the TextBlock
                baseIp = baseIPTextBlock.Text;

                // Get the list of IpItem objects, with the IsEven property assigned internally

                CancellationToken cancellationToken = new();

                var activeIps = await NetworkScanner.ScanNetworkAsync(baseIp, useSynScan, cancellationToken);

                // Assign the collection as the ItemsSource for the ListView
                ipListView.ItemsSource = activeIps;

                statusText.Text = "Scan completed.";
            }
            catch (OperationCanceledException)
            {
                statusText.Text = "Operation canceled.";
            }
            catch (Exception ex)
            {
                statusText.Text = $"Error: {ex.Message}";
            }
            finally
            {
                progressRing.IsActive = false;
                progressRing.Visibility = Visibility.Collapsed;
                _cancellationTokenSource = null;
                scanButton.IsEnabled = true;
                BeaconStatusBar.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Colors.LightGray);
            }
        }


        private async Task<(bool Success, string Username, string Password)> ShowCredentialsDialogAsync(string ip, int port)
        {
            var dialog = new CredentialsDialog
            {
                XamlRoot = this.Content.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                string username = dialog.UsernameTextBox.Text;
                string password = dialog.PasswordBox.Password;
                return (true, username, password);
            }

            return (false, string.Empty, string.Empty);
        }

        private void UseSynScanCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            useSynScan = true;
            Console.WriteLine("SYN Scan enabled.");
        }

        private void UseSynScanCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            useSynScan = false;
            Console.WriteLine("SYN Scan disabled.");
        }

        private async void OnOpenWebViewButtonClicked(object sender, RoutedEventArgs e)
        {
            string httpUrl = "";
            var httpWebViewWindow = new WebViewWindow(httpUrl);

            if (!string.IsNullOrEmpty(_selectedIp) && portsListView.SelectedItem is PortDetails selectedPort)
            {
                Debug.WriteLine($"Selected IP: {_selectedIp}");
                Debug.WriteLine($"Selected port: {selectedPort.PortNumber}");

                // Decide based on the selected port
                switch (selectedPort.PortNumber)
                {
                    case 80: // HTTP
                    case 8080: // HTTP Proxy
                        httpUrl = $"http://{_selectedIp}:{selectedPort.PortNumber}/";
                        Debug.WriteLine($"Opening HTTP at: {httpUrl}");
                        httpWebViewWindow = new WebViewWindow(httpUrl);
                        httpWebViewWindow.Activate();
                        break;
                    case 443: // HTTPS Proxy
                        httpUrl = $"https://{_selectedIp}:{selectedPort.PortNumber}/";
                        Debug.WriteLine($"Opening HTTP at: {httpUrl}");
                        httpWebViewWindow = new WebViewWindow(httpUrl);
                        httpWebViewWindow.Activate();
                        break;

                    // case 554: // RTSP (Streaming)
                    // var credentials = await ShowCredentialsDialogAsync(_selectedIp, selectedPort.PortNumber);

                    // if (credentials.Success) // If the user entered credentials
                    // {
                    //     string rtspUrl = $"rtsp://{credentials.Username}:{credentials.Password}@{_selectedIp}:{selectedPort.PortNumber}/live";
                    //     Debug.WriteLine($"Attempting to play RTSP: {rtspUrl}");

                    //     var rtspViewerWindow = new RtspViewerPage();
                    //     rtspViewerWindow.PlayStream(rtspUrl);
                    //     rtspViewerWindow.Activate();
                    // }
                    // else
                    // { 
                    //     Debug.WriteLine("User canceled credential entry.");
                    //     statusText.Text = "RTSP connection canceled by user.";
                    // }
                    // break;


                    case 3389: // RDP
                               // Show a dialog to enter domain/username and password
                        var rdpCredentials = await ShowCredentialsDialogAsync(_selectedIp, selectedPort.PortNumber);

                        if (rdpCredentials.Success)
                        {
                            Debug.WriteLine($"Starting RDP connection to {_selectedIp}");

                            // Create a temporary RDP file dynamically
                            string rdpFilePath = Path.Combine(Path.GetTempPath(), "tempRdpConnection.rdp");
                            string rdpContent = $"full address:s:{_selectedIp}\nusername:s:{rdpCredentials.Username}\n";

                            File.WriteAllText(rdpFilePath, rdpContent);

                            // Launch mstsc.exe with the generated RDP file
                            try
                            {
                                Process.Start("mstsc.exe", rdpFilePath);
                                statusText.Text = "RDP connection started.";
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error launching RDP: {ex.Message}");
                                statusText.Text = "Failed to start RDP connection.";
                            }
                        }
                        else
                        {
                            Debug.WriteLine("User canceled credential entry.");
                            statusText.Text = "RDP connection canceled by user.";
                        }
                        break;

                    default:
                        statusText.Text = "The selected port is not configured for automatic handling.";
                        Debug.WriteLine($"Unhandled port: {selectedPort.PortNumber}");
                        break;
                }
            }
            else
            {
                statusText.Text = "Please select an IP address and a port before continuing.";
                Debug.WriteLine("No IP address or port was selected.");
            }
        }


        private void PortsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = portsListView.SelectedItem;
            if (selectedItem != null && selectedItem is PortDetails selectedPort)
            {
                statusText.Text = $"Selected item: {selectedPort.ServiceName}";
                Debug.WriteLine($"Successfully selected: {selectedPort.ServiceName}, Protocol: {selectedPort.Protocol}, Port: {selectedPort.PortNumber}");
            }
            else
            {
                statusText.Text = "No item was selected.";
                System.Diagnostics.Debug.WriteLine("`SelectedItem` is null or not of type PortDetails.");
            }
        }

        private void OnExitButtonClicked(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }

        private async void OnIpSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ipListView.SelectedItem is IpItem selectedItem) // Now verifies if it's an IpItem
            {
                // Extract the IP address from the selected object
                string selectedIp = selectedItem.IpAddress;
                _selectedIp = selectedIp; // Update the selected IP
                Debug.WriteLine($"Selected IP (updated): {_selectedIp}");

                _cancellationTokenSource = new CancellationTokenSource();

                statusText.Text = $"Scanning ports for {selectedIp}...";
                progressRing.IsActive = true;
                progressRing.Visibility = Visibility.Visible;
                scanButton.IsEnabled = false;
                ipListView.IsEnabled = false;
                BeaconStatusBar.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Colors.Yellow);
                portsListView.Items.Clear();

                try
                {
                    var portRegistry = new PortRegistry();
                    var registeredPorts = portRegistry.GetRegisteredPortNumbers();

                    var cancellationToken = new CancellationTokenSource().Token;
                    var openPorts = await NetworkScanner.ScanPortsAsync(selectedIp, registeredPorts, cancellationToken);

                    foreach (var port in openPorts)
                    {
                        // Find port information using PortRegistry
                        var portInfo = portRegistry.FindByPortNumber(port.PortNumber);

                        // Convert PortInfo to PortDetails
                        var portDetails = new PortDetails
                        {
                            Protocol = portInfo.Protocol,
                            PortNumber = portInfo.PortNumber,
                            ServiceName = portInfo.ServiceName
                        };

                        // Add the PortDetails to the ListView
                        portsListView.Items.Add(portDetails);
                        Debug.WriteLine($"Added to portsListView: {portDetails.ServiceName}");
                    }

                    statusText.Text = "Port scan completed.";
                }
                catch (OperationCanceledException)
                {
                    portsListView.Items.Clear();
                    statusText.Text = "Port scan canceled.";
                }
                catch (Exception ex)
                {
                    portsListView.Items.Clear();
                    statusText.Text = $"Error: {ex.Message}";
                }
                finally
                {
                    progressRing.IsActive = false;
                    progressRing.Visibility = Visibility.Collapsed;
                    _cancellationTokenSource = null;
                    scanButton.IsEnabled = true;
                    ipListView.IsEnabled = true;
                    BeaconStatusBar.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Colors.LightGray);
                }
            }
            else
            {
                Debug.WriteLine("No IP was selected.");
                _selectedIp = null; // Reset the variable if no selection exists
            }
        }

        private void OnCancelarButtonClicked(object sender, RoutedEventArgs e)
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                statusText.Text = "Cancelling operation...";
            }
        }

        private void OnConnectSSH(object sender, RoutedEventArgs e)
        {
            string host = hostTextBox.Text;
            string username = loginTextBox.Text;
            string password = passwordBox.Password;

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                statusText.Text = "Please complete all fields.";
                return;
            }

            try
            {
                _sshManager = new SshManager(host, username, password);
                _sshManager.Connect();
                // Starts the interactive shell and registers the callback to update the terminal output.
                _sshManager.StartInteractiveShell(AppendSSHOutput);
                statusText.Text = $"Connected to {host}";
            }
            catch (Exception ex)
            {
                statusText.Text = $"Connection error: {ex.Message}";
            }
        }

        // Callback invoked each time the shell receives data.
        // In WinUI, DispatcherQueue is used to update the UI from a background thread.
        private void AppendSSHOutput(string data)
        {
            // We use DispatcherQueue to ensure the TextBlock is updated on the UI thread.
            DispatcherQueue.TryEnqueue(() =>
            {
                sshOutputTextBlock.Text += data;
                // If you're using a ScrollViewer for the terminal, you can also scroll to the end:
                terminalScrollViewer.UpdateLayout();
                terminalScrollViewer.ChangeView(null, terminalScrollViewer.ExtentHeight, null);
            });
        }

        // Event to send a command from the terminal control.
        private void OnSendCommand(object sender, RoutedEventArgs e)
        {
            if (_sshManager == null)
            {
                statusText.Text = "No active SSH connection.";
                return;
            }

            string inputCommand = commandTextBox.Text;
            if (!string.IsNullOrEmpty(inputCommand))
            {
                _sshManager.SendInput(inputCommand);
                commandTextBox.Text = string.Empty;
            }
        }

        // Event to connect via FTP/SFTP
        private async void OnConnectFTP(object sender, RoutedEventArgs e)
        {
            string host = ftpHostTextBox.Text;
            string username = ftpLoginTextBox.Text;
            string password = ftpPasswordBox.Password;
            string remoteDir = "";

            Console.WriteLine("Starting SFTP connection attempt...");
            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                UpdateStatus("Please complete all fields.");
                return;
            }

            try
            {
                // 1. SSH connection to retrieve user's default directory
                if (_sshManager == null)
                {
                    _sshManager = new SshManager(host, username, password);
                    _sshManager.Connect(); // Establish SSH connection
                    Console.WriteLine($"SSH connection successful to {host}");
                }

                // 2. Get the default directory via SSH
                remoteDir = _sshManager.ExecuteCommand("pwd").Trim(); // Execute 'pwd'
                remoteDirectory = remoteDir;
                Console.WriteLine($"Captured default directory: {remoteDir}");
                UpdateStatus($"Default directory: {remoteDir}");

                // 3. SFTP connection using the same SSH credentials
                _sftpManager = new SftpManager(host, username, password);
                await _sftpManager.ConnectAsync();
                UpdateStatus($"Connected to {host} via SFTP in directory: {remoteDir}");

                // 4. Load local and remote files
                await LoadRemoteFiles(remoteDir);
                await LoadLocalFiles(localDirectory);
            }
            catch (Exception ex)
            {
                UpdateStatus($"SFTP connection error: {ex.Message}");
            }
        }

        // Function to update the ListBox with remote files
        private async Task LoadRemoteFiles(string remotePath)
        {
            if (_sftpManager == null)
            {
                UpdateStatus("SFTP module is not connected.");
                return;
            }

            try
            {
                // Retrieve the remote file list as FileItem objects
                var remoteFiles = await _sftpManager.GetRemoteFileListAsync(remotePath);

                // Apply zebra striping by assigning IsEven for each item
                for (int i = 0; i < remoteFiles.Count; i++)
                {
                    remoteFiles[i].IsEven = (i % 2 == 0);
                }

                // Bind the list to the ListBox
                remoteDirListBox.ItemsSource = remoteFiles;
            }
            catch (Exception ex)
            {
                UpdateStatus($"Failed to retrieve remote files: {ex.Message}");
            }
        }

        // Function to load local files and populate the ListBox
        private async Task LoadLocalFiles(string localPath)
        {
            try
            {
                // Fetch the local file list, including the parent directory ".."
                var localFiles = await Task.Run(() =>
                {
                    var files = LocalFileHelper.GetLocalFileList(localPath).ToList();

                    // Add ".." as the first item for navigation (unless already at root)
                    var fileItems = new List<FileItem>();
                    if (localPath != Path.GetPathRoot(localPath))
                    {
                        fileItems.Add(new FileItem
                        {
                            Name = "..",
                            Type = "D"
                        });
                    }

                    fileItems.AddRange(files); // Add the rest of the files/directories
                    return fileItems;
                });

                // Apply zebra striping: assign IsEven based on the index
                for (int i = 0; i < localFiles.Count; i++)
                {
                    localFiles[i].IsEven = (i % 2 == 0);
                }

                // Bind the result to the ListBox
                localDirListBox.ItemsSource = localFiles;
            }
            catch (Exception ex)
            {
                UpdateStatus($"Failed to retrieve local files: {ex.Message}");
            }
        }

        // Event to upload a file (from local to remote)
        private async void OnUploadFile(object sender, RoutedEventArgs e)
        {
            if (localDirListBox.SelectedItem == null)
            {
                UpdateStatus("Please select a file from the local list.");
                return;
            }

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            string selectedFile = localDirListBox.SelectedItem.ToString();
#pragma warning restore CS8600
#pragma warning disable CS8604 // Possible null reference argument.
            string localFilePath = Path.Combine(localDirectory, selectedFile);
#pragma warning restore CS8604
            string remoteFilePath = remoteDirectory.TrimEnd('/') + "/" + selectedFile;

            progressRing.IsActive = true; // Show progress indicator
            try
            {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                await _sftpManager.UploadFileAsync(localFilePath, remoteFilePath);
#pragma warning restore CS8602
                UpdateStatus($"File '{selectedFile}' uploaded successfully.");
                await LoadRemoteFiles(remoteDirectory);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error uploading file: {ex.Message}");
            }
            finally
            {
                progressRing.IsActive = false; // Hide progress indicator
            }
        }

        // Event to download a file (from remote to local)
        private async void OnDownloadFile(object sender, RoutedEventArgs e)
        {
            if (remoteDirListBox.SelectedItem == null)
            {
                UpdateStatus("Please select a file from the remote list.");
                return;
            }

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            string selectedFile = remoteDirListBox.SelectedItem.ToString();
#pragma warning restore CS8600
            string remoteFilePath = remoteDirectory.TrimEnd('/') + "/" + selectedFile;
#pragma warning disable CS8604 // Possible null reference argument.
            string localFilePath = Path.Combine(localDirectory, selectedFile);
#pragma warning restore CS8604

            try
            {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                await _sftpManager.DownloadFileAsync(remoteFilePath, localFilePath);
#pragma warning restore CS8602
                UpdateStatus($"File '{selectedFile}' downloaded successfully.");
                await LoadLocalFiles(localDirectory);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error downloading file: {ex.Message}");
            }
        }

        // Event triggered on double-tap in the remote directory ListBox
        private void RemoteDirListBox_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (remoteDirListBox.SelectedItem is FileItem selectedItem)
            {
                try
                {
                    if (selectedItem.Name == "..")
                    {
                        // Attempt to calculate the parent directory
                        var parentDirectory = Path.GetDirectoryName(remoteDirectory.TrimEnd('/'));
                        if (string.IsNullOrEmpty(parentDirectory) || parentDirectory == "/")
                        {
                            UpdateStatus("You cannot go above the root directory.");
                            parentDirectory = "/";
                        }

                        // Navigate to the parent directory
                        remoteDirectory = Path.Combine(parentDirectory, "").Replace('\\', '/');
                    }
                    else if (selectedItem.Type == "D")
                    {
                        // Navigate into the selected directory
#pragma warning disable CS8604 // Possible null reference argument.
                        remoteDirectory = Path.Combine(remoteDirectory, selectedItem.Name).Replace('\\', '/');
#pragma warning restore CS8604
                    }

                    // Reload files for the new directory
                    _ = LoadRemoteFiles(remoteDirectory);
                }
                catch (Exception ex)
                {
                    UpdateStatus($"Error navigating remote directories: {ex.Message}");
                }
            }
        }


        // Event triggered on double-tap in the local directory ListBox
        private void LocalDirListBox_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (localDirListBox.SelectedItem is FileItem selectedItem)
            {
                if (selectedItem.Name == "..")
                {
                    // Navigate to the parent directory (stay in current if null)
                    localDirectory = Path.GetDirectoryName(localDirectory) ?? localDirectory;
                }
                else if (selectedItem.Type == "D")
                {
                    // Navigate into the selected directory
#pragma warning disable CS8604 // Possible null reference argument.
                    localDirectory = Path.Combine(localDirectory, selectedItem.Name);
#pragma warning restore CS8604
                }

                // Load files from the new local directory
                _ = LoadLocalFiles(localDirectory);
            }
        }

        // Navigates one level up in the remote directory hierarchy
        private void GoToParentRemoteDirectory()
        {
            if (remoteDirectory != "/") // Prevent navigating above root
            {
                remoteDirectory = Path.GetDirectoryName(remoteDirectory.TrimEnd('/')) ?? "/";
                _ = LoadRemoteFiles(remoteDirectory);
            }
        }

        // Navigates one level up in the local directory hierarchy
        private void GoToParentLocalDirectory()
        {
            if (localDirectory != Path.GetPathRoot(localDirectory)) // Prevent navigating above root
            {
#pragma warning disable CS8601 // Possible null reference assignment.
                localDirectory = Path.GetDirectoryName(localDirectory);
#pragma warning restore CS8601
#pragma warning disable CS8604 // Possible null reference argument.
                _ = LoadLocalFiles(localDirectory);
#pragma warning restore CS8604
            }
        }


        private void UpdateStatus(string message)
        {
            statusText.Text = message;
        }

        // General exception handler to update status based on the operation
        private void HandleException(Exception ex, string operation)
        {
            UpdateStatus($"Error during {operation}: {ex.Message}");
        }

        // Displays a dialog to prompt the user for a remote directory path
        private async Task<string?> ShowDirectoryDialogAsync()
        {
            var dialog = new ContentDialog
            {
                Title = "Select Remote Directory",
                Content = new StackPanel
                {
                    Children =
            {
                new TextBlock { Text = "Enter the remote directory you wish to connect to:" },
                new TextBox { Name = "RemoteDirTextBox", PlaceholderText = "~/", Margin = new Thickness(0, 10, 0, 0) }
            }
                },
                PrimaryButtonText = "Accept",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary
            };

            // Ensure the XamlRoot is properly initialized
            if (this.Content?.XamlRoot == null)
            {
                Console.WriteLine("Warning: XamlRoot is null. Check main window initialization.");
                return null;
            }

            dialog.XamlRoot = this.Content.XamlRoot;

            try
            {
                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    // Retrieve the user input from the TextBox
                    var textBox = (TextBox)((StackPanel)dialog.Content).Children[1];
                    return textBox.Text.Trim();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to display dialog: {ex.Message}");
            }

            // Return null if canceled or an error occurred
            return null;
        }
    }
}
