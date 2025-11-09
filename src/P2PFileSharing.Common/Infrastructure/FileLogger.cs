using System.Collections.Concurrent;

namespace P2PFileSharing.Common.Infrastructure;

/// <summary>
/// File-based logger implementation với thread-safety
/// </summary>
public class FileLogger : ILogger, IDisposable
{
    private readonly string _logFilePath;
    private readonly LogLevel _minimumLevel;
    private readonly ConcurrentQueue<string> _logQueue;
    private readonly StreamWriter? _writer;
    private readonly object _lockObject = new();
    private bool _disposed;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="logFilePath">Đường dẫn đến file log</param>
    /// <param name="minimumLevel">Log level tối thiểu để ghi (mặc định: Info)</param>
    public FileLogger(string logFilePath, LogLevel minimumLevel = LogLevel.Info)
    {
        _logFilePath = logFilePath;
        _minimumLevel = minimumLevel;
        _logQueue = new ConcurrentQueue<string>();

        try
        {
            // Tạo thư mục nếu chưa tồn tại
            var directory = Path.GetDirectoryName(logFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Mở file để append
            var fileStream = new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.Read);
            _writer = new StreamWriter(fileStream) { AutoFlush = true };
        }
        catch (Exception ex)
        {
            // Nếu không thể tạo file log, ghi vào console
            Console.WriteLine($"Warning: Cannot create log file {logFilePath}: {ex.Message}");
        }
    }

    public void LogInfo(string message)
    {
        Log(LogLevel.Info, message);
    }

    public void LogWarning(string message)
    {
        Log(LogLevel.Warning, message);
    }

    public void LogError(string message)
    {
        Log(LogLevel.Error, message);
    }

    public void LogError(string message, Exception exception)
    {
        Log(LogLevel.Error, message, exception);
    }

    public void LogDebug(string message)
    {
        Log(LogLevel.Debug, message);
    }

    public void Log(LogLevel level, string message)
    {
        Log(level, message, null);
    }

    public void Log(LogLevel level, string message, Exception? exception)
    {
        if (level < _minimumLevel)
            return;

        if (_disposed)
            return;

        var logEntry = FormatLogEntry(level, message, exception);

        // Ghi vào console
        WriteToConsole(level, logEntry);

        // Ghi vào file
        WriteToFile(logEntry);
    }

    private string FormatLogEntry(LogLevel level, string message, Exception? exception)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var levelStr = level.ToString().ToUpper().PadRight(7);
        var logEntry = $"[{timestamp}] [{levelStr}] {message}";

        if (exception != null)
        {
            logEntry += $"\nException: {exception.GetType().Name}: {exception.Message}";
            if (exception.StackTrace != null)
            {
                logEntry += $"\nStackTrace: {exception.StackTrace}";
            }
        }

        return logEntry;
    }

    private void WriteToConsole(LogLevel level, string message)
    {
        var originalColor = Console.ForegroundColor;
        try
        {
            Console.ForegroundColor = level switch
            {
                LogLevel.Debug => ConsoleColor.Gray,
                LogLevel.Info => ConsoleColor.White,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                _ => ConsoleColor.White
            };
            Console.WriteLine(message);
        }
        finally
        {
            Console.ForegroundColor = originalColor;
        }
    }

    private void WriteToFile(string logEntry)
    {
        if (_writer == null)
            return;

        lock (_lockObject)
        {
            try
            {
                _writer.WriteLine(logEntry);
            }
            catch
            {
                // Ignore write errors
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        lock (_lockObject)
        {
            _writer?.Dispose();
        }
    }
}

