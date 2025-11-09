namespace P2PFileSharing.Common.Models.Messages;

/// <summary>
/// Message gửi từ Peer A đến Peer B để yêu cầu truyền file
/// </summary>
public class FileTransferRequestMessage : Message
{
    public override MessageType Type => MessageType.FileTransferRequest;

    /// <summary>
    /// Tên file muốn tải
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// (Optional) Byte offset để resume transfer
    /// </summary>
    public long? ResumeOffset { get; set; }
}

