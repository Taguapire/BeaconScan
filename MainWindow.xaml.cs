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
using Windows.Foundation.Metadata; // Asegúrate de tener la referencia a Windows.UI.Core para el Dispatcher

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

        private string? _selectedIp; // Variable global para almacenar la IP seleccionada
        // Rutas base locales y remotas que puedes ajustar según tu entorno.
        // Variables globales
        private string localDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);


        private string remoteDirectory = "~/"; // Valor por defecto que luego se reemplazará según la elección del usuario

        private CancellationTokenSource? _cancellationTokenSource; // Declarado como nullable.
        
        bool useSynScan = true; // O según el estado del checkbox

        public MainWindow(Windows.ApplicationModel.PackageVersion version)
        {
            this.InitializeComponent();
            
            ChangeTitleMainBar("Beacon Scam Version " + $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}");
            baseIp = NetworkScanner.GetLocalBaseIP();
            baseIPTextBlock.Text = baseIp;
            bool useSynScan = UseSynScanCheckBox.IsChecked == true;
            // Si la Base IP es "unknown", deshabilitamos el botón Scan (o todos los que consideres).
            if (baseIp == "unknown")
            {
                scanButton.IsEnabled = false;
                // Opcionalmente, puedes deshabilitar otros botones relacionados:
                // cancelButton.IsEnabled = false;  // Si asignaste nombre a Cancel, por ejemplo.
                // Puedes también notificar al usuario, o cambiar el statusText:
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

            // Establecer el color de fondo
            titleBar.BackgroundColor = Colors.Blue;

            // Opcional: Establecer el color de primer plano
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
                // Obtenemos la base IP desde el TextBlock
                baseIp = baseIPTextBlock.Text;

                // Aquí se obtiene la lista de IpItem, con la propiedad IsEven asignada internamente.

                CancellationToken cancellationToken = new();

                var activeIps = await NetworkScanner.ScanNetworkAsync(baseIp, useSynScan, cancellationToken);

                // Asignamos la colección como ItemsSource del ListView
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
            Console.WriteLine("SYN Scan activado.");
        }

        private void UseSynScanCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            useSynScan = false;
            Console.WriteLine("SYN Scan desactivado.");
        }

        private async void OnOpenWebViewButtonClicked(object sender, RoutedEventArgs e)
        {
            string httpUrl = "";
            var httpWebViewWindow = new WebViewWindow(httpUrl);

            if (!string.IsNullOrEmpty(_selectedIp) && portsListView.SelectedItem is PortDetails selectedPort)
            {
                Debug.WriteLine($"IP seleccionada: {_selectedIp}");
                Debug.WriteLine($"Puerto seleccionado: {selectedPort.PortNumber}");

                // Decidir según el puerto
                switch (selectedPort.PortNumber)
                {
                    case 80: // HTTP
                    case 8080: // HTTP Proxy
                        httpUrl = $"http://{_selectedIp}:{selectedPort.PortNumber}/";
                        Debug.WriteLine($"Abriendo HTTP en: {httpUrl}");
                        httpWebViewWindow = new WebViewWindow(httpUrl);
                        httpWebViewWindow.Activate();
                        break;
                    case 443: // HTTPS Proxy
                        httpUrl = $"https://{_selectedIp}:{selectedPort.PortNumber}/";
                        Debug.WriteLine($"Abriendo HTTP en: {httpUrl}");
                        httpWebViewWindow = new WebViewWindow(httpUrl);
                        httpWebViewWindow.Activate();
                        break;

                        //case 554: // RTSP (Streaming)
                        //var credentials = await ShowCredentialsDialogAsync(_selectedIp, selectedPort.PortNumber);

                        //if (credentials.Success) // Si el usuario ingresó credenciales
                        //{
                        //    string rtspUrl = $"rtsp://{credentials.Username}:{credentials.Password}@{_selectedIp}:{selectedPort.PortNumber}/live";
                        //    Debug.WriteLine($"Intentando reproducir RTSP: {rtspUrl}");
                        //
                        // var rtspViewerWindow = new RtspViewerPage();
                        // rtspViewerWindow.PlayStream(rtspUrl);
                        // rtspViewerWindow.Activate();
                        //}
                        //else
                        //{ 
                        //Debug.WriteLine("El usuario canceló el ingreso de credenciales.");
                        //statusText.Text = "Conexión RTSP cancelada por el usuario.";
                        //}
                        //break;

                    case 3389: // RDP
                               // Mostrar un cuadro de diálogo para ingresar el dominio/usuario y contraseña
                        var rdpCredentials = await ShowCredentialsDialogAsync(_selectedIp, selectedPort.PortNumber);

                        if (rdpCredentials.Success)
                        {
                            Debug.WriteLine($"Iniciando conexión RDP con {_selectedIp}");

                            // Crear un archivo RDP dinámico
                            string rdpFilePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tempRdpConnection.rdp");
                            string rdpContent = $"full address:s:{_selectedIp}\nusername:s:{rdpCredentials.Username}\n";

                            System.IO.File.WriteAllText(rdpFilePath, rdpContent);

                            // Abrir mstsc.exe con el archivo RDP generado
                            try
                            {
                                System.Diagnostics.Process.Start("mstsc.exe", rdpFilePath);
                                statusText.Text = "Conexión RDP iniciada.";
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error al iniciar RDP: {ex.Message}");
                                statusText.Text = "Error al iniciar la conexión RDP.";
                            }
                        }
                        else
                        {
                            Debug.WriteLine("El usuario canceló el ingreso de credenciales.");
                            statusText.Text = "Conexión RDP cancelada por el usuario.";
                        }
                        break;

                    default:
                        statusText.Text = "El puerto seleccionado no está configurado para manejarlo automáticamente.";
                        Debug.WriteLine($"Puerto no manejado automáticamente: {selectedPort.PortNumber}");
                        break;
                }
            }
            else
            {
                statusText.Text = "Por favor, selecciona una IP y un puerto antes de continuar.";
                Debug.WriteLine("No se seleccionó una IP o un puerto.");
            }
        }


        private void PortsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = portsListView.SelectedItem;
            if (selectedItem != null && selectedItem is PortDetails selectedPort)
            {
                statusText.Text = $"Elemento seleccionado: {selectedPort.ServiceName}";
                Debug.WriteLine($"Seleccionado correctamente: {selectedPort.ServiceName}, Protocolo: {selectedPort.Protocol}, Puerto: {selectedPort.PortNumber}");
            }
            else
            {
                statusText.Text = "No se seleccionó ningún elemento.";
                System.Diagnostics.Debug.WriteLine("El `SelectedItem` es null o no es del tipo PortDetails.");
            }
        }

        private void OnExitButtonClicked(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }

        private async void OnIpSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ipListView.SelectedItem is IpItem selectedItem) // Ahora se verifica si es IpItem
            {
                // Se extrae la dirección IP del objeto seleccionado.
                string selectedIp = selectedItem.IpAddress;
                _selectedIp = selectedIp; // Actualizamos la IP seleccionada
                Debug.WriteLine($"IP seleccionada (actualizada): {_selectedIp}");

                _cancellationTokenSource = new CancellationTokenSource();

                statusText.Text = $"Escaneando puertos para {selectedIp}...";
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
                        // Encontramos información del puerto usando PortRegistry
                        var portInfo = portRegistry.FindByPortNumber(port.PortNumber);

                        // Convertimos PortInfo a PortDetails
                        var portDetails = new PortDetails
                        {
                            Protocol = portInfo.Protocol,
                            PortNumber = portInfo.PortNumber,
                            ServiceName = portInfo.ServiceName
                        };

                        // Agregamos el PortDetails al ListView
                        portsListView.Items.Add(portDetails);
                        Debug.WriteLine($"Agregado a portsListView: {portDetails.ServiceName}");
                    }

                    statusText.Text = "Escaneo de puertos completado.";
                }
                catch (OperationCanceledException)
                {
                    portsListView.Items.Clear();
                    statusText.Text = "Escaneo de puertos cancelado.";
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
                Debug.WriteLine("No se seleccionó ninguna IP.");
                _selectedIp = null; // Reinicia la variable si no hay selección
            }
        }


        private void OnCancelarButtonClicked(object sender, RoutedEventArgs e)
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                statusText.Text = "Cancelando operación...";
            }
        }

        // Instancia de SshManager
        // Maneja la conexión SSH
        // Evento para conectar vía SSH
        private void OnConnectSSH(object sender, RoutedEventArgs e)
        {
            string host = hostTextBox.Text;
            string username = loginTextBox.Text;
            string password = passwordBox.Password;

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                statusText.Text = "Por favor, completa todos los campos.";
                return;
            }

            try
            {
                _sshManager = new SshManager(host, username, password);
                _sshManager.Connect();
                // Inicia el shell interactivo y registra el callback para actualizar la salida del terminal.
                _sshManager.StartInteractiveShell(AppendSSHOutput);
                statusText.Text = $"Conectado a {host}";
            }
            catch (Exception ex)
            {
                statusText.Text = $"Error al conectar: {ex.Message}";
            }
        }

        // Callback que se invoca cada vez que la shell recibe datos.
        // En WinUI, se utiliza DispatcherQueue para actualizar la UI.
        private void AppendSSHOutput(string data)
        {
            // Usamos DispatcherQueue para asegurarnos de actualizar el TextBlock en el hilo UI.
            DispatcherQueue.TryEnqueue(() =>
            {
                sshOutputTextBlock.Text += data;
                // Si usas un ScrollViewer para el terminal, también puedes hacer scroll al final:
                terminalScrollViewer.UpdateLayout();
                terminalScrollViewer.ChangeView(null, terminalScrollViewer.ExtentHeight, null);
            });
        }

        // Evento para enviar un comando desde el control del terminal.
        private void OnSendCommand(object sender, RoutedEventArgs e)
        {
            if (_sshManager == null)
            {
                statusText.Text = "No existe una conexión SSH activa.";
                return;
            }

            string inputCommand = commandTextBox.Text;
            if (!string.IsNullOrEmpty(inputCommand))
            {
                _sshManager.SendInput(inputCommand);
                commandTextBox.Text = string.Empty;
            }
        }

        // Evento para conectar vía FTP/SFTP
        private async void OnConnectFTP(object sender, RoutedEventArgs e)
        {
            string host = ftpHostTextBox.Text;
            string username = ftpLoginTextBox.Text;
            string password = ftpPasswordBox.Password;
            string remoteDir = "";

            Console.WriteLine($"Entrando a conectar vía SFTP");
            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                UpdateStatus("Por favor, completa todos los campos.");
                return;
            }

            try
            {
                // 1. Conexión SSH para obtener el directorio predeterminado
                if (_sshManager == null)
                {
                    _sshManager = new SshManager(host, username, password);
                    _sshManager.Connect(); // Establecer conexión SSH
                    Console.WriteLine($"Conexión SSH exitosa a {host}");
                }

                // 2. Obtener el directorio predeterminado del usuario vía SSH
                remoteDir = _sshManager.ExecuteCommand("pwd").Trim(); // Ejecutar el comando 'pwd'
                remoteDirectory = remoteDir;
                Console.WriteLine($"Directorio predeterminado capturado: {remoteDir}");
                UpdateStatus($"Directorio predeterminado: {remoteDir}");

                // 3. Conexión SFTP utilizando las credenciales SSH
                _sftpManager = new SftpManager(host, username, password);
                await _sftpManager.ConnectAsync();
                UpdateStatus($"Conectado a {host} vía SFTP en el directorio: {remoteDir}");

                // 4. Cargar archivos locales y remotos
                await LoadRemoteFiles(remoteDir);
                await LoadLocalFiles(localDirectory);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error al conectar SFTP: {ex.Message}");
            }
        }

        // Función para actualizar el ListBox con archivos remotos
        private async Task LoadRemoteFiles(string remotePath)
        {
            if (_sftpManager == null)
            {
                UpdateStatus("El módulo SFTP no está conectado.");
                return;
            }

            try
            {
                // Obtener la lista remota con FileItem
                var remoteFiles = await _sftpManager.GetRemoteFileListAsync(remotePath);

                // Asignar zebra striping: recorrer la lista y asignar IsEven para cada valor
                for (int i = 0; i < remoteFiles.Count; i++)
                {
                    remoteFiles[i].IsEven = (i % 2 == 0);
                }

                // Asignar los datos al ListBox
                remoteDirListBox.ItemsSource = remoteFiles;
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error al obtener archivos remotos: {ex.Message}");
            }
        }

        private async Task LoadLocalFiles(string localPath)
        {
            try
            {
                // Obtenemos la lista local incluyendo el directorio padre ".."
                var localFiles = await Task.Run(() =>
                {
                    var files = LocalFileHelper.GetLocalFileList(localPath).ToList();

                    // Agregar ".." como el primer elemento para navegar hacia atrás
                    var fileItems = new List<FileItem>();
                    if (localPath != Path.GetPathRoot(localPath)) // No agregar ".." en la raíz
                    {
                        fileItems.Add(new FileItem
                        {
                            Name = "..",
                            Type = "D"
                        });
                    }

                    fileItems.AddRange(files); // Agregar el resto de los archivos/directorios
                    return fileItems;
                });

                // Configurar zebra striping: cada elemento obtiene IsEven en función del índice
                for (int i = 0; i < localFiles.Count; i++)
                {
                    localFiles[i].IsEven = (i % 2 == 0);
                }

                // Asignar al ListBox
                localDirListBox.ItemsSource = localFiles;
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error al obtener archivos locales: {ex.Message}");
            }
        }


        // Evento para subir archivo (de local a remoto)
        private async void OnUploadFile(object sender, RoutedEventArgs e)
        {
            if (localDirListBox.SelectedItem == null)
            {
                UpdateStatus("Selecciona un archivo del listado local.");
                return;
            }

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            string selectedFile = localDirListBox.SelectedItem.ToString();
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8604 // Possible null reference argument.
            string localFilePath = Path.Combine(localDirectory, selectedFile);
#pragma warning restore CS8604 // Possible null reference argument.
            string remoteFilePath = remoteDirectory.TrimEnd('/') + "/" + selectedFile;

            progressRing.IsActive = true; // Mostrar indicador 
            try
            {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                await _sftpManager.UploadFileAsync(localFilePath, remoteFilePath);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                UpdateStatus($"Archivo '{selectedFile}' subido correctamente.");
                await LoadRemoteFiles(remoteDirectory);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error al subir el archivo: {ex.Message}");
            }
            finally
            {
                progressRing.IsActive = false; // Ocultar indicador
            }
        }


        // Evento para descargar archivo (de remoto a local)
        private async void OnDownloadFile(object sender, RoutedEventArgs e)
        {
            if (remoteDirListBox.SelectedItem == null)
            {
                UpdateStatus("Selecciona un archivo del listado remoto.");
                return;
            }

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            string selectedFile = remoteDirListBox.SelectedItem.ToString();
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
            string remoteFilePath = remoteDirectory.TrimEnd('/') + "/" + selectedFile;
#pragma warning disable CS8604 // Possible null reference argument.
            string localFilePath = Path.Combine(localDirectory, selectedFile);
#pragma warning restore CS8604 // Possible null reference argument.

            try
            {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                await _sftpManager.DownloadFileAsync(remoteFilePath, localFilePath);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                UpdateStatus($"Archivo '{selectedFile}' descargado correctamente.");
                await LoadLocalFiles(localDirectory);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error al descargar el archivo: {ex.Message}");
            }
        }

        private void RemoteDirListBox_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (remoteDirListBox.SelectedItem is FileItem selectedItem)
            {
                try
                {
                    if (selectedItem.Name == "..")
                    {
                        // Intentar calcular el directorio padre
                        var parentDirectory = Path.GetDirectoryName(remoteDirectory.TrimEnd('/'));
                        if (string.IsNullOrEmpty(parentDirectory) || parentDirectory == "/")
                        {
                            UpdateStatus("No puedes retroceder más allá del directorio raíz.");
                            parentDirectory = "/";
                        }

                        // Navegar al directorio padre
                        remoteDirectory = Path.Combine(parentDirectory, "").Replace('\\', '/');
                    }
                    else if (selectedItem.Type == "D")
                    {
                        // Navegar al directorio seleccionado
#pragma warning disable CS8604 // Possible null reference argument.
                        remoteDirectory = Path.Combine(remoteDirectory, selectedItem.Name).Replace('\\', '/');
#pragma warning restore CS8604 // Possible null reference argument.
                    }

                    // Recargar archivos del nuevo directorio
                    _ = LoadRemoteFiles(remoteDirectory);
                }
                catch (Exception ex)
                {
                    UpdateStatus($"Error al navegar en directorios remotos: {ex.Message}");
                }
            }
        }


        private void LocalDirListBox_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (localDirListBox.SelectedItem is FileItem selectedItem)
            {
                if (selectedItem.Name == "..")
                {
                    // Volver al directorio anterior
                    localDirectory = Path.GetDirectoryName(localDirectory) ?? localDirectory;
                }
                else if (selectedItem.Type == "D")
                {
                    // Navegar al directorio seleccionado
#pragma warning disable CS8604 // Possible null reference argument.
                    localDirectory = Path.Combine(localDirectory, selectedItem.Name);
#pragma warning restore CS8604 // Possible null reference argument.
                }

                // Cargar los archivos del nuevo directorio
                _ = LoadLocalFiles(localDirectory);
            }
        }




        private void GoToParentRemoteDirectory()
        {
            if (remoteDirectory != "/") // Evitar salir de la raíz
            {
                remoteDirectory = Path.GetDirectoryName(remoteDirectory.TrimEnd('/')) ?? "/";
                _ = LoadRemoteFiles(remoteDirectory);
            }
        }

        private void GoToParentLocalDirectory()
        {
            if (localDirectory != Path.GetPathRoot(localDirectory)) // Evitar salir de la raíz local
            {
#pragma warning disable CS8601 // Possible null reference assignment.
                localDirectory = Path.GetDirectoryName(localDirectory);
#pragma warning restore CS8601 // Possible null reference assignment.
#pragma warning disable CS8604 // Possible null reference argument.
                _ = LoadLocalFiles(localDirectory);
#pragma warning restore CS8604 // Possible null reference argument.
            }
        }



        private void UpdateStatus(string message)
        {
            statusText.Text = message;
        }

        private void HandleException(Exception ex, string operation)
        {
            UpdateStatus($"Error durante {operation}: {ex.Message}");
        }

        private async Task<string?> ShowDirectoryDialogAsync()
        {
            // Crear el ContentDialog
            var dialog = new ContentDialog
            {
                Title = "Seleccionar Directorio Remoto",
                Content = new StackPanel
                {
                    Children =
            {
                new TextBlock { Text = "Escribe el directorio remoto al que deseas conectarte:" },
                new TextBox { Name = "RemoteDirTextBox", PlaceholderText = "~/", Margin = new Thickness(0, 10, 0, 0) }
            }
                },
                PrimaryButtonText = "Aceptar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary
            };

            // Asegurar que el XamlRoot no es nulo
            if (this.Content?.XamlRoot == null)
            {
                Console.WriteLine("Advertencia: XamlRoot es nulo. Verifica la inicialización de la ventana principal.");
                return null;
            }

            // Asignar el XamlRoot de la ventana principal al diálogo
            dialog.XamlRoot = this.Content.XamlRoot;

            try
            {
                // Mostrar el diálogo y capturar el resultado
                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    // Obtener el valor del TextBox
                    var textBox = (TextBox)((StackPanel)dialog.Content).Children[1];
                    return textBox.Text.Trim();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al mostrar el diálogo: {ex.Message}");
            }

            // Si se cancela o ocurre un error
            return null;
        }

    }
}
