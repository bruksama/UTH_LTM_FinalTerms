using System.Net.Sockets;
using P2PFileSharing.Common.Infrastructure;
using P2PFileSharing.Common.Models.Messages;
using P2PFileSharing.Common.Protocol;

namespace P2PFileSharing.Server;

/// <summary>
/// Xử lý các message từ clients
/// TODO: Implement message parsing và handling logic
/// </summary>
public static class MessageHandler
{
    /// <summary>
    /// Handle messages từ một client connection
    /// </summary>
    public static async Task HandleMessagesAsync(NetworkStream stream, PeerRegistry peerRegistry, ILogger logger)
    {
        try
        {
            while (stream.CanRead)
            {
                // Receive message
                var message = await MessageSerializer.ReceiveMessageAsync(stream);
                if (message == null)
                    break;

                logger.LogDebug($"Received message: {message.Type}");

                // Handle message based on type
                Message? response = message.Type switch
                {
                    MessageType.Register => await HandleRegisterAsync((RegisterMessage)message, peerRegistry, logger),
                    MessageType.QueryPeers => await HandleQueryAsync((QueryMessage)message, peerRegistry, logger),
                    MessageType.Deregister => await HandleDeregisterAsync((DeregisterMessage)message, peerRegistry, logger),
                    MessageType.Heartbeat => await HandleHeartbeatAsync((HeartbeatMessage)message, peerRegistry, logger),
                    _ => null
                };

                // Send response if any
                if (response != null)
                {
                    await MessageSerializer.SendMessageAsync(stream, response);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError("Error handling messages", ex);
        }
    }

    private static async Task<Message?> HandleRegisterAsync(RegisterMessage message, PeerRegistry peerRegistry, ILogger logger)
    {
        // TODO: Implement registration logic
        logger.LogInfo($"TODO: Handle Register message from {message.PeerInfo.Username}");
        return new RegisterAckMessage { PeerId = message.PeerInfo.PeerId };
    }

    private static async Task<Message?> HandleQueryAsync(QueryMessage message, PeerRegistry peerRegistry, ILogger logger)
    {
        // TODO: Implement query logic
        logger.LogInfo($"TODO: Handle Query message (filter: {message.FileNameFilter ?? "none"})");
        return new QueryResponseMessage { Peers = peerRegistry.GetAllPeers() };
    }

    private static async Task<Message?> HandleDeregisterAsync(DeregisterMessage message, PeerRegistry peerRegistry, ILogger logger)
    {
        // TODO: Implement deregistration logic
        logger.LogInfo($"TODO: Handle Deregister message for peer {message.PeerId}");
        return null;
    }

    private static async Task<Message?> HandleHeartbeatAsync(HeartbeatMessage message, PeerRegistry peerRegistry, ILogger logger)
    {
        // TODO: Update heartbeat
        peerRegistry.UpdateHeartbeat(message.PeerId);
        return null;
    }
}

