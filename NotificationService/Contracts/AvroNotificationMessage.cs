using Avro;
using Avro.Specific;

namespace NotificationService.Contracts;

/// <summary>
/// Avro-compatible version of NotificationMessage for Schema Registry deserialization.
/// This class matches the notification-message.avsc schema structure.
/// </summary>
public class AvroNotificationMessage : ISpecificRecord
{
    public static Schema _SCHEMA = Schema.Parse(@"{
        ""type"": ""record"",
        ""name"": ""NotificationMessage"",
        ""namespace"": ""com.weatherapp.notifications"",
        ""fields"": [
            { ""name"": ""messageId"", ""type"": ""string"" },
            { ""name"": ""subject"", ""type"": ""string"" },
            { ""name"": ""body"", ""type"": ""string"" },
            { ""name"": ""recipient"", ""type"": ""string"" },
            { ""name"": ""timestamp"", ""type"": { ""type"": ""long"", ""logicalType"": ""timestamp-millis"" } },
            { ""name"": ""metadata"", ""type"": [""null"", { ""type"": ""map"", ""values"": ""string"" }], ""default"": null }
        ]
    }");

    public string messageId { get; set; } = string.Empty;
    public string subject { get; set; } = string.Empty;
    public string body { get; set; } = string.Empty;
    public string recipient { get; set; } = string.Empty;
    public long timestamp { get; set; }
    public IDictionary<string, string>? metadata { get; set; }

    public Schema Schema => _SCHEMA;

    public object Get(int fieldPos)
    {
        return fieldPos switch
        {
            0 => messageId,
            1 => subject,
            2 => body,
            3 => recipient,
            4 => timestamp,
            5 => metadata ?? (object)new Dictionary<string, string>(),
            _ => throw new AvroRuntimeException($"Bad index {fieldPos} in Get()")
        };
    }

    public void Put(int fieldPos, object fieldValue)
    {
        switch (fieldPos)
        {
            case 0: messageId = (string)fieldValue; break;
            case 1: subject = (string)fieldValue; break;
            case 2: body = (string)fieldValue; break;
            case 3: recipient = (string)fieldValue; break;
            case 4: timestamp = (long)fieldValue; break;
            case 5: metadata = (IDictionary<string, string>?)fieldValue; break;
            default: throw new AvroRuntimeException($"Bad index {fieldPos} in Put()");
        }
    }

    /// <summary>
    /// Converts Avro DTO to domain NotificationMessage
    /// </summary>
    public NotificationMessage ToNotificationMessage(string topic)
    {
        var timestampDateTime = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).UtcDateTime;

        var metadataDict = metadata != null
            ? new Dictionary<string, string>(metadata)
            : null;

        return new NotificationMessage(
            messageId,
            topic,
            subject,
            body,
            recipient,
            timestampDateTime,
            metadataDict
        );
    }
}
