using P2PFileSharing.Common.Configuration;
using P2PFileSharing.Common.Infrastructure;
using P2PFileSharing.Common.Models;

namespace P2PFileSharing.Client.GUI.Services;

/// <summary>
/// Service để quản lý PeerClient instance và cung cấp interface cho ViewModel
/// </summary>
public class PeerClientService
{
    private readonly ClientConfig _config;
    private readonly ILogger _logger;
    private PeerClient? _peerClient;

    public PeerClientService(ClientConfig config, ILogger logger)
    {
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Khởi động PeerClient và đăng ký với Server
    /// </summary>
    public async Task<bool> StartAsync(PeerInfo peerInfo)
    {
        try
        {
            if (_peerClient?.IsRunning == true)
            {
                return true;
            }

            // Đồng bộ lại cấu hình dựa trên peer info được cung cấp từ UI
            if (!string.IsNullOrWhiteSpace(peerInfo.Username))
            {
                _config.Username = peerInfo.Username;
            }

            if (peerInfo.ListenPort > 0)
            {
                _config.ListenPort = peerInfo.ListenPort;
            }

            _peerClient = EnsureClient();
            await _peerClient.StartAsync();

            return _peerClient.IsRunning;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to start PeerClient: {ex.Message}", ex);
            return false;
        }
        
    }

    /// <summary>
    /// Dừng PeerClient và hủy đăng ký
    /// </summary>
    public async Task StopAsync()
    {
        if (_peerClient != null)
        {
            await _peerClient.StopAsync();
        }
    }

    /// <summary>
    /// Query danh sách peer từ Server
    /// </summary>
    public async Task<List<PeerInfo>> QueryPeersAsync(string? fileNameFilter = null)
    {
        var client = EnsureClient();
        return await client.QueryPeersAsync(fileNameFilter);
    }

    /// <summary>
    /// Scan mạng LAN bằng UDP broadcast
    /// </summary>
    public async Task<List<PeerInfo>> ScanNetworkAsync()
    {
        var client = EnsureClient();
        return await client.ScanLanAsync();
    }

    /// <summary>
    /// Gửi file đến peer
    /// </summary>
    public async Task<bool> SendFileAsync(string peerIpAddress, int peerPort, string filePath)
    {
        var client = EnsureClient();
        return await client.SendFileAsync($"{peerIpAddress}:{peerPort}", filePath);
    }

    /// <summary>
    /// Kiểm tra xem PeerClient có đang chạy không
    /// </summary>
    public bool IsRunning
    {
        get
        {
            return _peerClient?.IsRunning ?? false;
        }
    }
    private PeerClient EnsureClient()
    {
        return _peerClient ??= new PeerClient(_config, _logger);
    }
}

