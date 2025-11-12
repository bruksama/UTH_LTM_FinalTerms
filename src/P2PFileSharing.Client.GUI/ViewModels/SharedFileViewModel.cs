using P2PFileSharing.Common.Models;

namespace P2PFileSharing.Client.GUI.ViewModels;

/// <summary>
/// ViewModel cho một file được chia sẻ
/// TODO: Implement file information display
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
    /// TODO: Bind to UI
    /// </summary>
    public string FileName => _sharedFile.FileName;

    /// <summary>
    /// Kích thước file (formatted)
    /// TODO: Format bytes to human-readable format (KB, MB, GB)
    /// </summary>
    public string FileSize => FormatFileSize(_sharedFile.FileSize);

    /// <summary>
    /// Kích thước file (bytes)
    /// </summary>
    public long FileSizeBytes => _sharedFile.FileSize;

    /// <summary>
    /// Checksum của file
    /// TODO: Display if needed
    /// </summary>
    public string Checksum => _sharedFile.Checksum;

    /// <summary>
    /// Format file size to human-readable string
    /// TODO: Implement formatting logic
    /// </summary>
    private static string FormatFileSize(long bytes)
    {
        // TODO: Implement KB, MB, GB formatting
        return $"{bytes} bytes";
    }
}

