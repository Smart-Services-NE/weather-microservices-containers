using WeatherService.Contracts;
using Polly;
using Polly.Retry;

namespace WeatherService.Utilities;

public class RetryPolicyUtility : IRetryPolicyUtility
{
    private const int MaxRetryAttempts = 5;
    private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan MaxDelay = TimeSpan.FromMinutes(5);

    private readonly ResiliencePipeline _pipeline;

    public RetryPolicyUtility()
    {
        _pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = MaxRetryAttempts,
                Delay = InitialDelay,
                BackoffType = DelayBackoffType.Exponential,
                MaxDelay = MaxDelay,
                UseJitter = true
            })
            .Build();
    }

    public async Task<T> ExecuteWithRetryAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        return await _pipeline.ExecuteAsync(
            async ct => await operation(ct),
            cancellationToken);
    }

    public async Task ExecuteWithRetryAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        await _pipeline.ExecuteAsync(
            async ct => await operation(ct),
            cancellationToken);
    }
}
