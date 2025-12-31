namespace NotificationService.Contracts;

public interface IRetryPolicyUtility
{
    Task<T> ExecuteWithRetryAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default);

    Task ExecuteWithRetryAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default);

    TimeSpan CalculateDelay(int retryCount);
}
