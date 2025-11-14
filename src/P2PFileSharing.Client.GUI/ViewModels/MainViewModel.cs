/*using System;
using System.Collections.Generic;
using System.Threading.Tasks;*/
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using P2PFileSharing.Client;
using P2PFileSharing.Common.Configuration;
using P2PFileSharing.Common.Infrastructure;
using P2PFileSharing.Common.Models;

namespace P2PFileSharing.Client.GUI.ViewModels;

/// <summary>
/// Main ViewModel cho MainWindow
/// Quản lý kết nối, danh sách peers, shared files và chuyển file
/// </summary>
public class MainViewModel : BaseViewModel
{
    private readonly ClientConfig _config;
    private readonly ILogger _logger;
    private PeerClient? _peerClient;

    private string _username = string.Empty;
    private string _serverAddress = "127.0.0.1:5000";
    private bool _isConnected;
    private string _connectionStatus = "Disconnected";
    private PeerViewModel? _selectedPeer;
    private bool _isLoading;

    public MainViewModel(ClientConfig config, ILogger logger)
    {
        _config = config;
        _logger = logger;

        // Initialize từ config
        Username = config.Username;
        ServerAddress = $"{config.ServerIpAddress}:{config.ServerPort}";

        Peers = new ObservableCollection<PeerViewModel>();
        Transfers = new ObservableCollection<TransferViewModel>();
        SharedFiles = new ObservableCollection<SharedFileViewModel>();

        // Initialize commands (bám sát TODO gốc)
        ConnectCommand = new RelayCommand(
            async () => await ConnectAsync(),
            () => !IsConnected && !IsLoading);

        DisconnectCommand = new RelayCommand(
            async () => await DisconnectAsync(),
            () => IsConnected && !IsLoading);

        RefreshPeersCommand = new RelayCommand(
            async () => await RefreshPeersAsync(),
            () => IsConnected && !IsLoading);

        ScanNetworkCommand = new RelayCommand(
            async () => await ScanNetworkAsync(),
            () => !IsLoading);

        AddSharedFileCommand = new RelayCommand(
            () => AddSharedFile(),
            () => !IsLoading);
    }

    #region Properties

    /// <summary>
    /// Username của peer này (bind TextBox)
    /// </summary>
    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    /// <summary>
    /// Địa chỉ Server (IP:Port) (bind TextBox)
    /// </summary>
    public string ServerAddress
    {
        get => _serverAddress;
        set => SetProperty(ref _serverAddress, value);
    }

    /// <summary>
    /// Trạng thái kết nối với Server
    /// </summary>
    public bool IsConnected
    {
        get => _isConnected;
        set
        {
            if (SetProperty(ref _isConnected, value))
            {
                ConnectionStatus = value ? "Connected" : "Disconnected";
                // Force reevaluate CanExecute của commands
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    /// <summary>
    /// Trạng thái kết nối text (hiển thị trên UI)
    /// </summary>
    public string ConnectionStatus
    {
        get => _connectionStatus;
        set => SetProperty(ref _connectionStatus, value);
    }

    /// <summary>
    /// Peer đang được chọn trong danh sách
    /// </summary>
    public PeerViewModel? SelectedPeer
    {
        get => _selectedPeer;
        set => SetProperty(ref _selectedPeer, value);
    }

    /// <summary>
    /// Flag loading để disable một số thao tác
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (SetProperty(ref _isLoading, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    /// <summary>
    /// Danh sách các peer đang online
    /// </summary>
    public ObservableCollection<PeerViewModel> Peers { get; }

    /// <summary>
    /// Danh sách các file transfer đang diễn ra
    /// </summary>
    public ObservableCollection<TransferViewModel> Transfers { get; }

    /// <summary>
    /// Danh sách file đang chia sẻ
    /// </summary>
    public ObservableCollection<SharedFileViewModel> SharedFiles { get; }

    #endregion

    #region Commands

    /// <summary>
    /// Kết nối với Server
    /// </summary>
    public ICommand ConnectCommand { get; }

    /// <summary>
    /// Ngắt kết nối với Server
    /// </summary>
    public ICommand DisconnectCommand { get; }

    /// <summary>
    /// Refresh danh sách peer từ Server
    /// </summary>
    public ICommand RefreshPeersCommand { get; }

    /// <summary>
    /// Scan mạng LAN bằng UDP
    /// </summary>
    public ICommand ScanNetworkCommand { get; }

    /// <summary>
    /// Thêm file chia sẻ
    /// </summary>
    public ICommand AddSharedFileCommand { get; }

    #endregion

    #region Methods

    /// <summary>
    /// Kết nối với Server và đăng ký peer
    /// TODO (sau): gọi thêm ServerCommunicator.RegisterAsync(), start heartbeat...
    /// </summary>
    private async Task ConnectAsync()
    {
        IsLoading = true;
        ConnectionStatus = "Connecting...";

        try
        {
            // Parse ServerAddress -> IP + Port
            var parts = ServerAddress.Split(':');
            if (parts.Length != 2 || !int.TryParse(parts[1], out var port))
            {
                MessageBox.Show(
                    "Invalid server address format. Use IP:Port (e.g., 192.168.1.100:5000)",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Cập nhật config từ UI
            _config.ServerIpAddress = parts[0];
            _config.ServerPort = port;
            _config.Username = Username;

            // Tạo PeerClient và start
            _peerClient = new PeerClient(_config, _logger);
            await _peerClient.StartAsync();

            if (_peerClient.IsRunning)
            {
                IsConnected = true;
                _logger.LogInfo("Successfully connected to server");

                // Sau khi connect xong: load danh sách peer ban đầu
                await RefreshPeersAsync();
            }
            else
            {
                ConnectionStatus = "Connection failed";
                MessageBox.Show(
                    "Failed to connect to server. Check server address and ensure server is running.",
                    "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Error connecting to server", ex);
            ConnectionStatus = "Connection failed";
            MessageBox.Show(
                $"Error connecting to server: {ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Ngắt kết nối với Server
    /// TODO (sau): deregister với server, stop heartbeat...
    /// </summary>
    private async Task DisconnectAsync()
    {
        IsLoading = true;
        ConnectionStatus = "Disconnecting...";

        try
        {
            if (_peerClient != null)
            {
                await _peerClient.StopAsync();
                _peerClient = null;
            }

            IsConnected = false;
            Peers.Clear();
            _logger.LogInfo("Disconnected from server");
        }
        catch (Exception ex)
        {
            _logger.LogError("Error disconnecting", ex);
            MessageBox.Show(
                $"Error disconnecting: {ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Refresh danh sách peer từ Server.
    /// Hiện tại: placeholder, sau sẽ gọi ServerCommunicator.QueryPeersAsync().
    /// </summary>
    private async Task RefreshPeersAsync()
    {
        if (_peerClient == null || !_peerClient.IsRunning)
        {
            MessageBox.Show("Please connect to server first.",
                "Not Connected", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsLoading = true;
        ConnectionStatus = "Refreshing peers...";

        try
        {
            _logger.LogInfo("Refreshing peer list from registry server...");
            var peers = await _peerClient.QueryPeersAsync();

            if (peers == null || peers.Count == 0)
            {
                Application.Current.Dispatcher.Invoke(() => Peers.Clear());
                ConnectionStatus = "No peers found";
                _logger.LogInfo("No peers found on registry server.");
                return;
            }

            UpdatePeersList(peers);
            ConnectionStatus = $"Connected ({peers.Count} peers)";
            _logger.LogInfo($"Peer list refresh completed: {peers.Count} peer(s).");
        }
        catch (Exception ex)
        {
            _logger.LogError("Error refreshing peers", ex);
            ConnectionStatus = "Error refreshing peers";
            MessageBox.Show($"Error refreshing peers: {ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Scan mạng LAN bằng UDP broadcast qua PeerClient.ScanLanAsync()
    /// </summary>
    private async Task ScanNetworkAsync()
    {
        if (_peerClient == null || !_peerClient.IsRunning)
        {
            MessageBox.Show("Please connect to server first.",
                "Not Connected", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsLoading = true;
        ConnectionStatus = "Scanning LAN...";

        try
        {
            _logger.LogInfo("Scanning LAN for peers (UDP discovery)...");
            var peers = await _peerClient.ScanLanAsync();

            if (peers == null || peers.Count == 0)
            {
                _logger.LogInfo("Scan completed. No peers found.");
                MessageBox.Show("No peers found on LAN.",
                    "Scan Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Có thể chọn: chỉ hiển thị peers từ scan,
            // hoặc merge với list từ server. Đơn giản nhất: dùng luôn list mới.
            UpdatePeersList(peers);

            _logger.LogInfo($"Scan completed: {peers.Count} peer(s) discovered.");
            MessageBox.Show($"Found {peers.Count} peer(s) on LAN.",
                "Scan Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error scanning network", ex);
            MessageBox.Show($"Error scanning network: {ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            ConnectionStatus = IsConnected ? "Connected" : "Disconnected";
            IsLoading = false;
        }
    }

    /// <summary>
    /// Thêm file vào danh sách chia sẻ
    /// TODO (sau): sync danh sách này lên server
    /// </summary>
    private void AddSharedFile()
    {
        try
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Files to Share",
                Multiselect = true,
                Filter = "All Files|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                foreach (var filePath in dialog.FileNames)
                {
                    var fileInfo = new System.IO.FileInfo(filePath);
                    if (!fileInfo.Exists)
                        continue;

                    var sharedFile = new SharedFile
                    {
                        FileName = fileInfo.Name,
                        FilePath = filePath,
                        FileSize = fileInfo.Length
                    };

                    SharedFiles.Add(new SharedFileViewModel(sharedFile));
                    _logger.LogInfo($"Added shared file: {fileInfo.Name}");
                }

                // TODO: cập nhật lại shared files trên server
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Error adding shared file", ex);
            MessageBox.Show(
                $"Error adding file: {ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Cập nhật danh sách Peers từ list PeerInfo
    /// </summary>
    private void UpdatePeersList(List<PeerInfo> peerInfos)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            Peers.Clear();

            foreach (var peerInfo in peerInfos)
            {
                // Bỏ qua bản thân mình
                if (peerInfo.Username.Equals(_config.Username, StringComparison.OrdinalIgnoreCase))
                    continue;

                var peerViewModel = new PeerViewModel(peerInfo);
                Peers.Add(peerViewModel);
            }
        });
    }

    /// <summary>
    /// Xử lý khi file được drop vào một peer (drag & drop từ PeerItemControl)
    /// </summary>
    public async Task HandleFileDropAsync(PeerViewModel peer, IEnumerable<string> filePaths)
    {
        try
        {
            if (!peer.IsOnline)
            {
                MessageBox.Show("Peer is offline", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_peerClient == null || !_peerClient.IsRunning)
            {
                MessageBox.Show("Client is not connected.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            foreach (var filePath in filePaths)
            {
                if (!System.IO.File.Exists(filePath))
                    continue;

                var fileInfo = new System.IO.FileInfo(filePath);

                var transfer = new TransferViewModel
                {
                    FileName   = fileInfo.Name,
                    PeerName   = peer.Username,
                    TotalBytes = fileInfo.Length,
                    Status     = "Sending..."
                };

                Application.Current.Dispatcher.Invoke(() => Transfers.Add(transfer));

                // === Điểm quan trọng: dùng lại logic giống ConsoleUI ===
                var success = await _peerClient.SendFileAsync(peer.Username, filePath);

                if (success)
                {
                    transfer.MarkCompleted();
                    _logger.LogInfo($"File transfer completed: {fileInfo.Name} to {peer.Username}");
                }
                else
                {
                    transfer.MarkFailed();
                    _logger.LogError($"File transfer failed: {fileInfo.Name} to {peer.Username}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Error handling file drop", ex);
            MessageBox.Show($"Error sending file: {ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Xử lý khi nhận được file từ peer khác
    /// </summary>
    public void HandleFileReceived(string fileName, string fromPeer)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            MessageBox.Show(
                $"Received file: {fileName} from {fromPeer}",
                "File Received", MessageBoxButton.OK, MessageBoxImage.Information);

            _logger.LogInfo($"Received file: {fileName} from {fromPeer}");
        });
    }

    #endregion
}
