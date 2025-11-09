namespace P2PFileSharing.Common.Infrastructure;

/// <summary>
/// Interface cho logging system
/// </summary>
public interface ILogger
{
    /// <summary>
    /// Log message với level Info
    /// </summary>
    void LogInfo(string message);

    /// <summary>
    /// Log message với level Warning
    /// </summary>
    void LogWarning(string message);

    /// <summary>
    /// Log message với level Error
    /// </summary>
    void LogError(string message);

    /// <summary>
    /// Log message với level Error kèm exception
    /// </summary>
    void LogError(string message, Exception exception);

    /// <summary>
    /// Log message với level Debug
    /// </summary>
    void LogDebug(string message);

    /// <summary>
    /// Log message với level tùy chỉnh
    /// </summary>
    void Log(LogLevel level, string message);

    /// <summary>
    /// Log message với level tùy chỉnh kèm exception
    /// </summary>
    void Log(LogLevel level, string message, Exception? exception);
}

/// <summary>
/// Log levels
/// </summary>
public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error
}

