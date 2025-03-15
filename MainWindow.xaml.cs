using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System.Threading.Tasks;
using System.Threading;
using BeaconScan;
using System.Buffers.Text;
using Windows.UI.Core; // Asegúrate de tener la referencia a Windows.UI.Core para el Dispatcher

namespace BeaconScan
{

    public sealed partial class MainWindow : Window
    {
        private string baseIp;
        // Instancia de SshManager
        private SshManager? _sshManager;
        // Instancia de SshManager
        private SftpManager? _sftpManager;

        // Rutas base locales y remotas que puedes ajustar según tu entorno.
        // Variables globales
        private string localDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);


        private string remoteDirectory = "~/"; // Valor por defecto que luego se reemplazará según la elección del usuario

        private CancellationTokenSource? _cancellationTokenSource; // Declarado como nullable.

        public MainWindow()
        {
            this.InitializeComponent();
            baseIp = NetworkScanner.GetLocalBaseIP();
            baseIPTextBlock.Text = "Base IP: " + baseIp;

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

        private async void OnScanButtonClicked(object sender, RoutedEventArgs e)
        {
            var scanButton = sender as Button;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            scanButton.IsEnabled = false;
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            _cancellationTokenSource = new CancellationTokenSource();

            statusText.Text = "Please wait, this will take a few minutes"; // Actualizado al inglés
            progressRing.IsActive = true;
            progressRing.Visibility = Visibility.Visible;
            ipListView.Items.Clear();

            try
            {
                // Pasamos la Base IP dinámica como parámetro a ScanNetworkAsync.
                var activeIps = await NetworkScanner.ScanNetworkAsync(baseIp, _cancellationTokenSource.Token);

                foreach (var ip in activeIps)
                {
                    ipListView.Items.Add(ip);
                }

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
                progressRing.Visibility = Visibility.Collapsed; // Lo ocultamos al finalizar
                _cancellationTokenSource = null;

                // Rehabilitamos el botón después de que el proceso termine
                scanButton.IsEnabled = true;
            }
        }

        private void OnExitButtonClicked(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }

        private async void OnIpSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ipListView.SelectedItem is string selectedIp)
            {
                _cancellationTokenSource = new CancellationTokenSource();

                statusText.Text = $"Escaneando puertos para {selectedIp}...";
                progressRing.IsActive = true;
                portsListView.Items.Clear();

                try
                {
                    // Instanciamos PortRegistry para obtener los puertos registrados
                    var portRegistry = new PortRegistry();
                    var registeredPorts = portRegistry.GetRegisteredPortNumbers();

                    // Escaneamos únicamente los puertos registrados
                    var openPorts = await NetworkScanner.ScanPortsAsync(selectedIp, registeredPorts, _cancellationTokenSource.Token);

                    // Llenamos la lista con los resultados
                    portsListView.Items.Clear();

                    foreach (var port in openPorts)
                    {
                        var portDetails = portRegistry.FindByPortNumber(port.PortNumber);
                        portsListView.Items.Add(portDetails);
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
                    _cancellationTokenSource = null;
                }
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

                // Asignar los datos al ListBox
                remoteDirListBox.ItemsSource = remoteFiles; // remoteFiles es List<FileItem>
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

            string selectedFile = localDirListBox.SelectedItem.ToString();
            string localFilePath = Path.Combine(localDirectory, selectedFile);
            string remoteFilePath = remoteDirectory.TrimEnd('/') + "/" + selectedFile;

            progressRing.IsActive = true; // Mostrar indicador 
            try
            {
                await _sftpManager.UploadFileAsync(localFilePath, remoteFilePath);
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

            string selectedFile = remoteDirListBox.SelectedItem.ToString();
            string remoteFilePath = remoteDirectory.TrimEnd('/') + "/" + selectedFile;
            string localFilePath = Path.Combine(localDirectory, selectedFile);

            try
            {
                await _sftpManager.DownloadFileAsync(remoteFilePath, localFilePath);
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
                        remoteDirectory = parentDirectory;
                    }
                    else if (selectedItem.Type == "D")
                    {
                        // Navegar al directorio seleccionado
                        remoteDirectory = Path.Combine(remoteDirectory, selectedItem.Name).Replace('\\', '/');
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
                    localDirectory = Path.Combine(localDirectory, selectedItem.Name);
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
                localDirectory = Path.GetDirectoryName(localDirectory);
                _ = LoadLocalFiles(localDirectory);
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

        private async Task<string> ShowDirectoryDialogAsync()
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
