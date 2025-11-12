namespace P2PFileSharing.Client.GUI.ViewModels;

/// <summary>
/// ViewModel cho một file transfer đang diễn ra
/// TODO: Implement transfer progress tracking and display
/// </summary>
public class TransferViewModel : BaseViewModel
{
    private string _fileName = string.Empty;
    private string _peerName = string.Empty;
    private double _progress;
    private string _status = "Pending";
    private string _speed = "0 MB/s";
    private long _bytesTransferred;
    private long _totalBytes;
    private bool _isCompleted;
    private bool _isFailed;

    /// <summary>
    /// Tên file đang transfer
    /// TODO: Bind to UI
    /// </summary>
    public string FileName
    {
        get => _fileName;
        set => SetProperty(ref _fileName, value);
    }

    /// <summary>
    /// Tên peer đang transfer với
    /// TODO: Bind to UI
    /// </summary>
    public string PeerName
    {
        get => _peerName;
        set => SetProperty(ref _peerName, value);
    }

    /// <summary>
    /// Tiến độ transfer (0-100)
    /// TODO: Bind to ProgressBar
    /// </summary>
    public double Progress
    {
        get => _progress;
        set => SetProperty(ref _progress, value);
    }

    /// <summary>
    /// Trạng thái transfer
    /// TODO: Update based on transfer state
    /// </summary>
    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    /// <summary>
    /// Tốc độ transfer hiện tại
    /// TODO: Calculate from bytes transferred and time elapsed
    /// </summary>
    public string Speed
    {
        get => _speed;
        set => SetProperty(ref _speed, value);
    }

    /// <summary>
    /// Số bytes đã transfer
    /// TODO: Update during transfer
    /// </summary>
    public long BytesTransferred
    {
        get => _bytesTransferred;
        set
        {
            SetProperty(ref _bytesTransferred, value);
            UpdateProgress();
        }
    }

    /// <summary>
    /// Tổng số bytes cần transfer
    /// TODO: Set from file size
    /// </summary>
    public long TotalBytes
    {
        get => _totalBytes;
        set
        {
            SetProperty(ref _totalBytes, value);
            UpdateProgress();
        }
    }

    /// <summary>
    /// Transfer đã hoàn thành
    /// TODO: Set to true when transfer completes
    /// </summary>
    public bool IsCompleted
    {
        get => _isCompleted;
        set => SetProperty(ref _isCompleted, value);
    }

    /// <summary>
    /// Transfer bị lỗi
    /// TODO: Set to true when transfer fails
    /// </summary>
    public bool IsFailed
    {
        get => _isFailed;
        set => SetProperty(ref _isFailed, value);
    }

    /// <summary>
    /// Cập nhật progress dựa trên bytes transferred và total bytes
    /// TODO: Implement progress calculation
    /// </summary>
    private void UpdateProgress()
    {
        if (_totalBytes > 0)
        {
            Progress = (_bytesTransferred * 100.0) / _totalBytes;
        }
    }

    /// <summary>
    /// Cập nhật tốc độ transfer
    /// TODO: Calculate speed from bytes transferred and elapsed time
    /// </summary>
    public void UpdateSpeed(long bytesPerSecond)
    {
        // TODO: Format bytesPerSecond to MB/s or KB/s
        Speed = $"{bytesPerSecond / 1024.0 / 1024.0:F2} MB/s";
    }
}

