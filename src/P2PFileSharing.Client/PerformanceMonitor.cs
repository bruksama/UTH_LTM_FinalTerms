using P2PFileSharing.Common.Infrastructure;

namespace P2PFileSharing.Client;

/// <summary>
/// Đo lường hiệu năng truyền file (FR-07)
/// </summary>
public class PerformanceMonitor
{
    private readonly ILogger _logger;

    public PerformanceMonitor(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Bắt đầu đo lường một file transfer
    /// </summary>
    public TransferMetrics StartMonitoring(string transferId)
    {
        var metrics = new TransferMetrics
        {
            TransferId = transferId,
            StartTime = DateTime.UtcNow
        };

        _logger.LogDebug($"[Perf] Start monitoring transfer {transferId}");
        return metrics;
    }

    /// <summary>
    /// Update metrics trong quá trình transfer
    /// </summary>
    public void UpdateMetrics(TransferMetrics metrics, long bytesTransferred, TimeSpan elapsed)
    {
        metrics.BytesTransferred = bytesTransferred;
        metrics.ElapsedTime = elapsed;

        var seconds = elapsed.TotalSeconds;
        if (seconds <= 0)
        {
            metrics.ThroughputMBps = 0;
            return;
        }

        metrics.ThroughputMBps = bytesTransferred / seconds / (1024 * 1024.0);

        _logger.LogDebug(
            $"[Perf] {metrics.TransferId}: " +
            $"{bytesTransferred} bytes in {seconds:F2}s " +
            $"({metrics.ThroughputMBps:F2} MB/s)");
    }

    /// <summary>
    /// Kết thúc monitoring và log kết quả
    /// </summary>
    public void StopMonitoring(TransferMetrics metrics)
    {
        _logger.LogInfo(
            $"[Perf] Transfer {metrics.TransferId} completed: " +
            $"{metrics.BytesTransferred} bytes in {metrics.ElapsedTime.TotalSeconds:F2}s, " +
            $"avg {metrics.ThroughputMBps:F2} MB/s");
    }
}

/// <summary>
/// Metrics cho một file transfer
/// </summary>
public class TransferMetrics
{
    public string TransferId { get; set; } = string.Empty;
    public long BytesTransferred { get; set; }
    public TimeSpan ElapsedTime { get; set; }

    public double ThroughputMBps { get; set; }

    // Thêm để sau này có thể dùng nếu cần
    public DateTime StartTime { get; set; }

    public double CpuUsage { get; set; }
    public long MemoryUsage { get; set; }
}
