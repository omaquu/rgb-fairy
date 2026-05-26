using System;
using System.Windows;

namespace FairyRgbController
{
    public partial class App : Application
    {
        public App()
        {
            // Catch all unhandled exceptions and show them in a dialog
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                MessageBox.Show(
                    $"Fatal error:\n{ex?.Message}\n\n{ex?.StackTrace}",
                    "Fairy RGB Controller - Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            };

            DispatcherUnhandledException += (sender, args) =>
            {
                MessageBox.Show(
                    $"UI Error:\n{args.Exception.Message}",
                    "Fairy RGB Controller",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                args.Handled = true;
            };
        }
    }
}