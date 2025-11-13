using System.Net;
using System.Net.Sockets;
using P2PFileSharing.Common.Configuration;
using P2PFileSharing.Common.Infrastructure;
using P2PFileSharing.Common.Models.Messages;
using P2PFileSharing.Common.Protocol;
using P2PFileSharing.Common.Utilities;
namespace P2PFileSharing.Client;

/// <summary>
/// Quản lý P2P file transfer (FR-04, FR-05)
/// TODO: Implement file send và receive logic
/// </summary>
public class FileTransferManager
{
    private readonly ClientConfig _config;
    private readonly ILogger _logger;

    // Fields để quản lý receiver
    private TcpListener? _tcpListener;
    private CancellationTokenSource? _receiverCts;
    private Task? _receiverTask;

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

        if (string.IsNullOrEmpty(filePath))
        {
            _logger.LogError("File path is empty");
            return false;
        }

        if (!File.Exists(filePath))
        {
            _logger.LogError("File not found");
            return false;
        }

        var fileInfo = new FileInfo(filePath);
        var fileName = fileInfo.Name;
        var fileSize = fileInfo.Length;

        _logger.LogInfo($"Preparing to send file: {fileName} ({fileSize} bytes) to {peerIpAddress}:{peerPort}");

        TcpClient? tcpClient = null;
        NetworkStream? stream = null;
        FileStream? fileStream = null;

        try {
            tcpClient = new TcpClient();
            tcpClient.SendTimeout = (int)ProtocolConstants.ReadWriteTimeout.TotalMilliseconds;
            tcpClient.ReceiveTimeout = (int)ProtocolConstants.ReadWriteTimeout.TotalMilliseconds;

            var connectTask = tcpClient.ConnectAsync(peerIpAddress, peerPort);
            var timeoutTask = Task.Delay(ProtocolConstants.ConnectionTimeout);
            var completedTask = await Task.WhenAny(connectTask, timeoutTask);

            if (completedTask == timeoutTask || !tcpClient.Connected) {
                _logger.LogError($"Connection timeout to {peerIpAddress}:{peerPort}");
                return false;
            }

            _logger.LogInfo($"Connected to peer {peerIpAddress}:{peerPort}");
            stream = tcpClient.GetStream();

            var requestMessage = new FileTransferRequestMessage
            {
                FileName = fileName
            };

            var cts = new CancellationTokenSource(ProtocolConstants.ReadWriteTimeout);
            await MessageSerializer.SendMessageAsync(stream, requestMessage, cts.Token);
            _logger.LogDebug($"Sent FileTransferRequestMessage for file: {fileName}");

            var response = await MessageSerializer.ReceiveMessageAsync(stream, cts.Token);

            if (response == null) {
                _logger.LogError($"No response from peer {peerIpAddress}:{peerPort}");
                return false;
            }

            if (response is not FileTransferResponseMessage responseMessage)
            {
                _logger.LogError($"Unexpected message type: {response.GetType().Name}");
                return false;
            }
            
            if (!responseMessage.Accepted)
            {
                _logger.LogError($"File transfer rejected: {responseMessage.ErrorMessage ?? "Unknown reason"}");
                return false;
            }

            _logger.LogInfo($"File transfer accepted. Expected file size: {responseMessage.FileSize} bytes");

            if (responseMessage.FileSize != fileSize)
            {
                _logger.LogWarning($"File size mismatch: local={fileSize}, expected={responseMessage.FileSize}");
            }

            _logger.LogInfo("Calculating file checksum...");
            var checksum = await ChecksumCalculator.CalculateSHA256Async(filePath);
            _logger.LogDebug($"File checksum: {checksum}");

            fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, _config.BufferSize, useAsync: true);
            
            var buffer = new byte[_config.BufferSize];
            long totalBytesSent = 0;
            var startTime = DateTime.UtcNow;

            _logger.LogInfo($"Starting file transfer: {fileName}");

            while (totalBytesSent < fileSize) {
                var bytesToRead = (int)Math.Min(buffer.Length, fileSize - totalBytesSent);
                var bytesRead = await fileStream.ReadAsync(buffer, 0, bytesToRead, cts.Token);

                if (bytesRead == 0) break;

                await stream.WriteAsync(buffer, 0, bytesRead, cts.Token);
                totalBytesSent += bytesRead;

                if (fileSize > 0) {
                    var progress = (double)totalBytesSent / fileSize * 100;
                    if (totalBytesSent % (fileSize / 10 + 1) == 0 || totalBytesSent == fileSize) {
                        var elapsed = DateTime.UtcNow - startTime;
                        var speed = totalBytesSent / elapsed.TotalSeconds / 1024 / 1024;
                        _logger.LogInfo($"Transferred {totalBytesSent} of {fileSize} bytes ({progress:F1}%). Speed: {speed:F2} MB/s");
                    }
                }
            }

            await stream.FlushAsync(cts.Token);

            if (totalBytesSent != fileSize) {
                _logger.LogError($"File transfer incomplete: sent {totalBytesSent}/{fileSize} bytes");
                return false;
            }

            if (!string.IsNullOrEmpty(responseMessage.Checksum)) {
                if (string.Equals(checksum, responseMessage.Checksum, StringComparison.OrdinalIgnoreCase)) {
                    _logger.LogInfo("Checksum verification: PASSED");
                } else {
                    _logger.LogWarning($"Checksum mismatch: local={checksum}, remote={responseMessage.Checksum}");
                }
            }

            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.LogError("File transfer timeout");
            return false;
        }
        catch (SocketException ex)
        {
            _logger.LogError($"Network error during file transfer: {ex.Message}", ex);
            return false;
        }
        catch (IOException ex)
        {
            _logger.LogError($"IO error during file transfer: {ex.Message}", ex);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Unexpected error during file transfer: {ex.Message}", ex);
            return false;
        }
        finally
        {
            fileStream?.Dispose();
            stream?.Dispose();
            tcpClient?.Close();
            tcpClient?.Dispose();
        }
    }

    /// <summary>
    /// Start listener để nhận file từ peers khác (FR-05)
    /// </summary>
    public void StartReceiver()
    {
        if (_tcpListener != null && _receiverTask != null && !_receiverTask.IsCompleted) {
            _logger.LogWarning("File receiver is already running");
            return;
        }

        try {
            if (!Directory.Exists(_config.SharedDirectory)) {
                Directory.CreateDirectory(_config.SharedDirectory);
                _logger.LogInfo($"Created shared directory: {_config.SharedDirectory}");
            }

            _tcpListener = new TcpListener(IPAddress.Any, _config.ListenPort);
            _tcpListener.Start();

            _receiverCts = new CancellationTokenSource();
            _receiverTask = Task.Run(() => ListenForConnectionsAsync(_receiverCts.Token), _receiverCts.Token);

            _logger.LogInfo($"File receiver started on port {_config.ListenPort}");
            _logger.LogInfo($"Shared directory: {_config.SharedDirectory}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to start file receiver: {ex.Message}", ex);
            throw;
        }
    }

    /// <summary>
    /// Main loop để lắng nghe các kết nối đến
    /// </summary>
    private async Task ListenForConnectionsAsync(CancellationToken cancellationToken) {
        if (_tcpListener == null) return;

        _logger.LogInfo("File receiver listener started, waiting for connections...");

        while (!cancellationToken.IsCancellationRequested) {
            try {
                var tcpClient = await _tcpListener.AcceptTcpClientAsync();
                _ = Task.Run(async () => 
                {
                    try
                    {
                        await HandleFileTransferAsync(tcpClient, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error handling file transfer: {ex.Message}", ex);
                    }
                }, cancellationToken);
            }
            catch (ObjectDisposedException)
            {
                // Listener đã bị dispose, thoát loop
                break;
            }
            catch (OperationCanceledException) {
                break;
            }
            catch (Exception ex) {
                if (!cancellationToken.IsCancellationRequested) {
                    _logger.LogError($"Error accepting connection: {ex.Message}", ex);
                }
            }
        }

        _logger.LogInfo("File receiver listener stopped");
    }

    /// <summary>
    /// Xử lý một kết nối đến từ peer
    /// </summary>
    private async Task HandleFileTransferAsync(TcpClient tcpClient, CancellationToken cancellationToken)
    {
        var remoteEndPoint = tcpClient.Client.RemoteEndPoint?.ToString() ?? "unknown";
        NetworkStream? stream = null;
        FileStream? fileStream = null;

        try
        {
            tcpClient.SendTimeout = (int)ProtocolConstants.ReadWriteTimeout.TotalMilliseconds;
            tcpClient.ReceiveTimeout = (int)ProtocolConstants.ReadWriteTimeout.TotalMilliseconds;
            
            stream = tcpClient.GetStream();
            _logger.LogInfo($"Incoming connection from {remoteEndPoint}");

            // 1. Nhận FileTransferRequestMessage
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(ProtocolConstants.ReadWriteTimeout);

            var request = await MessageSerializer.ReceiveMessageAsync(stream, cts.Token);
            
            if (request == null)
            {
                _logger.LogWarning($"No request message received from {remoteEndPoint}");
                return;
            }

            if (request is not FileTransferRequestMessage requestMessage)
            {
                _logger.LogWarning($"Unexpected message type from {remoteEndPoint}: {request.GetType().Name}");
                return;
            }

            var fileName = requestMessage.FileName;
            _logger.LogInfo($"File transfer request from {remoteEndPoint}: {fileName}");

            // 2. Tìm file trong shared directory
            var filePath = Path.Combine(_config.SharedDirectory, fileName);
            
            // Security: Chỉ cho phép file trong shared directory (prevent path traversal)
            filePath = Path.GetFullPath(filePath);
            var sharedDirFullPath = Path.GetFullPath(_config.SharedDirectory);
            
            if (!filePath.StartsWith(sharedDirFullPath, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning($"Security: Attempted path traversal from {remoteEndPoint} - {fileName}");
                var rejectResponse = new FileTransferResponseMessage
                {
                    Accepted = false,
                    FileName = fileName,
                    ErrorMessage = "Invalid file path"
                };
                await MessageSerializer.SendMessageAsync(stream, rejectResponse, cts.Token);
                return;
            }

            // 3. Kiểm tra file có tồn tại không
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"File not found: {fileName} (requested by {remoteEndPoint})");
                var rejectResponse = new FileTransferResponseMessage
                {
                    Accepted = false,
                    FileName = fileName,
                    ErrorMessage = $"File not found: {fileName}"
                };
                await MessageSerializer.SendMessageAsync(stream, rejectResponse, cts.Token);
                return;
            }

            // 4. Lấy thông tin file
            var fileInfo = new FileInfo(filePath);
            var fileSize = fileInfo.Length;

            // 5. Tính checksum của file
            _logger.LogInfo($"Calculating checksum for {fileName}...");
            var checksum = await ChecksumCalculator.CalculateSHA256Async(filePath);
            _logger.LogDebug($"File checksum: {checksum}");

            // 6. Gửi FileTransferResponseMessage (accepted)
            var acceptResponse = new FileTransferResponseMessage
            {
                Accepted = true,
                FileName = fileName,
                FileSize = fileSize,
                Checksum = checksum
            };

            await MessageSerializer.SendMessageAsync(stream, acceptResponse, cts.Token);
            _logger.LogInfo($"File transfer accepted: {fileName} ({fileSize} bytes) to {remoteEndPoint}");

            // 7. Gửi file data
            fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, _config.BufferSize, useAsync: true);
            
            var buffer = new byte[_config.BufferSize];
            long totalBytesSent = 0;
            var startTime = DateTime.UtcNow;

            _logger.LogInfo($"Starting to send file: {fileName} to {remoteEndPoint}");

            while (totalBytesSent < fileSize && !cts.Token.IsCancellationRequested)
            {
                var bytesToRead = (int)Math.Min(buffer.Length, fileSize - totalBytesSent);
                var bytesRead = await fileStream.ReadAsync(buffer, 0, bytesToRead, cts.Token);

                if (bytesRead == 0)
                    break;

                await stream.WriteAsync(buffer, 0, bytesRead, cts.Token);
                totalBytesSent += bytesRead;

                // Log progress mỗi 10%
                if (fileSize > 0)
                {
                    var progress = (double)totalBytesSent / fileSize * 100;
                    if (totalBytesSent % (fileSize / 10 + 1) == 0 || totalBytesSent == fileSize)
                    {
                        var elapsed = DateTime.UtcNow - startTime;
                        var speed = totalBytesSent / elapsed.TotalSeconds / (1024 * 1024); // MB/s
                        _logger.LogInfo($"Sending to {remoteEndPoint}: {progress:F1}% ({totalBytesSent}/{fileSize} bytes, {speed:F2} MB/s)");
                    }
                }
            }

            await stream.FlushAsync(cts.Token);

            if (totalBytesSent != fileSize)
            {
                _logger.LogError($"File transfer incomplete to {remoteEndPoint}: sent {totalBytesSent}/{fileSize} bytes");
            }
            else
            {
                var transferTime = DateTime.UtcNow - startTime;
                var throughput = fileSize / transferTime.TotalSeconds / (1024 * 1024); // MB/s
                _logger.LogInfo($"File transfer completed to {remoteEndPoint}: {fileName} ({fileSize} bytes) in {transferTime.TotalSeconds:F2}s ({throughput:F2} MB/s)");
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInfo($"File transfer cancelled for {remoteEndPoint}");
        }
        catch (SocketException ex)
        {
            _logger.LogError($"Network error during file transfer with {remoteEndPoint}: {ex.Message}", ex);
        }
        catch (IOException ex)
        {
            _logger.LogError($"IO error during file transfer with {remoteEndPoint}: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Unexpected error during file transfer with {remoteEndPoint}: {ex.Message}", ex);
        }
        finally
        {
            fileStream?.Dispose();
            stream?.Dispose();
            tcpClient?.Close();
            tcpClient?.Dispose();
            _logger.LogDebug($"Connection closed: {remoteEndPoint}");
        }
    }

    /// <summary>
    /// Stop receiver
    /// </summary>
    public void StopReceiver()
    {
        if (_tcpListener == null)
        {
            _logger.LogWarning("File receiver is not running");
            return;
        }

        try
        {
            _logger.LogInfo("Stopping file receiver...");

            // Cancel receiver task
            _receiverCts?.Cancel();

            // Stop TCP listener
            _tcpListener?.Stop();

            // Wait for receiver task to complete (with timeout)
            if (_receiverTask != null)
            {
                try
                {
                    _receiverTask.Wait(TimeSpan.FromSeconds(5));
                }
                catch (AggregateException)
                {
                    // Expected when task is cancelled
                }
            }

            // Cleanup
            _tcpListener = null;
            _receiverTask = null;
            _receiverCts?.Dispose();
            _receiverCts = null;

            _logger.LogInfo("File receiver stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error stopping file receiver: {ex.Message}", ex);
        }
    }
}

