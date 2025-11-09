using P2PFileSharing.Common.Configuration;
using P2PFileSharing.Common.Infrastructure;

namespace P2PFileSharing.Client;

class Program
{
    static async Task Main(string[] args)
    {
        // Load configuration từ command-line arguments
        var config = ClientConfig.FromCommandLineArgs(args);

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

        try
        {
            // Create client
            var client = new PeerClient(config, logger);
            await client.StartAsync();

            // Run command loop
            var consoleUI = new ConsoleUI(client, logger);
            await consoleUI.RunCommandLoopAsync();
        }
        catch (Exception ex)
        {
            logger.LogError("Fatal error occurred", ex);
            Environment.Exit(1);
        }
    }
}
