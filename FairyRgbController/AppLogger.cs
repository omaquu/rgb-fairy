using System;
using System.IO;
using System.Reflection;

namespace FairyRgbController
{
    public static class AppLogger
    {
        private static readonly string LogPath;

        private static readonly object _lock = new();

        static AppLogger()
        {
            try
            {
                // Write log next to the .exe file
                string? exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (string.IsNullOrEmpty(exeDir))
                    exeDir = Environment.CurrentDirectory;
                LogPath = Path.Combine(exeDir, "FairyRgbController.log");

                if (File.Exists(LogPath))
                    File.Delete(LogPath);
                WriteLine("=== Fairy RGB Controller Log ===");
                WriteLine($"Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                WriteLine($"OS: {Environment.OSVersion}");
                WriteLine($".NET: {Environment.Version}");
                WriteLine($"Exe path: {Assembly.GetExecutingAssembly().Location}");
            }
            catch { LogPath = "FairyRgbController.log"; }
        }

        public static void WriteLine(string message)
        {
            try
            {
                lock (_lock)
                {
                    File.AppendAllText(LogPath,
                        $"{DateTime.Now:HH:mm:ss.fff} | {message}{Environment.NewLine}");
                }
            }
            catch { /* silently fail - don't crash the app */ }
        }

        public static void WriteLine(string category, string message)
            => WriteLine($"[{category}] {message}");

        public static void Error(string source, Exception ex)
        {
            WriteLine("ERROR", $"{source}: {ex.GetType().Name}: {ex.Message}");
            WriteLine("ERROR", $"StackTrace: {ex.StackTrace}");
            if (ex.InnerException != null)
                WriteLine("ERROR", $"Inner: {ex.InnerException.Message}");
        }
    }
}