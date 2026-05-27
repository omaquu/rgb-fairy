using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using FairyRgbController.Services;
using FairyRgbController.Models;

namespace FairyRgbController
{
    public partial class MainWindow : Window
    {
        private readonly HelloFairyService _fairyService;
        private BleDeviceInfo? _lastSelectedDevice;

        public MainWindow()
        {
            AppLogger.WriteLine("INIT", "MainWindow constructor...");
            InitializeComponent();
            AppLogger.WriteLine("INIT", "XAML initialized successfully!");
            _fairyService = new HelloFairyService();
            _fairyService.StatusChanged += (s, msg) =>
                Dispatcher.Invoke(() => StatusLabel.Text = msg);
            AppLogger.WriteLine("INIT", "MainWindow ready.");
        }

        private async void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            ScanButton.IsEnabled = false;
            StatusLabel.Text = "Scanning...";
            try
            {
                var devices = await _fairyService.ScanAsync(10000);
                var list = devices.ToList();
                StatusLabel.Text = list.Count > 0
                    ? $"Found {list.Count} device(s): {string.Join(", ", list.Select(d => d.Name))}"
                    : "No devices found";
                _lastSelectedDevice = list.FirstOrDefault();
            }
            catch (Exception ex)
            {
                StatusLabel.Text = $"Scan failed: {ex.Message}";
                AppLogger.Error("Scan", ex);
            }
            finally
            {
                ScanButton.IsEnabled = true;
            }
        }
    }
}