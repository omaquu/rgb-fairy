using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
        private bool _isSettingColor;

        public MainWindow()
        {
            InitializeComponent();
            _fairyService = new HelloFairyService();
            _fairyService.StatusChanged += OnServiceStatusChanged;
            UpdateColorPreview();
        }

        private void OnServiceStatusChanged(object? sender, string message)
        {
            Dispatcher.Invoke(() =>
            {
                ActionFeedback.Text = message;
            });
        }

        private void UpdateStatusBar(string text, bool isConnected)
        {
            StatusText.Text = isConnected ? "🟢 Connected" : "🔴 Disconnected";
            if (isConnected)
            {
                StatusBadge.Background = new SolidColorBrush(Color.FromRgb(0x1A, 0x3A, 0x2E));
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50));
            }
            else
            {
                StatusBadge.Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x4E));
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0xAA));
            }
            ActionFeedback.Text = text;
        }

        // ═══ SCAN ═══
        private async void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            ScanButton.IsEnabled = false;
            ScanButton.Content = "⏳ Scanning...";
            DevicesListBox.ItemsSource = null;
            _foundDevices.Clear();
            ConnectButton.IsEnabled = false;

            try
            {
                var devices = await _fairyService.ScanAsync(12000);
                _foundDevices = devices.ToList();
                DevicesListBox.ItemsSource = _foundDevices;

                if (_foundDevices.Count == 0)
                {
                    ActionFeedback.Text = "No BLE devices found. Check device is powered on and try again.";
                }
                else
                {
                    ConnectButton.IsEnabled = _foundDevices.Any();

                    // Auto-select if only one device found
                    if (_foundDevices.Count == 1)
                    {
                        DevicesListBox.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Scan failed:\n{ex.Message}", "Scan Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                ActionFeedback.Text = "Scan failed. Try again.";
            }
            finally
            {
                ScanButton.IsEnabled = true;
                ScanButton.Content = "🔄 Scan for Devices";
            }
        }

        // ═══ SELECT DEVICE ═══
        private void DevicesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedDevice = DevicesListBox.SelectedItem as BleDeviceInfo;
            ConnectButton.IsEnabled = _selectedDevice != null;
            if (_selectedDevice != null)
            {
                ActionFeedback.Text = $"Selected: {_selectedDevice.Name}";
            }
        }

        // ═══ CONNECT ═══
        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDevice == null) return;

            ConnectButton.IsEnabled = false;
            ConnectButton.Content = "⏳ Connecting...";
            ScanButton.IsEnabled = false;

            try
            {
                await _fairyService.ConnectAsync(_selectedDevice);

                _isPowerOn = false;
                PowerButton.Content = "🔛 TURN ON";
                PowerButton.Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x4A, 0x2E));
                SetColorButton.IsEnabled = true;
                DisconnectButton.IsEnabled = true;
                UpdateStatusBar($"Connected to {_selectedDevice.Name} ✓", true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Connection failed:\n{ex.Message}\n\n" +
                    "Tips:\n" +
                    "• Make sure the fairy device is powered on\n" +
                    "• Try unpairing the device in Windows Bluetooth settings\n" +
                    "• Restart the device and scan again",
                    "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ActionFeedback.Text = "Connection failed.";
                ConnectButton.IsEnabled = true;
                ScanButton.IsEnabled = true;
            }
            finally
            {
                ConnectButton.Content = "🔗 Connect";
            }
        }

        // ═══ DISCONNECT ═══
        private async void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            await _fairyService.DisconnectAsync();
            _isPowerOn = false;
            SetColorButton.IsEnabled = false;
            DisconnectButton.IsEnabled = false;
            PowerButton.Content = "🔛 TURN ON";
            PowerButton.Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x4A, 0x2E));
            ScanButton.IsEnabled = true;
            UpdateStatusBar("Disconnected.", false);
        }

        // ═══ COLOR SLIDERS ═══
        private void ColorSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isSettingColor)
                UpdateColorPreview();
        }

        private void UpdateColorPreview()
        {
            double h = HueSlider.Value;
            double s = SatSlider.Value / 1000.0;
            double v = BrightSlider.Value / 1000.0;

            double r = 0, g = 0, b = 0;
            double c = v * s;
            double x = c * (1 - Math.Abs((h / 60.0) % 2 - 1));
            double m = v - c;

            if (h < 60) { r = c; g = x; b = 0; }
            else if (h < 120) { r = x; g = c; b = 0; }
            else if (h < 180) { r = 0; g = c; b = x; }
            else if (h < 240) { r = 0; g = x; b = c; }
            else if (h < 300) { r = x; g = 0; b = c; }
            else { r = c; g = 0; b = x; }

            ColorPreviewBrush.Color = Color.FromRgb(
                (byte)((r + m) * 255),
                (byte)((g + m) * 255),
                (byte)((b + m) * 255));
        }

        // ═══ PRESET COLORS ═══
        private void PresetColor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                var parts = tag.Split(',');
                if (parts.Length == 3 &&
                    double.TryParse(parts[0], out double h) &&
                    double.TryParse(parts[1], out double s) &&
                    double.TryParse(parts[2], out double v))
                {
                    _isSettingColor = true;
                    HueSlider.Value = h;
                    SatSlider.Value = s;
                    BrightSlider.Value = v;
                    _isSettingColor = false;
                    UpdateColorPreview();

                    // Auto-send if connected
                    _ = SendColorAsync((int)h, (int)s, (int)v);
                }
            }
        }

        // ═══ SEND COLOR ═══
        private async void SetColorButton_Click(object sender, RoutedEventArgs e)
        {
            await SendColorAsync((int)HueSlider.Value, (int)SatSlider.Value, (int)BrightSlider.Value);
        }

        private async Task SendColorAsync(int hue, int sat, int val)
        {
            if (!await _fairyService.IsConnectedAsync())
            {
                ActionFeedback.Text = "Not connected. Connect to a device first.";
                return;
            }

            try
            {
                SetColorButton.IsEnabled = false;
                SetColorButton.Content = "⏳ Sending...";
                await _fairyService.SetPowerAsync(true);
                await Task.Delay(50);
                await _fairyService.SetHsvAsync(hue, sat, val);
                _isPowerOn = true;
                PowerButton.Content = "🔴 TURN OFF";
                PowerButton.Background = new SolidColorBrush(Color.FromRgb(0x4A, 0x2A, 0x2E));
                ActionFeedback.Text = $"Color sent! H:{hue}° S:{sat} V:{val}";
            }
            catch (Exception ex)
            {
                ActionFeedback.Text = $"Failed to send color: {ex.Message}";
            }
            finally
            {
                SetColorButton.IsEnabled = true;
                SetColorButton.Content = "🎨 Send Color";
            }
        }

        // ═══ EFFECTS ═══
        private async void EffectButton_Click(object sender, RoutedEventArgs e)
        {
            if (!await _fairyService.IsConnectedAsync())
            {
                ActionFeedback.Text = "Not connected. Connect to a device first.";
                return;
            }

            if (sender is Button btn && btn.Tag is string tagStr && byte.TryParse(tagStr, out byte presetId))
            {
                try
                {
                    btn.IsEnabled = false;
                    int brightness = (int)SpeedSlider.Value;
                    await _fairyService.SetPowerAsync(true);
                    await Task.Delay(50);
                    await _fairyService.SetPresetAsync(presetId, brightness);
                    _isPowerOn = true;
                    PowerButton.Content = "🔴 TURN OFF";
                    PowerButton.Background = new SolidColorBrush(Color.FromRgb(0x4A, 0x2A, 0x2E));
                    ActionFeedback.Text = $"Effect {presetId} activated at speed {brightness}";
                }
                catch (Exception ex)
                {
                    ActionFeedback.Text = $"Effect failed: {ex.Message}";
                }
                finally
                {
                    btn.IsEnabled = true;
                }
            }
        }

        // ═══ POWER ═══
        private async void PowerButton_Click(object sender, RoutedEventArgs e)
        {
            if (!await _fairyService.IsConnectedAsync())
            {
                ActionFeedback.Text = "Not connected.";
                return;
            }

            try
            {
                PowerButton.IsEnabled = false;
                _isPowerOn = !_isPowerOn;
                await _fairyService.SetPowerAsync(_isPowerOn);

                if (_isPowerOn)
                {
                    PowerButton.Content = "🔴 TURN OFF";
                    PowerButton.Background = new SolidColorBrush(Color.FromRgb(0x4A, 0x2A, 0x2E));
                    ActionFeedback.Text = "Power ON";
                }
                else
                {
                    PowerButton.Content = "🔛 TURN ON";
                    PowerButton.Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x4A, 0x2E));
                    ActionFeedback.Text = "Power OFF";
                }
            }
            catch (Exception ex)
            {
                ActionFeedback.Text = $"Power toggle failed: {ex.Message}";
                _isPowerOn = !_isPowerOn; // revert
            }
            finally
            {
                PowerButton.IsEnabled = true;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _fairyService.StatusChanged -= OnServiceStatusChanged;
            _ = _fairyService.DisconnectAsync();
            base.OnClosed(e);
        }
    }
}