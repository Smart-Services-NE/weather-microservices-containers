using Microsoft.Extensions.Logging;
using WeatherService.Contracts;

namespace WeatherService.Managers;

public class SubscriptionManager : ISubscriptionManager
{
    private readonly ISubscriptionAccessor _subscriptionAccessor;
    private readonly IWeatherManager _weatherManager;
    private readonly IWeatherAlertEngine _alertEngine;
    private readonly IAlertPublisherAccessor _alertPublisher;
    private readonly ITelemetryUtility _telemetry;
    private readonly ILogger<SubscriptionManager> _logger;

    public SubscriptionManager(
        ISubscriptionAccessor subscriptionAccessor,
        IWeatherManager weatherManager,
        IWeatherAlertEngine alertEngine,
        IAlertPublisherAccessor alertPublisher,
        ITelemetryUtility telemetry,
        ILogger<SubscriptionManager> logger)
    {
        _subscriptionAccessor = subscriptionAccessor;
        _weatherManager = weatherManager;
        _alertEngine = alertEngine;
        _alertPublisher = alertPublisher;
        _telemetry = telemetry;
        _logger = logger;
    }

    public async Task<Result> SubscribeAsync(SubscriptionRequest request, CancellationToken ct)
    {
        using var activity = _telemetry.StartActivity("Subscribe");
        _telemetry.SetTag("subscriber.email", request.Email);
        _telemetry.SetTag("subscriber.zipcode", request.ZipCode);
        _telemetry.SetTag("subscriber.type", request.Type.ToString());

        var record = new SubscriptionRecord(
            Guid.NewGuid(),
            request.Email,
            request.ZipCode,
            request.Type,
            request.Value,
            request.ComparisonOperator,
            DateTime.UtcNow
        );

        return await _subscriptionAccessor.CreateSubscriptionAsync(record, ct);
    }

    public async Task<Result> ProcessSubscriptionsAsync(CancellationToken ct)
    {
        using var activity = _telemetry.StartActivity("ProcessSentinelsBatch");
        _logger.LogInformation("Starting batch processing of weather sentinel subscriptions.");

        var subscriptions = await _subscriptionAccessor.GetAllSubscriptionsAsync(ct);
        int successCount = 0;
        int failCount = 0;
        int triggeredCount = 0;

        foreach (var sub in subscriptions)
        {
            try
            {
                // 1. Get current weather for this zip code
                var weatherResult = await _weatherManager.GetWeatherForecastAsync(sub.ZipCode);
                if (!weatherResult.Success || weatherResult.Forecast == null)
                {
                    _logger.LogWarning("Could not fetch weather for sentinel {Email} at {ZipCode}", sub.Email, sub.ZipCode);
                    failCount++;
                    continue;
                }

                // 2. Identify the value to check based on ThresholdType
                double currentValue = sub.Type switch
                {
                    ThresholdType.TemperatureHigh or ThresholdType.TemperatureLow => weatherResult.Forecast.TemperatureF,
                    ThresholdType.WindSpeed => (weatherResult.Forecast.HourlyForecasts?.FirstOrDefault()?.WindSpeed ?? 0) * 0.621371, // km/h to mph approx
                    ThresholdType.PrecipitationProbability => weatherResult.Forecast.DailyForecasts?.FirstOrDefault()?.PrecipitationProbability ?? 0,
                    _ => 0
                };

                // 3. Evaluate threshold
                if (_alertEngine.EvaluateThreshold(sub.Type, currentValue, sub.Value, sub.ComparisonOperator))
                {
                    _logger.LogInformation("Sentinel triggered for {Email}! {Type} {Current} {Op} {Threshold}", 
                        sub.Email, sub.Type, currentValue, sub.ComparisonOperator, sub.Value);
                    
                    // 4. Publish alert
                    var alertResult = await _alertPublisher.PublishSentinelAlertAsync(
                        sub.Email, sub.ZipCode, sub.Type, currentValue, sub.Value, sub.ComparisonOperator, ct);
                    
                    if (alertResult.Success) triggeredCount++;
                }

                successCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing sentinel for {Email}", sub.Email);
                failCount++;
            }
        }

        _logger.LogInformation("Sentinel batch completed. Processed: {Success}, Failed: {Failed}, Triggered: {Triggered}", 
            successCount, failCount, triggeredCount);
        
        _telemetry.RecordMetric("sentinels.batch.processed", successCount);
        _telemetry.RecordMetric("sentinels.batch.triggered", triggeredCount);
        _telemetry.RecordMetric("sentinels.batch.failed", failCount);

        return new Result(true);
    }
}
