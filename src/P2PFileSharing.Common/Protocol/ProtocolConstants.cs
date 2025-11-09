namespace P2PFileSharing.Common.Protocol;

/// <summary>
/// Các hằng số cho protocol
/// </summary>
public static class ProtocolConstants
{
    /// <summary>
    /// Port mặc định cho Registry Server
    /// </summary>
    public const int DefaultServerPort = 5000;

    /// <summary>
    /// Port mặc định cho Client lắng nghe kết nối P2P
    /// </summary>
    public const int DefaultClientListenPort = 5001;

    /// <summary>
    /// Port mặc định cho UDP Broadcast discovery
    /// </summary>
    public const int DefaultDiscoveryPort = 5002;

    /// <summary>
    /// Timeout mặc định cho peer heartbeat (5 phút)
    /// </summary>
    public static readonly TimeSpan DefaultPeerTimeout = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Interval để gửi heartbeat (1 phút)
    /// </summary>
    public static readonly TimeSpan HeartbeatInterval = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Kích thước buffer mặc định cho file transfer (64KB)
    /// </summary>
    public const int DefaultBufferSize = 64 * 1024;

    /// <summary>
    /// Kích thước buffer lớn cho file transfer (1MB)
    /// </summary>
    public const int LargeBufferSize = 1024 * 1024;

    /// <summary>
    /// Protocol version
    /// </summary>
    public const int ProtocolVersion = 1;

    /// <summary>
    /// Magic number để identify protocol (4 bytes)
    /// </summary>
    public static readonly byte[] MagicNumber = { 0x50, 0x32, 0x50, 0x46 }; // "P2PF"

    /// <summary>
    /// Timeout cho TCP connection (30 giây)
    /// </summary>
    public static readonly TimeSpan ConnectionTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Timeout cho read/write operations (60 giây)
    /// </summary>
    public static readonly TimeSpan ReadWriteTimeout = TimeSpan.FromSeconds(60);
}

