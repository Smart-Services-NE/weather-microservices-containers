using Dapr.Client;
using Microsoft.Extensions.Logging;
using WeatherService.Contracts;

namespace WeatherService.Accessors;

public class AlertPublisherAccessor : IAlertPublisherAccessor
{
    private readonly DaprClient _dapr;
    private readonly IRetryPolicyUtility _retry;
    private readonly ITelemetryUtility _telemetry;
    private readonly ILogger<AlertPublisherAccessor> _logger;
    private const string PubSubName = "pubsub";
    private const string TopicName = "weather-alerts";

    public AlertPublisherAccessor(
        DaprClient dapr,
        IRetryPolicyUtility retry,
        ITelemetryUtility telemetry,
        ILogger<AlertPublisherAccessor> logger)
    {
        _dapr = dapr;
        _retry = retry;
        _telemetry = telemetry;
        _logger = logger;
    }

    public async Task<Result> PublishFreezingAlertAsync(string email, string zipCode, double temperature, CancellationToken ct)
    {
        try
        {
            var alert = new
            {
                messageId = Guid.NewGuid().ToString(),
                subject = "Freezing Temperature Alert",
                body = $"Warning: The temperature in {zipCode} is {temperature:F1}°C, which is freezing!",
                recipient = email,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                metadata = new Dictionary<string, string>
                {
                    { "zipCode", zipCode },
                    { "temperature", temperature.ToString("F1") },
                    { "alertType", "FREEZING" }
                }
            };

            _logger.LogInformation("Publishing freezing alert for {ZipCode} to {Email}", zipCode, email);

            await _retry.ExecuteWithRetryAsync(async (cToken) =>
            {
                await _dapr.PublishEventAsync(PubSubName, TopicName, alert, cToken);
            }, ct);

            _telemetry.RecordMetric("weather.alert.freezing.published", 1,
                new KeyValuePair<string, object?>("zipcode", zipCode));

            return new Result(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish freezing alert for {ZipCode}", zipCode);
            return new Result(false, new ErrorInfo("PUBLISH_FAILED", ex.Message));
        }
    }

    public async Task<Result> PublishSentinelAlertAsync(string email, string zipCode, ThresholdType type, double value, double threshold, string op, CancellationToken ct)
    {
        try
        {
            var subject = type switch
            {
                ThresholdType.TemperatureHigh => "High Temperature Alert",
                ThresholdType.TemperatureLow => "Low Temperature Alert",
                ThresholdType.WindSpeed => "High Wind Alert",
                ThresholdType.PrecipitationProbability => "Precipitation Alert",
                _ => "Weather Sentinel Alert"
            };

            var unit = type switch
            {
                ThresholdType.TemperatureHigh or ThresholdType.TemperatureLow => "°F",
                ThresholdType.WindSpeed => "mph",
                ThresholdType.PrecipitationProbability => "%",
                _ => ""
            };

            var conditionText = op switch
            {
                "greater-than" => "is above",
                "less-than" => "is below",
                "greater-than-or-equal" => "is at or above",
                "less-than-or-equal" => "is at or below",
                _ => "has reached"
            };

            var alert = new
            {
                messageId = Guid.NewGuid().ToString(),
                subject = subject,
                body = $"Weather Sentinel triggered for {zipCode}! Current {type} {conditionText} your threshold: {value:F1}{unit} (Threshold: {threshold:F1}{unit})",
                recipient = email,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                metadata = new Dictionary<string, string>
                {
                    { "zipCode", zipCode },
                    { "type", type.ToString() },
                    { "currentValue", value.ToString("F1") },
                    { "threshold", threshold.ToString("F1") },
                    { "operator", op }
                }
            };

            _logger.LogInformation("Publishing sentinel alert ({Type}) for {ZipCode} to {Email}", type, zipCode, email);

            await _retry.ExecuteWithRetryAsync(async (cToken) =>
            {
                await _dapr.PublishEventAsync(PubSubName, TopicName, alert, cToken);
            }, ct);

            _telemetry.RecordMetric("weather.alert.sentinel.published", 1,
                new KeyValuePair<string, object?>("type", type.ToString()));

            return new Result(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish sentinel alert for {ZipCode}", zipCode);
            return new Result(false, new ErrorInfo("PUBLISH_FAILED", ex.Message));
        }
    }
}
