namespace P2PFileSharing.Common.Models.Messages;

/// <summary>
/// Base class cho tất cả các message trong protocol
/// </summary>
public abstract class Message
{
    /// <summary>
    /// Loại message (được định nghĩa bởi các class con)
    /// </summary>
    public abstract MessageType Type { get; }

    /// <summary>
    /// Timestamp khi message được tạo
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Message ID để tracking (optional)
    /// </summary>
    public string MessageId { get; set; } = Guid.NewGuid().ToString();
}

/// <summary>
/// Enum định nghĩa các loại message trong protocol
/// </summary>
public enum MessageType
{
    // Client -> Server messages
    Register,
    Deregister,
    QueryPeers,
    Heartbeat,

    // Server -> Client messages
    RegisterAck,
    RegisterNack,
    QueryResponse,
    HeartbeatAck,

    // Peer -> Peer messages (P2P)
    FileTransferRequest,
    FileTransferResponse,
    FileTransferData,
    FileTransferComplete,
    FileTransferError
}

