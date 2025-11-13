namespace P2PFileSharing.Common.Models.Messages;

/// <summary>
/// Message gửi từ Peer A đến Peer B để yêu cầu truyền file
/// </summary>
public class FileTransferRequestMessage : Message
{
    public override MessageType Type => MessageType.FileTransferRequest;

    /// <summary>
    /// Tên file muốn gửi
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Kích thước file (bytes)
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Checksum của file (SHA256)
    /// </summary>
    public string Checksum { get; set; } = string.Empty;

    /// <summary>
    /// (Optional) Byte offset để resume transfer
    /// </summary>
    public long? ResumeOffset { get; set; }
}

