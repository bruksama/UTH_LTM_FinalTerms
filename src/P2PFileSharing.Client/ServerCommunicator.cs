using P2PFileSharing.Common.Configuration;
using P2PFileSharing.Common.Infrastructure;
using P2PFileSharing.Common.Models;
using P2PFileSharing.Common.Models.Messages;

namespace P2PFileSharing.Client;

/// <summary>
/// Giao tiếp với Registry Server
/// TODO: Implement client-server communication (FR-01, FR-03, FR-09)
/// </summary>
public class ServerCommunicator
{
    private readonly ClientConfig _config;
    private readonly ILogger _logger;

    public ServerCommunicator(ClientConfig config, ILogger logger)
    {
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Đăng ký peer với Server (FR-01)
    /// </summary>
    public async Task<bool> RegisterAsync(PeerInfo peerInfo)
    {
        // TODO: Implement registration
        // 1. Connect to server
        // 2. Send RegisterMessage
        // 3. Receive RegisterAckMessage
        // 4. Return success/failure
        _logger.LogInfo($"TODO: Register peer {peerInfo.Username} with server");
        return false;
    }

    /// <summary>
    /// Query danh sách peers từ Server (FR-03)
    /// </summary>
    public async Task<List<PeerInfo>> QueryPeersAsync(string? fileNameFilter = null)
    {
        // TODO: Implement query
        // 1. Connect to server
        // 2. Send QueryMessage
        // 3. Receive QueryResponseMessage
        // 4. Return list of peers
        _logger.LogInfo($"TODO: Query peers from server (filter: {fileNameFilter ?? "none"})");
        return new List<PeerInfo>();
    }

    /// <summary>
    /// Hủy đăng ký với Server (FR-09)
    /// </summary>
    public async Task<bool> DeregisterAsync(string peerId)
    {
        // TODO: Implement deregistration
        _logger.LogInfo($"TODO: Deregister peer {peerId} from server");
        return false;
    }

    /// <summary>
    /// Gửi heartbeat đến Server
    /// </summary>
    public async Task SendHeartbeatAsync(string peerId)
    {
        // TODO: Implement heartbeat
        _logger.LogDebug($"TODO: Send heartbeat for peer {peerId}");
    }
}

