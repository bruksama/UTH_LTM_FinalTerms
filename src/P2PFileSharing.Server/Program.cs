using P2PFileSharing.Common.Configuration;
using P2PFileSharing.Common.Infrastructure;
using P2PFileSharing.Common.Utilities;

namespace P2PFileSharing.Server;

class Program
{
    static async Task Main(string[] args)
    {
        // Load configuration từ command-line arguments
        var config = ServerConfig.FromCommandLineArgs(args);

        // Validate configuration
        if (!config.Validate(out var errorMessage))
        {
            Console.WriteLine($"Configuration error: {errorMessage}");
            Environment.Exit(1);
        }

        // Initialize logger
        using var logger = new FileLogger(config.LogFilePath, config.LogLevel);

        logger.LogInfo("=== P2P File Sharing Registry Server Starting ===");
        logger.LogInfo($"Port: {config.Port}");
        logger.LogInfo($"Peer Timeout: {config.PeerTimeout.TotalMinutes} minutes");
        logger.LogInfo($"Max Peers: {config.MaxPeers}");

        // Hiển thị IP addresses để admin biết cách kết nối
        try
        {
            var localIP = NetworkHelper.GetLocalIPAddress();
            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine("  Server đang lắng nghe trên:");
            Console.WriteLine($"  • Localhost:    127.0.0.1:{config.Port}");
            Console.WriteLine($"  • LAN IP:       {localIP}:{config.Port}");
            Console.WriteLine();
            Console.WriteLine("  Để các máy khác kết nối, sử dụng:");
            Console.WriteLine($"  dotnet run -- --server {localIP}:{config.Port}");
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine();
            
            logger.LogInfo($"Server listening on all interfaces (0.0.0.0:{config.Port})");
            logger.LogInfo($"Local IP address: {localIP}");
        }
        catch (Exception ex)
        {
            logger.LogWarning($"Could not determine local IP address: {ex.Message}");
        }

        try
        {
            // Create và start server
            var server = new RegistryServer(config, logger);
            await server.StartAsync();

            logger.LogInfo("Server started. Press Ctrl+C to stop.");

            // Wait for cancellation
            var cancellationTokenSource = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                cancellationTokenSource.Cancel();
            };

            await Task.Delay(Timeout.Infinite, cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            logger.LogError("Fatal error occurred", ex);
            Environment.Exit(1);
        }
    }
}
