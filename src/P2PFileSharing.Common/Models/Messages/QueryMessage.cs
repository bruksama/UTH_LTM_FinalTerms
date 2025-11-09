namespace P2PFileSharing.Common.Models.Messages;

/// <summary>
/// Message gửi từ Client đến Server để truy vấn danh sách peers
/// </summary>
public class QueryMessage : Message
{
    public override MessageType Type => MessageType.QueryPeers;

    /// <summary>
    /// (Optional) Tên file cụ thể để tìm peer có chia sẻ file đó
    /// </summary>
    public string? FileNameFilter { get; set; }
}

