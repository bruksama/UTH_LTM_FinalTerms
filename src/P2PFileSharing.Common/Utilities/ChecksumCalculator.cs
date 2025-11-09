using System.Security.Cryptography;
using System.Text;

namespace P2PFileSharing.Common.Utilities;

/// <summary>
/// Utility class để tính checksum của file (MD5 hoặc SHA256)
/// </summary>
public static class ChecksumCalculator
{
    /// <summary>
    /// Tính MD5 hash của file
    /// </summary>
    public static async Task<string> CalculateMD5Async(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        using var md5 = MD5.Create();
        using var stream = File.OpenRead(filePath);

        var hash = await md5.ComputeHashAsync(stream);
        return ConvertToHexString(hash);
    }

    /// <summary>
    /// Tính SHA256 hash của file
    /// </summary>
    public static async Task<string> CalculateSHA256Async(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);

        var hash = await sha256.ComputeHashAsync(stream);
        return ConvertToHexString(hash);
    }

    /// <summary>
    /// Tính checksum của file với algorithm được chỉ định
    /// </summary>
    public static async Task<string> CalculateChecksumAsync(string filePath, string algorithm = "SHA256")
    {
        return algorithm.ToUpperInvariant() switch
        {
            "MD5" => await CalculateMD5Async(filePath),
            "SHA256" => await CalculateSHA256Async(filePath),
            _ => throw new ArgumentException($"Unsupported algorithm: {algorithm}", nameof(algorithm))
        };
    }

    /// <summary>
    /// Tính MD5 hash của byte array
    /// </summary>
    public static string CalculateMD5(byte[] data)
    {
        if (data == null || data.Length == 0)
            return string.Empty;

        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(data);
        return ConvertToHexString(hash);
    }

    /// <summary>
    /// Tính SHA256 hash của byte array
    /// </summary>
    public static string CalculateSHA256(byte[] data)
    {
        if (data == null || data.Length == 0)
            return string.Empty;

        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(data);
        return ConvertToHexString(hash);
    }

    /// <summary>
    /// Verify checksum của file
    /// </summary>
    public static async Task<bool> VerifyChecksumAsync(string filePath, string expectedChecksum, string algorithm = "SHA256")
    {
        var actualChecksum = await CalculateChecksumAsync(filePath, algorithm);
        return string.Equals(actualChecksum, expectedChecksum, StringComparison.OrdinalIgnoreCase);
    }

    private static string ConvertToHexString(byte[] bytes)
    {
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }
}

