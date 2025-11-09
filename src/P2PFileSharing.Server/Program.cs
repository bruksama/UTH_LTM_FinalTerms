using P2PFileSharing.Common.Configuration;
using P2PFileSharing.Common.Infrastructure;

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
