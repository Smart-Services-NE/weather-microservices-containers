using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WeatherService.Contracts;

namespace WeatherService.Accessors;

public class SubscriptionAccessor : ISubscriptionAccessor
{
    private readonly IRetryPolicyUtility _retry;
    private readonly ITelemetryUtility _telemetry;
    private readonly WeatherDbContext _dbContext;
    private readonly ILogger<SubscriptionAccessor> _logger;

    public SubscriptionAccessor(
        WeatherDbContext dbContext,
        IRetryPolicyUtility retry,
        ITelemetryUtility telemetry,
        ILogger<SubscriptionAccessor> logger)
    {
        _dbContext = dbContext;
        _retry = retry;
        _telemetry = telemetry;
        _logger = logger;
    }

    public async Task<Result> CreateSubscriptionAsync(SubscriptionRecord record, CancellationToken ct)
    {
        using var activity = _telemetry.StartActivity("DbCreateSubscription");
        try
        {
            await _retry.ExecuteWithRetryAsync(async (cToken) =>
            {
                await _dbContext.Subscriptions.AddAsync(record, cToken);
                await _dbContext.SaveChangesAsync(cToken);
            }, ct);
            return new Result(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create subscription for {Email}", record.Email);
            return new Result(false, new ErrorInfo("DB_ERROR", ex.Message));
        }
    }

    public async Task<IEnumerable<SubscriptionRecord>> GetAllSubscriptionsAsync(CancellationToken ct)
    {
        return await _dbContext.Subscriptions.ToListAsync(ct);
    }

    public async Task<Result> DeleteSubscriptionAsync(string email, string zipCode, CancellationToken ct)
    {
        try
        {
            var subs = await _dbContext.Subscriptions
                .Where(s => s.Email == email && s.ZipCode == zipCode)
                .ToListAsync(ct);

            if (subs.Any())
            {
                _dbContext.Subscriptions.RemoveRange(subs);
                await _dbContext.SaveChangesAsync(ct);
            }
            return new Result(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete subscription for {Email}", email);
            return new Result(false, new ErrorInfo("DB_ERROR", ex.Message));
        }
    }
}
