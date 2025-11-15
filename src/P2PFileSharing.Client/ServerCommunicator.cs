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
            var timeoutMs = (int)P2PFileSharing.Common.Protocol.ProtocolConstants.ConnectionTimeout.TotalMilliseconds;
            var cts = new CancellationTokenSource(timeoutMs);
            
            _logger.LogInfo($"Connecting to server {_config.ServerIpAddress}:{_config.ServerPort}...");
            
            var connectTask = client.ConnectAsync(_config.ServerIpAddress, _config.ServerPort);
            var timeoutTask = Task.Delay(timeoutMs, cts.Token);
            var completed = await Task.WhenAny(connectTask, timeoutTask);
            
            if (completed == timeoutTask || !client.Connected)
            {
                var errorMsg = completed == timeoutTask
                    ? $"Connection timeout after {timeoutMs}ms"
                    : "Connection failed";
                _logger.LogInfo($"Register: {errorMsg} - Cannot connect to server {_config.ServerIpAddress}:{_config.ServerPort}");
                _logger.LogInfo($"  Hãy kiểm tra:");
                _logger.LogInfo($"  1. Server đã được khởi động chưa?");
                _logger.LogInfo($"  2. IP address và port có đúng không?");
                _logger.LogInfo($"  3. Firewall có chặn kết nối không?");
                _logger.LogInfo($"  4. Cả hai máy có cùng mạng LAN không?");
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
        try
        {
            using var client = new System.Net.Sockets.TcpClient();

            var timeoutMs = (int)P2PFileSharing.Common.Protocol.ProtocolConstants.ConnectionTimeout.TotalMilliseconds;
            var cts = new CancellationTokenSource(timeoutMs);

            _logger.LogInfo($"Connecting to server {_config.ServerIpAddress}:{_config.ServerPort} to query peers...");

            var connectTask = client.ConnectAsync(_config.ServerIpAddress, _config.ServerPort);
            var timeoutTask = Task.Delay(timeoutMs, cts.Token);
            var completed = await Task.WhenAny(connectTask, timeoutTask);

            if (completed == timeoutTask || !client.Connected)
            {
                var errorMsg = completed == timeoutTask
                    ? $"Connection timeout after {timeoutMs}ms"
                    : "Connection failed";
                _logger.LogInfo($"QueryPeers: {errorMsg} - Cannot connect to server {_config.ServerIpAddress}:{_config.ServerPort}");
                _logger.LogInfo($"  Hãy kiểm tra:");
                _logger.LogInfo($"  1. Server đã được khởi động chưa?");
                _logger.LogInfo($"  2. IP address và port có đúng không?");
                _logger.LogInfo($"  3. Firewall có chặn kết nối không?");
                _logger.LogInfo($"  4. Cả hai máy có cùng mạng LAN không?");
                return new List<PeerInfo>();
            }

            using var stream = client.GetStream();

            client.SendTimeout = (int)P2PFileSharing.Common.Protocol.ProtocolConstants.ReadWriteTimeout.TotalMilliseconds;
            client.ReceiveTimeout = (int)P2PFileSharing.Common.Protocol.ProtocolConstants.ReadWriteTimeout.TotalMilliseconds;

            var queryMessage = new QueryMessage
            {
                FileNameFilter = fileNameFilter
            };

            await P2PFileSharing.Common.Protocol.MessageSerializer.SendMessageAsync(stream, queryMessage, cts.Token);

            var response = await P2PFileSharing.Common.Protocol.MessageSerializer.ReceiveMessageAsync(stream, cts.Token);
            if (response == null)
            {
                _logger.LogInfo("QueryPeers: no response from server");
                return new List<PeerInfo>();
            }

            if (response is QueryResponseMessage qrm)
            {
                _logger.LogInfo($"QueryPeers: received {qrm.TotalPeers} peers" + (string.IsNullOrWhiteSpace(fileNameFilter) ? string.Empty : $" (filter='{fileNameFilter}')"));
                return qrm.Peers ?? new List<PeerInfo>();
            }

            _logger.LogInfo($"QueryPeers: unexpected response type: {response.GetType().Name}");
            return new List<PeerInfo>();
        }
        catch (OperationCanceledException)
        {
            _logger.LogInfo("QueryPeers: connection timed out");
            return new List<PeerInfo>();
        }
        catch (Exception ex)
        {
            _logger.LogError("QueryPeers failed", ex);
            return new List<PeerInfo>();
        }
    }

    /// <summary>
    /// Hủy đăng ký với Server (FR-09)
    /// </summary>
    public async Task<bool> DeregisterAsync(string peerId)
    {
        if (string.IsNullOrWhiteSpace(peerId))
            return false;

        try
        {
            using var client = new System.Net.Sockets.TcpClient();

            var timeoutMs = (int)P2PFileSharing.Common.Protocol.ProtocolConstants.ConnectionTimeout.TotalMilliseconds;
            var cts = new CancellationTokenSource(timeoutMs);

            _logger.LogInfo($"Connecting to server {_config.ServerIpAddress}:{_config.ServerPort} to deregister...");

            var connectTask = client.ConnectAsync(_config.ServerIpAddress, _config.ServerPort);
            var timeoutTask = Task.Delay(timeoutMs, cts.Token);
            var completed = await Task.WhenAny(connectTask, timeoutTask);

            if (completed == timeoutTask || !client.Connected)
            {
                var errorMsg = completed == timeoutTask
                    ? $"Connection timeout after {timeoutMs}ms"
                    : "Connection failed";
                _logger.LogInfo($"Deregister: {errorMsg} - Cannot connect to server {_config.ServerIpAddress}:{_config.ServerPort}");
                _logger.LogInfo($"  Hãy kiểm tra:");
                _logger.LogInfo($"  1. Server đã được khởi động chưa?");
                _logger.LogInfo($"  2. IP address và port có đúng không?");
                _logger.LogInfo($"  3. Firewall có chặn kết nối không?");
                _logger.LogInfo($"  4. Cả hai máy có cùng mạng LAN không?");
                return false;
            }

            using var stream = client.GetStream();

            client.SendTimeout = (int)P2PFileSharing.Common.Protocol.ProtocolConstants.ReadWriteTimeout.TotalMilliseconds;
            client.ReceiveTimeout = (int)P2PFileSharing.Common.Protocol.ProtocolConstants.ReadWriteTimeout.TotalMilliseconds;

            var deregisterMessage = new DeregisterMessage
            {
                PeerId = peerId
            };

            await P2PFileSharing.Common.Protocol.MessageSerializer.SendMessageAsync(stream, deregisterMessage, cts.Token);

            // Server doesn't send a response for deregistration; consider success if send succeeded
            _logger.LogInfo($"Deregister: sent request for PeerId={peerId}");
            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInfo("Deregister: connection timed out");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError("Deregister failed", ex);
            return false;
        }
    }

    /// <summary>
    /// Gửi heartbeat đến Server
    /// </summary>
    public async Task SendHeartbeatAsync(string peerId)
    {
        if (string.IsNullOrWhiteSpace(peerId))
        {
            return;
        }

        try
        {
            using var client = new System.Net.Sockets.TcpClient();

            var timeoutMs = (int)P2PFileSharing.Common.Protocol.ProtocolConstants.ConnectionTimeout.TotalMilliseconds;
            var cts = new CancellationTokenSource(timeoutMs);

            _logger.LogDebug($"Connecting to server {_config.ServerIpAddress}:{_config.ServerPort} to send heartbeat...");

            var connectTask = client.ConnectAsync(_config.ServerIpAddress, _config.ServerPort);
            var timeoutTask = Task.Delay(timeoutMs, cts.Token);
            var completed = await Task.WhenAny(connectTask, timeoutTask);

            if (completed == timeoutTask || !client.Connected)
            {
                var errorMsg = completed == timeoutTask
                    ? $"Connection timeout after {timeoutMs}ms"
                    : "Connection failed";
                _logger.LogDebug($"Heartbeat: {errorMsg} - Cannot connect to server {_config.ServerIpAddress}:{_config.ServerPort}");
                return;
            }

            using var stream = client.GetStream();

            client.SendTimeout = (int)P2PFileSharing.Common.Protocol.ProtocolConstants.ReadWriteTimeout.TotalMilliseconds;
            client.ReceiveTimeout = (int)P2PFileSharing.Common.Protocol.ProtocolConstants.ReadWriteTimeout.TotalMilliseconds;

            var hb = new HeartbeatMessage
            {
                PeerId = peerId
            };

            await P2PFileSharing.Common.Protocol.MessageSerializer.SendMessageAsync(stream, hb, cts.Token);

            _logger.LogDebug($"Heartbeat sent for PeerId={peerId}");
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Heartbeat: connection timed out");
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"Heartbeat failed: {ex.Message}");
        }
    }
}
