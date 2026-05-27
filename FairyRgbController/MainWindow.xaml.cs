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
            AppLogger.WriteLine("INIT", "MainWindow constructor...");
            InitializeComponent();
            AppLogger.WriteLine("INIT", "XAML OK");
            _fairyService = new HelloFairyService();
            _fairyService.StatusChanged += OnServiceStatus;
            _fairyService.DevicesUpdated += OnDevicesUpdated;
            UpdateColorPreview();
            AppLogger.WriteLine("INIT", "Ready");
        }

        private void OnServiceStatus(object? sender, string msg)
            => Dispatcher.Invoke(() => ActionFeedback.Text = msg);

        private void OnDevicesUpdated(object? sender, List<BleDeviceInfo> devices)
        {
            Dispatcher.Invoke(() =>
            {
                _foundDevices = devices;
                DevicesListBox.ItemsSource = null;
                DevicesListBox.ItemsSource = devices;
                DeviceCountText.Text = devices.Count > 0
                    ? $"Found {devices.Count} device(s)"
                    : "Scanning... no devices yet";
                ConnectButton.IsEnabled = devices.Count > 0;
            });
        }

        private void UpdateStatus(string text, bool connected)
        {
            StatusText.Text = connected ? "🟢 Connected" : "🔴 Disconnected";
            StatusBadge.Background = new SolidColorBrush(
                connected ? Color.FromRgb(0x1A, 0x3A, 0x2E) : Color.FromRgb(0x2A, 0x2A, 0x4E));
            StatusText.Foreground = new SolidColorBrush(
                connected ? Color.FromRgb(0x4C, 0xAF, 0x50) : Color.FromRgb(0x88, 0x88, 0xAA));
        }

        // ═══ SCAN ═══
        private async void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            ScanButton.IsEnabled = false;
            ScanButton.Content = "⏳ Scanning...";
            DevicesListBox.ItemsSource = null;
            _foundDevices.Clear();
            ConnectButton.IsEnabled = false;
            DeviceCountText.Text = "Scanning BLE devices...";

            try
            {
                AppLogger.WriteLine("SCAN", "Starting BLE scan with DeviceWatcher...");
                var devices = await _fairyService.ScanAsync(12000);
                _foundDevices = devices.ToList();
                AppLogger.WriteLine("SCAN", $"Found {_foundDevices.Count} devices");
                DevicesListBox.ItemsSource = _foundDevices;
                DeviceCountText.Text = _foundDevices.Count > 0
                    ? $"Found {_foundDevices.Count} device(s)"
                    : "No BLE devices found. Check power & range.";
                ConnectButton.IsEnabled = _foundDevices.Count > 0;
                if (_foundDevices.Count == 1)
                    DevicesListBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                AppLogger.Error("SCAN", ex);
                ActionFeedback.Text = $"Scan error: {ex.Message}";
                DeviceCountText.Text = "Scan failed";
            }
            finally
            {
                ScanButton.IsEnabled = true;
                ScanButton.Content = "🔄 Scan for BLE";
            }
        }

        private void DevicesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedDevice = DevicesListBox.SelectedItem as BleDeviceInfo;
            ConnectButton.IsEnabled = _selectedDevice != null;
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
                AppLogger.WriteLine("CONNECT", $"Connecting to {_selectedDevice.Name}...");
                await _fairyService.ConnectAsync(_selectedDevice);
                _isPowerOn = false;
                PowerButton.Content = "🔛 TURN ON";
                PowerButton.Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x4A, 0x2E));
                SetColorButton.IsEnabled = true;
                DisconnectButton.IsEnabled = true;
                UpdateStatus($"Connected to {_selectedDevice.Name} ✓", true);
                AppLogger.WriteLine("CONNECT", $"Connected to {_selectedDevice.Name}");
            }
            catch (Exception ex)
            {
                AppLogger.Error("CONNECT", ex);
                MessageBox.Show($"Connection failed:\n{ex.Message}",
                    "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ConnectButton.IsEnabled = true;
                ScanButton.IsEnabled = true;
            }
            finally
            {
                ConnectButton.Content = "🔗 Connect";
            }
        }

        private async void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            await _fairyService.DisconnectAsync();
            _isPowerOn = false;
            SetColorButton.IsEnabled = false;
            DisconnectButton.IsEnabled = false;
            PowerButton.Content = "🔛 TURN ON";
            PowerButton.Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x4A, 0x2E));
            ScanButton.IsEnabled = true;
            UpdateStatus("Disconnected.", false);
        }

        // ═══ COLOR ═══
        private void ColorSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isSettingColor) UpdateColorPreview();
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

            if (h < 60) { r = c; g = x; }
            else if (h < 120) { r = x; g = c; }
            else if (h < 180) { g = c; b = x; }
            else if (h < 240) { g = x; b = c; }
            else if (h < 300) { r = x; b = c; }
            else { r = c; b = x; }

            ColorPreview.Background = new SolidColorBrush(Color.FromRgb(
                (byte)((r + m) * 255),
                (byte)((g + m) * 255),
                (byte)((b + m) * 255)));
        }

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
                    _ = SendColorAsync((int)h, (int)s, (int)v);
                }
            }
        }

        private async void SetColorButton_Click(object sender, RoutedEventArgs e)
            => await SendColorAsync((int)HueSlider.Value, (int)SatSlider.Value, (int)BrightSlider.Value);

        private async Task SendColorAsync(int hue, int sat, int val)
        {
            if (!await _fairyService.IsConnectedAsync())
            {
                ActionFeedback.Text = "Not connected.";
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
                ActionFeedback.Text = $"Color sent! H:{hue} S:{sat} V:{val}";
            }
            catch (Exception ex)
            {
                ActionFeedback.Text = $"Color failed: {ex.Message}";
                AppLogger.Error("COLOR", ex);
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
            if (!await _fairyService.IsConnectedAsync()) { ActionFeedback.Text = "Not connected."; return; }

            if (sender is Button btn && btn.Tag is string tagStr && byte.TryParse(tagStr, out byte presetId))
            {
                try
                {
                    btn.IsEnabled = false;
                    await _fairyService.SetPowerAsync(true);
                    await Task.Delay(50);
                    await _fairyService.SetPresetAsync(presetId, (int)SpeedSlider.Value);
                    _isPowerOn = true;
                    PowerButton.Content = "🔴 TURN OFF";
                    PowerButton.Background = new SolidColorBrush(Color.FromRgb(0x4A, 0x2A, 0x2E));
                    ActionFeedback.Text = $"Effect #{presetId} activated";
                }
                catch (Exception ex)
                {
                    ActionFeedback.Text = $"Effect failed: {ex.Message}";
                    AppLogger.Error("EFFECT", ex);
                }
                finally { btn.IsEnabled = true; }
            }
        }

        // ═══ POWER ═══
        private async void PowerButton_Click(object sender, RoutedEventArgs e)
        {
            if (!await _fairyService.IsConnectedAsync()) { ActionFeedback.Text = "Not connected."; return; }
            try
            {
                PowerButton.IsEnabled = false;
                _isPowerOn = !_isPowerOn;
                await _fairyService.SetPowerAsync(_isPowerOn);
                PowerButton.Content = _isPowerOn ? "🔴 TURN OFF" : "🔛 TURN ON";
                PowerButton.Background = new SolidColorBrush(
                    _isPowerOn ? Color.FromRgb(0x4A, 0x2A, 0x2E) : Color.FromRgb(0x2A, 0x4A, 0x2E));
                ActionFeedback.Text = _isPowerOn ? "Power ON" : "Power OFF";
            }
            catch (Exception ex)
            {
                _isPowerOn = !_isPowerOn;
                AppLogger.Error("POWER", ex);
                ActionFeedback.Text = $"Power failed: {ex.Message}";
            }
            finally { PowerButton.IsEnabled = true; }
        }

        protected override void OnClosed(EventArgs e)
        {
            _ = _fairyService.DisconnectAsync();
            base.OnClosed(e);
        }
    }
}