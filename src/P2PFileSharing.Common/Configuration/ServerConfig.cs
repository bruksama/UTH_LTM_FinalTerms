using P2PFileSharing.Common.Protocol;

namespace P2PFileSharing.Common.Configuration;

/// <summary>
/// Configuration cho Server
/// </summary>
public class ServerConfig
{
    /// <summary>
    /// Port để lắng nghe kết nối từ clients
    /// </summary>
    public int Port { get; set; } = ProtocolConstants.DefaultServerPort;

    /// <summary>
    /// Timeout cho peer heartbeat (sau khoảng thời gian này không nhận heartbeat, peer sẽ bị xóa)
    /// </summary>
    public TimeSpan PeerTimeout { get; set; } = ProtocolConstants.DefaultPeerTimeout;

    /// <summary>
    /// Đường dẫn đến file log
    /// </summary>
    public string LogFilePath { get; set; } = "server.log";

    /// <summary>
    /// Log level tối thiểu
    /// </summary>
    public Infrastructure.LogLevel LogLevel { get; set; } = Infrastructure.LogLevel.Info;

    /// <summary>
    /// Số lượng peer tối đa có thể đăng ký đồng thời
    /// </summary>
    public int MaxPeers { get; set; } = 1000;

    /// <summary>
    /// Validate configuration
    /// </summary>
    public bool Validate(out string? errorMessage)
    {
        if (Port < 1 || Port > 65535)
        {
            errorMessage = "Port must be between 1 and 65535";
            return false;
        }

        if (PeerTimeout <= TimeSpan.Zero)
        {
            errorMessage = "PeerTimeout must be greater than zero";
            return false;
        }

        if (MaxPeers < 1)
        {
            errorMessage = "MaxPeers must be at least 1";
            return false;
        }

        errorMessage = null;
        return true;
    }

    /// <summary>
    /// Load configuration từ command-line arguments
    /// </summary>
    public static ServerConfig FromCommandLineArgs(string[] args)
    {
        var config = new ServerConfig();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLower())
            {
                case "--port" or "-p":
                    if (i + 1 < args.Length && int.TryParse(args[i + 1], out var port))
                    {
                        config.Port = port;
                        i++;
                    }
                    break;

                case "--log" or "-l":
                    if (i + 1 < args.Length)
                    {
                        config.LogFilePath = args[i + 1];
                        i++;
                    }
                    break;

                case "--timeout" or "-t":
                    if (i + 1 < args.Length && int.TryParse(args[i + 1], out var timeoutMinutes))
                    {
                        config.PeerTimeout = TimeSpan.FromMinutes(timeoutMinutes);
                        i++;
                    }
                    break;

                case "--help" or "-h":
                    PrintHelp();
                    Environment.Exit(0);
                    break;
            }
        }

        return config;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("P2P File Sharing - Registry Server");
        Console.WriteLine("Usage: P2PFileSharing.Server [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -p, --port <port>      Port to listen on (default: 5000)");
        Console.WriteLine("  -l, --log <file>       Log file path (default: server.log)");
        Console.WriteLine("  -t, --timeout <minutes> Peer timeout in minutes (default: 5)");
        Console.WriteLine("  -h, --help             Show this help message");
    }
}

