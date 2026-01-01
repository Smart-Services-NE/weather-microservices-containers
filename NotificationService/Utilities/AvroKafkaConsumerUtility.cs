using Confluent.Kafka;
using Avro.Generic;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NotificationService.Contracts;

namespace NotificationService.Utilities;

/// <summary>
/// Kafka consumer utility that deserializes Avro messages using Confluent Schema Registry
/// Supports multiple Avro schemas: weather-alert.avsc and notification-message.avsc
/// </summary>
public class AvroKafkaConsumerUtility : IKafkaConsumerUtility, IDisposable
{
    private readonly IConsumer<string, GenericRecord> _consumer;
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

        // Build Consumer with GenericRecord Avro Deserializer (wrapped for sync operation)
        // GenericRecord allows us to deserialize any Avro schema at runtime
        var asyncAvroDeserializer = new AvroDeserializer<GenericRecord>(_schemaRegistryClient);

        _consumer = new ConsumerBuilder<string, GenericRecord>(consumerConfig)
            .SetValueDeserializer(new SyncOverAsyncDeserializer<GenericRecord>(asyncAvroDeserializer))
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

            // Get the GenericRecord and determine schema type
            var genericRecord = consumeResult.Message.Value;
            var schemaName = genericRecord.Schema.Name;

            _logger.LogDebug("Detected Avro schema: {SchemaName}", schemaName);

            // Route to appropriate mapper based on schema name
            var notificationMessage = schemaName switch
            {
                "WeatherAlert" => MapWeatherAlertRecord(genericRecord, consumeResult.Topic),
                "NotificationMessage" => MapSimpleRecord(genericRecord, consumeResult.Topic),
                _ => throw new InvalidOperationException($"Unknown Avro schema: {schemaName}. Expected 'WeatherAlert' or 'NotificationMessage'")
            };

            _logger.LogInformation(
                "Successfully deserialized {SchemaName} message: MessageId={MessageId}, Subject={Subject}",
                schemaName,
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

    /// <summary>
    /// Maps WeatherAlert Avro schema to NotificationMessage domain model
    /// </summary>
    private NotificationMessage MapWeatherAlertRecord(GenericRecord record, string topic)
    {
        // Extract base notification fields
        var messageId = (string)record["messageId"];
        var subject = (string)record["subject"];
        var body = (string)record["body"];
        var recipient = (string)record["recipient"];
        var timestamp = ExtractTimestamp(record["timestamp"]);

        // Extract metadata (nullable map)
        var metadata = ExtractMetadata(record["metadata"]);

        // Extract weather-specific fields
        var alertType = MapAlertType(record["alertType"]);
        var severity = MapSeverity(record["severity"]);

        // Extract location record
        var locationRecord = (GenericRecord)record["location"];
        var location = new LocationData(
            (string)locationRecord["zipCode"],
            locationRecord["city"] as string,
            locationRecord["state"] as string,
            locationRecord["latitude"] as double?,
            locationRecord["longitude"] as double?
        );

        // Extract weather conditions record
        var weatherConditionsRecord = (GenericRecord)record["weatherConditions"];
        var weatherConditions = new WeatherConditionsData(
            weatherConditionsRecord["currentTemperature"] as double?,
            weatherConditionsRecord["weatherCode"] as int?,
            weatherConditionsRecord["weatherDescription"] as string,
            weatherConditionsRecord["windSpeed"] as double?,
            weatherConditionsRecord["precipitation"] as double?
        );

        // Create weather alert data
        var weatherData = new WeatherAlertData(alertType, severity, location, weatherConditions);

        return new NotificationMessage(
            messageId,
            topic,
            subject,
            body,
            recipient,
            timestamp,
            metadata,
            weatherData
        );
    }

    /// <summary>
    /// Maps simple NotificationMessage Avro schema to NotificationMessage domain model
    /// </summary>
    private NotificationMessage MapSimpleRecord(GenericRecord record, string topic)
    {
        var messageId = (string)record["messageId"];
        var subject = (string)record["subject"];
        var body = (string)record["body"];
        var recipient = (string)record["recipient"];
        var timestamp = ExtractTimestamp(record["timestamp"]);

        var metadata = ExtractMetadata(record["metadata"]);

        return new NotificationMessage(
            messageId,
            topic,
            subject,
            body,
            recipient,
            timestamp,
            metadata,
            null // No weather data for simple messages
        );
    }

    /// <summary>
    /// Extracts metadata map from Avro record (handles nullable map)
    /// </summary>
    private Dictionary<string, string>? ExtractMetadata(object? metadataObject)
    {
        if (metadataObject == null)
        {
            return null;
        }

        if (metadataObject is IDictionary<string, object> metadataDict)
        {
            return metadataDict.ToDictionary(
                kv => kv.Key,
                kv => kv.Value?.ToString() ?? string.Empty
            );
        }

        return null;
    }

    /// <summary>
    /// Extracts timestamp from Avro record (handles both DateTime and long types)
    /// Avro's timestamp-millis logical type can be deserialized as either DateTime or long
    /// </summary>
    private DateTime ExtractTimestamp(object timestampObject)
    {
        return timestampObject switch
        {
            DateTime dt => dt.ToUniversalTime(),
            long unixMillis => DateTimeOffset.FromUnixTimeMilliseconds(unixMillis).UtcDateTime,
            _ => throw new InvalidOperationException($"Unexpected timestamp type: {timestampObject?.GetType()}")
        };
    }

    /// <summary>
    /// Maps Avro alert type enum to domain AlertType enum
    /// </summary>
    private AlertType MapAlertType(object avroEnum)
    {
        var enumValue = avroEnum.ToString();
        return enumValue switch
        {
            "SEVERE_WEATHER" => AlertType.SevereWeather,
            "TEMPERATURE_EXTREME" => AlertType.TemperatureExtreme,
            "PRECIPITATION_HEAVY" => AlertType.PrecipitationHeavy,
            "WIND_WARNING" => AlertType.WindWarning,
            "STORM_WARNING" => AlertType.StormWarning,
            "GENERAL_ALERT" => AlertType.GeneralAlert,
            _ => AlertType.GeneralAlert // Default fallback
        };
    }

    /// <summary>
    /// Maps Avro severity enum to domain Severity enum
    /// </summary>
    private Severity MapSeverity(object avroEnum)
    {
        var enumValue = avroEnum.ToString();
        return enumValue switch
        {
            "INFO" => Severity.Info,
            "WARNING" => Severity.Warning,
            "SEVERE" => Severity.Severe,
            "CRITICAL" => Severity.Critical,
            _ => Severity.Info // Default fallback
        };
    }

    public void Dispose()
    {
        _consumer?.Dispose();
        _schemaRegistryClient?.Dispose();
    }
}
