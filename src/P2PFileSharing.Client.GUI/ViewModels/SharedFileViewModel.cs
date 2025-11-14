using P2PFileSharing.Common.Models;

namespace P2PFileSharing.Client.GUI.ViewModels;

/// <summary>
/// ViewModel cho một file được chia sẻ
/// Dùng để hiển thị thông tin file trong GUI
/// </summary>
public class SharedFileViewModel : BaseViewModel
{
    private readonly SharedFile _sharedFile;

    public SharedFileViewModel(SharedFile sharedFile)
    {
        _sharedFile = sharedFile;
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