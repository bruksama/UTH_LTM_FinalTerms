using System.Collections.ObjectModel;
using System.Windows.Input;
using P2PFileSharing.Common.Configuration;
using P2PFileSharing.Common.Infrastructure;
using P2PFileSharing.Common.Models;

namespace P2PFileSharing.Client.GUI.ViewModels;

/// <summary>
/// Main ViewModel cho MainWindow
/// TODO: Implement main UI logic, peer list management, and file transfer coordination
/// </summary>
public class MainViewModel : BaseViewModel
{
    private readonly ClientConfig _config;
    private readonly ILogger _logger;
    private string _username = string.Empty;
    private string _serverAddress = "127.0.0.1:5000";
    private bool _isConnected;
    private string _connectionStatus = "Disconnected";
    private PeerViewModel? _selectedPeer;

    public MainViewModel(ClientConfig config, ILogger logger)
    {
        _config = config;
        _logger = logger;
        
        // TODO: Initialize from config
        Username = config.Username;
        ServerAddress = $"{config.ServerIpAddress}:{config.ServerPort}";
        
        Peers = new ObservableCollection<PeerViewModel>();
        Transfers = new ObservableCollection<TransferViewModel>();
        SharedFiles = new ObservableCollection<SharedFileViewModel>();

        // TODO: Initialize commands
        ConnectCommand = new RelayCommand(async () => await ConnectAsync(), () => !IsConnected);
        DisconnectCommand = new RelayCommand(async () => await DisconnectAsync(), () => IsConnected);
        RefreshPeersCommand = new RelayCommand(async () => await RefreshPeersAsync());
        ScanNetworkCommand = new RelayCommand(async () => await ScanNetworkAsync());
        AddSharedFileCommand = new RelayCommand(() => AddSharedFile());
    }

    /// <summary>
    /// Username của peer này
    /// TODO: Bind to TextBox in UI
    /// </summary>
    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    /// <summary>
    /// Địa chỉ Server (IP:Port)
    /// TODO: Bind to TextBox in UI
    /// </summary>
    public string ServerAddress
    {
        get => _serverAddress;
        set => SetProperty(ref _serverAddress, value);
    }

    /// <summary>
    /// Trạng thái kết nối với Server
    /// TODO: Update when connection state changes
    /// </summary>
    public bool IsConnected
    {
        get => _isConnected;
        set
        {
            SetProperty(ref _isConnected, value);
            // TODO: Update command CanExecute states
            ConnectionStatus = value ? "Connected" : "Disconnected";
        }
    }

    /// <summary>
    /// Trạng thái kết nối (text)
    /// TODO: Bind to StatusLabel in UI
    /// </summary>
    public string ConnectionStatus
    {
        get => _connectionStatus;
        set => SetProperty(ref _connectionStatus, value);
    }

    /// <summary>
    /// Peer được chọn trong danh sách
    /// TODO: Bind to SelectedItem of ListView/ItemsControl
    /// </summary>
    public PeerViewModel? SelectedPeer
    {
        get => _selectedPeer;
        set => SetProperty(ref _selectedPeer, value);
    }

    /// <summary>
    /// Danh sách các peer đang online
    /// TODO: Bind to ItemsControl/ListView in UI
    /// </summary>
    public ObservableCollection<PeerViewModel> Peers { get; }

    /// <summary>
    /// Danh sách các file transfer đang diễn ra
    /// TODO: Bind to TransferList in UI
    /// </summary>
    public ObservableCollection<TransferViewModel> Transfers { get; }

    /// <summary>
    /// Danh sách file đang chia sẻ
    /// TODO: Bind to SharedFilesList in UI
    /// </summary>
    public ObservableCollection<SharedFileViewModel> SharedFiles { get; }

    /// <summary>
    /// Command để kết nối với Server
    /// TODO: Implement connection logic
    /// </summary>
    public ICommand ConnectCommand { get; }

    /// <summary>
    /// Command để ngắt kết nối với Server
    /// TODO: Implement disconnection logic
    /// </summary>
    public ICommand DisconnectCommand { get; }

    /// <summary>
    /// Command để refresh danh sách peer từ Server
    /// TODO: Implement refresh logic
    /// </summary>
    public ICommand RefreshPeersCommand { get; }

    /// <summary>
    /// Command để scan mạng LAN bằng UDP
    /// TODO: Implement UDP scan logic
    /// </summary>
    public ICommand ScanNetworkCommand { get; }

    /// <summary>
    /// Command để thêm file vào danh sách chia sẻ
    /// TODO: Implement file selection dialog
    /// </summary>
    public ICommand AddSharedFileCommand { get; }

    /// <summary>
    /// Kết nối với Server và đăng ký peer
    /// TODO: Call PeerClient.StartAsync()
    /// TODO: Register with server using ServerCommunicator.RegisterAsync()
    /// TODO: Start file receiver listener
    /// TODO: Start UDP discovery listener
    /// TODO: Start heartbeat task
    /// </summary>
    private async Task ConnectAsync()
    {
        // TODO: Parse ServerAddress to get IP and Port
        // TODO: Create/initialize PeerClient
        // TODO: Call PeerClient.StartAsync()
        // TODO: Update IsConnected
        // TODO: Load initial peer list
        await Task.CompletedTask;
    }

    /// <summary>
    /// Ngắt kết nối với Server
    /// TODO: Call PeerClient.StopAsync()
    /// TODO: Deregister from server
    /// TODO: Stop all listeners
    /// TODO: Clear peer list
    /// </summary>
    private async Task DisconnectAsync()
    {
        // TODO: Call PeerClient.StopAsync()
        // TODO: Update IsConnected
        // TODO: Clear Peers collection
        await Task.CompletedTask;
    }

    /// <summary>
    /// Refresh danh sách peer từ Server
    /// TODO: Call ServerCommunicator.QueryPeersAsync()
    /// TODO: Update Peers collection
    /// </summary>
    private async Task RefreshPeersAsync()
    {
        // TODO: Query peers from server
        // TODO: Update Peers collection
        // TODO: Show loading indicator
        await Task.CompletedTask;
    }

    /// <summary>
    /// Scan mạng LAN bằng UDP broadcast
    /// TODO: Call UdpDiscovery.ScanNetworkAsync()
    /// TODO: Merge results with existing peer list
    /// </summary>
    private async Task ScanNetworkAsync()
    {
        // TODO: Call UdpDiscovery.ScanNetworkAsync()
        // TODO: Add discovered peers to Peers collection
        // TODO: Show scan progress
        await Task.CompletedTask;
    }

    /// <summary>
    /// Thêm file vào danh sách chia sẻ
    /// TODO: Show file selection dialog
    /// TODO: Add file to SharedFiles collection
    /// TODO: Update shared files list on server
    /// </summary>
    private void AddSharedFile()
    {
        // TODO: Show OpenFileDialog
        // TODO: Add selected file to SharedFiles
        // TODO: Update server registration with new shared files
    }

    /// <summary>
    /// Xử lý khi file được drop vào một peer
    /// TODO: Called from PeerItemControl drag & drop handler
    /// TODO: Create TransferViewModel
    /// TODO: Start file transfer
    /// </summary>
    public async Task HandleFileDropAsync(PeerViewModel peer, IEnumerable<string> filePaths)
    {
        // TODO: Validate peer is online
        // TODO: For each file:
        //   - Create TransferViewModel
        //   - Add to Transfers collection
        //   - Call peer.SendFileAsync()
        //   - Update transfer progress
        await Task.CompletedTask;
    }

    /// <summary>
    /// Xử lý khi nhận được file từ peer khác
    /// TODO: Called from FileTransferManager when file is received
    /// TODO: Show notification
    /// TODO: Update UI
    /// </summary>
    public void HandleFileReceived(string fileName, string fromPeer)
    {
        // TODO: Show notification/toast
        // TODO: Update received files list
        // TODO: Log event
    }
}

