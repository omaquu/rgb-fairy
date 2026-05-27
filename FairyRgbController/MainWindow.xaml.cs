using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
        private byte _selectedPresetId = 1;
        private List<SavedColor> _savedColors = new();
        private bool _isUpdatingSliders;

        // Preset mode definitions (id, name, icon, description)
        private static readonly PresetDef[] Presets = new PresetDef[]
        {
            new PresetDef(1,  "Kiinteä",        "💡", "Yksi väri, ei tehostetta"),
            new PresetDef(2,  "Hengitä",        "🌫", "Pehmeä sisään/ulos-hengitys"),
            new PresetDef(3,  "Värihyppy",      "🔀", "Hyppii satunnaisesti värien välillä"),
            new PresetDef(4,  "Aalto",          "🌊", "Väri kulkee aaltomaisesti"),
            new PresetDef(5,  "Stroboskooppi",  "⚡", "Nopea välkyntä"),
            new PresetDef(6,  "Heikkeneminen",  "💨", "Värit himmentyvät pehmeästi"),
            new PresetDef(7,  "Sadekaari",      "🌈", "Sateenkaari-pyörteinen"),
            new PresetDef(8,  "7 väriä",        "🎆", "Kaikki 7 väriä vuorotellen"),
            new PresetDef(9,  "RGB-ajo",        "🔁", "RGB-värien ajosykli"),
            new PresetDef(10, "DIY 1",          "1️⃣", "Räätälöity tila 1"),
            new PresetDef(11, "DIY 2",          "2️⃣", "Räätälöity tila 2"),
            new PresetDef(12, "Räjähtävä",      "💥", "Värit räjähtävät ulos"),
            new PresetDef(13, "Virtaava",       "➡",  "Virtaava yhden suuntainen"),
            new PresetDef(14, "Jazz",           "🎷", "Värikäs jazz"),
            new PresetDef(15, "Disco",          "🪩", "Disco-tehoste"),
            new PresetDef(16, "Valoisa",        "☀", "Kirkas valo"),
        };

        public MainWindow()
        {
            AppLogger.WriteLine("INIT", "MainWindow constructing full UI...");
            try
            {
                AppLogger.WriteLine("INIT", "Calling InitializeComponent...");
                InitializeComponent();
                AppLogger.WriteLine("INIT", "XAML initialized.");
            }
            catch (Exception ex)
            {
                AppLogger.Error("INIT", ex);
                throw;
            }
            _fairyService = new HelloFairyService();
            _fairyService.StatusChanged += (s, msg) => Dispatcher.Invoke(() => ActionFeedback.Text = msg);
            _fairyService.DevicesUpdated += OnDevicesUpdated;
            _fairyService.AutoConnected += OnAutoConnected;
            AppLogger.WriteLine("INIT", "Events wired.");
            BuildPresetButtons();
            LoadSavedColors();
            AppLogger.WriteLine("INIT", "Preset buttons and saved colors built.");
            UpdateColorPreview();
            AppLogger.WriteLine("INIT", "Ready.");

            Loaded += async (s, e) =>
            {
                AppLogger.WriteLine("AUTO", "Loaded event fired!");
                try
                {
                    await Task.Delay(500);
                    AppLogger.WriteLine("AUTO", "Calling ScanButton_Click...");
                    ScanButton_Click(ScanButton, new RoutedEventArgs());
                    AppLogger.WriteLine("AUTO", "ScanButton_Click completed.");
                }
                catch (Exception ex)
                {
                    AppLogger.Error("AUTO", ex);
                }
            };

            ContentRendered += (s, e) => AppLogger.WriteLine("AUTO", "ContentRendered - window visible!");
        }

        #region Device Management

        private void OnDevicesUpdated(object? sender, List<BleDeviceInfo> devices)
        {
            _foundDevices = devices;
            DevicesListBox.ItemsSource = devices;
            if (devices.Count > 0 && _selectedDevice == null)
            {
                DevicesListBox.SelectedIndex = 0;
                _selectedDevice = devices[0];
                ConnectButton.IsEnabled = true;
            }
        }

        private void OnAutoConnected(object? sender, BleDeviceInfo device)
        {
            _selectedDevice = device;
            ConnectButton.IsEnabled = false;
            DisconnectButton.IsEnabled = true;
            PowerButton.IsEnabled = true;
            ApplyColorButton.IsEnabled = true;
            SaveCurrentColorButton.IsEnabled = true;
            QuickOffButton.IsEnabled = true;
            DeviceInfoPanel.Visibility = Visibility.Visible;
            ConnectedDeviceName.Text = device.Name;
            HeaderStatus.Text = $"Yhdistetty: {device.Name}";
            ConnectionDot.Fill = new SolidColorBrush(Color.FromRgb(0x44, 0xFF, 0x88));
            StatusLabel.Text = $"Yhdistetty: {device.Name}";
            ActionFeedback.Text = $"{device.Name} valmiina — säädä väriä!";
        }

        private async void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            ScanButton.IsEnabled = false;
            ScanButton.Content = "⏳ Skannataan...";
            _selectedDevice = null;
            DevicesListBox.ItemsSource = null;
            ConnectButton.IsEnabled = false;
            try
            {
                await _fairyService.ScanAsync(15000);
            }
            catch (Exception ex)
            {
                ActionFeedback.Text = $"Virhe: {ex.Message}";
                AppLogger.Error("Scan", ex);
            }
            finally
            {
                ScanButton.IsEnabled = true;
                ScanButton.Content = "🔄 Hae uudelleen";
            }
        }

        private void DevicesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
            }
            catch (Exception ex)
            {
                ActionFeedback.Text = $"Yhteys epäonnistui: {ex.Message}";
                AppLogger.Error("Connect", ex);
                ConnectButton.IsEnabled = true;
            }
        }

        private async void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            await _fairyService.DisconnectAsync();
            DisconnectButton.IsEnabled = false;
            ConnectButton.IsEnabled = _foundDevices.Count > 0;
            PowerButton.IsEnabled = false;
            PowerButton.Content = "🔴 OFF";
            _isPowerOn = false;
            ApplyColorButton.IsEnabled = false;
            SaveCurrentColorButton.IsEnabled = false;
            DeviceInfoPanel.Visibility = Visibility.Collapsed;
            HeaderStatus.Text = "Irrotettu";
            ConnectionDot.Fill = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44));
            StatusLabel.Text = "Irrotettu";
            ActionFeedback.Text = "Laite irrotettu";
            QuickOffButton.IsEnabled = false;
        }

        #endregion

        #region Power

        private async void PowerButton_Click(object sender, RoutedEventArgs e)
        {
            _isPowerOn = !_isPowerOn;
            PowerButton.Content = _isPowerOn ? "🟢 ON" : "🔴 OFF";
            if (_isPowerOn)
                PowerButton.Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x4A, 0x2E));
            else
                PowerButton.Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E));
            await _fairyService.SetPowerAsync(_isPowerOn);
        }

        private async void QuickOff_Click(object sender, RoutedEventArgs e)
        {
            _isPowerOn = false;
            PowerButton.Content = "🔴 OFF";
            PowerButton.Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E));
            await _fairyService.SetPowerAsync(false);
            ActionFeedback.Text = "Valot sammutettu";
        }

        #endregion

        #region Color Picker

        private void BuildPresetButtons()
        {
            PresetButtons.Children.Clear();
            foreach (var preset in Presets)
            {
                var btn = new Button
                {
                    Content = new StackPanel
                    {
                        Children =
                        {
                            new TextBlock { Text = preset.Icon, FontSize = 22, HorizontalAlignment = HorizontalAlignment.Center },
                            new TextBlock { Text = preset.Name, FontSize = 9, HorizontalAlignment = HorizontalAlignment.Center, Foreground = Brushes.White }
                        }
                    },
                    Tag = preset.Id,
                    Width = 68,
                    Height = 68,
                    Margin = new Thickness(2),
                    Background = new SolidColorBrush(Color.FromRgb(0x0F, 0x34, 0x60)),
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0),
                    ToolTip = preset.Desc
                };
                btn.Click += PresetButton_Click;
                PresetButtons.Children.Add(btn);
            }

            if (PresetButtons.Children.Count > 0)
                SelectPreset(1);
        }

        private void SelectPreset(byte id)
        {
            _selectedPresetId = id;
            var preset = Presets.FirstOrDefault(p => p.Id == id);
            ModeDescription.Inlines.Clear();
            ModeDescription.Inlines.Add(new System.Windows.Documents.Run(preset?.Icon + " ") { FontSize = 16 });
            ModeDescription.Inlines.Add(new System.Windows.Documents.Run(preset?.Desc ?? ""));

            foreach (Button btn in PresetButtons.Children)
            {
                if (btn.Tag is byte tag && tag == id)
                    btn.Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x6A, 0xB0));
                else
                    btn.Background = new SolidColorBrush(Color.FromRgb(0x0F, 0x34, 0x60));
            }
        }

        private async void PresetButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is byte id)
            {
                SelectPreset(id);
                await ApplyCurrentColorOrPreset();
            }
        }

        private void HsvSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isUpdatingSliders) return;
            _isUpdatingSliders = true;
            try
            {
                if (HueValue != null) HueValue.Text = $"{(int)HueSlider.Value}°";
                if (SatValue != null) SatValue.Text = $"{(int)(SatSlider.Value / 10)}%";
                if (ValValue != null) ValValue.Text = $"{(int)(ValSlider.Value / 10)}%";
                UpdateColorPreview();
            }
            catch { /* ignore during init */ }
            finally { _isUpdatingSliders = false; }
        }

        private void UpdateColorPreview()
        {
            if (ColorPreview == null || HueSlider == null || SatSlider == null || ValSlider == null) return;

            double h = HueSlider.Value;
            double s = SatSlider.Value / 1000.0;
            double v = ValSlider.Value / 1000.0;

            double c = v * s;
            double x = c * (1 - Math.Abs((h / 60.0) % 2 - 1));
            double m = v - c;
            double r = 0, g = 0, b = 0;

            if (h < 60) { r = c; g = x; b = 0; }
            else if (h < 120) { r = x; g = c; b = 0; }
            else if (h < 180) { r = 0; g = c; b = x; }
            else if (h < 240) { r = 0; g = x; b = c; }
            else if (h < 300) { r = x; g = 0; b = c; }
            else { r = c; g = 0; b = x; }

            byte R = (byte)Math.Min(255, (r + m) * 255);
            byte G = (byte)Math.Min(255, (g + m) * 255);
            byte B = (byte)Math.Min(255, (b + m) * 255);

            ColorPreview.Background = new SolidColorBrush(Color.FromRgb(R, G, B));
        }

        private async void QuickColor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string rgb)
            {
                var parts = rgb.Split(',');
                if (parts.Length == 3)
                {
                    byte r = byte.Parse(parts[0]);
                    byte g = byte.Parse(parts[1]);
                    byte b = byte.Parse(parts[2]);

                    var (h, s, v) = RgbToHsv(r, g, b);
                    _isUpdatingSliders = true;
                    HueSlider.Value = h;
                    SatSlider.Value = s;
                    ValSlider.Value = v;
                    HueValue.Text = $"{(int)h}°";
                    SatValue.Text = $"{(int)(s / 10)}%";
                    ValValue.Text = $"{(int)(v / 10)}%";
                    _isUpdatingSliders = false;
                    UpdateColorPreview();
                    await ApplyCurrentColorOrPreset();
                }
            }
        }

        private async void ApplyColor_Click(object sender, RoutedEventArgs e)
        {
            await ApplyCurrentColorOrPreset();
        }

        private async Task ApplyCurrentColorOrPreset()
        {
            if (!_isPowerOn)
            {
                _isPowerOn = true;
                PowerButton.Content = "🟢 ON";
                PowerButton.Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x4A, 0x2E));
                await _fairyService.SetPowerAsync(true);
            }

            int brightness = (int)Math.Max(100, BrightnessSlider.Value);
            try
            {
                await _fairyService.SetPresetAsync(_selectedPresetId, brightness);
                ActionFeedback.Text = $"Tehostus {_selectedPresetId} käytössä (kirkkaus {brightness / 10}%)";
            }
            catch
            {
                int h = (int)(HueSlider.Value / 360.0 * 65535);
                int s = (int)SatSlider.Value;
                int v = (int)ValSlider.Value;
                await _fairyService.SetHsvAsync(h, s, v);
                ActionFeedback.Text = "Väri asetettu!";
            }
        }

        private static (double h, double s, double v) RgbToHsv(byte r, byte g, byte b)
        {
            double rd = r / 255.0, gd = g / 255.0, bd = b / 255.0;
            double max = Math.Max(rd, Math.Max(gd, bd));
            double min = Math.Min(rd, Math.Min(gd, bd));
            double delta = max - min;

            double h = 0;
            if (delta > 0)
            {
                if (max == rd) h = 60 * (((gd - bd) / delta) % 6);
                else if (max == gd) h = 60 * (((bd - rd) / delta) + 2);
                else h = 60 * (((rd - gd) / delta) + 4);
            }
            if (h < 0) h += 360;

            double s = max == 0 ? 0 : delta / max;
            double v = max;

            return (h, s * 1000, v * 1000);
        }

        #endregion

        #region Saved Colors

        private void SaveCurrentColor_Click(object sender, RoutedEventArgs e)
        {
            var brush = ColorPreview.Background as SolidColorBrush;
            byte r = brush?.Color.R ?? 255;
            byte g = brush?.Color.G ?? 255;
            byte b = brush?.Color.B ?? 255;

            var color = new SavedColor
            {
                R = r, G = g, B = b,
                Hue = (int)HueSlider.Value,
                SavedAt = DateTime.Now
            };
            _savedColors.Add(color);
            BuildSavedColorButtons();
            SaveSavedColors();
            ActionFeedback.Text = $"Väri tallennettu #{r:X2}{g:X2}{b:X2}";
        }

        private void BuildSavedColorButtons()
        {
            SavedColorsPanel.Children.Clear();
            for (int i = 0; i < _savedColors.Count; i++)
            {
                var sc = _savedColors[i];
                var btn = new Button
                {
                    Width = 40, Height = 40, Margin = new Thickness(2),
                    Background = new SolidColorBrush(Color.FromRgb(sc.R, sc.G, sc.B)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x55)),
                    BorderThickness = new Thickness(2),
                    Tag = i,
                    ToolTip = $"#{sc.R:X2}{sc.G:X2}{sc.B:X2}",
                    Cursor = Cursors.Hand,
                    ContextMenu = new ContextMenu
                    {
                        Items =
                        {
                            new MenuItem
                            {
                                Header = "Poista",
                                Tag = i
                            }
                        }
                    }
                };
                btn.Click += SavedColorButton_Click;
                // Wire up the context menu delete
                if (btn.ContextMenu.Items[0] is MenuItem mi)
                    mi.Click += (s, ev) =>
                    {
                        var mi2 = (MenuItem)s!;
                        var idx = (int)mi2.Tag;
                        _savedColors.RemoveAt(idx);
                        BuildSavedColorButtons();
                        SaveSavedColors();
                    };
                SavedColorsPanel.Children.Add(btn);
            }
        }

        private async void SavedColorButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int idx && idx < _savedColors.Count)
            {
                var sc = _savedColors[idx];
                _isUpdatingSliders = true;
                HueSlider.Value = sc.Hue;
                SatSlider.Value = 1000;
                ValSlider.Value = 1000;
                HueValue.Text = $"{sc.Hue}°";
                SatValue.Text = "100%";
                ValValue.Text = "100%";
                _isUpdatingSliders = false;
                UpdateColorPreview();
                await ApplyCurrentColorOrPreset();
            }
        }

        private void SaveSavedColors()
        {
            try
            {
                var path = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(
                        System.Reflection.Assembly.GetExecutingAssembly().Location) ?? ".",
                    "saved_colors.txt");
                var lines = _savedColors.Select(c => $"{c.R},{c.G},{c.B},{c.Hue}");
                System.IO.File.WriteAllLines(path, lines);
            }
            catch { }
        }

        private void LoadSavedColors()
        {
            try
            {
                var path = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(
                        System.Reflection.Assembly.GetExecutingAssembly().Location) ?? ".",
                    "saved_colors.txt");
                if (System.IO.File.Exists(path))
                {
                    var lines = System.IO.File.ReadAllLines(path);
                    foreach (var line in lines)
                    {
                        var p = line.Split(',');
                        if (p.Length >= 4
                            && byte.TryParse(p[0], out byte r)
                            && byte.TryParse(p[1], out byte g)
                            && byte.TryParse(p[2], out byte b)
                            && int.TryParse(p[3], out int h))
                        {
                            _savedColors.Add(new SavedColor { R = r, G = g, B = b, Hue = h });
                        }
                    }
                    BuildSavedColorButtons();
                }
            }
            catch { }
        }

        #endregion

        #region Brightness

        private async void BrightnessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (BrightnessValue == null || BrightnessSlider == null) return;
            BrightnessValue.Text = $"{(int)(BrightnessSlider.Value / 10)}%";
            if (_isPowerOn && DisconnectButton.IsEnabled)
            {
                await _fairyService.SetPresetAsync(_selectedPresetId, (int)BrightnessSlider.Value);
            }
        }

        #endregion
    }

    public class SavedColor
    {
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public int Hue { get; set; }
        public DateTime SavedAt { get; set; }
    }

    public class PresetDef
    {
        public byte Id { get; }
        public string Name { get; }
        public string Icon { get; }
        public string Desc { get; }

        public PresetDef(byte id, string name, string icon, string desc)
        {
            Id = id;
            Name = name;
            Icon = icon;
            Desc = desc;
        }
    }
}