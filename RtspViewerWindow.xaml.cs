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

namespace BeaconScan
{
    public sealed partial class RtspViewerPage : Page
    {
        public RtspViewerPage()
        {
            this.InitializeComponent();
        }

        public async void PlayStream(string rtspUrl)
        {
            try
            {
                LoadingText.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                RtspWebView.Visibility = Microsoft.UI.Xaml.Visibility.Visible;

                // Inicializa WebView2 y carga la URL
                await RtspWebView.EnsureCoreWebView2Async();
                RtspWebView.Source = new Uri(rtspUrl);
            }
            catch (Exception ex)
            {
                LoadingText.Text = $"Error al cargar el stream: {ex.Message}";
            }
        }
    }
}
