using System.Text.Json.Serialization;

namespace NotificationService.Contracts;

/// <summary>
/// DTO for deserializing Kafka messages in JSON format.
/// This matches the structure of messages sent to the topics.
/// </summary>
public record KafkaNotificationDto(
    [property: JsonPropertyName("messageId")] string? MessageId,
    [property: JsonPropertyName("subject")] string? Subject,
    [property: JsonPropertyName("body")] string? Body,
    [property: JsonPropertyName("recipient")] string? Recipient,
    [property: JsonPropertyName("timestamp")] long? Timestamp,
    [property: JsonPropertyName("metadata")] Dictionary<string, string>? Metadata
);
