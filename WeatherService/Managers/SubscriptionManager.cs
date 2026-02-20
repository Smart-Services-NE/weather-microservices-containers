using Microsoft.Extensions.Logging;
using WeatherService.Contracts;

namespace WeatherService.Managers;

public class SubscriptionManager : ISubscriptionManager
{
    private readonly ISubscriptionAccessor _subscriptionAccessor;
    private readonly IWeatherManager _weatherManager;
    private readonly ITelemetryUtility _telemetry;
    private readonly ILogger<SubscriptionManager> _logger;

    public SubscriptionManager(
        ISubscriptionAccessor subscriptionAccessor,
        IWeatherManager weatherManager,
        ITelemetryUtility telemetry,
        ILogger<SubscriptionManager> logger)
    {
        _subscriptionAccessor = subscriptionAccessor;
        _weatherManager = weatherManager;
        _telemetry = telemetry;
        _logger = logger;
    }

    public async Task<Result> SubscribeAsync(SubscriptionRequest request, CancellationToken ct)
    {
        using var activity = _telemetry.StartActivity("Subscribe");
        _telemetry.SetTag("subscriber.email", request.Email);
        _telemetry.SetTag("subscriber.zipcode", request.ZipCode);

        var record = new SubscriptionRecord(
            Guid.NewGuid(),
            request.Email,
            request.ZipCode,
            DateTime.UtcNow
        );

        return await _subscriptionAccessor.CreateSubscriptionAsync(record, ct);
    }

    public async Task<Result> ProcessSubscriptionsAsync(CancellationToken ct)
    {
        using var activity = _telemetry.StartActivity("ProcessSubscriptionsBatch");
        _logger.LogInformation("Starting batch processing of freezing alert subscriptions.");

        var subscriptions = await _subscriptionAccessor.GetAllSubscriptionsAsync(ct);
        int successCount = 0;
        int failCount = 0;

        foreach (var sub in subscriptions)
        {
            try
            {
                var result = await _weatherManager.NotifyIfFreezingAsync(sub.ZipCode, sub.Email, ct);
                if (result.Success) successCount++;
                else failCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing subscription for {Email}", sub.Email);
                failCount++;
            }
        }

        _logger.LogInformation("Batch processing completed. Success: {Success}, Failed: {Failed}", successCount, failCount);
        _telemetry.RecordMetric("subscriptions.batch.success", successCount);
        _telemetry.RecordMetric("subscriptions.batch.failed", failCount);

        return new Result(true);
    }
}
