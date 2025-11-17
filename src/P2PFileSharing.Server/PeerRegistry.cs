using System.Collections.Concurrent;
using P2PFileSharing.Common.Configuration;
using P2PFileSharing.Common.Infrastructure;
using P2PFileSharing.Common.Models;
using System.Reflection;
using System.Linq;
using System;
using System.Net;   // để dùng IPAddress.TryParse


namespace P2PFileSharing.Server;

/// <summary>
/// Quản lý danh sách peers đang online
/// </summary>
public class PeerRegistry
{
    private readonly ConcurrentDictionary<string, PeerInfo> _peers;
    private readonly ServerConfig _config;
    private readonly ILogger _logger;
    private readonly TimeSpan _peerTtl;

     private const int DefaultPeerTimeoutSeconds = 45;
    public PeerRegistry(ServerConfig config, ILogger logger)
    {
        _config = config;
        _logger = logger;
        _peers = new ConcurrentDictionary<string, PeerInfo>();
        _peerTtl = ResolvePeerTtlFromConfig(config);
    }

    /// <summary>
    /// Đăng ký một peer mới
    /// </summary>
    public bool RegisterPeer(PeerInfo peerInfo)
    {
        if (peerInfo is null)
        {
            _logger.LogInfo("RegisterPeer: peerInfo is null");
            return false;
        }
        if (string.IsNullOrWhiteSpace(peerInfo.Username))
        {
            _logger.LogInfo("RegisterPeer: username is empty");
            return false;
        }
        if (string.IsNullOrWhiteSpace(peerInfo.IpAddress))
        {
            _logger.LogInfo("RegisterPeer: IpAddress is empty");
            return false;
        }

        if (!IPAddress.TryParse(peerInfo.IpAddress, out _))
        {
            _logger.LogInfo($"RegisterPeer: invalid IpAddress={peerInfo.IpAddress}");
            return false;
        }
        if (peerInfo.ListenPort < 0 || peerInfo.ListenPort > 65535)
        {
            _logger.LogInfo($"RegisterPeer: invalid ListenPort={peerInfo.ListenPort} for [{peerInfo.Username}]");
            return false;
        }

        // (Optional) Check MaxPeers nếu có trong config
        // - chỉ chặn khi là đăng ký mới, KHÔNG chặn re-register
        int? maxPeers = TryGetIntConfig(_config, "MaxPeers");
        bool isExisting = _peers.ContainsKey(peerInfo.Username);
        if (!isExisting && maxPeers.HasValue && _peers.Count >= maxPeers.Value)
        {
            _logger.LogInfo($"RegisterPeer: reach MaxPeers={maxPeers.Value}, reject [{peerInfo.Username}]");
            return false;
        }

        // Chuẩn hoá trạng thái "còn sống"
        var now = DateTime.UtcNow;
        
        var isNew = false;

        _peers.AddOrUpdate(
            peerInfo.Username,
            addValueFactory: _ =>
            {
                isNew = true;
                return new PeerInfo
                {
                    Username    = peerInfo.Username,
                    IpAddress   = peerInfo.IpAddress,
                    ListenPort  = peerInfo.ListenPort,
                    SharedFiles = peerInfo.SharedFiles, // có thể rỗng
                    LastSeen    = now,
                    PeerId      = peerInfo.PeerId
                };
            },
            updateValueFactory: (_, existing) =>
            {
                // Tạo mới thay vì mutate để an toàn nếu model có init;
                return new PeerInfo
                {
                    Username    = existing.Username,         // giữ key
                    IpAddress   = peerInfo.IpAddress,
                    ListenPort  = peerInfo.ListenPort,
                    SharedFiles = peerInfo.SharedFiles ?? existing.SharedFiles,
                    LastSeen    = now,
                    PeerId      = existing.PeerId 
                };
            }
        );

        if (isNew)
            _logger.LogInfo($"Register [{peerInfo.Username}] {peerInfo.IpAddress}:{peerInfo.ListenPort}");
        else
            _logger.LogInfo($"Re-register (update) [{peerInfo.Username}] {peerInfo.IpAddress}:{peerInfo.ListenPort}");

        return true;
    }

    /// <summary>
    /// Hủy đăng ký một peer
    /// </summary>
    public bool DeregisterPeer(string peerId)
    {
        if (string.IsNullOrWhiteSpace(peerId))
        {
            _logger.LogInfo("DeregisterPeer: peerId is empty");
            return false;
        }

        var removed = _peers.TryRemove(peerId, out _);
        if (removed)
            _logger.LogInfo($"Deregister [{peerId}]");
        else
            _logger.LogInfo($"Deregister: not found [{peerId}]");

        return removed;
    }

    /// <summary>
    /// Lấy danh sách tất cả peers đang online
    /// </summary>
    public List<PeerInfo> GetAllPeers()
    {
        var now = DateTime.UtcNow;
        return _peers.Values
            .Where(p => (now - p.LastSeen) <= _peerTtl)
            .OrderBy(p => p.Username, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
    /// <summary>
    /// Lấy danh sách peers có chia sẻ file cụ thể
    /// </summary>
    public List<PeerInfo> GetPeersWithFile(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return new List<PeerInfo>();

        var now = DateTime.UtcNow;
        return _peers.Values
            .Where(p => (now - p.LastSeen) <= _peerTtl)
            .Where(p => p.SharedFiles != null &&
                        p.SharedFiles.Any(f =>
                            string.Equals(f.FileName, fileName, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(p => p.Username, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Query danh sách peers (lọc theo tên file nếu có), đã áp dụng TTL & sort
    /// </summary>
    public List<PeerInfo> QueryPeers(string? fileNameFilter = null)
    {
        if (string.IsNullOrWhiteSpace(fileNameFilter))
            return GetAllPeers();

        return GetPeersWithFile(fileNameFilter);
    }

    /// <summary>
    /// Update heartbeat cho một peer
    /// </summary>
    public void UpdateHeartbeat(string peerId)
    {
       if (string.IsNullOrWhiteSpace(peerId)) return;
        if (_peers.TryGetValue(peerId, out var existing))
        {
            _peers[peerId] = new PeerInfo
            {
                Username    = existing.Username,
                IpAddress   = existing.IpAddress,
                ListenPort  = existing.ListenPort,
                SharedFiles = existing.SharedFiles,
                LastSeen    = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Cleanup các peers đã timeout
    /// </summary>
    public void CleanupTimeoutPeers()
    {
       var now = DateTime.UtcNow;
        var removed = 0;

        foreach (var kv in _peers)
        {
            if ((now - kv.Value.LastSeen) > _peerTtl)
            {
                if (_peers.TryRemove(kv.Key, out _))
                {
                    removed++;
                    _logger.LogInfo($"Cleanup: removed stale peer [{kv.Key}]");
                }
            }
        }

        _logger.LogDebug(removed == 0 ? "Cleanup: no stale peers" : $"Cleanup: removed {removed} stale peers");
    }

    /// <summary>
    /// Lấy số lượng peers đang online
    /// </summary>
    public int GetPeerCount()
    {
        return _peers.Count;
    }

    private static TimeSpan ResolvePeerTtlFromConfig(ServerConfig cfg)
    {
        // Dùng reflection để linh hoạt theo tên thuộc tính cấu hình khác nhau giữa các nhóm
        // Ưu tiên: HeartbeatTimeoutSeconds, PeerTimeoutSeconds
        if (cfg.PeerTimeout > TimeSpan.Zero)
        return cfg.PeerTimeout;

    // fallback nếu cấu hình bị lỗi
        return TimeSpan.FromSeconds(DefaultPeerTimeoutSeconds);
    }
    
    private static int? TryGetIntConfig(ServerConfig cfg, string name)
    {
        var prop = cfg.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
        if (prop == null) return null;
        var val = prop.GetValue(cfg);
        return val is int i ? i : (int?)null;
    }

}


