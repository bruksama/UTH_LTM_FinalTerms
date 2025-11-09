using System.Collections.Concurrent;
using P2PFileSharing.Common.Configuration;
using P2PFileSharing.Common.Infrastructure;
using P2PFileSharing.Common.Models;

namespace P2PFileSharing.Server;

/// <summary>
/// Quản lý danh sách peers đang online
/// TODO: Implement register, query, deregister, timeout logic
/// </summary>
public class PeerRegistry
{
    private readonly ConcurrentDictionary<string, PeerInfo> _peers;
    private readonly ServerConfig _config;
    private readonly ILogger _logger;

    public PeerRegistry(ServerConfig config, ILogger logger)
    {
        _config = config;
        _logger = logger;
        _peers = new ConcurrentDictionary<string, PeerInfo>();
    }

    /// <summary>
    /// Đăng ký một peer mới
    /// </summary>
    public bool RegisterPeer(PeerInfo peerInfo)
    {
        // TODO: Implement registration logic
        // - Validate peer info
        // - Check max peers limit
        // - Add/update peer in dictionary
        // - Log registration
        _logger.LogInfo($"TODO: Register peer {peerInfo.Username} ({peerInfo.IpAddress}:{peerInfo.ListenPort})");
        return false;
    }

    /// <summary>
    /// Hủy đăng ký một peer
    /// </summary>
    public bool DeregisterPeer(string peerId)
    {
        // TODO: Implement deregistration logic
        _logger.LogInfo($"TODO: Deregister peer {peerId}");
        return false;
    }

    /// <summary>
    /// Lấy danh sách tất cả peers đang online
    /// </summary>
    public List<PeerInfo> GetAllPeers()
    {
        // TODO: Return list of online peers
        return new List<PeerInfo>();
    }

    /// <summary>
    /// Lấy danh sách peers có chia sẻ file cụ thể
    /// </summary>
    public List<PeerInfo> GetPeersWithFile(string fileName)
    {
        // TODO: Filter peers by shared file
        return new List<PeerInfo>();
    }

    /// <summary>
    /// Update heartbeat cho một peer
    /// </summary>
    public void UpdateHeartbeat(string peerId)
    {
        // TODO: Update LastSeen timestamp
    }

    /// <summary>
    /// Cleanup các peers đã timeout
    /// </summary>
    public void CleanupTimeoutPeers()
    {
        // TODO: Remove peers that haven't sent heartbeat within timeout period
        _logger.LogDebug("TODO: Cleanup timeout peers");
    }

    /// <summary>
    /// Lấy số lượng peers đang online
    /// </summary>
    public int GetPeerCount()
    {
        return _peers.Count;
    }
}

