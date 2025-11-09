using System.Collections.Generic;

namespace P2PFileSharing.Common.Models;

/// <summary>
/// Thông tin về một Peer trong hệ thống
/// </summary>
public class PeerInfo
{
    /// <summary>
    /// Địa chỉ IP của peer
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// Port mà peer lắng nghe để nhận kết nối P2P
    /// </summary>
    public int ListenPort { get; set; }

    /// <summary>
    /// Tên người dùng của peer
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Thời điểm cuối cùng peer được nhìn thấy (heartbeat)
    /// </summary>
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Danh sách các file mà peer đang chia sẻ
    /// </summary>
    public List<SharedFile> SharedFiles { get; set; } = new();

    /// <summary>
    /// Unique identifier của peer (có thể là GUID được tạo khi đăng ký)
    /// </summary>
    public string PeerId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Kiểm tra xem peer có còn online không dựa trên LastSeen và timeout
    /// </summary>
    public bool IsOnline(TimeSpan timeout)
    {
        return DateTime.UtcNow - LastSeen < timeout;
    }

    public override string ToString()
    {
        return $"{Username} ({IpAddress}:{ListenPort})";
    }
}

