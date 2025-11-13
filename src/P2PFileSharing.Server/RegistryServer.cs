using System.Net;
using System.Net.Sockets;
using P2PFileSharing.Common.Configuration;
using P2PFileSharing.Common.Infrastructure;

namespace P2PFileSharing.Server;

public class RegistryServer
{
    private readonly ServerConfig _config;
    private readonly ILogger _logger;
    private readonly PeerRegistry _peerRegistry;
    private TcpListener? _tcpListener;
    private bool _isRunning;

    public RegistryServer(ServerConfig config, ILogger logger)
    {
        _config = config;
        _logger = logger;
        _peerRegistry = new PeerRegistry(config, logger);
    }

    public async Task StartAsync()
    {
        try
        {
            _tcpListener = new TcpListener(IPAddress.Any, _config.Port);
            _tcpListener.Start();
            _isRunning = true;

            _logger.LogInfo($"Server listening on port {_config.Port}");

            // Start background task để cleanup peers timeout
            _ = Task.Run(async () => await CleanupTimeoutPeersAsync());

            // Accept connections
            while (_isRunning)
            {
                try
                {
                    var tcpClient = await _tcpListener.AcceptTcpClientAsync();
                    _ = Task.Run(async () => await HandleClientAsync(tcpClient));
                }
                catch (ObjectDisposedException)
                {
                    // Listener đã bị dispose, thoát loop
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error accepting client connection", ex);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to start server", ex);
            throw;
        }
    }

    private async Task HandleClientAsync(TcpClient tcpClient)
    {
        var clientEndPoint = tcpClient.Client.RemoteEndPoint?.ToString() ?? "Unknown";
        _logger.LogInfo($"Client connected: {clientEndPoint}");

        try
        {
            try
            {
                tcpClient.NoDelay = true;
                tcpClient.LingerState = new LingerOption(enable: false, seconds: 0);
            }
            catch { /* bỏ qua nếu platform không hỗ trợ */ }
            using (tcpClient)
            using (var stream = tcpClient.GetStream())
            {
                await MessageHandler.HandleMessagesAsync(stream, _peerRegistry, _logger);
                // TODO: Implement message handling using MessageHandler
                // await MessageHandler.HandleMessagesAsync(stream, _peerRegistry, _logger);
            }
        }
        catch (IOException ioex)
        {
        // Lỗi I/O do client đóng kết nối đột ngột là bình thường
            _logger.LogDebug($"Client I/O closed: {clientEndPoint}. {ioex.Message}");
        }
        catch (ObjectDisposedException)
        {
            _logger.LogDebug($"Client stream disposed: {clientEndPoint}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error handling client {clientEndPoint}", ex);
        }
        finally
        {
            _logger.LogDebug($"Client disconnected: {clientEndPoint}");
        }
    }

    /// <summary>
    /// Background task để cleanup peers timeout
    /// </summary>
    private async Task CleanupTimeoutPeersAsync()
    {
        while (_isRunning)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1)); // Check mỗi phút
                _peerRegistry.CleanupTimeoutPeers();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in cleanup task", ex);
            }
        }
    }

    /// <summary>
    /// Stop server
    /// </summary>
    public void Stop()
    {
        _isRunning = false;
        _tcpListener?.Stop();
        _logger.LogInfo("Server stopped");
    }
}

