using System;
using System.IO;
using System.Windows;

namespace FairyRgbController
{
    public partial class App : Application
    {
        public App()
        {
            AppLogger.WriteLine("INIT", "App constructor starting...");

            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                AppLogger.Error("FATAL", ex ?? new Exception("Unknown fatal error"));
                MessageBox.Show(
                    $"Fatal error:\n{ex?.Message}\n\n" +
                    $"Check log: {Path.Combine(Path.GetTempPath(), "FairyRgbController.log")}",
                    "Fairy RGB Controller - Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            };

            DispatcherUnhandledException += (sender, args) =>
            {
                AppLogger.Error("UI", args.Exception);
                MessageBox.Show(
                    $"UI Error:\n{args.Exception.Message}\n\n" +
                    $"Check log: {Path.Combine(Path.GetTempPath(), "FairyRgbController.log")}",
                    "Fairy RGB Controller",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                args.Handled = true;
            };

            AppLogger.WriteLine("INIT", "App constructor done.");
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            AppLogger.WriteLine("INIT", "OnStartup...");
            base.OnStartup(e);
            AppLogger.WriteLine("INIT", "OnStartup done.");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            AppLogger.WriteLine("SHUTDOWN", "App exiting.");
            base.OnExit(e);
        }
    }
}