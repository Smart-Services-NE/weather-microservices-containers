namespace WeatherService.Contracts;

public interface IAlertPublisherAccessor
{
    Task<Result> PublishFreezingAlertAsync(string email, string zipCode, double temperature, CancellationToken ct);
}
