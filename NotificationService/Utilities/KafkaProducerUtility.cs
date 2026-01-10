using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NotificationService.Contracts;

namespace NotificationService.Utilities;

public class KafkaProducerUtility : IKafkaProducerUtility, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ITelemetryUtility _telemetryUtility;
    private readonly ILogger<KafkaProducerUtility> _logger;
    private readonly string _topic;
    private bool _disposed;

    public KafkaProducerUtility(
        IConfiguration configuration,
        ITelemetryUtility telemetryUtility,
        ILogger<KafkaProducerUtility> logger)
    {
        _telemetryUtility = telemetryUtility;
        _logger = logger;
        _topic = configuration["Kafka:ProducerTopic"] ?? "notification-records";

        var config = new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            Acks = ParseAcks(configuration["Kafka:ProducerAcks"]),
            EnableIdempotence = bool.Parse(configuration["Kafka:EnableIdempotence"] ?? "true"),
            CompressionType = ParseCompressionType(configuration["Kafka:CompressionType"]),
            ClientId = "notification-service-producer",
            MessageSendMaxRetries = 3,
            RetryBackoffMs = 100,
            RequestTimeoutMs = 30000,
            MaxInFlight = 5,
        };

        // Add security configuration if present
        var securityProtocol = configuration["Kafka:SecurityProtocol"];
        if (!string.IsNullOrEmpty(securityProtocol))
        {
            config.SecurityProtocol = Enum.Parse<SecurityProtocol>(securityProtocol, ignoreCase: true);
            config.SaslMechanism = Enum.Parse<SaslMechanism>(
                configuration["Kafka:SaslMechanism"] ?? "Plain",
                ignoreCase: true);
            config.SaslUsername = configuration["Kafka:SaslUsername"];
            config.SaslPassword = configuration["Kafka:SaslPassword"];
        }

        _producer = new ProducerBuilder<string, string>(config)
            .SetErrorHandler((_, error) =>
            {
                _logger.LogError(
                    "Kafka producer error: Code={ErrorCode}, Reason={Reason}, IsFatal={IsFatal}",
                    error.Code,
                    error.Reason,
                    error.IsFatal);
            })
            .SetStatisticsHandler((_, stats) =>
            {
                _logger.LogDebug("Kafka producer statistics: {Statistics}", stats);
            })
            .Build();

        _logger.LogInformation(
            "Kafka producer initialized: Topic={Topic}, BootstrapServers={BootstrapServers}",
            _topic,
            config.BootstrapServers);
    }

    public async Task PublishNotificationRecordAsync(
        NotificationRecord record,
        CancellationToken cancellationToken)
    {
        if (record == null)
        {
            throw new ArgumentNullException(nameof(record));
        }

        using var activity = _telemetryUtility.StartActivity("PublishNotificationRecord");
        _telemetryUtility.SetTag("topic", _topic);
        _telemetryUtility.SetTag("messageId", record.MessageId);
        _telemetryUtility.SetTag("status", record.Status.ToString());

        try
        {
            // Serialize NotificationRecord to JSON
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            var value = JsonSerializer.Serialize(record, jsonOptions);

            // Use messageId as the key for partition routing
            var message = new Message<string, string>
            {
                Key = record.MessageId,
                Value = value,
                Timestamp = new Timestamp(record.CreatedAt)
            };

            _logger.LogDebug(
                "Publishing notification record: MessageId={MessageId}, Topic={Topic}, Status={Status}",
                record.MessageId,
                _topic,
                record.Status);

            var deliveryResult = await _producer.ProduceAsync(
                _topic,
                message,
                cancellationToken);

            _logger.LogInformation(
                "Published notification record: MessageId={MessageId}, Partition={Partition}, Offset={Offset}",
                record.MessageId,
                deliveryResult.Partition.Value,
                deliveryResult.Offset.Value);

            _telemetryUtility.RecordMetric("notification.cosmos.published", 1);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish notification record: MessageId={MessageId}, ErrorCode={ErrorCode}, Reason={Reason}",
                record.MessageId,
                ex.Error.Code,
                ex.Error.Reason);

            _telemetryUtility.RecordMetric("notification.cosmos.publish_failed", 1);

            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex,
                "Failed to serialize notification record: MessageId={MessageId}",
                record.MessageId);

            _telemetryUtility.RecordMetric("notification.cosmos.serialization_failed", 1);

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error publishing notification record: MessageId={MessageId}",
                record.MessageId);

            _telemetryUtility.RecordMetric("notification.cosmos.publish_error", 1);

            throw;
        }
    }

    private static Acks ParseAcks(string? acksConfig)
    {
        if (string.IsNullOrEmpty(acksConfig))
        {
            return Acks.All;
        }

        return acksConfig.ToLower() switch
        {
            "all" or "-1" => Acks.All,
            "1" => Acks.Leader,
            "0" or "none" => Acks.None,
            _ => Acks.All
        };
    }

    private static CompressionType ParseCompressionType(string? compressionConfig)
    {
        if (string.IsNullOrEmpty(compressionConfig))
        {
            return CompressionType.Gzip;
        }

        return compressionConfig.ToLower() switch
        {
            "gzip" => CompressionType.Gzip,
            "snappy" => CompressionType.Snappy,
            "lz4" => CompressionType.Lz4,
            "zstd" => CompressionType.Zstd,
            "none" => CompressionType.None,
            _ => CompressionType.Gzip
        };
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _logger.LogInformation("Disposing Kafka producer");

        try
        {
            // Flush any pending messages (wait up to 10 seconds)
            _producer.Flush(TimeSpan.FromSeconds(10));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error flushing Kafka producer");
        }
        finally
        {
            _producer.Dispose();
            _disposed = true;
        }
    }
}
