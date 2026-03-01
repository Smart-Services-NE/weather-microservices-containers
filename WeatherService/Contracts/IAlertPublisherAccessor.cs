namespace WeatherService.Contracts;

public interface IAlertPublisherAccessor
{
    Task<Result> PublishFreezingAlertAsync(string email, string zipCode, double temperature, CancellationToken ct);
    Task<Result> PublishSentinelAlertAsync(string email, string zipCode, ThresholdType type, double value, double threshold, string op, CancellationToken ct);
}
