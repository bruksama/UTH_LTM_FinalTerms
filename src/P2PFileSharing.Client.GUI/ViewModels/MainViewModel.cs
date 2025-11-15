using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
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
        
        Username = config.Username;
        ServerAddress = $"{config.ServerIpAddress}:{config.ServerPort}";

        Peers = new ObservableCollection<PeerViewModel>();
        Transfers = new ObservableCollection<TransferViewModel>();
        SharedFiles = new ObservableCollection<SharedFileViewModel>();
        
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
    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    public string ServerAddress
    {
        get => _serverAddress;
        set => SetProperty(ref _serverAddress, value);
    }

    public bool IsConnected
    {
        get => _isConnected;
        set
        {
            if (SetProperty(ref _isConnected, value))
            {
                ConnectionStatus = value ? "Connected" : "Disconnected";
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public string ConnectionStatus
    {
        get => _connectionStatus;
        set => SetProperty(ref _connectionStatus, value);
    }

    public PeerViewModel? SelectedPeer
    {
        get => _selectedPeer;
        set => SetProperty(ref _selectedPeer, value);
    }

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

    public ObservableCollection<PeerViewModel> Peers { get; }
    public ObservableCollection<TransferViewModel> Transfers { get; }
    public ObservableCollection<SharedFileViewModel> SharedFiles { get; }

    #endregion

    #region Commands
    public ICommand ConnectCommand { get; }
    public ICommand DisconnectCommand { get; }
    public ICommand RefreshPeersCommand { get; }
    public ICommand ScanNetworkCommand { get; }
    public ICommand AddSharedFileCommand { get; }

    #endregion

    #region Methods

    /// <summary>
    /// Kết nối với Server và đăng ký peer
    /// </summary>
    private async Task ConnectAsync()
    {
        IsLoading = true;
        ConnectionStatus = "Connecting...";

        try
        {
            var parts = ServerAddress.Split(':');
            if (parts.Length != 2 || !int.TryParse(parts[1], out var port))
            {
                MessageBox.Show(
                    "Invalid server address format. Use IP:Port (e.g., 192.168.1.100:5000)",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _config.ServerIpAddress = parts[0];
            _config.ServerPort = port;
            _config.Username = Username;

            _peerClient = new PeerClient(_config, _logger);
            await _peerClient.StartAsync();
            
            _peerClient.SetFileTransferRequestHandler(HandleIncomingFileTransferRequestAsync);
            _peerClient.OnFileReceived += HandleFileReceived; 

            if (_peerClient.IsRunning)
            {
                IsConnected = true;
                _logger.LogInfo("Successfully connected to server");
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
    /// </summary>
    private async Task DisconnectAsync()
    {
        IsLoading = true;
        ConnectionStatus = "Disconnecting...";

        try
        {
            if (_peerClient != null)
            {
                _peerClient.SetFileTransferRequestHandler(null);
                _peerClient.OnFileReceived -= HandleFileReceived;
                
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
                    
                    var vm = new SharedFileViewModel(sharedFile)
                    {
                        Direction = FileDirection.Sharing 
                    };

                    SharedFiles.Add(vm);
                    
                    _logger.LogInfo($"Added shared file: {fileInfo.Name}");
                }
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
                    MessageBoxButton.OK, MessageBoxImage.Error);
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
    public void HandleFileReceived(string fileName, string fullSavePath, string fromPeer)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            try
            {
                _logger.LogInfo($"GUI received file: {fileName} from {fromPeer}. Path: {fullSavePath}");
                
                long fileSize = 0;
                if (File.Exists(fullSavePath))
                {
                    fileSize = new FileInfo(fullSavePath).Length;
                }
                
                var sharedFile = new SharedFile
                {
                    FileName = fileName,
                    FilePath = fullSavePath,
                    FileSize = fileSize
                };
                
                var vm = new SharedFileViewModel(sharedFile)
                {
                    Direction = FileDirection.Received 
                };

                SharedFiles.Add(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error adding received file to UI: {ex.Message}", ex);
            }
        });
    }


    /// <summary>
    /// Xử lý yêu cầu chuyển file đến (incoming file transfer request)
    /// </summary>
    private async Task<bool> HandleIncomingFileTransferRequestAsync(
        string fileName, 
        long fileSize, 
        string fromPeer, 
        string checksum)
    {
        bool accepted = false;
        
        try
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var result = MessageBox.Show(
                    $"Incoming File Transfer Request\n\n" +
                    $"From: {fromPeer}\n" +
                    $"File: {fileName}\n" +
                    $"Size: {FormatFileSize(fileSize)}\n" +
                    (!string.IsNullOrEmpty(checksum) 
                        ? $"Checksum: {checksum.Substring(0, Math.Min(16, checksum.Length))}...\n" 
                        : "") +
                    $"\nDo you want to accept this file?",
                    "File Transfer Request",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question,
                    MessageBoxResult.No); 
                
                accepted = (result == MessageBoxResult.Yes);
                
                _logger.LogInfo($"User {(accepted ? "accepted" : "rejected")} file transfer: {fileName} from {fromPeer}");
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error showing file transfer request dialog: {ex.Message}", ex);
            accepted = false; 
        }
        
        return accepted;
    }

    /// <summary>
    /// Format file size thành dạng dễ đọc (B, KB, MB, GB, TB)
    /// </summary>
    private static string FormatFileSize(long bytes)
    {
        // (Không thay đổi)
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    #endregion
}