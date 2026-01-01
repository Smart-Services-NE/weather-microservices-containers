using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NotificationService.Contracts;
using System.Text.Json;

namespace NotificationService.Utilities;

public class KafkaConsumerUtility : IKafkaConsumerUtility, IDisposable
{
    private readonly IConsumer<string, string> _consumer;
    private readonly ILogger<KafkaConsumerUtility> _logger;

    public KafkaConsumerUtility(IConfiguration configuration, ILogger<KafkaConsumerUtility> logger)
    {
        _logger = logger;

        var config = new ConsumerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            GroupId = configuration["Kafka:GroupId"] ?? "notification-service",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            EnableAutoOffsetStore = false,

            // Session timeout (recommended for Confluent Cloud)
            SessionTimeoutMs = 45000,

            // Error handling
            EnablePartitionEof = false
        };

        // Confluent Cloud Security Configuration
        var securityProtocol = configuration["Kafka:SecurityProtocol"];
        if (!string.IsNullOrEmpty(securityProtocol))
        {
            config.SecurityProtocol = Enum.Parse<SecurityProtocol>(securityProtocol, ignoreCase: true);
            config.SaslUsername = configuration["Kafka:SaslUsername"];
            config.SaslPassword = configuration["Kafka:SaslPassword"];

            var saslMechanism = configuration["Kafka:SaslMechanism"];
            if (!string.IsNullOrEmpty(saslMechanism))
            {
                config.SaslMechanism = Enum.Parse<SaslMechanism>(saslMechanism, ignoreCase: true);
            }
        }

        _consumer = new ConsumerBuilder<string, string>(config).Build();
    }

    public void Subscribe(IEnumerable<string> topics)
    {
        _consumer.Subscribe(topics);
        _logger.LogInformation("Subscribed to Kafka topics: {Topics}", string.Join(", ", topics));
    }

    public async Task<NotificationMessage?> ConsumeMessageAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var consumeResult = _consumer.Consume(cancellationToken);

            if (consumeResult == null || consumeResult.IsPartitionEOF)
            {
                return null;
            }

            _logger.LogDebug(
                "Consumed message from topic {Topic}, partition {Partition}, offset {Offset}",
                consumeResult.Topic,
                consumeResult.Partition.Value,
                consumeResult.Offset.Value);

            var messageContent = consumeResult.Message.Value;
            var topic = consumeResult.Topic;

            // Log first few bytes for diagnostics
            if (messageContent != null && messageContent.Length > 0)
            {
                var firstBytes = System.Text.Encoding.UTF8.GetBytes(messageContent.Substring(0, Math.Min(10, messageContent.Length)));
                var hexString = BitConverter.ToString(firstBytes);
                _logger.LogDebug(
                    "Message content preview (first bytes hex): {HexBytes}, Length: {Length}",
                    hexString,
                    messageContent.Length);
            }

            var notificationMessage = ParseKafkaMessage(topic, messageContent);

            return await Task.FromResult(notificationMessage);
        }
        catch (ConsumeException ex)
        {
            _logger.LogError(ex, "Error consuming message from Kafka");
            return null;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Kafka consumer operation was cancelled");
            return null;
        }
    }

    private NotificationMessage ParseKafkaMessage(string topic, string messageContent)
    {
        try
        {
            // Deserialize to WeatherAlertDto
            var weatherAlert = JsonSerializer.Deserialize<WeatherAlertDto>(messageContent);

            if (weatherAlert == null)
            {
                _logger.LogWarning("Deserialized message was null for topic {Topic}", topic);
                return CreateFallbackMessage(topic, messageContent);
            }

            // Convert DTO to domain model
            return weatherAlert.ToNotificationMessage(topic);
        }
        catch (JsonException ex)
        {
            // Check if message starts with Avro magic byte (0x00)
            var startsWithMagicByte = messageContent != null && messageContent.Length > 0 && messageContent[0] == '\0';

            if (startsWithMagicByte)
            {
                _logger.LogError(
                    "Message appears to be Avro-serialized with Schema Registry format (starts with 0x00 magic byte). " +
                    "This consumer is configured for JSON messages. " +
                    "Please ensure messages are sent as plain JSON, not Avro. " +
                    "Topic: {Topic}, Message length: {Length}",
                    topic,
                    messageContent?.Length ?? 0);
            }
            else
            {
                var preview = messageContent != null && messageContent.Length > 50
                    ? messageContent.Substring(0, 50) + "..."
                    : messageContent ?? "(null)";

                _logger.LogWarning(ex,
                    "Failed to parse Kafka message as JSON. Topic: {Topic}, Preview: {Preview}",
                    topic,
                    preview);
            }

            return CreateFallbackMessage(topic, messageContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error parsing Kafka message from topic {Topic}", topic);
            return CreateFallbackMessage(topic, messageContent);
        }
    }

    private static NotificationMessage CreateFallbackMessage(string topic, string messageContent)
    {
        return new NotificationMessage(
            Guid.NewGuid().ToString(),
            topic,
            "Raw Message",
            messageContent,
            string.Empty,
            DateTime.UtcNow
        );
    }

    public async Task CommitOffsetAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _consumer.Commit();
            await Task.CompletedTask;
        }
        catch (KafkaException ex)
        {
            _logger.LogError(ex, "Error committing Kafka offset");
            throw;
        }
    }

    public void Close()
    {
        _consumer.Close();
        _logger.LogInformation("Kafka consumer closed");
    }

    public void Dispose()
    {
        _consumer?.Dispose();
    }
}
