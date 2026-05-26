using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using FairyRgbController.Services;
using FairyRgbController.Models;

namespace FairyRgbController
{
    public partial class MainWindow : Window
    {
        private readonly IFairyLedService _fairyService;
        private List<BleDeviceInfo> _foundDevices = new();
        private BleDeviceInfo? _selectedDevice;

        public MainWindow()
        {
            InitializeComponent();
            _fairyService = new HelloFairyService();
        }

        private async void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            ScanButton.IsEnabled = false;
            DevicesListBox.ItemsSource = null;
            _foundDevices.Clear();

            try
            {
                var devices = await _fairyService.ScanAsync(10000);
                _foundDevices = devices.ToList();
                DevicesListBox.ItemsSource = _foundDevices;
                ConnectButton.IsEnabled = _foundDevices.Any();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Scan failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ScanButton.IsEnabled = true;
            }
        }

        private void DevicesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedDevice = DevicesListBox.SelectedItem as BleDeviceInfo;
            ConnectButton.IsEnabled = _selectedDevice != null;
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDevice == null) return;

            ConnectButton.IsEnabled = false;

            try
            {
                await _fairyService.ConnectAsync(_selectedDevice);
                SetColorButton.IsEnabled = true;
                SetEffectButton.IsEnabled = true;
                ScanButton.IsEnabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to connect: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ConnectButton.IsEnabled = true;
            }
        }

        private async void SetColorButton_Click(object sender, RoutedEventArgs e)
        {
            if (!await _fairyService.IsConnectedAsync()) return;

            try
            {
                // Cycle through a few colors as a demo
                await _fairyService.SetHsvAsync(0, 1000, 500);  // Red
                await Task.Delay(500);
                await _fairyService.SetHsvAsync(120, 1000, 500); // Green
                await Task.Delay(500);
                await _fairyService.SetHsvAsync(240, 1000, 500); // Blue
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to set color: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SetEffectButton_Click(object sender, RoutedEventArgs e)
        {
            if (!await _fairyService.IsConnectedAsync()) return;

            try
            {
                // Cycle through preset effects
                await _fairyService.SetPresetAsync(1, 500);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to set effect: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}