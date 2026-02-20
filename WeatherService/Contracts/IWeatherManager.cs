namespace WeatherService.Contracts;

public interface IWeatherManager
{
    Task<WeatherForecastResult> GetWeatherForecastAsync(string zipCode);
    Task<LocationValidationResult> ValidateLocationAsync(string zipCode);
    Task<WeatherForecastResult?> GetCachedForecastAsync(string zipCode);
    Task<Result> NotifyIfFreezingAsync(string zipCode, string email, CancellationToken ct);
}
