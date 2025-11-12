using System.Windows;
using P2PFileSharing.Client.GUI.ViewModels;
using P2PFileSharing.Common.Configuration;
using P2PFileSharing.Common.Infrastructure;

namespace P2PFileSharing.Client.GUI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// TODO: Initialize ViewModel and set up data binding
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // TODO: Create ClientConfig from settings or command-line args
        // TODO: Create ILogger instance
        // TODO: Create MainViewModel
        // TODO: Set DataContext to MainViewModel
        
        // Example (commented out until implementation):
        // var config = ClientConfig.FromCommandLineArgs(Environment.GetCommandLineArgs());
        // var logger = new FileLogger(config.LogFilePath, config.LogLevel);
        // var viewModel = new MainViewModel(config, logger);
        // DataContext = viewModel;
    }
}