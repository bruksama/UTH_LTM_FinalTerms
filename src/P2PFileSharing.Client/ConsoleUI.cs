using P2PFileSharing.Common.Infrastructure;

namespace P2PFileSharing.Client;

/// <summary>
/// Console UI để hiển thị và xử lý commands (FR-06)
/// TODO: Implement command parser và UI
/// </summary>
public class ConsoleUI
{
    private readonly PeerClient _client;
    private readonly ILogger _logger;

    public ConsoleUI(PeerClient client, ILogger logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <summary>
    /// Run command loop
    /// </summary>
    public async Task RunCommandLoopAsync()
    {
        PrintWelcomeMessage();
        PrintHelp();

        while (_client.IsRunning)
        {
            try
            {
                Console.Write("> ");
                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                    continue;

                var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var command = parts[0].ToLower();

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
                            Console.WriteLine("Usage: send <peer_name> <file_name>");
                        break;

                    case "help" or "?":
                        PrintHelp();
                        break;

                    case "quit" or "exit":
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
        Console.WriteLine("  help                    - Show this help message");
        Console.WriteLine("  quit/exit               - Exit application");
        Console.WriteLine();
    }

    private async Task HandleListCommandAsync()
    {
        // TODO: Query peers from server và display
        Console.WriteLine("TODO: List peers");
    }

    private async Task HandleScanCommandAsync()
    {
        // TODO: Scan network using UDP
        Console.WriteLine("TODO: Scan network");
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

        _logger.LogInfo($"Scan completed: {peers.Count} peers discovered.");
    }

    private async Task HandleSendCommandAsync(string peerName, string fileName)
    {
        // TODO: Send file to peer
        Console.WriteLine($"TODO: Send {fileName} to {peerName}");
    }

}

