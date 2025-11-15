using P2PFileSharing.Common.Configuration;
using P2PFileSharing.Common.Infrastructure;
using P2PFileSharing.Common.Models;
using P2PFileSharing.Common.Utilities;

namespace P2PFileSharing.Client;

/// <summary>
/// Main client class - quản lý peer client
/// </summary>
public class PeerClient
{
    // ✅ THÊM 1 EVENT MỚI (Mục 2)
    /// <summary>
    /// Bắn event khi một file đã được nhận và lưu thành công
    /// Params: (fileName, fullSavePath, fromPeer)
    /// </summary>
    public event Action<string, string, string>? OnFileReceived;

    private readonly ClientConfig _config;
    private readonly ILogger _logger;
    private readonly ServerCommunicator _serverCommunicator;
    private readonly UdpDiscovery _udpDiscovery;
    private readonly FileTransferManager _fileTransferManager;
    private bool _isRunning;
    private PeerInfo? _currentPeerInfo;

    public PeerClient(ClientConfig config, ILogger logger)
    {
        _config = config;
        _logger = logger;
        _serverCommunicator = new ServerCommunicator(config, logger);
        _udpDiscovery = new UdpDiscovery(config, logger);
        _fileTransferManager = new FileTransferManager(config, logger);
        
        _fileTransferManager.OnFileReceived += (fileName, savePath, fromPeer) =>
        {
            OnFileReceived?.Invoke(fileName, savePath, fromPeer);
        };
    }

    /// <summary>
    /// Start client - đăng ký với server, start listeners
    /// </summary>
    public async Task StartAsync()
    {
        if (_isRunning) return;

        _isRunning = true;

        _fileTransferManager.StartReceiver();
        
        var actualListenPort = _fileTransferManager.GetActualListenPort();
        
        if (actualListenPort == -1 || !_fileTransferManager.IsReceiverRunning)
        {
            _logger.LogError("File receiver failed to start. Cannot register with server.");
            actualListenPort = _config.ListenPort; 
        }
        
        _currentPeerInfo = new PeerInfo
        {
            Username = _config.Username,
            IpAddress = NetworkHelper.GetLocalIPAddress(),
            ListenPort = actualListenPort,
            LastSeen = DateTime.UtcNow,
            PeerId = Guid.NewGuid().ToString(),
            SharedFiles = GetSharedFiles()
        };

        _udpDiscovery.LocalPeerProvider = () => new PeerInfo
        {
            Username = _currentPeerInfo.Username,
            IpAddress = _currentPeerInfo.IpAddress,
            ListenPort = _currentPeerInfo.ListenPort,
            LastSeen = DateTime.UtcNow,
            PeerId = _currentPeerInfo.PeerId,
            SharedFiles = _currentPeerInfo.SharedFiles
        };

        _udpDiscovery.StartListener();

        if (_fileTransferManager.IsReceiverRunning && actualListenPort > 0)
        {
            var registered = await _serverCommunicator.RegisterAsync(_currentPeerInfo);
            if (registered)
            {
                _logger.LogInfo($"Successfully registered with server as {_currentPeerInfo.Username} on {_currentPeerInfo.IpAddress}:{_currentPeerInfo.ListenPort}");
            }
            else
            {
                _logger.LogWarning("Failed to register with server, but continuing anyway (UDP discovery still works)");
            }
        }
        else
        {
            _logger.LogWarning("Cannot register with server: file receiver is not running");
        }

        _logger.LogInfo($"PeerClient started. Listen Port: {actualListenPort}");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Stop client - hủy đăng ký với server, cleanup
    /// </summary>
    public async Task StopAsync()
    {
        if (!_isRunning) return;

        if (_currentPeerInfo != null && !string.IsNullOrEmpty(_currentPeerInfo.PeerId))
        {
            await _serverCommunicator.DeregisterAsync(_currentPeerInfo.PeerId);
        }

        _fileTransferManager.StopReceiver();
        _udpDiscovery.StopListener();

        _isRunning = false;

        _logger.LogInfo("PeerClient stopped.");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Quét LAN bằng UDP discovery để tìm các peers đang online.
    /// </summary>
    public async Task<List<PeerInfo>> ScanLanAsync()
    {
        try
        {
            if (_udpDiscovery.LocalPeerProvider == null)
            {
                var actualPort = _fileTransferManager.GetActualListenPort();
                if (actualPort == -1)
                {
                    actualPort = _config.ListenPort;
                }
                _udpDiscovery.LocalPeerProvider = () => new PeerInfo
                {
                    Username = _config.Username,
                    IpAddress = NetworkHelper.GetLocalIPAddress(),
                    ListenPort = actualPort,
                    LastSeen = DateTime.UtcNow,
                    PeerId = _currentPeerInfo?.PeerId ?? Guid.NewGuid().ToString(),
                    SharedFiles = GetSharedFiles()
                };
            }
            
            _udpDiscovery.StartListener();

            var peers = await _udpDiscovery.ScanNetworkAsync();
            _logger.LogInfo($"ScanLanAsync: found {peers.Count} peer(s).");

            return peers;
        }
        catch (Exception ex)
        {
            _logger.LogError($"ScanLanAsync failed: {ex.Message}", ex);
            return new List<PeerInfo>();
        }
    }

    /// <summary>
    /// Query danh sách peers từ Server (FR-03)
    /// </summary>
    public async Task<List<PeerInfo>> QueryPeersAsync(string? fileNameFilter = null)
    {
        return await _serverCommunicator.QueryPeersAsync(fileNameFilter);
    }

    /// <summary>
    /// Gửi file đến một peer (FR-04)
    /// </summary>
    /// <param name="peerName">Tên peer (username) hoặc IP:Port</param>
    /// <param name="filePath">Đường dẫn file cần gửi</param>
    public async Task<bool> SendFileAsync(string peerName, string filePath)
    {
        try
        {
            PeerInfo? targetPeer = null;

            if (peerName.Contains(':'))
            {
                var parts = peerName.Split(':');
                if (parts.Length == 2 && 
                    System.Net.IPAddress.TryParse(parts[0], out _) && 
                    int.TryParse(parts[1], out var port))
                {
                    targetPeer = new PeerInfo
                    {
                        IpAddress = parts[0],
                        ListenPort = port,
                        Username = peerName
                    };
                }
            }

            if (targetPeer == null)
            {
                var peers = await _serverCommunicator.QueryPeersAsync();
                targetPeer = peers.FirstOrDefault(p => 
                    string.Equals(p.Username, peerName, StringComparison.OrdinalIgnoreCase));
            }

            if (targetPeer == null)
            {
                var scannedPeers = await ScanLanAsync();
                targetPeer = scannedPeers.FirstOrDefault(p => 
                    string.Equals(p.Username, peerName, StringComparison.OrdinalIgnoreCase));
            }

            if (targetPeer == null)
            {
                _logger.LogError($"Peer not found: {peerName}");
                return false;
            }

            _logger.LogInfo($"Sending file to peer: {targetPeer.Username} ({targetPeer.IpAddress}:{targetPeer.ListenPort})");
            return await _fileTransferManager.SendFileAsync(targetPeer.IpAddress, targetPeer.ListenPort, filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error sending file to {peerName}: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// Lấy danh sách file trong shared directory
    /// </summary>
    private List<Common.Models.SharedFile> GetSharedFiles()
    {
        var sharedFiles = new List<Common.Models.SharedFile>();
        
        try
        {
            if (Directory.Exists(_config.SharedDirectory))
            {
                var files = Directory.GetFiles(_config.SharedDirectory);
                foreach (var filePath in files)
                {
                    try
                    {
                        var fileInfo = new FileInfo(filePath);
                        sharedFiles.Add(new Common.Models.SharedFile
                        {
                            FileName = fileInfo.Name,
                            FileSize = fileInfo.Length,
                            FilePath = filePath
                        });
                    }
                    catch
                    {
                        // Skip files that can't be accessed
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error reading shared files: {ex.Message}");
        }
        
        return sharedFiles;
    }

    public bool IsRunning => _isRunning;

    /// <summary>
    /// Set ConsoleUI reference để FileTransferManager có thể tạm dừng command loop
    /// </summary>
    public void SetConsoleUI(ConsoleUI? consoleUI)
    {
        _fileTransferManager.SetConsoleUI(consoleUI);
    }

    /// <summary>
    /// Set handler cho incoming file transfer requests (cho GUI mode)
    /// </summary>
    /// <param name="handler">Callback sẽ được gọi khi có file transfer request</param>
    public void SetFileTransferRequestHandler(
        FileTransferManager.FileTransferRequestHandler? handler)
    {
        _fileTransferManager.SetFileTransferRequestHandler(handler);
    }

    /// <summary>
    /// Thay đổi username của peer và re-register với server nếu đang running
    /// </summary>
    public async Task<bool> ChangeUsernameAsync(string newUsername)
    {
        if (string.IsNullOrWhiteSpace(newUsername))
        {
            _logger.LogError("Cannot change username: new username cannot be empty");
            return false;
        }

        var oldUsername = _config.Username;
        
        _config.Username = newUsername;

        if (_currentPeerInfo != null)
        {
            _currentPeerInfo.Username = newUsername;
        }

        if (_isRunning)
        {
            if (_currentPeerInfo == null)
            {
                _logger.LogWarning($"Cannot change username: peer info is not initialized yet. Please wait for client to fully start.");
                _config.Username = oldUsername;
                return false;
            }

            if (!_fileTransferManager.IsReceiverRunning)
            {
                _logger.LogWarning($"Cannot change username: file receiver is not running. Username change requires re-registration.");
                _config.Username = oldUsername;
                _currentPeerInfo.Username = oldUsername;
                return false;
            }

            string? oldPeerId = null;
            bool hadOldPeerId = false;
            if (!string.IsNullOrEmpty(_currentPeerInfo.PeerId))
            {
                oldPeerId = _currentPeerInfo.PeerId;
                hadOldPeerId = true;
            }

            if (hadOldPeerId)
            {
                await _serverCommunicator.DeregisterAsync(_currentPeerInfo.PeerId);
                _logger.LogInfo($"Deregistered old username: {oldUsername}");
            }

            _currentPeerInfo.PeerId = Guid.NewGuid().ToString();
            _currentPeerInfo.LastSeen = DateTime.UtcNow;
            _currentPeerInfo.SharedFiles = GetSharedFiles();

            var registered = await _serverCommunicator.RegisterAsync(_currentPeerInfo);
            if (registered)
            {
                _logger.LogInfo($"Successfully re-registered with new username: {newUsername}");
            }
            else
            {
                _logger.LogWarning($"Failed to re-register with new username: {newUsername}");
                _config.Username = oldUsername;
                _currentPeerInfo.Username = oldUsername;
                _currentPeerInfo.PeerId = oldPeerId ?? string.Empty;
                
                if (hadOldPeerId && !string.IsNullOrEmpty(oldPeerId))
                {
                    _logger.LogInfo($"Attempting to re-register with old PeerId to restore previous state...");
                    var restored = await _serverCommunicator.RegisterAsync(_currentPeerInfo);
                    if (restored)
                    {
                        _logger.LogInfo($"Successfully re-registered with old PeerId. Username change rolled back.");
                    }
                    else
                    {
                        _logger.LogError($"Failed to re-register with old PeerId. Client is now unregistered from server.");
                    }
                }
                
                return false;
            }

            if (_currentPeerInfo != null)
            {
                _udpDiscovery.LocalPeerProvider = () => new PeerInfo
                {
                    Username = _currentPeerInfo.Username,
                    IpAddress = _currentPeerInfo.IpAddress,
                    ListenPort = _currentPeerInfo.ListenPort,
                    LastSeen = DateTime.UtcNow,
                    PeerId = _currentPeerInfo.PeerId,
                    SharedFiles = _currentPeerInfo.SharedFiles
                };
            }
        }

        _logger.LogInfo($"Username changed from '{oldUsername}' to '{newUsername}'");
        return true;
    }
}