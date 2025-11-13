using P2PFileSharing.Common.Configuration;
using P2PFileSharing.Common.Infrastructure;

namespace P2PFileSharing.Client;

/// <summary>
/// Quản lý P2P file transfer (FR-04, FR-05)
/// TODO: Implement file send và receive logic
/// </summary>
public class FileTransferManager
{
    private readonly ClientConfig _config;
    private readonly ILogger _logger;

    public FileTransferManager(ClientConfig config, ILogger logger)
    {
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Gửi file đến một peer (FR-04)
    /// </summary>
    public async Task<bool> SendFileAsync(string peerIpAddress, int peerPort, string filePath)
    {
        // TODO: Implement file sending
        // 1. Connect to peer via TCP
        // 2. Send FileTransferRequestMessage
        // 3. Receive FileTransferResponseMessage
        // 4. If accepted, send file data
        // 5. Verify checksum
        _logger.LogInfo($"TODO: Send file {filePath} to {peerIpAddress}:{peerPort}");
        await Task.CompletedTask;
        return false;
    }

    /// <summary>
    /// Start listener để nhận file từ peers khác (FR-05)
    /// </summary>
    public void StartReceiver()
    {
        // TODO: Implement TCP listener for incoming file transfers
        _logger.LogInfo($"TODO: Start file receiver on port {_config.ListenPort}");
    }

    /// <summary>
    /// Stop receiver
    /// </summary>
    public void StopReceiver()
    {
        // TODO: Stop listener
        _logger.LogInfo("TODO: Stop file receiver");
    }
}

