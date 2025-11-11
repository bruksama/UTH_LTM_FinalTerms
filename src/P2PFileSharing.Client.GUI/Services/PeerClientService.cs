using P2PFileSharing.Common.Configuration;
using P2PFileSharing.Common.Infrastructure;
using P2PFileSharing.Common.Models;

namespace P2PFileSharing.Client.GUI.Services;

/// <summary>
/// Service để quản lý PeerClient instance và cung cấp interface cho ViewModel
/// TODO: Implement wrapper around PeerClient to expose functionality to ViewModels
/// </summary>
public class PeerClientService
{
    private readonly ClientConfig _config;
    private readonly ILogger _logger;
    // TODO: Add PeerClient instance when Client project is ready
    // private PeerClient? _peerClient;

    public PeerClientService(ClientConfig config, ILogger logger)
    {
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Khởi động PeerClient và đăng ký với Server
    /// TODO: Create PeerClient instance
    /// TODO: Call PeerClient.StartAsync()
    /// TODO: Return success/failure
    /// </summary>
    public async Task<bool> StartAsync(PeerInfo peerInfo)
    {
        // TODO: Create PeerClient instance
        // TODO: Call StartAsync()
        // TODO: Return result
        await Task.CompletedTask;
        return false;
    }

    /// <summary>
    /// Dừng PeerClient và hủy đăng ký
    /// TODO: Call PeerClient.StopAsync()
    /// </summary>
    public async Task StopAsync()
    {
        // TODO: Call PeerClient.StopAsync() if exists
        await Task.CompletedTask;
    }

    /// <summary>
    /// Query danh sách peer từ Server
    /// TODO: Call ServerCommunicator.QueryPeersAsync()
    /// </summary>
    public async Task<List<PeerInfo>> QueryPeersAsync(string? fileNameFilter = null)
    {
        // TODO: Call ServerCommunicator.QueryPeersAsync()
        await Task.CompletedTask;
        return new List<PeerInfo>();
    }

    /// <summary>
    /// Scan mạng LAN bằng UDP broadcast
    /// TODO: Call UdpDiscovery.ScanNetworkAsync()
    /// </summary>
    public async Task<List<PeerInfo>> ScanNetworkAsync()
    {
        // TODO: Call UdpDiscovery.ScanNetworkAsync()
        await Task.CompletedTask;
        return new List<PeerInfo>();
    }

    /// <summary>
    /// Gửi file đến peer
    /// TODO: Call FileTransferManager.SendFileAsync()
    /// TODO: Return transfer progress observable
    /// </summary>
    public async Task<bool> SendFileAsync(string peerIpAddress, int peerPort, string filePath)
    {
        // TODO: Call FileTransferManager.SendFileAsync()
        await Task.CompletedTask;
        return false;
    }

    /// <summary>
    /// Kiểm tra xem PeerClient có đang chạy không
    /// TODO: Check PeerClient.IsRunning
    /// </summary>
    public bool IsRunning
    {
        get
        {
            // TODO: Return _peerClient?.IsRunning ?? false
            return false;
        }
    }
}

