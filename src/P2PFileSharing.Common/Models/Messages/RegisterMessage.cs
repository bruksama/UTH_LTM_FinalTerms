using P2PFileSharing.Common.Models;

namespace P2PFileSharing.Common.Models.Messages;

/// <summary>
/// Message gửi từ Client đến Server để đăng ký peer
/// </summary>
public class RegisterMessage : Message
{
    public override MessageType Type => MessageType.Register;

    /// <summary>
    /// Thông tin peer đang đăng ký
    /// </summary>
    public PeerInfo PeerInfo { get; set; } = new();
}

