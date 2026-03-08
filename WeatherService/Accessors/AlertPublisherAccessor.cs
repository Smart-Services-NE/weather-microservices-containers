using Dapr.Client;
using Microsoft.Extensions.Logging;
using WeatherService.Contracts;
using Microsoft.Extensions.Configuration;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using com.weatherapp.notifications;

namespace WeatherService.Accessors;

public class AlertPublisherAccessor : IAlertPublisherAccessor, IDisposable
{
    private readonly DaprClient _dapr;
    private readonly IRetryPolicyUtility _retry;
    private readonly ITelemetryUtility _telemetry;
    private readonly ILogger<AlertPublisherAccessor> _logger;
    private readonly IConfiguration _configuration;
    
    private readonly IProducer<string, com.weatherapp.notifications.WeatherAlert>? _avroProducer;
    private readonly ISchemaRegistryClient? _schemaRegistryClient;

    private const string PubSubName = "pubsub";
    private const string TopicName = "weather-weather-alerts";

    public AlertPublisherAccessor(
        DaprClient dapr,
        IRetryPolicyUtility retry,
        ITelemetryUtility telemetry,
        ILogger<AlertPublisherAccessor> logger,
        IConfiguration configuration)
    {
        _dapr = dapr;
        _retry = retry;
        _telemetry = telemetry;
        _logger = logger;
        _configuration = configuration;

        if (_configuration.GetValue<bool>("Kafka:UseAvro", defaultValue: false))
        {
            try
            {
                var schemaRegistryConfig = new SchemaRegistryConfig
                {
                    Url = _configuration["Kafka:SchemaRegistryUrl"],
                    BasicAuthCredentialsSource = AuthCredentialsSource.UserInfo,
                    BasicAuthUserInfo = $"{_configuration["Kafka:SchemaRegistryKey"]}:{_configuration["Kafka:SchemaRegistrySecret"]}"
                };

                _schemaRegistryClient = new CachedSchemaRegistryClient(schemaRegistryConfig);

                var producerConfig = new ProducerConfig
                {
                    BootstrapServers = _configuration["Kafka:BootstrapServers"],
                    SecurityProtocol = Enum.TryParse<SecurityProtocol>(_configuration["Kafka:SecurityProtocol"], true, out var sp) ? sp : SecurityProtocol.Plaintext,
                    SaslMechanism = Enum.TryParse<SaslMechanism>(_configuration["Kafka:SaslMechanism"], true, out var sm) ? sm : SaslMechanism.Plain,
                    SaslUsername = _configuration["Kafka:SaslUsername"],
                    SaslPassword = _configuration["Kafka:SaslPassword"]
                };

                _avroProducer = new ProducerBuilder<string, com.weatherapp.notifications.WeatherAlert>(producerConfig)
                    .SetValueSerializer(new AvroSerializer<com.weatherapp.notifications.WeatherAlert>(_schemaRegistryClient))
                    .Build();

                _logger.LogInformation("Avro Kafka Producer initialized");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Avro Kafka Producer");
            }
        }
    }

    public async Task<Result> PublishFreezingAlertAsync(string email, string zipCode, double temperature, CancellationToken ct)
    {
        try
        {
            if (_avroProducer != null)
            {
                return await PublishAvroFreezingAlertAsync(email, zipCode, temperature, ct);
            }

            var alert = new WeatherAlertDto
            {
                MessageId = Guid.NewGuid().ToString(),
                Subject = "Freezing Temperature Alert",
                Body = $"Warning: The temperature in {zipCode} is {temperature:F1}°C, which is freezing!",
                Recipient = email,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                AlertType = "TEMPERATURE_EXTREME",
                Severity = "WARNING",
                Location = new LocationDto { ZipCode = zipCode },
                WeatherConditions = new WeatherConditionsDto
                {
                    CurrentTemperature = temperature,
                    WeatherDescription = "Freezing"
                }
            };

            _logger.LogInformation("Publishing freezing alert (JSON via Dapr) for {ZipCode} to {Email}", zipCode, email);
            var metadata = new Dictionary<string, string> { { "rawPayload", "true" } };
            await _retry.ExecuteWithRetryAsync(async (cToken) =>
            {
                await _dapr.PublishEventAsync(PubSubName, TopicName, alert, metadata, cToken);
            }, ct);

            return new Result(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish freezing alert for {ZipCode}", zipCode);
            return new Result(false, new ErrorInfo("PUBLISH_FAILED", ex.Message));
        }
    }

    private async Task<Result> PublishAvroFreezingAlertAsync(string email, string zipCode, double temperature, CancellationToken ct)
    {
        var alert = new com.weatherapp.notifications.WeatherAlert
        {
            messageId = Guid.NewGuid().ToString(),
            subject = "Freezing Temperature Alert (Avro)",
            body = $"Warning: The temperature in {zipCode} is {temperature:F1}°C, which is freezing!",
            recipient = email,
            timestamp = DateTime.UtcNow,
            alertType = com.weatherapp.notifications.AlertType.TEMPERATURE_EXTREME,
            severity = com.weatherapp.notifications.Severity.WARNING,
            location = new com.weatherapp.notifications.Location { zipCode = zipCode },
            weatherConditions = new com.weatherapp.notifications.WeatherConditions
            {
                currentTemperature = temperature,
                weatherDescription = "Freezing"
            }
        };

        _logger.LogInformation("Publishing freezing alert (Avro direct) for {ZipCode} to {Email}", zipCode, email);

        await _avroProducer!.ProduceAsync(TopicName, new Message<string, com.weatherapp.notifications.WeatherAlert>
        {
            Key = alert.messageId,
            Value = alert
        }, ct);

        return new Result(true);
    }

    public async Task<Result> PublishSentinelAlertAsync(string email, string zipCode, ThresholdType type, double value, double threshold, string op, CancellationToken ct)
    {
        try
        {
            if (_avroProducer != null)
            {
                return await PublishAvroSentinelAlertAsync(email, zipCode, type, value, threshold, op, ct);
            }

            var subject = type switch
            {
                ThresholdType.TemperatureHigh => "High Temperature Alert",
                ThresholdType.TemperatureLow => "Low Temperature Alert",
                ThresholdType.WindSpeed => "High Wind Alert",
                ThresholdType.PrecipitationProbability => "Precipitation Alert",
                _ => "Weather Sentinel Alert"
            };

            var alert = new WeatherAlertDto
            {
                MessageId = Guid.NewGuid().ToString(),
                Subject = subject,
                Body = $"Weather Sentinel triggered for {zipCode}! Current {type} is {value:F1} (Threshold: {threshold:F1})",
                Recipient = email,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                AlertType = "GENERAL_ALERT",
                Severity = "INFO",
                Location = new LocationDto { ZipCode = zipCode },
                WeatherConditions = new WeatherConditionsDto
                {
                    CurrentTemperature = (type == ThresholdType.TemperatureHigh || type == ThresholdType.TemperatureLow) ? ((value - 32) * 5 / 9) : null
                }
            };

            _logger.LogInformation("Publishing sentinel alert (JSON via Dapr) for {ZipCode} to {Email}", zipCode, email);
            var metadata = new Dictionary<string, string> { { "rawPayload", "true" } };
            await _retry.ExecuteWithRetryAsync(async (cToken) =>
            {
                await _dapr.PublishEventAsync(PubSubName, TopicName, alert, metadata, cToken);
            }, ct);

            return new Result(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish sentinel alert for {ZipCode}", zipCode);
            return new Result(false, new ErrorInfo("PUBLISH_FAILED", ex.Message));
        }
    }

    private async Task<Result> PublishAvroSentinelAlertAsync(string email, string zipCode, ThresholdType type, double value, double threshold, string op, CancellationToken ct)
    {
        var subject = type switch
        {
            ThresholdType.TemperatureHigh => "High Temperature Alert",
            ThresholdType.TemperatureLow => "Low Temperature Alert",
            ThresholdType.WindSpeed => "High Wind Alert",
            ThresholdType.PrecipitationProbability => "Precipitation Alert",
            _ => "Weather Sentinel Alert"
        };

        var alert = new com.weatherapp.notifications.WeatherAlert
        {
            messageId = Guid.NewGuid().ToString(),
            subject = subject,
            body = $"Weather Sentinel triggered for {zipCode}! Current {type} is {value:F1} (Threshold: {threshold:F1})",
            recipient = email,
            timestamp = DateTime.UtcNow,
            alertType = com.weatherapp.notifications.AlertType.GENERAL_ALERT,
            severity = com.weatherapp.notifications.Severity.INFO,
            location = new com.weatherapp.notifications.Location { zipCode = zipCode },
            weatherConditions = new com.weatherapp.notifications.WeatherConditions
            {
                currentTemperature = (type == ThresholdType.TemperatureHigh || type == ThresholdType.TemperatureLow) ? ((value - 32) * 5 / 9) : null
            }
        };

        _logger.LogInformation("Publishing sentinel alert (Avro direct) for {ZipCode} to {Email}", zipCode, email);

        await _avroProducer!.ProduceAsync(TopicName, new Message<string, com.weatherapp.notifications.WeatherAlert>
        {
            Key = alert.messageId,
            Value = alert
        }, ct);

        return new Result(true);
    }

    public void Dispose()
    {
        _avroProducer?.Dispose();
        _schemaRegistryClient?.Dispose();
    }
}
