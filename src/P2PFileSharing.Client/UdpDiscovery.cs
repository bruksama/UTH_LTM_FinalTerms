using P2PFileSharing.Common.Configuration;
using P2PFileSharing.Common.Infrastructure;
using P2PFileSharing.Common.Models;
using P2PFileSharing.Common.Utilities;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace P2PFileSharing.Client;

/// <summary>
/// UDP Broadcast discovery để tìm peers trong LAN (FR-02)
/// TODO: Implement UDP broadcast sender và listener
/// </summary>
public class UdpDiscovery
{
    private const int DefaultDiscoveryPort = 9999;
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(3);
    private static readonly JsonSerializerOptions JsonOpt = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private readonly ClientConfig _config;
    private readonly ILogger _logger;

    // state cho listener
    private UdpClient? _listenerUdp;
    private CancellationTokenSource? _listenerCts;
    private Task? _listenerTask;

    /// <summary>
    /// Cho phép UI/PeerClient set thông tin peer cục bộ để trả lời discovery
    /// </summary>
    public Func<PeerInfo>? LocalPeerProvider { get; set; }

    /// <summary>
    /// Cổng dùng cho discovery
    /// </summary>
    public int DiscoveryPort { get; init; } = DefaultDiscoveryPort;

    /// <summary>
    /// Timeout gom kết quả khi scan
    /// </summary>
    public TimeSpan Timeout { get; init; } = DefaultTimeout;

    public UdpDiscovery(ClientConfig config, ILogger logger)
    {
        _config = config;
        _logger = logger;
    }


    /// <summary>
    /// Gửi UDP broadcast để tìm peers trong LAN và gom kết quả trong một cửa sổ thời gian ngắn.
    /// Request:  "DISCOVER_P2P_V1?"
    /// Response: "PEER:{json(PeerInfo)}"
    /// </summary>
    public async Task<List<PeerInfo>> ScanNetworkAsync()
    {
        const int discoveryPort = 9999;
        var timeout = TimeSpan.FromSeconds(3);
        var results = new List<PeerInfo>();
        var dedup = new HashSet<string>();

        using var udp = new UdpClient(AddressFamily.InterNetwork);
        try
        {
            udp.EnableBroadcast = true;
            udp.Client.ReceiveTimeout = (int)timeout.TotalMilliseconds;

            // 1) Send broadcast packet
            var probeBytes = Encoding.UTF8.GetBytes("DISCOVER_P2P_V1?");
            var broadcast = new IPEndPoint(IPAddress.Broadcast, discoveryPort);

            await udp.SendAsync(probeBytes, probeBytes.Length, broadcast);
            await Task.Delay(120); // slight delay to allow responses to arrive
            await udp.SendAsync(probeBytes, probeBytes.Length, broadcast);

            _logger.LogInfo($"UDP broadcast sent to *:{discoveryPort}. Waiting for replies...");

            // 2) Listen for responses
            var start = DateTime.UtcNow;
            while (DateTime.UtcNow - start < timeout)
            {
                try
                {
                    var recvTask = udp.ReceiveAsync();
                    if (!recvTask.Wait(TimeSpan.FromMilliseconds(400))) continue;

                    var res = recvTask.Result;
                    var text = Encoding.UTF8.GetString(res.Buffer);

                    // 3) Parse responses
                    if (!text.StartsWith("PEER:", StringComparison.Ordinal)) continue;

                    var json = text["PEER:".Length..];
                    var peer = JsonSerializer.Deserialize<PeerInfo>(json, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    if (peer == null) continue;

                    // Nếu IpAddress rỗng, gán IP của responder
                    if (string.IsNullOrWhiteSpace(peer.IpAddress))
                        peer.IpAddress = res.RemoteEndPoint.Address.ToString();

                    var key = string.IsNullOrWhiteSpace(peer.PeerId)
                        ? $"{peer.IpAddress}:{peer.ListenPort}"
                        : peer.PeerId;

                    if (dedup.Add(key))
                        results.Add(peer);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug($"UDP scan warn: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"UDP scan error: {ex.Message}");
        }

        // 4) Return list of discovered peers
        _logger.LogInfo($"UDP scan done. Found {results.Count} peer(s).");
        return results;
    }

    /// <summary>
    /// Start UDP listener để nhận discovery requests từ peers khác
    /// </summary>
    public void StartListener()
    {
        if (_listenerTask != null) return;
        _listenerCts = new CancellationTokenSource();
        _listenerUdp = new UdpClient(new IPEndPoint(IPAddress.Any, DiscoveryPort));
        _listenerTask = Task.Run(() => ListenLoopAsync(_listenerUdp, _listenerCts.Token));

        _logger.LogInfo($"UDP discovery listener started on *:{DiscoveryPort}");
    }

    /// <summary>
    /// Stop UDP listener
    /// </summary>
    public void StopListener()
    {
        try
        {
            _listenerCts?.Cancel();
            _listenerUdp?.Close();
            _listenerUdp?.Dispose();
        }
        catch { /* ignore */ }
        finally
        {
            _listenerUdp = null;
            _listenerTask = null;
            _listenerCts = null;
        }
        _logger.LogInfo("UDP discovery listener stopped");
    }
    private async Task ListenLoopAsync(UdpClient udp, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            UdpReceiveResult res;
            try { res = await udp.ReceiveAsync(ct); }
            catch (OperationCanceledException) { break; }
            catch (ObjectDisposedException) { break; }
            catch (Exception) { continue; }

            try
            {
                var text = Encoding.UTF8.GetString(res.Buffer);
                if (!text.Equals("DISCOVER_P2P_V1?")) continue;

                var provider = LocalPeerProvider;
                if (provider == null) continue;

                var peer = provider();

                if (string.IsNullOrWhiteSpace(peer.IpAddress))
                    peer.IpAddress = NetworkHelper.GetLocalIPAddress() ?? "127.0.0.1";

                var json = JsonSerializer.Serialize(peer, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var payload = Encoding.UTF8.GetBytes("PEER:" + json);
                await udp.SendAsync(payload, payload.Length, res.RemoteEndPoint);
            }
            catch { /* ignore packet error */ }
        }
    }


}

