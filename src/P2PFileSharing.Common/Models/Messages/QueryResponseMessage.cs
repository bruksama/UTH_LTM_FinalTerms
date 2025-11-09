using P2PFileSharing.Common.Models;

namespace P2PFileSharing.Common.Models.Messages;

/// <summary>
/// Message phản hồi từ Server chứa danh sách peers
/// </summary>
public class QueryResponseMessage : Message
{
    public override MessageType Type => MessageType.QueryResponse;

    /// <summary>
    /// Danh sách các peer đang online
    /// </summary>
    public List<PeerInfo> Peers { get; set; } = new();

    /// <summary>
    /// Tổng số peer
    /// </summary>
    public int TotalPeers => Peers.Count;
}

