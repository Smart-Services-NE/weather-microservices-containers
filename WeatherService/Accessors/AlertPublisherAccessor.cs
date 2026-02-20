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
}
