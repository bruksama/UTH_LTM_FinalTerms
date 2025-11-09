namespace P2PFileSharing.Common.Models;

/// <summary>
/// Thông tin về một file được chia sẻ
/// </summary>
public class SharedFile
{
    /// <summary>
    /// Tên file (không bao gồm đường dẫn đầy đủ)
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Kích thước file tính bằng bytes
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Đường dẫn đầy đủ đến file trên máy peer
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Checksum của file (MD5 hoặc SHA256) để verify integrity
    /// </summary>
    public string Checksum { get; set; } = string.Empty;

    /// <summary>
    /// Phương thức checksum được sử dụng (MD5, SHA256)
    /// </summary>
    public string ChecksumAlgorithm { get; set; } = "SHA256";

    public override string ToString()
    {
        return $"{FileName} ({FormatFileSize(FileSize)})";
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}

