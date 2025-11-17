using System.Net.Sockets;
using P2PFileSharing.Common.Infrastructure;
using P2PFileSharing.Common.Models.Messages;
using P2PFileSharing.Common.Protocol;

namespace P2PFileSharing.Server;

/// <summary>
/// Xử lý các message từ clients
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
                Message? response = null;
                switch (message.Type)
                {
                    case MessageType.Register:
                        response = await HandleRegisterAsync((RegisterMessage)message, peerRegistry, logger);
                        break;

                    case MessageType.QueryPeers:
                        response = await HandleQueryAsync((QueryMessage)message, peerRegistry, logger);
                        break;

                    case MessageType.Deregister:
                        response = await HandleDeregisterAsync((DeregisterMessage)message, peerRegistry, logger);
                        break;

                    case MessageType.Heartbeat:
                        response = await HandleHeartbeatAsync((HeartbeatMessage)message, peerRegistry, logger);
                        break;

                    default:
                        logger.LogWarning($"Unsupported message type: {message.Type}");
                        break;
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
        await Task.Yield();

        var ok = peerRegistry.RegisterPeer(message.PeerInfo);

        if (ok)
        {
            logger.LogInfo(
                $"Register OK: {message.PeerInfo.Username} ({message.PeerInfo.IpAddress}:{message.PeerInfo.ListenPort})");
            return new RegisterAckMessage
            {
                PeerId = message.PeerInfo.PeerId
            };
        }
        else
        {
            logger.LogInfo($"Register FAIL: {message.PeerInfo.Username}");
            return new RegisterNackMessage
            {
                Reason = "Validation failed or server reached MaxPeers"
            };
        }
    }

    private static async Task<Message?> HandleQueryAsync(QueryMessage message, PeerRegistry peerRegistry, ILogger logger)
    {
        await Task.Yield();

        var peers = string.IsNullOrWhiteSpace(message.FileNameFilter)
            ? peerRegistry.GetAllPeers()
            : peerRegistry.GetPeersWithFile(message.FileNameFilter!);

        logger.LogInfo($"Query => {peers.Count} peers" +
                       (string.IsNullOrWhiteSpace(message.FileNameFilter) ? "" : $" (filter='{message.FileNameFilter}')"));

        return new QueryResponseMessage { Peers = peers };
    }

    private static async Task<Message?> HandleDeregisterAsync(DeregisterMessage message, PeerRegistry peerRegistry, ILogger logger)
    {
        await Task.Yield();

        // Registry key là Username; message mang PeerId → map PeerId -> Username
        var username = FindUsernameByPeerId(peerRegistry, message.PeerId);
        if (username == null)
        {
            logger.LogInfo($"Deregister: PeerId '{message.PeerId}' not found (online)");
            return null; // không cần response
        }

        var ok = peerRegistry.DeregisterPeer(username);
        logger.LogInfo(ok ? $"Deregister OK: {username}" : $"Deregister NOT FOUND: {username}");
        return null;
    }

    private static async Task<Message?> HandleHeartbeatAsync(HeartbeatMessage message, PeerRegistry peerRegistry, ILogger logger)
    {
        await Task.Yield();

        var username = FindUsernameByPeerId(peerRegistry, message.PeerId);
        if (username == null)
        {
            logger.LogDebug($"Heartbeat: PeerId '{message.PeerId}' not found (online)");
            return null;
        }

        peerRegistry.UpdateHeartbeat(username);
        logger.LogDebug($"Heartbeat OK: {username}");
        return null;
    }
     private static string? FindUsernameByPeerId(PeerRegistry registry, string peerId)
    {
        if (string.IsNullOrWhiteSpace(peerId)) return null;
        // tra trong peers đang online (GetAllPeers đã lọc TTL)
        var p = registry.GetAllPeers().FirstOrDefault(x => x.PeerId == peerId);
        return p?.Username;
    }
}

