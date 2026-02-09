namespace WeatherWeb.Models;

public record DailyForecast(
    string Date,
    double TemperatureMaxF,
    double TemperatureMinF,
    int WeatherCode,
    string Summary
);
