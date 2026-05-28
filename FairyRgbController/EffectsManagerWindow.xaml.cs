using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FairyRgbController.Models;

namespace FairyRgbController
{
    public partial class EffectsManagerWindow : Window
    {
        private EffectFolder? _selectedFolder;

        private static readonly PresetDef[] AllPresets = new PresetDef[]
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

        public EffectsManagerWindow()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            var data = EffectsManager.Load();
            FoldersListBox.ItemsSource = data.Folders;

            if (data.Folders.Count > 0)
                FoldersListBox.SelectedIndex = 0;
        }

        private void FoldersListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedFolder = FoldersListBox.SelectedItem as EffectFolder;
            BuildEffectsList();
        }

        private void BuildEffectsList()
        {
            EffectsPanel.Children.Clear();

            if (_selectedFolder == null) return;

            var folderEffectIds = _selectedFolder.EffectIds.Select(int.Parse).ToHashSet();

            foreach (var preset in AllPresets)
            {
                var isInFolder = folderEffectIds.Contains(preset.Id);
                var effect = EffectsManager.GetOrCreateEffect((byte)preset.Id, preset.Name);
                var displayName = !string.IsNullOrEmpty(effect.CustomName) ? effect.CustomName : preset.Name;

                var btn = new Button
                {
                    Content = isInFolder ? $"✓ {preset.Icon} {displayName}" : $"  {preset.Icon} {displayName}",
                    Tag = preset.Id,
                    Padding = new Thickness(8, 4, 8, 4),
                    Margin = new Thickness(2),
                    Background = isInFolder
                        ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x63, 0x66, 0xF1))
                        : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1E, 0x1E, 0x3F)),
                    Foreground = System.Windows.Media.Brushes.White,
                    BorderThickness = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand
                };

                btn.Click += (s, e) =>
                {
                    if (s is Button b && b.Tag is int id)
                    {
                        ToggleEffectInFolder(id);
                        BuildEffectsList();
                    }
                };

                // Right-click to rename
                btn.MouseRightButtonDown += (s, e) =>
                {
                    if (s is Button b && b.Tag is int id)
                    {
                        var preset2 = AllPresets.First(p => p.Id == id);
                        var effect2 = EffectsManager.GetOrCreateEffect((byte)id, preset2.Name);
                        ShowRenameDialog(effect2, preset2.Name);
                    }
                    e.Handled = true;
                };

                EffectsPanel.Children.Add(btn);
            }
        }

        private void ToggleEffectInFolder(int effectId)
        {
            if (_selectedFolder == null) return;

            var effectIdStr = effectId.ToString();

            if (_selectedFolder.EffectIds.Contains(effectIdStr))
            {
                _selectedFolder.EffectIds.Remove(effectIdStr);
            }
            else
            {
                _selectedFolder.EffectIds.Add(effectIdStr);
            }

            EffectsManager.Save();
        }

        private void AddFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Window
            {
                Title = "Uusi kansio",
                Width = 280,
                Height = 120,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Background = System.Windows.Media.Brushes.Black,
                Foreground = System.Windows.Media.Brushes.White,
                Owner = this
            };

            var panel = new StackPanel { Margin = new Thickness(16) };
            panel.Children.Add(new TextBlock { Text = "Kansion nimi:", Margin = new Thickness(0, 0, 0, 8) });

            var textBox = new TextBox { Padding = new Thickness(8, 4, 8, 4) };
            panel.Children.Add(textBox);

            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };

            var saveBtn = new Button { Content = "Tallenna", Padding = new Thickness(16, 6, 16, 6), Margin = new Thickness(0, 0, 8, 0) };
            saveBtn.Click += (s, ev) =>
            {
                if (!string.IsNullOrWhiteSpace(textBox.Text))
                {
                    EffectsManager.AddFolder(textBox.Text);
                    LoadData();
                    dialog.Close();
                }
            };

            var cancelBtn = new Button { Content = "Peruuta", Padding = new Thickness(16, 6, 16, 6) };
            cancelBtn.Click += (s, ev) => dialog.Close();

            btnPanel.Children.Add(saveBtn);
            btnPanel.Children.Add(cancelBtn);
            panel.Children.Add(btnPanel);

            dialog.Content = panel;
            dialog.ShowDialog();
        }

        private void DeleteFolder_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedFolder == null) return;

            var data = EffectsManager.Load();
            if (data.Folders.Count <= 1)
            {
                MessageBox.Show("Ei voi poistaa viimeistä kansiota!", "Virhe", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Poistetaanko kansio '{_selectedFolder.Name}'?\nEfektit siirretään Oletus-kansioon.",
                "Vahvista",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                EffectsManager.DeleteFolder(_selectedFolder.Id);
                LoadData();
            }
        }

        private void ShowRenameDialog(CustomEffect effect, string originalName)
        {
            var dialog = new Window
            {
                Title = "Nimeä efekti",
                Width = 300,
                Height = 140,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Background = System.Windows.Media.Brushes.Black,
                Foreground = System.Windows.Media.Brushes.White,
                Owner = this
            };

            var panel = new StackPanel { Margin = new Thickness(16) };
            panel.Children.Add(new TextBlock { Text = $"Alkuperäinen: {originalName}", Margin = new Thickness(0, 0, 0, 8) });

            var textBox = new TextBox
            {
                Text = effect.CustomName,
                Padding = new Thickness(8, 4, 8, 4)
            };
            panel.Children.Add(textBox);

            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };

            var saveBtn = new Button { Content = "Tallenna", Padding = new Thickness(16, 6, 16, 6), Margin = new Thickness(0, 0, 8, 0) };
            saveBtn.Click += (s, ev) =>
            {
                EffectsManager.RenameEffect(effect.Id, textBox.Text);
                BuildEffectsList();
                dialog.Close();
            };

            var cancelBtn = new Button { Content = "Peruuta", Padding = new Thickness(16, 6, 16, 6) };
            cancelBtn.Click += (s, ev) => dialog.Close();

            btnPanel.Children.Add(saveBtn);
            btnPanel.Children.Add(cancelBtn);
            panel.Children.Add(btnPanel);

            dialog.Content = panel;
            dialog.ShowDialog();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

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