namespace WeatherService.Contracts;

public record WeatherForecastResult(
    bool Success,
    WeatherForecastData? Data,
    ErrorInfo? Error
);
