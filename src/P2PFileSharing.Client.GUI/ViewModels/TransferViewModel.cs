using System;
using System.Diagnostics;
using System.Windows.Input;

namespace P2PFileSharing.Client.GUI.ViewModels;

/// <summary>
/// Hướng của file transfer
/// </summary>
public enum TransferDirection
{
    Send,
    Receive
}

/// <summary>
/// ViewModel cho một file transfer đang diễn ra
/// Theo dõi tiến độ, trạng thái và tốc độ để bind ra UI
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
    private TransferDirection _direction;
    private string _directionText = string.Empty;
    private string _directionIcon = string.Empty;
    private string _fullFilePath = string.Empty;

    /// <summary>
    /// Khởi tạo ViewModel và Command
    /// </summary>
    public TransferViewModel()
    {
        OpenFileCommand = new RelayCommand(
            () => OpenFile(),
            () => IsCompleted && !string.IsNullOrEmpty(FullFilePath) && System.IO.File.Exists(FullFilePath)
        );
    }

    /// <summary>
    /// Tên file đang transfer
    /// </summary>
    public string FileName
    {
        get => _fileName;
        set => SetProperty(ref _fileName, value);
    }

    /// <summary>
    /// Tên peer (người gửi/nhận)
    /// </summary>
    public string PeerName
    {
        get => _peerName;
        set => SetProperty(ref _peerName, value);
    }

    /// <summary>
    /// Tiến độ transfer (0-100) — bind vào ProgressBar
    /// </summary>
    public double Progress
    {
        get => _progress;
        set => SetProperty(ref _progress, value);
    }

    /// <summary>
    /// Trạng thái transfer: Pending / In Progress / Completed / Failed ...
    /// </summary>
    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    /// <summary>
    /// Tốc độ transfer hiện tại (text đã format, ví dụ: "1.25 MB/s")
    /// </summary>
    public string Speed
    {
        get => _speed;
        set => SetProperty(ref _speed, value);
    }

    /// <summary>
    /// Số bytes đã gửi/nhận
    /// Mỗi lần set sẽ tự cập nhật Progress
    /// </summary>
    public long BytesTransferred
    {
        get => _bytesTransferred;
        set
        {
            if (SetProperty(ref _bytesTransferred, value))
            {
                UpdateProgress();
            }
        }
    }

    /// <summary>
    /// Tổng số bytes của file
    /// </summary>
    public long TotalBytes
    {
        get => _totalBytes;
        set
        {
            if (SetProperty(ref _totalBytes, value))
            {
                UpdateProgress();
            }
        }
    }

    /// <summary>
    /// Đã hoàn thành
    /// </summary>
    public bool IsCompleted
    {
        get => _isCompleted;
        set
        {
            if (SetProperty(ref _isCompleted, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    /// <summary>
    /// Bị lỗi
    /// </summary>
    public bool IsFailed
    {
        get => _isFailed;
        set => SetProperty(ref _isFailed, value);
    }

    /// <summary>
    /// Hướng transfer (Send/Receive)
    /// </summary>
    public TransferDirection Direction
    {
        get => _direction;
        set
        {
            if (SetProperty(ref _direction, value))
            {
                // Tự động cập nhật Icon và Text
                DirectionText = value == TransferDirection.Send ? "to" : "from";
                DirectionIcon = value == TransferDirection.Send ? "⏫" : "⏬"; // Gửi / Nhận
            }
        }
    }

    /// <summary>
    /// Text chỉ hướng ("to" hoặc "from")
    /// </summary>
    public string DirectionText
    {
        get => _directionText;
        set => SetProperty(ref _directionText, value);
    }

    /// <summary>
    /// Icon chỉ hướng (Gửi ⏫ / Nhận ⏬)
    /// </summary>
    public string DirectionIcon
    {
        get => _directionIcon;
        set => SetProperty(ref _directionIcon, value);
    }
    
    /// <summary>
    /// Đường dẫn đầy đủ đến file (để mở khi double-click)
    /// </summary>
    public string FullFilePath
    {
        get => _fullFilePath;
        set
        {
            if (SetProperty(ref _fullFilePath, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    /// <summary>
    /// Command để mở file khi double-click
    /// </summary>
    public ICommand OpenFileCommand { get; }

    /// <summary>
    /// Cập nhật progress dựa trên BytesTransferred và TotalBytes
    /// </summary>
    private void UpdateProgress()
    {
        if (_totalBytes > 0)
        {
            Progress = (_bytesTransferred * 100.0) / _totalBytes;
            
            if (_bytesTransferred >= _totalBytes && _totalBytes > 0 && !IsFailed)
            {
                MarkCompleted();
            }
        }
        else
        {
            Progress = 0;
        }
    }

    /// <summary>
    /// Cập nhật tốc độ truyền (input là bytes/second)
    /// </summary>
    public void UpdateSpeed(long bytesPerSecond)
    {
        const double KB = 1024.0;
        const double MB = KB * 1024.0;

        if (bytesPerSecond <= 0)
        {
            Speed = "0 KB/s";
            return;
        }

        if (bytesPerSecond < MB)
        {
            Speed = $"{bytesPerSecond / KB:0.##} KB/s";
        }
        else
        {
            Speed = $"{bytesPerSecond / MB:0.##} MB/s";
        }
    }

    /// <summary>
    /// Helper: gọi khi transfer thành công
    /// </summary>
    public void MarkCompleted()
    {
        IsCompleted = true;
        IsFailed = false;
        Status = "Completed";
        Progress = 100;
        Speed = "Done";
    }

    /// <summary>
    /// Helper: gọi khi transfer bị lỗi
    /// </summary>
    public void MarkFailed(string? errorMessage = null)
    {
        IsFailed = true;
        IsCompleted = false;
        Status = string.IsNullOrWhiteSpace(errorMessage) ? "Failed" : $"Failed: {errorMessage}";
        Speed = "Error";
    }

    /// <summary>
    /// Mở file (hoặc thư mục chứa file)
    /// </summary>
    private void OpenFile()
    {
        if (string.IsNullOrEmpty(FullFilePath) || !System.IO.File.Exists(FullFilePath))
        {
            return;
        }

        try
        {
            // Mở thư mục và trỏ vào file
            Process.Start("explorer.exe", $"/select,\"{FullFilePath}\"");
        }
        catch (Exception)
        {
            try
            {
                Process.Start(new ProcessStartInfo(FullFilePath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to open file: {ex.Message}");
            }
        }
    }
}