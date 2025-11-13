using P2PFileSharing.Common.Configuration;
using P2PFileSharing.Common.Infrastructure;
using P2PFileSharing.Common.Models;
using P2PFileSharing.Common.Utilities; 

namespace P2PFileSharing.Client;

/// <summary>
/// Main client class - quản lý peer client
/// TODO: Implement client lifecycle và coordination giữa các components
/// </summary>
public class PeerClient
{
    private readonly ClientConfig _config;
    private readonly ILogger _logger;
    private readonly ServerCommunicator _serverCommunicator;
    private readonly UdpDiscovery _udpDiscovery;
    private readonly FileTransferManager _fileTransferManager;
    private bool _isRunning;

    public PeerClient(ClientConfig config, ILogger logger)
    {
        _config = config;
        _logger = logger;
        _serverCommunicator = new ServerCommunicator(config, logger);
        _udpDiscovery = new UdpDiscovery(config, logger);
        _fileTransferManager = new FileTransferManager(config, logger);
    }

    /// <summary>
    /// Start client - đăng ký với server, start listeners
    /// </summary>
    public async Task StartAsync()
    {
        _isRunning = true;

        // TODO: Implement startup sequence
        // 1. Register with server
        // 2. Start P2P listener
        // 3. Start UDP discovery listener
        // 4. Start heartbeat task

        _logger.LogInfo("TODO: Start client");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Stop client - hủy đăng ký với server, cleanup
    /// </summary>
    public async Task StopAsync()
    {
        _isRunning = false;

        // TODO: Implement shutdown sequence
        // 1. Deregister from server
        // 2. Stop all listeners
        // 3. Cleanup resources

        _logger.LogInfo("TODO: Stop client");
        await Task.CompletedTask;
    }
    public async Task<List<PeerInfo>> ScanLanAsync()
    {
        try
        {
            if (_udpDiscovery.LocalPeerProvider == null)
            {
                _udpDiscovery.LocalPeerProvider = () => new PeerInfo
                {
                    Username   = Environment.UserName,
                    IpAddress  = NetworkHelper.GetLocalIPAddress(),
                    ListenPort = NetworkHelper.FindAvailablePort(5050, 5999),
                    LastSeen   = DateTime.UtcNow,
                    PeerId     = Guid.NewGuid().ToString()
                };
            }

            _udpDiscovery.StartListener();

            var peers = await _udpDiscovery.ScanNetworkAsync();
            _logger.LogInfo($"ScanLanAsync: found {peers.Count} peer(s).");

            return peers ?? new List<PeerInfo>();
        }
        catch (Exception ex)
        {
            _logger.LogError($"ScanLanAsync failed: {ex.Message}", ex);
            return new List<PeerInfo>();
        }
    }

    public bool IsRunning => _isRunning;
}

