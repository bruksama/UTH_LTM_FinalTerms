namespace P2PFileSharing.Common.Models.Messages;

/// <summary>
/// Message phản hồi từ Server xác nhận đăng ký thành công
/// </summary>
public class RegisterAckMessage : Message
{
    public override MessageType Type => MessageType.RegisterAck;

    /// <summary>
    /// Peer ID được cấp bởi Server
    /// </summary>
    public string PeerId { get; set; } = string.Empty;

    /// <summary>
    /// Thông báo (optional)
    /// </summary>
    public string Message { get; set; } = "Registration successful";
}

