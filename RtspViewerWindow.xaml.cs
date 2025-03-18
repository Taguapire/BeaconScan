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
using LibVLCSharp.Shared;
using System.Runtime.InteropServices;
using System.Diagnostics;


namespace BeaconScan
{
    public sealed partial class RtspViewerPage : Window
    {
        private LibVLC _libVLC;
        private MediaPlayer _mediaPlayer;

        public RtspViewerPage()
        {
            InitializeComponent();
            Core.Initialize();

            // Inicializar LibVLC y MediaPlayer
            _libVLC = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVLC);

            // Configurar el destino de renderizado para SwapChainPanel
            IntPtr videoHwnd = GetSwapChainPanelHandle(VideoPanel);
            _mediaPlayer.Hwnd = videoHwnd;
        }

        public void PlayStream(string rtspUrl)
        {
            try
            {
                var media = new Media(_libVLC, new Uri(rtspUrl));
                _mediaPlayer.Play(media);
                Debug.WriteLine($"Reproduciendo stream RTSP: {rtspUrl}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al intentar reproducir el stream RTSP: {ex.Message}");
            }
        }

        //protected override void OnClosed(EventArgs e)
        //{
        //    base.OnClosed(e);
        //    _mediaPlayer?.Dispose();
        //    _libVLC?.Dispose();
        //}

        private IntPtr GetSwapChainPanelHandle(SwapChainPanel swapChainPanel)
        {
            // Utiliza Reflection para obtener el puntero nativo (esta técnica es necesaria en WinUI)
            var swapChainPanelNative = Marshal.GetIUnknownForObject(swapChainPanel);
            return swapChainPanelNative;
        }

    }
}