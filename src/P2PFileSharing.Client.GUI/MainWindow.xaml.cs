using System;
using System.Diagnostics;
using System.Windows;
using P2PFileSharing.Client.GUI.ViewModels;
using P2PFileSharing.Common.Configuration;
using P2PFileSharing.Common.Infrastructure;

namespace P2PFileSharing.Client.GUI;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        // Create default config
        var config = new ClientConfig
        {
            Username = Environment.UserName,
            ServerIpAddress = "127.0.0.1",
            ServerPort = 5000
        };

        // GUI logger
        ILogger logger = new GuiConsoleLogger("GUI");

        // Main VM
        _viewModel = new MainViewModel(config, logger);
        DataContext = _viewModel;
    }

    /// <summary>
    /// Logger đơn giản cho GUI — đúng chuẩn ILogger của project
    /// </summary>
    private class GuiConsoleLogger : ILogger
    {
        private readonly string _tag;

        public GuiConsoleLogger(string tag)
        {
            _tag = tag;
        }

        public void LogInfo(string message)
        {
            Write("[INFO]", message);
        }

        public void LogWarning(string message)
        {
            Write("[WARN]", message);
        }

        public void LogError(string message)
        {
            Write("[ERROR]", message);
        }

        public void LogError(string message, Exception exception)
        {
            Write("[ERROR]", $"{message} :: {exception.Message}");
            Debug.WriteLine(exception.ToString());
        }

        public void LogDebug(string message)
        {
            Write("[DEBUG]", message);
        }

        public void Log(LogLevel level, string message)
        {
            Write($"[{level.ToString().ToUpper()}]", message);
        }

        public void Log(LogLevel level, string message, Exception? exception)
        {
            Write($"[{level.ToString().ToUpper()}]", $"{message} :: {exception?.Message}");
            if (exception != null)
                Debug.WriteLine(exception.ToString());
        }

        private void Write(string level, string message)
        {
            var line = $"{DateTime.Now:HH:mm:ss} {_tag} {level} {message}";
            Debug.WriteLine(line);
            Console.WriteLine(line);
        }
    }
}
