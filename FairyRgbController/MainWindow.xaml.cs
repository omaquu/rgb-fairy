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
        private byte _selectedPresetId = 8; // Default to Kurpitsa
        private string? _currentFolderId;

        // ============================================
        // RGB FAIRY 58 PRESET EFFECTS (F15C Compatible)
        // ============================================
        private static readonly PresetDef[] Presets = new PresetDef[]
        {
            new PresetDef(1, "Valkoinen", "⚪"),
            new PresetDef(2, "Punainen", "🔴"),
            new PresetDef(3, "Vihreä", "🟢"),
            new PresetDef(4, "Sininen", "🔵"),
            new PresetDef(5, "Keltainen", "🟡"),
            new PresetDef(6, "Violetti", "🟣"),
            new PresetDef(7, "Oranssi", "🟠"),
            new PresetDef(8, "Kurpitsa", "🎃"),
            new PresetDef(9, "Lumihiutale", "❄️"),
            new PresetDef(10, "Sydän", "❤️"),
            new PresetDef(11, "Ruusu", "🌹"),
            new PresetDef(12, "Kukka", "🌸"),
            new PresetDef(13, "Taivas", "🌌"),
            new PresetDef(14, "Aalto", "🌊"),
            new PresetDef(15, "Strobo", "⚡"),
            new PresetDef(16, "Sade", "🌧️"),
            new PresetDef(17, "Savu", "🌫️"),
            new PresetDef(18, "Soihtu", "🔥"),
            new PresetDef(19, "Pallo", "🎈"),
            new PresetDef(20, "Pupu", "🐰"),
            new PresetDef(21, "Lehti", "🍃"),
            new PresetDef(22, "Puunsiru", "🌿"),
            new PresetDef(23, "Perhonen", "🦋"),
            new PresetDef(24, "Kissa", "🐱"),
            new PresetDef(25, "Koira", "🐕"),
            new PresetDef(26, "Linnut", "🐦"),
            new PresetDef(27, "Kala", "🐟"),
            new PresetDef(28, "Simpukka", "🐚"),
            new PresetDef(29, "Daalhia", "🌺"),
            new PresetDef(30, "Orchidea", "🪻"),
            new PresetDef(31, "Jouluhattu", "🎅"),
            new PresetDef(32, "Joulukuusi", "🎄"),
            new PresetDef(33, "Lumiukko", "⛄"),
            new PresetDef(34, "Tonttu", "🧝"),
            new PresetDef(35, "Kynttilä", "🕯️"),
            new PresetDef(36, "Tähti", "⭐"),
            new PresetDef(37, "Muffinssi", "🧁"),
            new PresetDef(38, "Suklaa", "🍫"),
            new PresetDef(39, "Karkki", "🍬"),
            new PresetDef(40, "Piparkakku", "🥧"),
            new PresetDef(41, "Imppi", "🧚"),
            new PresetDef(42, "Poro", "🦌"),
            new PresetDef(43, "Reki", "🛷"),
            new PresetDef(44, "Suklaapatukka", "🍫"),
            new PresetDef(45, "Kermakakku", "🍥"),
            new PresetDef(46, "Klovni", "🤡"),
            new PresetDef(47, "Hassu", "😜"),
            new PresetDef(48, "Ilo", "😊"),
            new PresetDef(49, "Surullinen", "😢"),
            new PresetDef(50, "Vihainen", "😡"),
            new PresetDef(51, "Yllätys", "🎁"),
            new PresetDef(52, "Kulta", "🏆"),
            new PresetDef(53, "Hopea", "🥈"),
            new PresetDef(54, "Pronssi", "🥉"),
            new PresetDef(55, "Palkinto", "🏅"),
            new PresetDef(56, "Sateenkaari", "🌈"),
            new PresetDef(57, "Vuorovesi", "🌊"),
            new PresetDef(58, "Meri", "🏖️"),
        };

        #region Initialization

        public MainWindow()
        {
            InitializeComponent();

            _fairyService = new HelloFairyService();
            _fairyService.StatusChanged += OnStatusChanged;
            _fairyService.DevicesUpdated += OnDevicesUpdated;

            LoadEffectsData();
            BuildFoldersList();
            BuildAllEffectButtons();
        }

        private void LoadEffectsData()
        {
            var data = EffectsManager.Load();
            _currentFolderId = string.IsNullOrEmpty(data.LastSelectedFolderId) ? null : data.LastSelectedFolderId;
        }

        private void SaveLastFolder()
        {
            var data = EffectsManager.Load();
            data.LastSelectedFolderId = _currentFolderId ?? "";
            EffectsManager.Save();
        }

        #endregion

        #region Device Scanning & Connection

        private async void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            if (_fairyService.IsConnected)
            {
                await _fairyService.DisconnectAsync();
                UpdateConnectionUI(false);
            }

            ScanButton.IsEnabled = false;
            ScanButton.Content = "⏳ Skannaa...";

            try
            {
                _foundDevices = (await _fairyService.ScanAsync(10000))
                    .Where(d => d.Name.IndexOf("fairy", StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();

                DevicesListBox.ItemsSource = _foundDevices;

                if (_foundDevices.Count == 0)
                {
                    ConnectionStatus.Text = "Laitteita ei löytynyt";
                }
                else if (_foundDevices.Count == 1)
                {
                    // Auto-select single device
                    _selectedDevice = _foundDevices[0];
                    DeviceName.Text = _foundDevices[0].Name;
                    ConnectButton.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                ConnectionStatus.Text = $"Virhe: {ex.Message}";
            }
            finally
            {
                ScanButton.IsEnabled = true;
                ScanButton.Content = "🔍 Skannaa";
            }
        }

        private void DevicesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DevicesListBox.SelectedItem is BleDeviceInfo device)
            {
                _selectedDevice = device;
                DeviceName.Text = device.Name;
                ConnectButton.IsEnabled = true;
            }
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (_fairyService.IsConnected)
            {
                await _fairyService.DisconnectAsync();
                UpdateConnectionUI(false);
                ConnectButton.Content = "Yhdistä";
                PixelEditorButton.IsEnabled = false;
                return;
            }

            if (_selectedDevice == null) return;

            ConnectButton.IsEnabled = false;
            ConnectButton.Content = "⏳ Yhdistetään...";

            try
            {
                await _fairyService.ConnectAsync(_selectedDevice);
                UpdateConnectionUI(true);
                ConnectButton.Content = "Katkaise";
                ConnectButton.IsEnabled = true;
                PixelEditorButton.IsEnabled = true;

                // Power on
                _isPowerOn = true;
                await _fairyService.SetPowerAsync(true);
                await _fairyService.SetPresetAsync(_selectedPresetId, (int)BrightnessSlider.Value);
                PowerButton.Content = "💡 ON";
                PowerButton.Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x4A, 0x2E));
            }
            catch (Exception ex)
            {
                ConnectionStatus.Text = $"Virhe: {ex.Message}";
                ConnectButton.Content = "Yhdistä";
                ConnectButton.IsEnabled = true;
            }
        }

        private void OnStatusChanged(object? sender, string status)
        {
            Dispatcher.Invoke(() => ConnectionStatus.Text = status);
        }

        private void OnDevicesUpdated(object? sender, List<BleDeviceInfo> devices)
        {
            Dispatcher.Invoke(() =>
            {
                DevicesListBox.ItemsSource = null;
                DevicesListBox.ItemsSource = devices;
            });
        }

        private void UpdateConnectionUI(bool connected)
        {
            ConnectionStatus.Text = connected ? $"Yhdistetty: {_selectedDevice?.Name}" : "Ei yhdistetty";
        }

        #endregion

        #region Effects Management

        private void BuildFoldersList()
        {
            var data = EffectsManager.Load();
            FoldersListBox.ItemsSource = data.Folders;

            // Select last folder or first
            if (!string.IsNullOrEmpty(_currentFolderId))
            {
                var folder = data.Folders.FirstOrDefault(f => f.Id == _currentFolderId);
                if (folder != null)
                    FoldersListBox.SelectedItem = folder;
            }

            if (FoldersListBox.SelectedItem == null && data.Folders.Count > 0)
                FoldersListBox.SelectedIndex = 0;
        }

        private void FoldersListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FoldersListBox.SelectedItem is EffectFolder folder)
            {
                _currentFolderId = folder.Id;
                SaveLastFolder();
                BuildEffectsForCurrentFolder();
            }
        }

        private void BuildEffectsForCurrentFolder()
        {
            EffectsWrapPanel.Children.Clear();

            var data = EffectsManager.Load();
            var folder = data.Folders.FirstOrDefault(f => f.Id == _currentFolderId);

            IEnumerable<PresetDef> effectsToShow;

            if (folder != null && folder.EffectIds.Count > 0)
            {
                // Show only effects in this folder
                var ids = folder.EffectIds.Select(int.Parse).ToHashSet();
                effectsToShow = Presets.Where(p => ids.Contains(p.Id));
            }
            else
            {
                // Show all effects
                effectsToShow = Presets;
            }

            foreach (var preset in effectsToShow)
            {
                var effect = EffectsManager.GetOrCreateEffect((byte)preset.Id, preset.Name);
                var displayName = !string.IsNullOrEmpty(effect.CustomName) ? effect.CustomName : preset.Name;

                var btn = new Button
                {
                    Content = $"{preset.Icon} {displayName}",
                    Tag = (byte)preset.Id,
                    Style = (Style)FindResource("EffectButton"),
                    ToolTip = $"ID: {preset.Id}\nKlikkaa nimetäksesi"
                };

                if (preset.Id == _selectedPresetId)
                    btn.Tag = "active";

                btn.Click += EffectButton_Click;
                btn.MouseRightButtonDown += EffectButton_RightClick;

                EffectsWrapPanel.Children.Add(btn);
            }
        }

        private void BuildAllEffectButtons()
        {
            EffectsWrapPanel.Children.Clear();

            foreach (var preset in Presets)
            {
                var effect = EffectsManager.GetOrCreateEffect((byte)preset.Id, preset.Name);
                var displayName = !string.IsNullOrEmpty(effect.CustomName) ? effect.CustomName : preset.Name;

                var btn = new Button
                {
                    Content = $"{preset.Icon} {displayName}",
                    Tag = (byte)preset.Id,
                    Style = (Style)FindResource("EffectButton")
                };

                btn.Click += EffectButton_Click;
                btn.MouseRightButtonDown += EffectButton_RightClick;

                EffectsWrapPanel.Children.Add(btn);
            }
        }

        private async void EffectButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is byte id)
            {
                _selectedPresetId = id;

                // Update button states
                foreach (Button b in EffectsWrapPanel.Children)
                {
                    b.Tag = (b.Tag as byte?) ?? 0;
                }
                btn.Tag = "active";

                if (_isPowerOn)
                {
                    await _fairyService.SetPresetAsync(id, (int)BrightnessSlider.Value);
                }
            }
        }

        private void EffectButton_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is Button btn && btn.Tag is byte id)
            {
                var preset = Presets.FirstOrDefault(p => p.Id == id);
                if (preset == null) return;

                var effect = EffectsManager.GetOrCreateEffect(id, preset.Name);

                // Simple rename dialog
                var dialog = new Window
                {
                    Title = "Nimeä efekti",
                    Width = 300,
                    Height = 150,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Background = (Brush)FindResource("CardBorder"),
                    Foreground = Brushes.White
                };

                var panel = new StackPanel { Margin = new Thickness(16) };
                panel.Children.Add(new TextBlock { Text = $"Efekti: {preset.Name} (ID: {id})", Margin = new Thickness(0, 0, 0, 8) });

                var textBox = new TextBox
                {
                    Text = effect.CustomName,
                    Padding = new Thickness(8, 4, 8, 4)
                };
                panel.Children.Add(textBox);

                var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };

                var saveBtn = new Button { Content = "Tallenna", Padding = new Thickness(16, 8, 16, 8), Margin = new Thickness(0, 0, 8, 0) };
                saveBtn.Click += (s, ev) =>
                {
                    EffectsManager.RenameEffect(effect.Id, textBox.Text);
                    BuildEffectsForCurrentFolder();
                    dialog.Close();
                };

                var cancelBtn = new Button { Content = "Peruuta", Padding = new Thickness(16, 8, 16, 8) };
                cancelBtn.Click += (s, ev) => dialog.Close();

                buttonPanel.Children.Add(saveBtn);
                buttonPanel.Children.Add(cancelBtn);
                panel.Children.Add(buttonPanel);

                dialog.Content = panel;
                dialog.Owner = this;
                dialog.ShowDialog();
            }
        }

        private void ShowAllEffectsButton_Click(object sender, RoutedEventArgs e)
        {
            BuildAllEffectButtons();
        }

        private void ManageEffectsButton_Click(object sender, RoutedEventArgs e)
        {
            // Open effects management window
            var manageWindow = new EffectsManagerWindow();
            manageWindow.Owner = this;
            manageWindow.ShowDialog();

            // Refresh after managing
            LoadEffectsData();
            BuildFoldersList();
            BuildEffectsForCurrentFolder();
        }

        #endregion

        #region Brightness

        private DateTime _lastBrightnessSend = DateTime.MinValue;
        private const int BRIGHTNESS_DEBOUNCE_MS = 50;

        private async void BrightnessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (BrightnessValue == null || BrightnessSlider == null) return;

            BrightnessValue.Text = $"{(int)(BrightnessSlider.Value / 10)}%";

            var now = DateTime.Now;
            if ((now - _lastBrightnessSend).TotalMilliseconds < BRIGHTNESS_DEBOUNCE_MS)
                return;
            _lastBrightnessSend = now;

            if (_isPowerOn && _fairyService.IsConnected)
            {
                await _fairyService.SetPresetAsync(_selectedPresetId, (int)BrightnessSlider.Value);
            }
        }

        #endregion

        #region Power & Colors

        private async void PowerButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_fairyService.IsConnected) return;

            _isPowerOn = !_isPowerOn;

            if (_isPowerOn)
            {
                await _fairyService.SetPowerAsync(true);
                await _fairyService.SetPresetAsync(_selectedPresetId, (int)BrightnessSlider.Value);
                PowerButton.Content = "💡 ON";
                PowerButton.Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x4A, 0x2E));
            }
            else
            {
                await _fairyService.SetPowerAsync(false);
                PowerButton.Content = "⚪ OFF";
                PowerButton.Background = new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x5A));
            }
        }

        private async void OffButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_fairyService.IsConnected) return;

            _isPowerOn = false;
            await _fairyService.SetPowerAsync(false);
            PowerButton.Content = "⚪ OFF";
            PowerButton.Background = new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x5A));
        }

        private async void QuickColor_Click(object sender, RoutedEventArgs e)
        {
            if (!_fairyService.IsConnected) return;

            if (sender is Button btn && btn.Tag is string colorHex)
            {
                // Convert hex to RGB and find closest preset
                var r = Convert.ToByte(colorHex.Substring(0, 2), 16);
                var g = Convert.ToByte(colorHex.Substring(2, 2), 16);
                var b = Convert.ToByte(colorHex.Substring(4, 2), 16);

                // F15C uses presets - find closest match or use white (1)
                byte presetId = 1; // Default white

                // Map some basic colors
                if (r > 200 && g < 50 && b < 50) presetId = 2; // Red
                else if (r < 50 && g > 200 && b < 50) presetId = 3; // Green
                else if (r < 50 && g < 50 && b > 200) presetId = 4; // Blue
                else if (r > 200 && g > 200 && b < 50) presetId = 5; // Yellow
                else if (r > 200 && g < 50 && b > 200) presetId = 6; // Purple
                else if (r > 200 && g > 100 && b < 50) presetId = 7; // Orange

                _selectedPresetId = presetId;
                await _fairyService.SetPresetAsync(presetId, (int)BrightnessSlider.Value);

                if (!_isPowerOn)
                {
                    _isPowerOn = true;
                    await _fairyService.SetPowerAsync(true);
                    PowerButton.Content = "💡 ON";
                    PowerButton.Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x4A, 0x2E));
                }
            }
        }

        #endregion

        #region Pixel Editor

        private void OpenPixelEditor_Click(object sender, RoutedEventArgs e)
        {
            if (!_fairyService.IsConnected)
            {
                MessageBox.Show("Yhdistä ensin laite!", "Virhe", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var pixelWindow = new PixelEditorWindow(_fairyService);
                pixelWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Pixel-editorin avaaminen epäonnistui: {ex.Message}", "Virhe", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }

    // Simple preset definition
    public class PresetDef
    {
        public int Id { get; }
        public string Name { get; }
        public string Icon { get; }

        public PresetDef(int id, string name, string icon)
        {
            Id = id;
            Name = name;
            Icon = icon;
        }
    }
}