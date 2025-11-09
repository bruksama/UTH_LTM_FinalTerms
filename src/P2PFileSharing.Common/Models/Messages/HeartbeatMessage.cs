namespace P2PFileSharing.Common.Models.Messages;

/// <summary>
/// Message heartbeat gửi định kỳ từ Client đến Server để báo hiệu còn online
/// </summary>
public class HeartbeatMessage : Message
{
    public override MessageType Type => MessageType.Heartbeat;

    /// <summary>
    /// Peer ID của peer gửi heartbeat
    /// </summary>
    public string PeerId { get; set; } = string.Empty;
}

