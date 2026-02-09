namespace WeatherService.Contracts;

public record DailyForecast(
    string Date,
    double TemperatureMaxF,
    double TemperatureMinF,
    int WeatherCode,
    string Summary
);
