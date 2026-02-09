namespace WeatherWeb.Models;

public record HourlyForecast(
    string Time,
    double TemperatureF,
    int WeatherCode,
    string Summary
);
