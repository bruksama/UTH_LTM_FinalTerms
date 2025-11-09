namespace P2PFileSharing.Common.Models.Messages;

/// <summary>
/// Message phản hồi từ Peer B về yêu cầu truyền file
/// </summary>
public class FileTransferResponseMessage : Message
{
    public override MessageType Type => MessageType.FileTransferResponse;

    /// <summary>
    /// True nếu peer đồng ý gửi file, False nếu từ chối
    /// </summary>
    public bool Accepted { get; set; }

    /// <summary>
    /// Tên file
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Kích thước file (bytes)
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Checksum của file
    /// </summary>
    public string Checksum { get; set; } = string.Empty;

    /// <summary>
    /// Thông báo lỗi nếu từ chối
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Byte offset để resume (nếu có)
    /// </summary>
    public long? ResumeOffset { get; set; }
}

