using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using FairyRgbController.Models;
using FairyRgbController.Services;
using Microsoft.Win32;
using WpfColor = System.Windows.Media.Color;
using WpfColors = System.Windows.Media.Colors;
using WpfRectangle = System.Windows.Shapes.Rectangle;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace FairyRgbController
{
    public partial class PixelEditorWindow : Window
    {
        // Grid state
        private int _gridWidth = 8;
        private int _gridHeight = 32;
        private WpfColor[,] _pixels;
        
        // Current drawing state
        private WpfColor _currentColor = WpfColors.Red;
        private bool _isDrawing;
        
        // Animation
        private DispatcherTimer? _animationTimer;
        private int _animationSpeed = 200;
        
        // Service for sending to device
        private HelloFairyService? _fairyService;
        
        // Pixel cell dictionary for quick access
        private readonly Dictionary<(int x, int y), WpfRectangle> _cells = new();
        
        public PixelEditorWindow()
        {
            InitializeComponent();
            InitializeGrid();
            UpdateSpeedLabel();
        }
        
        public void SetFairyService(HelloFairyService service)
        {
            _fairyService = service;
        }
        
        private void InitializeGrid()
        {
            _pixels = new WpfColor[_gridHeight, _gridWidth];
            PixelCanvas.Children.Clear();
            _cells.Clear();
            
            for (int y = 0; y < _gridHeight; y++)
            {
                for (int x = 0; x < _gridWidth; x++)
                {
                    var rect = new WpfRectangle
                    {
                        Width = 20,
                        Height = 20,
                        Fill = new SolidColorBrush(WpfColors.Black),
                        Stroke = new SolidColorBrush(WpfColor.FromRgb(45, 45, 45)),
                        StrokeThickness = 0.5,
                        RadiusX = 2,
                        RadiusY = 2,
                        Tag = (x, y),
                        Cursor = Cursors.Hand
                    };
                    
                    Canvas.SetLeft(rect, x * 22);
                    Canvas.SetTop(rect, y * 22);
                    
                    rect.MouseLeftButtonDown += Cell_MouseLeftButtonDown;
                    rect.MouseLeftButtonUp += Cell_MouseLeftButtonUp;
                    rect.MouseEnter += Cell_MouseEnter;
                    
                    PixelCanvas.Children.Add(rect);
                    _cells[(x, y)] = rect;
                    
                    _pixels[y, x] = WpfColors.Black;
                }
            }
            
            PixelCanvas.Width = _gridWidth * 22;
            PixelCanvas.Height = _gridHeight * 22;
            
            GridSizeLabel.Text = $" ({_gridWidth}×{_gridHeight} = {_gridWidth * _gridHeight} pikseliä)";
        }
        
        private void GridSizeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GridSizeCombo == null) return;
            
            switch (GridSizeCombo.SelectedIndex)
            {
                case 0: SetGridSize(8, 32); break;   // F15C Curtain
                case 1: SetGridSize(15, 20); break;   // Christmas calendar
                case 2: SetGridSize(17, 17); break;  // Heart
                case 3: SetGridSize(20, 15); break;  // Rose
                case 4: // Custom - TODO
                    MessageBox.Show("Mukautettu koko tulee pian!", "Pikseli-Editor", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    GridSizeCombo.SelectedIndex = 0;
                    break;
            }
        }
        
        private void SetGridSize(int width, int height)
        {
            _gridWidth = width;
            _gridHeight = height;
            InitializeGrid();
        }
        
        private void Cell_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDrawing = true;
            ApplyToolToCell(sender as WpfRectangle);
            e.Handled = true;
        }
        
        private void Cell_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDrawing = false;
        }
        
        private void Cell_MouseEnter(object sender, WpfMouseEventArgs e)
        {
            if (_isDrawing && sender is WpfRectangle rect)
            {
                ApplyToolToCell(rect);
            }
        }
        
        private void ApplyToolToCell(WpfRectangle? rect)
        {
            if (rect == null) return;
            
            var (x, y) = ((int, int))rect.Tag;
            
            if (DrawTool.IsChecked == true)
            {
                _pixels[y, x] = _currentColor;
                rect.Fill = new SolidColorBrush(_currentColor);
            }
            else if (EraseTool.IsChecked == true)
            {
                _pixels[y, x] = WpfColors.Black;
                rect.Fill = new SolidColorBrush(WpfColors.Black);
            }
            else if (FillTool.IsChecked == true)
            {
                // Flood fill - TODO
                StatusLabel.Text = "Täyttötyökalu tulee pian!";
            }
        }
        
        private void PixelCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDrawing = true;
        }
        
        private void PixelCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDrawing = false;
        }
        
        private void PixelCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            // Handled by individual cell mouse events
        }
        
        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string colorHex)
            {
                _currentColor = (WpfColor)ColorConverter.ConvertFromString(colorHex);
                CurrentColorPreview.Background = new SolidColorBrush(_currentColor);
            }
        }
        
        private void CustomColor_Click(object sender, RoutedEventArgs e)
        {
            // Use Windows color picker via WinForms
            var dialog = new System.Windows.Forms.ColorDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _currentColor = WpfColor.FromArgb(255, dialog.Color.R, dialog.Color.G, dialog.Color.B);
                CurrentColorPreview.Background = new SolidColorBrush(_currentColor);
            }
        }
        
        private void SpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SpeedLabel != null)
            {
                _animationSpeed = (int)SpeedSlider.Value;
                UpdateSpeedLabel();
            }
        }
        
        private void UpdateSpeedLabel()
        {
            if (SpeedLabel != null)
            {
                SpeedLabel.Text = $"{_animationSpeed}ms";
            }
        }
        
        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (_animationTimer == null)
            {
                _animationTimer = new DispatcherTimer();
                _animationTimer.Tick += AnimationTimer_Tick;
            }
            
            _animationTimer.Interval = TimeSpan.FromMilliseconds(_animationSpeed);
            _animationTimer.Start();
            PlayButton.IsEnabled = false;
            StopButton.IsEnabled = true;
            StatusLabel.Text = $"🎬 Toistetaan... ({_animationSpeed}ms/kehys)";
        }
        
        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            _animationTimer?.Stop();
            PlayButton.IsEnabled = true;
            StopButton.IsEnabled = false;
            StatusLabel.Text = "Pysäytetty";
        }
        
        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            // Simple strobe effect for testing
            bool isOddFrame = (DateTime.Now.Millisecond / _animationSpeed) % 2 == 0;
            
            foreach (var cell in _cells.Values)
            {
                var (x, y) = ((int, int))cell.Tag;
                if (_pixels[y, x] != WpfColors.Black)
                {
                    cell.Fill = new SolidColorBrush(isOddFrame ? _currentColor : WpfColors.Black);
                }
            }
        }
        
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "JSON Files (*.json)|*.json",
                DefaultExt = ".json",
                FileName = $"pixel_art_{DateTime.Now:yyyyMMdd_HHmmss}.json"
            };
            
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var json = ExportToJson();
                    File.WriteAllText(dialog.FileName, json);
                    StatusLabel.Text = $"💾 Tallennettu: {Path.GetFileName(dialog.FileName)}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Virhe tallennuksessa: {ex.Message}", "Virhe", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                DefaultExt = ".json"
            };
            
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var json = File.ReadAllText(dialog.FileName);
                    ImportFromJson(json);
                    StatusLabel.Text = $"📂 Ladattu: {Path.GetFileName(dialog.FileName)}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Virhe ladattaessa: {ex.Message}", "Virhe", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (_fairyService == null || !_fairyService.IsConnected)
            {
                MessageBox.Show("Yhdistä ensin RGB Fairy -laitteeseen!", "Ei yhteyttä", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            try
            {
                // Build DIY command - trying format: AA DA LEN W H RGB_DATA CK
                var command = BuildDiyCommand();
                
                AppLogger.WriteLine("DIY", $"TX: {BitConverter.ToString(command)}");
                _fairyService.SendRawCommand(command);
                
                StatusLabel.Text = $"📤 Lähetetty! ({command.Length} tavua)";
            }
            catch (Exception ex)
            {
                StatusLabel.Text = $"❌ Virhe: {ex.Message}";
            }
        }
        
        public string ExportToJson()
        {
            // Convert pixel array to APK-compatible format
            var dotList = new List<List<List<int>>>();
            
            for (int y = 0; y < _gridHeight; y++)
            {
                var row = new List<List<int>>();
                for (int x = 0; x < _gridWidth; x++)
                {
                    var c = _pixels[y, x];
                    row.Add(new List<int> { c.R, c.G, c.B, c.A > 0 ? 1 : 0 });
                }
                dotList.Add(row);
            }
            
            var data = new Dictionary<string, object>
            {
                ["form"] = new Dictionary<string, object>
                {
                    ["widthPixel"] = _gridWidth,
                    ["heightPixel"] = _gridHeight,
                    ["type"] = "pencil",
                    ["color"] = $"rgba({_currentColor.R}, {_currentColor.G}, {_currentColor.B}, 1)",
                    ["monochrome"] = false,
                    ["grid"] = true,
                    ["size"] = _gridWidth * _gridHeight
                },
                ["dotList"] = dotList
            };
            
            return JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        }
        
        public void ImportFromJson(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            
            // Get dimensions
            if (root.TryGetProperty("form", out var form) &&
                form.TryGetProperty("widthPixel", out var wp) &&
                form.TryGetProperty("heightPixel", out var hp))
            {
                int w = wp.GetInt32();
                int h = hp.GetInt32();
                
                // Validate size
                if (w > 50 || h > 50)
                {
                    throw new Exception("Kuva on liian suuri!");
                }
                
                _gridWidth = w;
                _gridHeight = h;
            }
            
            // Resize grid if needed
            if (_pixels.GetLength(0) != _gridHeight || _pixels.GetLength(1) != _gridWidth)
            {
                _pixels = new WpfColor[_gridHeight, _gridWidth];
            }
            
            // Load pixel data
            if (root.TryGetProperty("dotList", out var dotList))
            {
                int y = 0;
                foreach (var rowElement in dotList.EnumerateArray())
                {
                    int x = 0;
                    foreach (var pixelElement in rowElement.EnumerateArray())
                    {
                        if (x < _gridWidth && y < _gridHeight)
                        {
                            var pixel = pixelElement.EnumerateArray();
                            var enumerator = pixel.GetEnumerator();
                            enumerator.MoveNext(); int r = enumerator.Current.GetInt32();
                            enumerator.MoveNext(); int g = enumerator.Current.GetInt32();
                            enumerator.MoveNext(); int b = enumerator.Current.GetInt32();
                            enumerator.MoveNext(); int a = enumerator.Current.GetInt32();
                            
                            _pixels[y, x] = WpfColor.FromArgb((byte)a, (byte)r, (byte)g, (byte)b);
                        }
                        x++;
                    }
                    y++;
                }
            }
            
            // Update UI
            InitializeGrid();
            
            // Restore pixel colors
            for (int py = 0; py < _gridHeight; py++)
            {
                for (int px = 0; px < _gridWidth; px++)
                {
                    if (_cells.TryGetValue((px, py), out var rect))
                    {
                        rect.Fill = new SolidColorBrush(_pixels[py, px]);
                    }
                }
            }
        }
        
        public byte[] BuildDiyCommand()
        {
            // Build DIY pixel data command
            // Format: AA DA LEN W H RGB_DATA CK
            // We don't know exact format - this is experimental
            
            var pixels = new List<byte>();
            
            // Pack RGB values row by row
            for (int y = 0; y < _gridHeight; y++)
            {
                for (int x = 0; x < _gridWidth; x++)
                {
                    var c = _pixels[y, x];
                    pixels.Add(c.R);
                    pixels.Add(c.G);
                    pixels.Add(c.B);
                }
            }
            
            // Try format 1: AA DA [len] [w] [h] [pixels] [checksum]
            byte len = (byte)(2 + pixels.Count); // w(1) + h(1) + pixels
            
            var packet = new List<byte>();
            packet.Add(0xAA);            // Prefix
            packet.Add(0xDA);            // CMD_DIY (experimental)
            packet.Add(len);             // Length
            packet.Add((byte)_gridWidth); // Width
            packet.Add((byte)_gridHeight); // Height
            packet.AddRange(pixels);      // RGB data
            
            // Calculate checksum: sum of all bytes before checksum
            int sum = 0;
            for (int i = 0; i < packet.Count; i++)
            {
                sum += packet[i];
            }
            packet.Add((byte)(sum % 256));
            
            return packet.ToArray();
        }
    }
}