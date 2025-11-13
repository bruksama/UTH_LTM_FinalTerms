using P2PFileSharing.Common.Configuration;
using P2PFileSharing.Common.Infrastructure;

namespace P2PFileSharing.Client;

class Program
{
    static async Task Main(string[] args)
    {
        // Load configuration từ command-line arguments
        var config = ClientConfig.FromCommandLineArgs(args);

        // Nếu server address không được cung cấp qua command-line, hỏi người dùng
        if (config.ServerIpAddress == "127.0.0.1" && !args.Contains("--server") && !args.Contains("-s"))
        {
            config = PromptForServerAddress(config);
        }

        // Validate configuration
        if (!config.Validate(out var errorMessage))
        {
            Console.WriteLine($"Configuration error: {errorMessage}");
            Environment.Exit(1);
        }

        // Initialize logger
        using var logger = new FileLogger(config.LogFilePath, config.LogLevel);

        logger.LogInfo("=== P2P File Sharing Peer Client Starting ===");
        logger.LogInfo($"Server: {config.ServerIpAddress}:{config.ServerPort}");
        logger.LogInfo($"Listen Port: {config.ListenPort}");
        logger.LogInfo($"Username: {config.Username}");
        logger.LogInfo($"Shared Directory: {config.SharedDirectory}");
        
        // Hiển thị thông tin kết nối trên console
        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════════");
        Console.WriteLine("  P2P File Sharing Client");
        Console.WriteLine($"  Đang kết nối đến Server: {config.ServerIpAddress}:{config.ServerPort}");
        Console.WriteLine($"  Username: {config.Username}");
        Console.WriteLine($"  Listen Port (P2P): {config.ListenPort}");
        Console.WriteLine("═══════════════════════════════════════════════════════");
        Console.WriteLine();

        try
        {
            // Create client
            var client = new PeerClient(config, logger);
            await client.StartAsync();

            // Kiểm tra xem client có start thành công không
            if (!client.IsRunning)
            {
                Console.WriteLine("Warning: Client may not have started properly. Check logs for details.");
            }

            // Run command loop
            var consoleUI = new ConsoleUI(client, logger);
            // Set ConsoleUI reference để FileTransferManager có thể tạm dừng command loop khi chờ file transfer input
            client.SetConsoleUI(consoleUI);
            await consoleUI.RunCommandLoopAsync();
        }
        catch (Exception ex)
        {
            logger.LogError("Fatal error occurred", ex);
            Console.WriteLine($"Fatal error: {ex.Message}");
            Console.WriteLine("Check log file for more details.");
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// Prompt người dùng nhập server address nếu chưa được cung cấp
    /// </summary>
    private static ClientConfig PromptForServerAddress(ClientConfig config)
    {
        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════════");
        Console.WriteLine("  Thiết lập kết nối Server");
        Console.WriteLine("═══════════════════════════════════════════════════════");
        Console.WriteLine();
        Console.WriteLine("Bạn chưa chỉ định địa chỉ Server.");
        Console.WriteLine("Vui lòng nhập địa chỉ IP và Port của Server.");
        Console.WriteLine();
        Console.WriteLine("Ví dụ:");
        Console.WriteLine("  - 192.168.1.100:5000  (IP và Port)");
        Console.WriteLine("  - 192.168.1.100       (chỉ IP, dùng port mặc định 5000)");
        Console.WriteLine("  - localhost            (kết nối local)");
        Console.WriteLine();

        while (true)
        {
            Console.Write("Server address [Enter để dùng localhost:5000]: ");
            var input = Console.ReadLine()?.Trim();

            if (string.IsNullOrWhiteSpace(input))
            {
                // Dùng localhost mặc định
                config.ServerIpAddress = "127.0.0.1";
                config.ServerPort = 5000;
                Console.WriteLine($"Đã đặt Server: {config.ServerIpAddress}:{config.ServerPort}");
                break;
            }

            // Parse input: có thể là "ip:port" hoặc chỉ "ip"
            var parts = input.Split(':');
            if (parts.Length == 1)
            {
                // Chỉ có IP
                if (System.Net.IPAddress.TryParse(parts[0], out _) || parts[0].ToLower() == "localhost")
                {
                    config.ServerIpAddress = parts[0].ToLower() == "localhost" ? "127.0.0.1" : parts[0];
                    config.ServerPort = 5000; // Dùng port mặc định
                    Console.WriteLine($"Đã đặt Server: {config.ServerIpAddress}:{config.ServerPort}");
                    break;
                }
                else
                {
                    Console.WriteLine("❌ Địa chỉ IP không hợp lệ. Vui lòng thử lại.");
                    continue;
                }
            }
            else if (parts.Length == 2)
            {
                // Có cả IP và Port
                if ((System.Net.IPAddress.TryParse(parts[0], out _) || parts[0].ToLower() == "localhost") &&
                    int.TryParse(parts[1], out var port) && port > 0 && port <= 65535)
                {
                    config.ServerIpAddress = parts[0].ToLower() == "localhost" ? "127.0.0.1" : parts[0];
                    config.ServerPort = port;
                    Console.WriteLine($"Đã đặt Server: {config.ServerIpAddress}:{config.ServerPort}");
                    break;
                }
                else
                {
                    Console.WriteLine("❌ Địa chỉ IP hoặc Port không hợp lệ. Vui lòng thử lại.");
                    Console.WriteLine("   Port phải là số từ 1 đến 65535.");
                    continue;
                }
            }
            else
            {
                Console.WriteLine("❌ Định dạng không hợp lệ. Vui lòng nhập theo dạng: IP:Port hoặc chỉ IP");
                continue;
            }
        }

        Console.WriteLine();
        return config;
    }
}
