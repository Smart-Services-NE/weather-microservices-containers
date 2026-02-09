namespace WeatherService.Contracts;

public record WeatherDataResult(
    bool Success,
    double? TemperatureF,
    int? WeatherCode,
    IEnumerable<HourlyForecast>? HourlyForecasts,
    IEnumerable<DailyForecast>? DailyForecasts,
    ErrorInfo? Error
);
