using P2PFileSharing.Common.Protocol;

namespace P2PFileSharing.Common.Configuration;

/// <summary>
/// Configuration cho Client
/// </summary>
public class ClientConfig
{
    /// <summary>
    /// Địa chỉ IP của Registry Server
    /// </summary>
    public string ServerIpAddress { get; set; } = "127.0.0.1";

    /// <summary>
    /// Port của Registry Server
    /// </summary>
    public int ServerPort { get; set; } = ProtocolConstants.DefaultServerPort;

    /// <summary>
    /// Port để lắng nghe kết nối P2P từ các peer khác
    /// </summary>
    public int ListenPort { get; set; } = ProtocolConstants.DefaultClientListenPort;

    /// <summary>
    /// Port cho UDP Broadcast discovery
    /// </summary>
    public int DiscoveryPort { get; set; } = ProtocolConstants.DefaultDiscoveryPort;

    /// <summary>
    /// Tên người dùng của peer này
    /// </summary>
    public string Username { get; set; } = Environment.UserName;

    /// <summary>
    /// Thư mục chứa các file chia sẻ
    /// </summary>
    public string SharedDirectory { get; set; } = Path.Combine(Environment.CurrentDirectory, "shared");

    /// <summary>
    /// Thư mục lưu file nhận được từ peers khác
    /// </summary>
    public string DownloadDirectory { get; set; } = Path.Combine(Environment.CurrentDirectory, "downloads");

    /// <summary>
    /// Đường dẫn đến file log
    /// </summary>
    public string LogFilePath { get; set; } = "client.log";

    /// <summary>
    /// Log level tối thiểu
    /// </summary>
    public Infrastructure.LogLevel LogLevel { get; set; } = Infrastructure.LogLevel.Info;

    /// <summary>
    /// Interval để gửi heartbeat đến Server (seconds)
    /// </summary>
    public int HeartbeatIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Buffer size cho file transfer (bytes)
    /// </summary>
    public int BufferSize { get; set; } = ProtocolConstants.DefaultBufferSize;

    /// <summary>
    /// Validate configuration
    /// </summary>
    public bool Validate(out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(ServerIpAddress))
        {
            errorMessage = "ServerIpAddress cannot be empty";
            return false;
        }

        // Validate IP address format
        if (!System.Net.IPAddress.TryParse(ServerIpAddress, out _))
        {
            errorMessage = $"Invalid IP address format: {ServerIpAddress}";
            return false;
        }

        if (ServerPort < 1 || ServerPort > 65535)
        {
            errorMessage = "ServerPort must be between 1 and 65535";
            return false;
        }

        if (ListenPort < 1 || ListenPort > 65535)
        {
            errorMessage = "ListenPort must be between 1 and 65535";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Username))
        {
            errorMessage = "Username cannot be empty";
            return false;
        }

        if (HeartbeatIntervalSeconds < 10)
        {
            errorMessage = "HeartbeatIntervalSeconds must be at least 10";
            return false;
        }

        if (BufferSize < 1024)
        {
            errorMessage = "BufferSize must be at least 1024 bytes";
            return false;
        }

        errorMessage = null;
        return true;
    }

    /// <summary>
    /// Load configuration từ command-line arguments
    /// </summary>
    public static ClientConfig FromCommandLineArgs(string[] args)
    {
        var config = new ClientConfig();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLower())
            {
                case "--server" or "-s":
                    if (i + 1 < args.Length)
                    {
                        var parts = args[i + 1].Split(':');
                        config.ServerIpAddress = parts[0];
                        if (parts.Length > 1 && int.TryParse(parts[1], out var port))
                        {
                            config.ServerPort = port;
                        }
                        i++;
                    }
                    break;

                case "--port" or "-p":
                    if (i + 1 < args.Length && int.TryParse(args[i + 1], out var listenPort))
                    {
                        config.ListenPort = listenPort;
                        i++;
                    }
                    break;

                case "--username" or "-u":
                    if (i + 1 < args.Length)
                    {
                        config.Username = args[i + 1];
                        i++;
                    }
                    break;

                case "--shared" or "-d":
                    if (i + 1 < args.Length)
                    {
                        config.SharedDirectory = args[i + 1];
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
        Console.WriteLine("P2P File Sharing - Peer Client");
        Console.WriteLine("Usage: P2PFileSharing.Client [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -s, --server <ip[:port]>    Registry server address (default: 127.0.0.1:5000)");
        Console.WriteLine("  -p, --port <port>           Port to listen for P2P connections (default: 5001)");
        Console.WriteLine("  -u, --username <name>         Username (default: current user)");
        Console.WriteLine("  -d, --shared <directory>     Shared directory path (default: ./shared)");
        Console.WriteLine("  -l, --log <file>             Log file path (default: client.log)");
        Console.WriteLine("  -h, --help                   Show this help message");
    }
}

