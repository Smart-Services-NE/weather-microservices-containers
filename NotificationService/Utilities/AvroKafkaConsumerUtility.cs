using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NotificationService.Contracts;

namespace NotificationService.Utilities;

/// <summary>
/// Wrapper to convert IAsyncDeserializer to IDeserializer for use with ConsumerBuilder
/// </summary>
internal class SyncOverAsyncDeserializer<T> : IDeserializer<T>
{
    private readonly IAsyncDeserializer<T> _asyncDeserializer;

    public SyncOverAsyncDeserializer(IAsyncDeserializer<T> asyncDeserializer)
    {
        _asyncDeserializer = asyncDeserializer;
    }

    public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        // Synchronously wait for async deserialization
        return _asyncDeserializer.DeserializeAsync(data.ToArray(), isNull, context).GetAwaiter().GetResult();
    }
}

/// <summary>
/// Kafka consumer utility that deserializes Avro messages using Confluent Schema Registry
/// </summary>
public class AvroKafkaConsumerUtility : IKafkaConsumerUtility, IDisposable
{
    private readonly IConsumer<string, AvroNotificationMessage> _consumer;
    private readonly ISchemaRegistryClient _schemaRegistryClient;
    private readonly ILogger<AvroKafkaConsumerUtility> _logger;
    private string _currentTopic = string.Empty;

    public AvroKafkaConsumerUtility(IConfiguration configuration, ILogger<AvroKafkaConsumerUtility> logger)
    {
        _logger = logger;

        // Kafka Consumer Configuration
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            GroupId = configuration["Kafka:GroupId"] ?? "notification-service",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            EnableAutoOffsetStore = false,
            SessionTimeoutMs = 45000,
            EnablePartitionEof = false
        };

        // Confluent Cloud Security Configuration
        var securityProtocol = configuration["Kafka:SecurityProtocol"];
        if (!string.IsNullOrEmpty(securityProtocol))
        {
            consumerConfig.SecurityProtocol = Enum.Parse<SecurityProtocol>(securityProtocol, ignoreCase: true);
            consumerConfig.SaslUsername = configuration["Kafka:SaslUsername"];
            consumerConfig.SaslPassword = configuration["Kafka:SaslPassword"];

            var saslMechanism = configuration["Kafka:SaslMechanism"];
            if (!string.IsNullOrEmpty(saslMechanism))
            {
                consumerConfig.SaslMechanism = Enum.Parse<SaslMechanism>(saslMechanism, ignoreCase: true);
            }
        }

        // Schema Registry Configuration
        var schemaRegistryUrl = configuration["Kafka:SchemaRegistryUrl"];
        if (string.IsNullOrEmpty(schemaRegistryUrl))
        {
            throw new InvalidOperationException(
                "Schema Registry URL is required for Avro deserialization. " +
                "Please configure Kafka:SchemaRegistryUrl in appsettings.json or environment variables.");
        }

        var schemaRegistryConfig = new SchemaRegistryConfig
        {
            Url = schemaRegistryUrl
        };

        // Schema Registry Authentication
        var schemaRegistryKey = configuration["Kafka:SchemaRegistryKey"];
        var schemaRegistrySecret = configuration["Kafka:SchemaRegistrySecret"];

        if (!string.IsNullOrEmpty(schemaRegistryKey) && !string.IsNullOrEmpty(schemaRegistrySecret))
        {
            schemaRegistryConfig.BasicAuthUserInfo = $"{schemaRegistryKey}:{schemaRegistrySecret}";
            _logger.LogInformation("Schema Registry configured with authentication");
        }
        else
        {
            _logger.LogWarning("Schema Registry configured without authentication - this may fail in production");
        }

        // Create Schema Registry Client
        _schemaRegistryClient = new CachedSchemaRegistryClient(schemaRegistryConfig);

        // Build Consumer with Avro Deserializer (wrapped for sync operation)
        var asyncAvroDeserializer = new AvroDeserializer<AvroNotificationMessage>(_schemaRegistryClient);

        _consumer = new ConsumerBuilder<string, AvroNotificationMessage>(consumerConfig)
            .SetValueDeserializer(new SyncOverAsyncDeserializer<AvroNotificationMessage>(asyncAvroDeserializer))
            .SetErrorHandler((_, e) =>
            {
                _logger.LogError("Kafka consumer error: {Reason}. Code: {Code}", e.Reason, e.Code);
            })
            .Build();

        _logger.LogInformation(
            "Avro Kafka Consumer initialized with Schema Registry: {SchemaRegistryUrl}",
            schemaRegistryUrl);
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
                "Consumed Avro message from topic {Topic}, partition {Partition}, offset {Offset}",
                consumeResult.Topic,
                consumeResult.Partition.Value,
                consumeResult.Offset.Value);

            _currentTopic = consumeResult.Topic;

            // Convert Avro DTO to domain model
            var avroMessage = consumeResult.Message.Value;
            var notificationMessage = avroMessage.ToNotificationMessage(consumeResult.Topic);

            _logger.LogInformation(
                "Successfully deserialized Avro message: MessageId={MessageId}, Subject={Subject}",
                notificationMessage.MessageId,
                notificationMessage.Subject);

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error consuming/deserializing Avro message from topic {Topic}", _currentTopic);
            return null;
        }
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
        _schemaRegistryClient?.Dispose();
    }
}
