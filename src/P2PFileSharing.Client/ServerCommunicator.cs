using P2PFileSharing.Common.Configuration;
using P2PFileSharing.Common.Infrastructure;
using P2PFileSharing.Common.Models;
using P2PFileSharing.Common.Models.Messages;

namespace P2PFileSharing.Client;

/// <summary>
/// Giao tiếp với Registry Server
/// TODO: Implement client-server communication (FR-01, FR-03, FR-09)
/// </summary>
public class ServerCommunicator
{
    private readonly ClientConfig _config;
    private readonly ILogger _logger;

    public ServerCommunicator(ClientConfig config, ILogger logger)
    {
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Đăng ký peer với Server (FR-01)
    /// </summary>
    public async Task<bool> RegisterAsync(PeerInfo peerInfo)
    {
        if (peerInfo == null)
            throw new ArgumentNullException(nameof(peerInfo));

        try
        {
            // Ensure peerInfo has reasonable values
            if (string.IsNullOrWhiteSpace(peerInfo.IpAddress))
            {
                peerInfo.IpAddress = P2PFileSharing.Common.Utilities.NetworkHelper.GetLocalIPAddress();
            }

            if (peerInfo.ListenPort == 0)
            {
                peerInfo.ListenPort = _config.ListenPort;
            }

            if (string.IsNullOrWhiteSpace(peerInfo.Username))
            {
                peerInfo.Username = _config.Username;
            }

            using var client = new System.Net.Sockets.TcpClient();

            // Connect with timeout
            var cts = new CancellationTokenSource((int)P2PFileSharing.Common.Protocol.ProtocolConstants.ConnectionTimeout.TotalMilliseconds);
            var connectTask = client.ConnectAsync(_config.ServerIpAddress, _config.ServerPort);
            var completed = await Task.WhenAny(connectTask, Task.Delay(Timeout.Infinite, cts.Token));
            if (completed != connectTask || !client.Connected)
            {
                _logger.LogInfo($"Register: cannot connect to server {_config.ServerIpAddress}:{_config.ServerPort}");
                return false;
            }

            using var stream = client.GetStream();

            // Optionally set send/receive timeouts
            client.SendTimeout = (int)P2PFileSharing.Common.Protocol.ProtocolConstants.ReadWriteTimeout.TotalMilliseconds;
            client.ReceiveTimeout = (int)P2PFileSharing.Common.Protocol.ProtocolConstants.ReadWriteTimeout.TotalMilliseconds;

            // Prepare and send RegisterMessage
            var registerMessage = new P2PFileSharing.Common.Models.Messages.RegisterMessage
            {
                PeerInfo = peerInfo
            };

            await P2PFileSharing.Common.Protocol.MessageSerializer.SendMessageAsync(stream, registerMessage, cts.Token);

            // Wait for response
            var response = await P2PFileSharing.Common.Protocol.MessageSerializer.ReceiveMessageAsync(stream, cts.Token);
            if (response == null)
            {
                _logger.LogInfo("Register: no response from server");
                return false;
            }

            if (response is P2PFileSharing.Common.Models.Messages.RegisterAckMessage ack)
            {
                // Update peerId from server ack (server may echo the PeerId)
                if (!string.IsNullOrWhiteSpace(ack.PeerId))
                {
                    peerInfo.PeerId = ack.PeerId;
                }

                _logger.LogInfo($"Register: success, PeerId={peerInfo.PeerId}");
                return true;
            }

            _logger.LogInfo($"Register: unexpected response type: {response.GetType().Name}");
            return false;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInfo("Register: connection timed out");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError("Register failed", ex);
            return false;
        }
    }

    /// <summary>
    /// Query danh sách peers từ Server (FR-03)
    /// </summary>
    public async Task<List<PeerInfo>> QueryPeersAsync(string? fileNameFilter = null)
    {
        // TODO: Implement query
        // 1. Connect to server
        // 2. Send QueryMessage
        // 3. Receive QueryResponseMessage
        // 4. Return list of peers
        _logger.LogInfo($"TODO: Query peers from server (filter: {fileNameFilter ?? "none"})");
        return new List<PeerInfo>();
    }

    /// <summary>
    /// Hủy đăng ký với Server (FR-09)
    /// </summary>
    public async Task<bool> DeregisterAsync(string peerId)
    {
        // TODO: Implement deregistration
        _logger.LogInfo($"TODO: Deregister peer {peerId} from server");
        return false;
    }

    /// <summary>
    /// Gửi heartbeat đến Server
    /// </summary>
    public async Task SendHeartbeatAsync(string peerId)
    {
        // TODO: Implement heartbeat
        _logger.LogDebug($"TODO: Send heartbeat for peer {peerId}");
    }
}

