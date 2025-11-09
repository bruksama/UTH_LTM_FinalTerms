using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace P2PFileSharing.Common.Utilities;

/// <summary>
/// Utility class cho các thao tác mạng
/// </summary>
public static class NetworkHelper
{
    /// <summary>
    /// Lấy địa chỉ IP local của máy hiện tại (IPv4)
    /// </summary>
    public static string GetLocalIPAddress()
    {
        try
        {
            // Thử kết nối đến một địa chỉ để xác định interface đang dùng
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            socket.Connect("8.8.8.8", 65530);
            var endPoint = socket.LocalEndPoint as IPEndPoint;
            return endPoint?.Address.ToString() ?? "127.0.0.1";
        }
        catch
        {
            // Fallback: lấy IP đầu tiên không phải loopback
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                {
                    return ip.ToString();
                }
            }
            return "127.0.0.1";
        }
    }

    /// <summary>
    /// Lấy địa chỉ broadcast của subnet hiện tại
    /// </summary>
    public static string GetBroadcastAddress()
    {
        try
        {
            var localIP = GetLocalIPAddress();
            var ipAddress = IPAddress.Parse(localIP);

            // Tìm network interface tương ứng
            foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (networkInterface.OperationalStatus != OperationalStatus.Up)
                    continue;

                var properties = networkInterface.GetIPProperties();
                foreach (var address in properties.UnicastAddresses)
                {
                    if (address.Address.AddressFamily == AddressFamily.InterNetwork &&
                        address.Address.Equals(ipAddress))
                    {
                        // Tính broadcast address
                        var ip = address.Address.GetAddressBytes();
                        var mask = address.IPv4Mask.GetAddressBytes();
                        var broadcast = new byte[4];

                        for (int i = 0; i < 4; i++)
                        {
                            broadcast[i] = (byte)(ip[i] | ~mask[i]);
                        }

                        return new IPAddress(broadcast).ToString();
                    }
                }
            }

            // Fallback: sử dụng broadcast address mặc định cho subnet 192.168.x.x
            var parts = localIP.Split('.');
            if (parts.Length == 4 && parts[0] == "192" && parts[1] == "168")
            {
                return $"{parts[0]}.{parts[1]}.{parts[2]}.255";
            }

            return "255.255.255.255"; // Global broadcast
        }
        catch
        {
            return "255.255.255.255";
        }
    }

    /// <summary>
    /// Kiểm tra xem một port có đang được sử dụng không
    /// </summary>
    public static bool IsPortAvailable(int port)
    {
        try
        {
            var listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            listener.Stop();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Tìm một port available trong khoảng [startPort, endPort]
    /// </summary>
    public static int FindAvailablePort(int startPort = 5000, int endPort = 6000)
    {
        for (int port = startPort; port <= endPort; port++)
        {
            if (IsPortAvailable(port))
                return port;
        }
        throw new InvalidOperationException($"No available port found in range {startPort}-{endPort}");
    }

    /// <summary>
    /// Kiểm tra xem có thể kết nối đến một địa chỉ IP và port không
    /// </summary>
    public static async Task<bool> CanConnectAsync(string ipAddress, int port, int timeoutMs = 5000)
    {
        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(ipAddress, port);
            var timeoutTask = Task.Delay(timeoutMs);

            var completedTask = await Task.WhenAny(connectTask, timeoutTask);
            if (completedTask == timeoutTask)
                return false;

            return client.Connected;
        }
        catch
        {
            return false;
        }
    }
}

