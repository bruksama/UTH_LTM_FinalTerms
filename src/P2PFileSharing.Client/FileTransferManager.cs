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
    private int _actualListenPort;

    public FileTransferManager(ClientConfig config, ILogger logger)
    {
        _config = config;
        _logger = logger;
        _actualListenPort = config.ListenPort;
    }

    /// <summary>
    /// Lấy port thực tế đang được sử dụng để listen
    /// </summary>
    public int GetActualListenPort()
    {
        return _actualListenPort;
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

        // Tính checksum trước khi gửi
        _logger.LogInfo("Calculating file checksum...");
        var checksum = await ChecksumCalculator.CalculateSHA256Async(filePath);
        _logger.LogDebug($"File checksum: {checksum}");

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

            // Gửi FileTransferRequestMessage với thông tin file đầy đủ
            var requestMessage = new FileTransferRequestMessage
            {
                FileName = fileName,
                FileSize = fileSize,
                Checksum = checksum
            };

            var cts = new CancellationTokenSource(ProtocolConstants.ReadWriteTimeout);
            await MessageSerializer.SendMessageAsync(stream, requestMessage, cts.Token);
            _logger.LogDebug($"Sent FileTransferRequestMessage: {fileName} ({fileSize} bytes)");

            // Đợi phản hồi từ người nhận
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
                Console.WriteLine($"File transfer rejected by peer: {responseMessage.ErrorMessage ?? "Unknown reason"}");
                return false;
            }

            _logger.LogInfo($"File transfer accepted by peer. Starting to send file data...");
            Console.WriteLine($"Peer accepted file transfer. Sending {fileName}...");

            fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, _config.BufferSize, useAsync: true);
            
            var buffer = new byte[_config.BufferSize];
            long totalBytesSent = 0;
            var startTime = DateTime.UtcNow;

            // Gửi file data
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
                        Console.WriteLine($"  Progress: {progress:F1}% ({totalBytesSent}/{fileSize} bytes, {speed:F2} MB/s)");
                        _logger.LogInfo($"Transferred {totalBytesSent} of {fileSize} bytes ({progress:F1}%). Speed: {speed:F2} MB/s");
                    }
                }
            }

            await stream.FlushAsync(cts.Token);

            if (totalBytesSent != fileSize) {
                _logger.LogError($"File transfer incomplete: sent {totalBytesSent}/{fileSize} bytes");
                return false;
            }

            var transferTime = DateTime.UtcNow - startTime;
            var throughput = fileSize / transferTime.TotalSeconds / (1024 * 1024);
            Console.WriteLine($"File sent successfully! ({throughput:F2} MB/s)");
            _logger.LogInfo($"File transfer completed: {fileName} ({fileSize} bytes) in {transferTime.TotalSeconds:F2}s ({throughput:F2} MB/s)");

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

            // Thử bind port từ config
            int portToUse = _config.ListenPort;
            bool portAvailable = false;

            // Kiểm tra port có available không
            if (P2PFileSharing.Common.Utilities.NetworkHelper.IsPortAvailable(portToUse))
            {
                portAvailable = true;
            }
            else
            {
                // Port không available, tìm port khác
                _logger.LogWarning($"Port {portToUse} is already in use. Searching for available port...");
                try
                {
                    portToUse = P2PFileSharing.Common.Utilities.NetworkHelper.FindAvailablePort(
                        _config.ListenPort, 
                        _config.ListenPort + 100);
                    portAvailable = true;
                    _logger.LogInfo($"Found available port: {portToUse}");
                    // Cập nhật config và actual port với port mới
                    _config.ListenPort = portToUse;
                    _actualListenPort = portToUse;
                }
                catch (Exception portEx)
                {
                    _logger.LogError($"Could not find available port: {portEx.Message}");
                    portAvailable = false;
                }
            }

            if (!portAvailable)
            {
                _logger.LogError("Cannot start file receiver: no available port found");
                return; // Không throw exception, chỉ log và return
            }

            _tcpListener = new TcpListener(IPAddress.Any, portToUse);
            _tcpListener.Start();
            
            // Lưu port thực tế đang được sử dụng
            _actualListenPort = portToUse;

            _receiverCts = new CancellationTokenSource();
            _receiverTask = Task.Run(() => ListenForConnectionsAsync(_receiverCts.Token), _receiverCts.Token);

            _logger.LogInfo($"File receiver started on port {portToUse}");
            _logger.LogInfo($"Shared directory: {_config.SharedDirectory}");
        }
        catch (SocketException ex)
        {
            _logger.LogError($"Failed to start file receiver on port {_config.ListenPort}: {ex.Message}", ex);
            // Không throw exception, chỉ log và cleanup
            _tcpListener = null;
            _receiverTask = null;
            _receiverCts?.Dispose();
            _receiverCts = null;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to start file receiver: {ex.Message}", ex);
            // Không throw exception, chỉ log và cleanup
            _tcpListener = null;
            _receiverTask = null;
            _receiverCts?.Dispose();
            _receiverCts = null;
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
            var fileSize = requestMessage.FileSize;
            var checksum = requestMessage.Checksum;

            // Hiển thị thông tin file transfer request trên console
            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine("  Incoming File Transfer Request");
            Console.WriteLine($"  From: {remoteEndPoint}");
            Console.WriteLine($"  File: {fileName}");
            Console.WriteLine($"  Size: {FormatFileSize(fileSize)} ({fileSize} bytes)");
            if (!string.IsNullOrEmpty(checksum))
            {
                Console.WriteLine($"  Checksum: {checksum.Substring(0, Math.Min(16, checksum.Length))}...");
            }
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.Write("Accept this file transfer? (y/n): ");

            // Đọc user input (với timeout)
            bool accepted = false;
            string? userInput = null;
            
            var inputTask = Task.Run(() => Console.ReadLine());
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30)); // 30 giây timeout
            var completedTask = await Task.WhenAny(inputTask, timeoutTask);

            if (completedTask == inputTask)
            {
                userInput = await inputTask;
                accepted = userInput?.Trim().ToLowerInvariant() == "y" || 
                          userInput?.Trim().ToLowerInvariant() == "yes";
            }
            else
            {
                Console.WriteLine("\nTimeout - Transfer rejected (no response within 30 seconds)");
                accepted = false;
            }

            // Tạo download directory nếu chưa tồn tại
            if (!Directory.Exists(_config.DownloadDirectory))
            {
                Directory.CreateDirectory(_config.DownloadDirectory);
            }

            // Xác định đường dẫn file sẽ lưu
            var savePath = Path.Combine(_config.DownloadDirectory, fileName);
            
            // Nếu file đã tồn tại, thêm số vào tên
            int counter = 1;
            while (File.Exists(savePath))
            {
                var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                var ext = Path.GetExtension(fileName);
                savePath = Path.Combine(_config.DownloadDirectory, $"{nameWithoutExt}_{counter}{ext}");
                counter++;
            }

            // Gửi FileTransferResponseMessage
            FileTransferResponseMessage responseMessage;
            
            if (accepted)
            {
                responseMessage = new FileTransferResponseMessage
                {
                    Accepted = true,
                    FileName = fileName,
                    FileSize = fileSize,
                    Checksum = checksum
                };
                Console.WriteLine($"\n✓ File transfer accepted. Saving to: {savePath}");
                _logger.LogInfo($"File transfer accepted: {fileName} ({fileSize} bytes) from {remoteEndPoint}");
            }
            else
            {
                responseMessage = new FileTransferResponseMessage
                {
                    Accepted = false,
                    FileName = fileName,
                    ErrorMessage = userInput == null ? "Timeout" : "User rejected"
                };
                Console.WriteLine("\n✗ File transfer rejected.");
                _logger.LogInfo($"File transfer rejected: {fileName} from {remoteEndPoint}");
                await MessageSerializer.SendMessageAsync(stream, responseMessage, cts.Token);
                return;
            }

            await MessageSerializer.SendMessageAsync(stream, responseMessage, cts.Token);

            // Nhận file data và lưu vào disk
            fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None, _config.BufferSize, useAsync: true);
            
            var buffer = new byte[_config.BufferSize];
            long totalBytesReceived = 0;
            var startTime = DateTime.UtcNow;

            Console.WriteLine("Receiving file data...");

            while (totalBytesReceived < fileSize && !cts.Token.IsCancellationRequested)
            {
                var bytesToRead = (int)Math.Min(buffer.Length, fileSize - totalBytesReceived);
                var bytesRead = await stream.ReadAsync(buffer, 0, bytesToRead, cts.Token);

                if (bytesRead == 0)
                    break;

                await fileStream.WriteAsync(buffer, 0, bytesRead, cts.Token);
                totalBytesReceived += bytesRead;

                // Log progress mỗi 10%
                if (fileSize > 0)
                {
                    var progress = (double)totalBytesReceived / fileSize * 100;
                    if (totalBytesReceived % (fileSize / 10 + 1) == 0 || totalBytesReceived == fileSize)
                    {
                        var elapsed = DateTime.UtcNow - startTime;
                        var speed = totalBytesReceived / elapsed.TotalSeconds / (1024 * 1024);
                        Console.WriteLine($"  Progress: {progress:F1}% ({totalBytesReceived}/{fileSize} bytes, {speed:F2} MB/s)");
                    }
                }
            }

            await fileStream.FlushAsync(cts.Token);

            if (totalBytesReceived != fileSize)
            {
                Console.WriteLine($"\n✗ File transfer incomplete: received {totalBytesReceived}/{fileSize} bytes");
                _logger.LogError($"File transfer incomplete from {remoteEndPoint}: received {totalBytesReceived}/{fileSize} bytes");
                File.Delete(savePath); // Xóa file không hoàn chỉnh
            }
            else
            {
                // Verify checksum
                bool checksumValid = true;
                if (!string.IsNullOrEmpty(checksum))
                {
                    var receivedChecksum = await ChecksumCalculator.CalculateSHA256Async(savePath);
                    checksumValid = string.Equals(checksum, receivedChecksum, StringComparison.OrdinalIgnoreCase);
                    
                    if (checksumValid)
                    {
                        Console.WriteLine($"\n✓ File received successfully!");
                        Console.WriteLine($"✓ Checksum verification: PASSED");
                    }
                    else
                    {
                        Console.WriteLine($"\n✗ Checksum verification: FAILED");
                        Console.WriteLine($"  Expected: {checksum}");
                        Console.WriteLine($"  Received: {receivedChecksum}");
                    }
                }

                var transferTime = DateTime.UtcNow - startTime;
                var throughput = fileSize / transferTime.TotalSeconds / (1024 * 1024);
                Console.WriteLine($"  Saved to: {savePath}");
                Console.WriteLine($"  Transfer time: {transferTime.TotalSeconds:F2}s ({throughput:F2} MB/s)");
                Console.WriteLine("═══════════════════════════════════════════════════════");
                Console.WriteLine();
                
                _logger.LogInfo($"File transfer completed from {remoteEndPoint}: {fileName} ({fileSize} bytes) in {transferTime.TotalSeconds:F2}s ({throughput:F2} MB/s)");
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

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}

