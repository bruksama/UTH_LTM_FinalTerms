using P2PFileSharing.Common.Models;
using System;
using System.Diagnostics;
using System.Windows.Input;

namespace P2PFileSharing.Client.GUI.ViewModels;

/// <summary>
/// Trạng thái của file trong danh sách
/// </summary>
public enum FileDirection
{
    Sharing, // Đang chia sẻ
    Received // Đã nhận
}

/// <summary>
/// ViewModel cho một file được chia sẻ
/// Dùng để hiển thị thông tin file trong GUI
/// </summary>
public class SharedFileViewModel : BaseViewModel
{
    private readonly SharedFile _sharedFile;

    private FileDirection _direction;
    private string _directionIcon = string.Empty;

    public SharedFileViewModel(SharedFile sharedFile)
    {
        _sharedFile = sharedFile;

        OpenFileCommand = new RelayCommand(
            () => OpenFile(),
            () => !string.IsNullOrEmpty(FullFilePath) && System.IO.File.Exists(FullFilePath)
        );
    }

    /// <summary>
    /// Tên file
    /// Bind trực tiếp ra UI
    /// </summary>
    public string FileName => _sharedFile.FileName;

    /// <summary>
    /// Kích thước file (đã format, ví dụ: 12.3 MB)
    /// </summary>
    public string FileSize => FormatFileSize(_sharedFile.FileSize);

    /// <summary>
    /// Kích thước file (bytes)
    /// </summary>
    public long FileSizeBytes => _sharedFile.FileSize;

    /// <summary>
    /// Checksum của file (nếu cần hiển thị chi tiết)
    /// </summary>
    public string Checksum => _sharedFile.Checksum;
    
    /// <summary>
    /// Hướng của file (Sharing/Received)
    /// </summary>
    public FileDirection Direction
    {
        get => _direction;
        set
        {
            if (SetProperty(ref _direction, value))
            {
                // Tự động cập nhật Icon
                DirectionIcon = value == FileDirection.Sharing ? "⏫" : "⏬"; // Chia sẻ / Đã nhận
            }
        }
    }

    /// <summary>
    /// Icon chỉ hướng (Chia sẻ ⏫ / Đã nhận ⏬)
    /// </summary>
    public string DirectionIcon
    {
        get => _directionIcon;
        set => SetProperty(ref _directionIcon, value);
    }

    /// <summary>
    /// Đường dẫn đầy đủ đến file (để mở)
    /// Lấy từ model
    /// </summary>
    public string FullFilePath
    {
        get => _sharedFile.FilePath;
        set
        {
            if (_sharedFile.FilePath != value)
            {
                _sharedFile.FilePath = value;
                OnPropertyChanged();
                // Yêu cầu UI kiểm tra lại CanExecute của OpenFileCommand
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    /// <summary>
    /// Command để mở file khi double-click
    /// </summary>
    public ICommand OpenFileCommand { get; }
    
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
            // Fallback: Mở file trực tiếp (nếu explorer fail)
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

    /// <summary>
    /// Format file size: B, KB, MB, GB
    /// </summary>
    private static string FormatFileSize(long bytes)
    {
        const double KB = 1024.0;
        const double MB = KB * 1024.0;
        const double GB = MB * 1024.0;

        if (bytes < KB)
            return $"{bytes} B";
        if (bytes < MB)
            return $"{bytes / KB:0.##} KB";
        if (bytes < GB)
            return $"{bytes / MB:0.##} MB";

        return $"{bytes / GB:0.##} GB";
    }
}