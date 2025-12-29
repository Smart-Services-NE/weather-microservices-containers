namespace WeatherService.Contracts;

public record WeatherDataResult(
    bool Success,
    double? TemperatureF,
    int? WeatherCode,
    ErrorInfo? Error
);
