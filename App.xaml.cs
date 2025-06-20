using System;
using Windows.ApplicationModel;
using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BeaconScan
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
  
        public App()
        {
            this.InitializeComponent();
            // Global exception handling

            UnhandledException += (sender, e) =>
            {
                // Check if the exception is not null before using it.
                if (e.Exception != null)
                {
                    System.Diagnostics.Debug.WriteLine("Unhandled exception:");
                    System.Diagnostics.Debug.WriteLine(e.Exception.ToString());
                    System.Diagnostics.Debug.WriteLine("Stack Trace:");
                    System.Diagnostics.Debug.WriteLine(e.Exception.StackTrace);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Unhandled exception: (Exception object not found)");
                }

                // If the debugger is attached, break execution
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    System.Diagnostics.Debugger.Break();
                }
            };
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            var version = Package.Current.Id.Version;
            m_window = new MainWindow(version);
            m_window.Activate();
        }

        private Window? m_window;
    }
}
