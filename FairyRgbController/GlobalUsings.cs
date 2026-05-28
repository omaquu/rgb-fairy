// Global using directives to resolve WPF vs WinForms ambiguities
// This must come BEFORE any namespace-level using statements

global using WpfApplication = System.Windows.Application;
global using WpfMessageBox = System.Windows.MessageBox;
global using WpfMessageBoxButton = System.Windows.MessageBoxButton;
global using WpfMessageBoxImage = System.Windows.MessageBoxImage;
global using WpfColor = System.Windows.Media.Color;
global using WpfColors = System.Windows.Media.Colors;
global using WpfBrushes = System.Windows.Media.Brushes;
global using WpfColorConverter = System.Windows.Media.ColorConverter;
global using WpfCursors = System.Windows.Input.Cursors;