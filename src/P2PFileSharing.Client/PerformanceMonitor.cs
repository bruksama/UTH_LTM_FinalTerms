using P2PFileSharing.Common.Infrastructure;

namespace P2PFileSharing.Client;

/// <summary>
/// Đo lường hiệu năng truyền file (FR-07)
/// TODO: Implement performance metrics tracking
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
        // TODO: Create metrics object và start tracking
        _logger.LogDebug($"TODO: Start monitoring transfer {transferId}");
        return new TransferMetrics { TransferId = transferId };
    }

    /// <summary>
    /// Update metrics trong quá trình transfer
    /// </summary>
    public void UpdateMetrics(TransferMetrics metrics, long bytesTransferred, TimeSpan elapsed)
    {
        // TODO: Calculate throughput, update metrics
        metrics.BytesTransferred = bytesTransferred;
        metrics.ElapsedTime = elapsed;
        metrics.ThroughputMBps = bytesTransferred / elapsed.TotalSeconds / (1024 * 1024);
    }

    /// <summary>
    /// Kết thúc monitoring và log kết quả
    /// </summary>
    public void StopMonitoring(TransferMetrics metrics)
    {
        // TODO: Finalize metrics và log
        _logger.LogInfo($"TODO: Transfer {metrics.TransferId} completed: {metrics.ThroughputMBps:F2} MB/s");
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
    public double CpuUsage { get; set; }
    public long MemoryUsage { get; set; }
}

