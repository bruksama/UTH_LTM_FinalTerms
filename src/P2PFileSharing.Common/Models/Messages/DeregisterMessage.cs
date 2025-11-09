namespace P2PFileSharing.Common.Models.Messages;

/// <summary>
/// Message gửi từ Client đến Server để hủy đăng ký
/// </summary>
public class DeregisterMessage : Message
{
    public override MessageType Type => MessageType.Deregister;

    /// <summary>
    /// Peer ID của peer muốn hủy đăng ký
    /// </summary>
    public string PeerId { get; set; } = string.Empty;
}

