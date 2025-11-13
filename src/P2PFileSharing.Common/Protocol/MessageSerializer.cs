using System.Text;
using System.Text.Json;
using P2PFileSharing.Common.Models.Messages;

namespace P2PFileSharing.Common.Protocol;

/// <summary>
/// Utility class để serialize và deserialize messages
/// </summary>
public static class MessageSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Serialize message thành JSON string
    /// </summary>
    public static string Serialize(Message message)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        // Serialize message using its runtime type to include derived properties
        var dataJson = JsonSerializer.Serialize(message, message.GetType(), JsonOptions);

        // Build wrapper json: { "type": "...", "data": { ... } }
        using var ms = new MemoryStream();
        using (var writer = new Utf8JsonWriter(ms))
        {
            writer.WriteStartObject();
            writer.WriteString("type", message.Type.ToString());
            writer.WritePropertyName("data");
            using var doc = JsonDocument.Parse(dataJson);
            doc.RootElement.WriteTo(writer);
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(ms.ToArray());
    }

    /// <summary>
    /// Serialize message thành byte array (UTF-8)
    /// </summary>
    public static byte[] SerializeToBytes(Message message)
    {
        var json = Serialize(message);
        return Encoding.UTF8.GetBytes(json);
    }

    /// <summary>
    /// Deserialize JSON string thành Message object
    /// </summary>
    public static Message? Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("type", out var typeElement))
                return null;

            var typeString = typeElement.GetString();
            if (string.IsNullOrEmpty(typeString))
                return null;

            if (!Enum.TryParse<MessageType>(typeString, true, out var messageType))
                return null;

            if (!root.TryGetProperty("data", out var dataElement))
                return null;

            return DeserializeByType(dataElement, messageType);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Deserialize byte array thành Message object
    /// </summary>
    public static Message? DeserializeFromBytes(byte[] data)
    {
        if (data == null || data.Length == 0)
            return null;

        var json = Encoding.UTF8.GetString(data);
        return Deserialize(json);
    }

    private static Message? DeserializeByType(JsonElement dataElement, MessageType messageType)
    {
        return messageType switch
        {
            MessageType.Register      => JsonSerializer.Deserialize<RegisterMessage>(dataElement.GetRawText(), JsonOptions),
            MessageType.RegisterAck   => JsonSerializer.Deserialize<RegisterAckMessage>(dataElement.GetRawText(), JsonOptions),
            MessageType.RegisterNack  => JsonSerializer.Deserialize<RegisterNackMessage>(dataElement.GetRawText(), JsonOptions), // ✅ MỚI
            MessageType.QueryPeers    => JsonSerializer.Deserialize<QueryMessage>(dataElement.GetRawText(), JsonOptions),
            MessageType.QueryResponse => JsonSerializer.Deserialize<QueryResponseMessage>(dataElement.GetRawText(), JsonOptions),
            MessageType.Deregister    => JsonSerializer.Deserialize<DeregisterMessage>(dataElement.GetRawText(), JsonOptions),
            MessageType.Heartbeat     => JsonSerializer.Deserialize<HeartbeatMessage>(dataElement.GetRawText(), JsonOptions),
            MessageType.FileTransferRequest  => JsonSerializer.Deserialize<FileTransferRequestMessage>(dataElement.GetRawText(), JsonOptions),
            MessageType.FileTransferResponse => JsonSerializer.Deserialize<FileTransferResponseMessage>(dataElement.GetRawText(), JsonOptions),
            _ => null
        };
    }

    /// <summary>
    /// Gửi message qua NetworkStream (async)
    /// Format: [4 bytes length][JSON data]
    /// </summary>
    public static async Task SendMessageAsync(Stream stream, Message message, CancellationToken cancellationToken = default)
    {
        var data = SerializeToBytes(message);
        var lengthBytes = BitConverter.GetBytes(data.Length);

        // Gửi length trước
        await stream.WriteAsync(lengthBytes, 0, lengthBytes.Length, cancellationToken);
        // Gửi data
        await stream.WriteAsync(data, 0, data.Length, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    /// <summary>
    /// Nhận message từ NetworkStream (async)
    /// </summary>
    public static async Task<Message?> ReceiveMessageAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        // Đọc 4 bytes đầu tiên để biết độ dài
        var lengthBytes = new byte[4];
        var bytesRead = await stream.ReadAsync(lengthBytes, 0, 4, cancellationToken);

        if (bytesRead != 4)
            return null; // Connection closed

        var messageLength = BitConverter.ToInt32(lengthBytes, 0);

        if (messageLength <= 0 || messageLength > 10 * 1024 * 1024) // Max 10MB
            throw new InvalidOperationException($"Invalid message length: {messageLength}");

        // Đọc message data
        var messageBytes = new byte[messageLength];
        var totalRead = 0;

        while (totalRead < messageLength)
        {
            var read = await stream.ReadAsync(messageBytes, totalRead, messageLength - totalRead, cancellationToken);
            if (read == 0)
                return null; // Connection closed

            totalRead += read;
        }

        return DeserializeFromBytes(messageBytes);
    }
}

