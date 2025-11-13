using P2PFileSharing.Common.Infrastructure;

namespace P2PFileSharing.Client;

/// <summary>
/// Console UI để hiển thị và xử lý commands (FR-06)
/// </summary>
public class ConsoleUI
{
    private readonly PeerClient _client;
    private readonly ILogger _logger;
    private bool _isWaitingForFileTransferInput;

    public ConsoleUI(PeerClient client, ILogger logger)
    {
        _client = client;
        _logger = logger;
        _isWaitingForFileTransferInput = false;
    }

    /// <summary>
    /// Kiểm tra xem có đang chờ input từ file transfer prompt không
    /// </summary>
    public bool IsWaitingForFileTransferInput => _isWaitingForFileTransferInput;

    /// <summary>
    /// Set flag để tạm dừng command loop khi đang chờ file transfer input
    /// </summary>
    public void SetWaitingForFileTransferInput(bool waiting)
    {
        _isWaitingForFileTransferInput = waiting;
    }

    /// <summary>
    /// Vòng lặp chính đọc lệnh từ console
    /// </summary>
    public async Task RunCommandLoopAsync()
    {
        PrintWelcomeMessage();
        PrintHelp();

        while (_client.IsRunning)
        {
            try
            {
                // Tạm dừng nếu đang chờ file transfer input
                if (_isWaitingForFileTransferInput)
                {
                    await Task.Delay(100); // Sleep ngắn để tránh busy wait
                    continue;
                }

                Console.Write("> ");
                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                    continue;

                var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var command = parts[0].ToLowerInvariant();

                switch (command)
                {
                    case "list":
                        await HandleListCommandAsync();
                        break;

                    case "scan":
                        await HandleScanCommandAsync();
                        break;

                    case "send":
                        if (parts.Length >= 3)
                            await HandleSendCommandAsync(parts[1], parts[2]);
                        else
                            Console.WriteLine("Usage: send <peer_name> <file_path>");
                        break;

                    case "username":
                    case "setname":
                        if (parts.Length >= 2)
                            await HandleUsernameCommandAsync(parts[1]);
                        else
                            Console.WriteLine("Usage: username <new_username>");
                        break;

                    case "help":
                    case "?":
                        PrintHelp();
                        break;

                    case "quit":
                    case "exit":
                        await _client.StopAsync();
                        return;

                    default:
                        Console.WriteLine($"Unknown command: {command}. Type 'help' for available commands.");
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in command loop", ex);
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }

    private void PrintWelcomeMessage()
    {
        Console.WriteLine("=== P2P File Sharing Client ===");
        Console.WriteLine();
    }

    private void PrintHelp()
    {
        Console.WriteLine("Available commands:");
        Console.WriteLine("  list                    - List all online peers and their shared files");
        Console.WriteLine("  scan                    - Scan LAN using UDP broadcast");
        Console.WriteLine("  send <peer> <file>      - Send file to peer");
        Console.WriteLine("  username <name>         - Change peer username");
        Console.WriteLine("  help                    - Show this help message");
        Console.WriteLine("  quit/exit               - Exit application");
        Console.WriteLine();
    }

    private async Task HandleListCommandAsync()
    {
        Console.WriteLine("Querying peers from registry server...");
        var peers = await _client.QueryPeersAsync();

        if (peers == null || peers.Count == 0)
        {
            Console.WriteLine("No peers found on registry server.");
            Console.WriteLine("  Tip: Use 'scan' command to discover peers via UDP broadcast.");
            return;
        }

        Console.WriteLine($"Found {peers.Count} peer(s) on registry server:");
        Console.WriteLine();
        
        int i = 1;
        foreach (var peer in peers)
        {
            Console.WriteLine($"{i++}. {peer.Username} ({peer.IpAddress}:{peer.ListenPort})");
            if (peer.SharedFiles?.Count > 0)
            {
                foreach (var f in peer.SharedFiles)
                {
                    var sizeStr = FormatFileSize(f.FileSize);
                    Console.WriteLine($"     - {f.FileName} ({sizeStr})");
                }
            }
            else
            {
                Console.WriteLine("     (no shared files)");
            }
            Console.WriteLine();
        }

        _logger.LogInfo($"List command completed: {peers.Count} peer(s) displayed.");
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

    private async Task HandleScanCommandAsync()
    {
        Console.WriteLine("Scanning LAN for peers...");
        var peers = await _client.ScanLanAsync();  // Sử dụng phương thức mới thêm vào PeerClient

        if (peers == null || peers.Count == 0)
        {
            Console.WriteLine("No peers found.");
            return;
        }

        Console.WriteLine($"Found {peers.Count} peer(s):");
        int i = 1;
        foreach (var peer in peers)
        {
            Console.WriteLine($"{i++}. {peer.Username}  {peer.IpAddress}:{peer.ListenPort}");
            if (peer.SharedFiles?.Count > 0)
            {
                foreach (var f in peer.SharedFiles)
                    Console.WriteLine($"     - {f.FileName} ({f.FileSize} bytes)");
            }
        }

        _logger.LogInfo($"Scan completed: {peers.Count} peer(s) discovered.");
    }

    private async Task HandleSendCommandAsync(string peerName, string filePath)
    {
        // Validate file path
        if (string.IsNullOrWhiteSpace(filePath))
        {
            Console.WriteLine("Error: File path cannot be empty.");
            return;
        }

        // Resolve full path
        var fullPath = Path.GetFullPath(filePath);
        
        if (!File.Exists(fullPath))
        {
            Console.WriteLine($"Error: File not found: {fullPath}");
            return;
        }

        Console.WriteLine($"Sending file to peer '{peerName}'...");
        Console.WriteLine($"  File: {Path.GetFileName(fullPath)}");
        Console.WriteLine($"  Path: {fullPath}");
        Console.WriteLine();

        var success = await _client.SendFileAsync(peerName, fullPath);

        if (success)
        {
            Console.WriteLine($"File sent successfully to {peerName}!");
        }
        else
        {
            Console.WriteLine($"Failed to send file to {peerName}.");
            Console.WriteLine("  Check logs for more details.");
        }
    }

    private async Task HandleUsernameCommandAsync(string newUsername)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(newUsername))
        {
            Console.WriteLine("Error: Username cannot be empty.");
            return;
        }

        // Trim whitespace
        newUsername = newUsername.Trim();

        Console.WriteLine($"Changing username to '{newUsername}'...");
        
        var success = await _client.ChangeUsernameAsync(newUsername);
        
        if (success)
        {
            Console.WriteLine($"✓ Username changed successfully to '{newUsername}'");
            if (_client.IsRunning)
            {
                Console.WriteLine("  Note: Peer has been re-registered with the server using the new username.");
            }
        }
        else
        {
            Console.WriteLine($"✗ Failed to change username to '{newUsername}'");
            Console.WriteLine("  Check logs for more details.");
        }
    }
}
