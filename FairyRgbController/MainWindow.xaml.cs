using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using FairyRgbController.Models;
using FairyRgbController.Services;

namespace FairyRgbController
{
    public partial class MainWindow : Window
    {
        private readonly HelloFairyService _fairyService;
        private List<BleDeviceInfo> _foundDevices = new();
        private BleDeviceInfo? _selectedDevice;
        private bool _isPowerOn;

        public MainWindow()
        {
            AppLogger.WriteLine("INIT", "MainWindow constructor...");
            InitializeComponent();
            AppLogger.WriteLine("INIT", "XAML initialized.");
            _fairyService = new HelloFairyService();
            _fairyService.StatusChanged += (s, msg) => Dispatcher.Invoke(() => ActionFeedback.Text = msg);
            _fairyService.DevicesUpdated += (s, devices) => Dispatcher.Invoke(() => {
                _foundDevices = devices;
                DevicesListBox.ItemsSource = devices;
                if (devices.Count > 0 && _selectedDevice == null)
                {
                    DevicesListBox.SelectedIndex = 0;
                    _selectedDevice = devices[0];
                    ConnectButton.IsEnabled = true;
                }
            });
            _fairyService.AutoConnected += (s, device) => Dispatcher.Invoke(() => {
                _selectedDevice = device;
                ConnectButton.IsEnabled = false;
                DisconnectButton.IsEnabled = true;
                PowerButton.IsEnabled = true;
                StatusLabel.Text = $"Connected to {device.Name}";
                ActionFeedback.Text = $"{device.Name} connected - you can now control it!";
            });
            AppLogger.WriteLine("INIT", "Ready.");

            Loaded += async (s, e) =>
            {
                AppLogger.WriteLine("AUTO", "Starting scan...");
                await Task.Delay(500);
                ScanButton_Click(ScanButton, new RoutedEventArgs());
            };
        }

        private async void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            ScanButton.IsEnabled = false;
            ScanButton.Content = "⏳ Scanning...";
            try
            {
                await _fairyService.ScanAsync(30000);
            }
            catch (Exception ex)
            {
                ActionFeedback.Text = $"Error: {ex.Message}";
            }
            finally
            {
                ScanButton.IsEnabled = true;
                ScanButton.Content = "🔄 Scan";
            }
        }

        private void DevicesListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            _selectedDevice = DevicesListBox.SelectedItem as BleDeviceInfo;
            ConnectButton.IsEnabled = _selectedDevice != null && !DisconnectButton.IsEnabled;
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDevice == null) return;
            ConnectButton.IsEnabled = false;
            try
            {
                await _fairyService.ConnectAsync(_selectedDevice);
                // AutoConnected event handles the rest
            }
            catch (Exception ex)
            {
                ActionFeedback.Text = $"Connection failed: {ex.Message}";
                ConnectButton.IsEnabled = true;
            }
        }

        private async void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            await _fairyService.DisconnectAsync();
            DisconnectButton.IsEnabled = false;
            ConnectButton.IsEnabled = _foundDevices.Count > 0;
            PowerButton.IsEnabled = false;
            PowerButton.Content = "🔛 TURN ON";
            _isPowerOn = false;
            StatusLabel.Text = "Disconnected";
            ActionFeedback.Text = "Disconnected";
        }

        private async void PowerButton_Click(object sender, RoutedEventArgs e)
        {
            _isPowerOn = !_isPowerOn;
            await _fairyService.SetPowerAsync(_isPowerOn);
            PowerButton.Content = _isPowerOn ? "🔴 TURN OFF" : "🔛 TURN ON";
        }
    }
}